using System;
using System.IO;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Win32;
using System.Linq;

namespace MSBuilder
{
	class InstallVsixTest
	{
		// Ad-hoc test for TD.NET to try out behavior.
		public void Install()
		{
			var task = new InstallVsix
			{
				VisualStudioVersion = "14.0",
				VsixPath = @"[PATH_TO_A_VSIX]",
				RootSuffix = "Exp",
			};

			task.Execute();
		}
	}

	public class InstallVsix : Task
	{
		[Required]
		public string VisualStudioVersion { get; set; }

		[Required]
		public string VsixPath { get; set; }

		public bool PerMachine { get; set; }

		public string RootSuffix { get; set; }

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
				Log.LogMessage(MessageImportance.High, "Successfully installed extension '{0}' on {1}.", id, vsversion);
			}
			else
			{
				extension = managerType.InvokeMember("GetInstalledExtension", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, manager, new[] { id });
				var state = (int)extension.GetType().InvokeMember("State", BindingFlags.GetProperty, null, extension, null);
				if (state != 1)
				{
					managerType.InvokeMember("Enable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, manager, new[] { extension });
					Log.LogMessage(MessageImportance.High, "Successfully enabled previously installed extension '{0}' on {1}.", id, vsversion);
				}
				else
				{
					Log.LogMessage(MessageImportance.High, "Extension '{0}' was already installed and enabled on {1}.", id, vsversion);
				}
			}

			return true;
		}
	}
}
