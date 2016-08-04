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
	/// Ensures the extension with the given identifier is uninstalled from the 
	/// given Visual Studio version and optional hive/instance (i.e. 'Exp').
	/// </summary>
	public class UninstallVsix : Task
	{
		/// <summary>
		/// Visual Studio version to uninstall the VSIX from.
		/// </summary>
		[Required]
		public string VisualStudioVersion { get; set; }

		/// <summary>
		/// Identifier of the extension to uninstall.
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
		/// Optional hive/instance to uninstall from (i.e. 'Exp').
		/// </summary>
		public string RootSuffix { get; set; }

		/// <summary>
		/// Ensures the extension is uninstalled from the given 
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
				managerType.InvokeMember("Uninstall", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, manager, new[] { extension });
				Log.LogMessage(importance, "Successfully uninstalled extension '{0}' from {1}.", VsixId, vsversion);
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
