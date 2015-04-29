using Microsoft.Build.Construction;
using Microsoft.Build.Utilities;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace MSBuilder
{
	public abstract class TaskInlinerTest
	{
		const string xmlns = "{http://schemas.microsoft.com/developer/msbuild/2003}";
        static readonly string MSBuildPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\12.0", "MSBuildToolsPath", @"C:\Program Files (x86)\MSBuild\12.0\bin\");

		protected abstract string TargetsFile { get; }

		protected abstract void AddTask(ProjectTargetElement buildTarget);

        protected void Build(bool useCompiledTasks)
		{
			var outputFile = Path.GetTempFileName();

			foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(TargetsFile)))
			{
				File.Copy(file, Path.Combine(Path.GetTempPath(), Path.GetFileName(file)), true);
			}

			var xmlProject = ProjectRootElement.Create();
			xmlProject.DefaultTargets = "Build";
			xmlProject.AddImport(Path.GetFileName(TargetsFile));
			var targetXml = xmlProject.AddTarget("Build");

			AddTask(targetXml);

			var tempFile = Path.GetTempFileName();

			Console.WriteLine(tempFile);
			xmlProject.Save(tempFile);

			var psi = new ProcessStartInfo
			{
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
                FileName = Path.Combine(MSBuildPath, "MSBuild.exe"),
                Arguments = (useCompiledTasks ? "/p:UseCompiledTasks=true " : "/p:UseCompiledTasks=false ") +
					tempFile
			};

			var proc = Process.Start(psi);
			var output = proc.StandardOutput.ReadToEnd().Trim();
			var errors = proc.StandardError.ReadToEnd().Trim();
			if (errors.Length > 0)
				Assert.True(false, errors);

			proc.WaitForExit();

			Assert.True(proc.ExitCode == 0, output);
		}
	}
}
