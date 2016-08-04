using System;
using System.IO;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Win32;
using System.Linq;

namespace MSBuilder
{
	/// <summary>
	/// Installs a VSIX to the given Visual Studio version and optional hive/instance (i.e. 'Exp').
	/// Allows specifying per-machine install too.
	/// </summary>
	public class InstallVsix : Task
	{
		/// <summary>
		/// Visual Studio version to install the VSIX to.
		/// </summary>
		[Required]
		public string VisualStudioVersion { get; set; }

		/// <summary>
		/// Full path to the VSIX file to install.
		/// </summary>
		[Required]
		public string VsixPath { get; set; }

		/// <summary>
		/// Optional flag to install the VSIX on the machine-wide location.
		/// </summary>
		public bool PerMachine { get; set; }

		/// <summary>
		/// Optional hive/instance to install to (i.e. 'Exp').
		/// </summary>
		public string RootSuffix { get; set; }

		/// <summary>
		/// Optional importance for the task messages. Defaults to High.
		/// </summary>
		public MessageImportance MessageImportance { get; set; }

		/// <summary>
		/// Ensures the given VSIX is installed and enabled for the given 
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

			object extension = managerType.InvokeMember("CreateInstallableExtension", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, null, null, new[] { VsixPath });
			var header = extension.GetType().InvokeMember("Header", BindingFlags.GetProperty, null, extension, null);
			var id = header.GetType().InvokeMember("Identifier", BindingFlags.GetProperty, null, header, null);
			var vsversion = "Visual Studio " + VisualStudioVersion;
			if (!string.IsNullOrEmpty(RootSuffix))
				vsversion += " (" + RootSuffix + ")";

			var installed = (bool)managerType.InvokeMember("IsInstalled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, manager, new[] { extension });
			if (!installed)
			{
				managerType.InvokeMember("Install", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, manager, new[] { extension, PerMachine });
				Log.LogMessage(MessageImportance, "Successfully installed extension '{0}' on {1}.", id, vsversion);
			}
			else
			{
				extension = managerType.InvokeMember("GetInstalledExtension", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, manager, new[] { id });
				var state = (int)extension.GetType().InvokeMember("State", BindingFlags.GetProperty, null, extension, null);
				if (state != 1)
				{
					managerType.InvokeMember("Enable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, manager, new[] { extension });
					Log.LogMessage(MessageImportance, "Successfully enabled previously installed extension '{0}' on {1}.", id, vsversion);
				}
				else
				{
					Log.LogMessage(MessageImportance, "Extension '{0}' was already installed and enabled on {1}.", id, vsversion);
				}
			}

			return true;
		}
	}
}
