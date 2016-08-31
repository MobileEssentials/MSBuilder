using System;
using Microsoft.Build.Framework;
using Xunit;

namespace MSBuilder
{
	// Ad-hoc tests for TD.NET to try out behavior.
	partial class VsixInstallerTests
	{
		string vsixPath = "YOUR_VSIX_PATH"; // << assign in a ctor in the partial ManualTests.VsixPath.cs

		public void Install()
		{
			var task = new InstallVsix
			{
				VisualStudioVersion = "14.0",
				// Provide your test vsixPath in a class file named 
				// ManualTests.VsixPath.cs alongside this file, 
				// containing a partial class like the following:
				//
				// namespace MSBuilder
				// {
				//		partial class VsixInstallerTests
				//		{
				//			string vsixPath = @"[PATH_TO_TEST_VSIX]";
				//		}
				// }
				// If this line doesn't compile, read above ^^
				VsixPath = vsixPath,
				RootSuffix = "Exp",
				BuildEngine = new MockBuildEngine()
			};

			Console.WriteLine("Execute: " + task.Execute());
		}

		public void Disable()
		{
			var task = new DisableVsix
			{
				VisualStudioVersion = "12.0",
				VsixId = "Xamarin.VisualStudio",
				RootSuffix = "Exp",
				FailIfNotInstalled = true,
				BuildEngine = new MockBuildEngine()
			};

			Console.WriteLine("Execute: " + task.Execute());
		}

		public void Enable()
		{
			var task = new EnableVsix
			{
				VisualStudioVersion = "12.0",
				VsixId = "Merq",
				RootSuffix = "Exp",
				FailIfNotInstalled = true,
				BuildEngine = new MockBuildEngine()
			};

			Console.WriteLine("Execute: " + task.Execute());
		}


		public void Uninstall()
		{
			var task = new UninstallVsix
			{
				VisualStudioVersion = "12.0",
				VsixId = "Merq",
				RootSuffix = "Exp",
				FailIfNotInstalled = true,
				BuildEngine = new MockBuildEngine()
			};

			Console.WriteLine("Execute: " + task.Execute());
		}

		public void ListInstalled()
		{
			var task = new ListVsix
			{
				VisualStudioVersion = "14.0",
				RootSuffix = "Exp",
				VsixIdFilter = "Merq",
				BuildEngine = new MockBuildEngine()
			};

			Console.WriteLine("Execute: {0} ({1})", task.Execute(), task.InstalledVsix.Length);

			foreach (var extension in task.InstalledVsix)
			{
				Console.WriteLine("Extension {0} v{1} ({2} metadata items, InstalledPerMachine={3}).", 
					extension.ItemSpec, extension.GetMetadata("Version"), extension.MetadataCount, extension.GetMetadata("InstalledPerMachine"));
			}
		}
	}
}
