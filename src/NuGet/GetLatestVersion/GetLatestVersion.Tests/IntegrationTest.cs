using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MSBuilder.NuGet
{
    public class GetLatestVersionTests
    {
        const string xmlns = "{http://schemas.microsoft.com/developer/msbuild/2003}";
        static readonly string MSBuildPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\12.0", "MSBuildToolsPath", @"C:\Program Files (x86)\MSBuild\12.0\bin\");

        [Fact]
		public void when_retrieving_latest_version_then_succeeds()
		{
			var task = new GetLatestVersion
			{
                BuildEngine = new MockBuildEngine(true),
                IncludePreRelease = true,
				PackageId = "xunit"
			};

			Assert.True(task.Execute());

			Assert.NotEqual("0.0.0", task.PackageVersion);
		}

        [Fact]
		public void when_retrieving_latest_version_for_non_existent_package_then_returns_zero()
		{
			var task = new GetLatestVersion
			{
                BuildEngine = new MockBuildEngine(true),
                IncludePreRelease = true,
				PackageId = Guid.NewGuid().ToString()
			};

			Assert.True(task.Execute());

			Assert.Equal("0.0.0", task.PackageVersion);
			Assert.Equal("0.0.0", task.SimpleVersion);
			Assert.Equal(0, task.Major);
			Assert.Equal(0, task.Minor);
			Assert.Equal(0, task.Patch);
		}

        [Fact]
        public void when_executing_task_then_succeeds()
        {
            var tasksFile = new FileInfo(@"..\..\..\GetLatestVersion\bin\MSBuilder.NuGet.GetLatestVersion.props").FullName;

            var xmlProject = ProjectRootElement.Create();
            xmlProject.DefaultTargets = "Build";
            xmlProject.AddImport(tasksFile);
            xmlProject.AddImport(Path.Combine(MSBuildPath, "Microsoft.Common.tasks"));

            var buildXml = xmlProject.AddTarget("Build");
            var taskXml = buildXml.AddTask("GetLatestVersion");

            taskXml.SetParameter("PackageId", "xunit");
            taskXml.SetParameter("IncludePreRelease", "true");
            taskXml.AddOutputProperty("PackageVersion", "PackageVersion");

            var msgXml = buildXml.AddTask("Message");
            msgXml.SetParameter("Text", "PackageVersion=$(PackageVersion)");
            msgXml.SetParameter("Importance", "high");

            var tempFile = Path.GetTempFileName();
            xmlProject.Save(tempFile);

            var psi = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = Path.Combine(MSBuildPath, "MSBuild.exe"),
                Arguments = tempFile
            };

            var proc = Process.Start(psi);
            var output = proc.StandardOutput.ReadToEnd().Trim();
            var errors = proc.StandardError.ReadToEnd().Trim();
            if (errors.Length > 0)
                Assert.True(false, errors);

            proc.WaitForExit();

            Assert.True(proc.ExitCode == 0, output);
            Console.WriteLine(output);
        }

    }
}
