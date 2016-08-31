using System;
using System.IO;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Win32;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MSBuilder
{
	/// <summary>
	/// Lists the installed extensions in the given Visual Studio 
	/// version and optional hive/instance (i.e. 'Exp').
	/// </summary>
	public class ListVsix : Task
	{
		/// <summary>
		/// Visual Studio version to lookup the installed VSIXes in.
		/// </summary>
		[Required]
		public string VisualStudioVersion { get; set; }

		/// <summary>
		/// Optional hive/instance to lookup the installed VSIXes in (i.e. 'Exp').
		/// </summary>
		public string RootSuffix { get; set; }

		/// <summary>
		/// Optional regular expression used to match against the installed 
		/// extensions identifiers.
		/// </summary>
		public string VsixIdFilter { get; set; }

		/// <summary>
		/// Optional regular expression used to match against the installed 
		/// extensions name.
		/// </summary>
		public string VsixNameFilter { get; set; }

		/// <summary>
		/// The installed extensions (that match the optional 
		/// VsixIdFilter or VsixNameFilter expressions).
		/// </summary>
		[Output]
		public Microsoft.Build.Framework.ITaskItem[] InstalledVsix { get; set; }

		/// <summary>
		/// Disables the extension in the given 
		/// Visual Studio version and instance/hive.
		/// </summary>
		public override bool Execute()
		{
			string vsdir = null;
			using (var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
			using (var key = root.OpenSubKey(@"Software\Microsoft\VisualStudio\" + VisualStudioVersion))
			{
				if (key != null)
				{
					vsdir = key.GetValue("InstallDir") as string;
				}
				else
				{
					Log.LogError("Failed to locate installation directory for VisualStudioVersion '{0}'.", VisualStudioVersion);
					return false;
				}
			}

			var managerAsm = Assembly.LoadFrom(Path.Combine(vsdir, @"PrivateAssemblies\Microsoft.VisualStudio.ExtensionManager.Implementation.dll"));

			var vssdk = new DirectoryInfo(Path.Combine(vsdir, @"..\..\VSSDK\VisualStudioIntegration\Common\Assemblies\v4.0")).FullName;
			if (!Directory.Exists(vssdk))
				throw new ArgumentException("Visual Studio SDK was not found at expected path '" + vssdk + "'.");

			var settingsAsm = Assembly.LoadFrom(Path.Combine(vssdk, string.Format(@"Microsoft.VisualStudio.Settings.{0}.dll", VisualStudioVersion)));
			var settingsType = settingsAsm.GetType("Microsoft.VisualStudio.Settings.ExternalSettingsManager");
			var settings = settingsType.InvokeMember("CreateForApplication", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, null, null,
				new[] { Path.Combine(vsdir, "devenv.exe"), RootSuffix ?? "" });

			var managerType = managerAsm.GetType("Microsoft.VisualStudio.ExtensionManager.ExtensionManagerService", true);
			var manager = Activator.CreateInstance(managerType, new[] { settings });

			var vsversion = "Visual Studio " + VisualStudioVersion;
			if (!string.IsNullOrEmpty(RootSuffix))
				vsversion += " (" + RootSuffix + ")";

			var installed = (IEnumerable)managerType.InvokeMember("GetInstalledExtensions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, manager, new object[0]);
			var extensions = new List<ITaskItem>();

			var idFilter = string.IsNullOrEmpty(VsixIdFilter) ?
				((Func<string, bool>)(id => true)) :
				((Func<string, bool>)(id => Regex.IsMatch(id, VsixIdFilter)));

			var nameFilter = string.IsNullOrEmpty(VsixNameFilter) ?
				((Func<string, bool>)(name => true)) :
				((Func<string, bool>)(name => Regex.IsMatch(name, VsixNameFilter)));

			Action<Dictionary<string, string>, object> addMetadata = (metadata, target) =>
			{
				foreach (var property in target.GetType().GetProperties().Where(prop => prop.Name != "License"))
				{
					if (property.PropertyType.IsValueType || property.PropertyType == typeof(string) ||
						property.PropertyType == typeof(Version))
					{
						try
						{
							var value = property.GetValue(target);
							if (value == null)
								continue;

							if (value is string)
								metadata[property.Name] = (string)value;
							else
								metadata[property.Name] = value.ToString();
						}
						catch { }
					}
				}
			};

			foreach (var extension in installed)
			{
				var header = extension.GetType().InvokeMember("Header", BindingFlags.GetProperty, null, extension, null);
				var id = (string)header.GetType().InvokeMember("Identifier", BindingFlags.GetProperty, null, header, null);
				var name = (string)header.GetType().InvokeMember("Name", BindingFlags.GetProperty, null, header, null);
				var metadata = new Dictionary<string, string>();
				if (idFilter(id) && nameFilter(name))
				{
					addMetadata(metadata, extension);
					addMetadata(metadata, header);

					// Preserve the original VS version and RootSuffix used to fetch the extensions
					metadata["VisualStudioVersion"] = VisualStudioVersion;
					metadata["RootSuffix"] = RootSuffix ?? "";

					extensions.Add(new TaskItem(id, metadata));
				}
			}

			InstalledVsix = extensions.ToArray();

			return true;
		}
	}
}
