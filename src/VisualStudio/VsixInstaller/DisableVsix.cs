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
	/// Disables the extension with the given identifier in the 
	/// given Visual Studio version and optional hive/instance (i.e. 'Exp').
	/// </summary>
	public class DisableVsix : Task
	{
		/// <summary>
		/// Visual Studio version to disable the VSIX for.
		/// </summary>
		[Required]
		public string VisualStudioVersion { get; set; }

		/// <summary>
		/// Identifier of the extension to disable.
		/// </summary>
		[Required]
		public string VsixId { get; set; }

		/// <summary>
		/// Optional message importance for the task messages.
		/// </summary>
		public string MessageImportance { get; set; }

		/// <summary>
		/// Optional flag to fail if the extension is not already installed.
		/// </summary>
		public bool FailIfNotInstalled { get; set; }

		/// <summary>
		/// Optional hive/instance to disable in (i.e. 'Exp').
		/// </summary>
		public string RootSuffix { get; set; }

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

			var importance = Microsoft.Build.Framework.MessageImportance.Normal;
			if (!string.IsNullOrEmpty(MessageImportance))
					importance = (MessageImportance)Enum.Parse(typeof(MessageImportance), MessageImportance, true);

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

			try
			{
				var extension = managerType.InvokeMember("GetInstalledExtension", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, manager, new[] { VsixId });
				var state = (int)extension.GetType().InvokeMember("State", BindingFlags.GetProperty, null, extension, null);
				if (state != 0)
				{
					managerType.InvokeMember("Disable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, manager, new[] { extension });
					managerType.InvokeMember("UpdateLastExtensionsChange", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, manager, new object[0]);
					Log.LogMessage(importance, "Successfully disabled extension '{0}' on {1}.", VsixId, vsversion);
				}
				else
				{
					Log.LogMessage(importance, "Extension '{0}' was already disabled on {1}.", VsixId, vsversion);
				}
			}
			catch (TargetInvocationException tie)
			{
				if (tie.InnerException.GetType().FullName == "Microsoft.VisualStudio.ExtensionManager.NotInstalledException")
				{
					if (FailIfNotInstalled)
					{
						Log.LogError("Extension '{0}' is not installed on {1}.", VsixId, vsversion);
						return false;
					}
					else
					{
						Log.LogMessage(importance, "Extension '{0}' is not installed on {1}.", VsixId, vsversion);
					}
				}
			}

			return true;
		}
	}
}
