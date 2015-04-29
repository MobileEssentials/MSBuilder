using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Build.Utilities;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions;

namespace MSBuilder.TaskInliner
{
	public class IntegrationTest
	{
		const string xmlns = "{http://schemas.microsoft.com/developer/msbuild/2003}";
        static readonly string MSBuildPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\12.0", "MSBuildToolsPath", @"C:\Program Files (x86)\MSBuild\12.0\bin\");

		MockBuildEngine engine;
		ITestOutputHelper output;

		public IntegrationTest(ITestOutputHelper output)
		{
			engine = new MockBuildEngine(output);
			this.output = output;
		}

		[InlineData(true)]
		[InlineData(false)]
        [Theory]
        public void when_executing_task_then_succeeds(bool useCompiledTasks)
		{
			var outputFile = Path.GetTempFileName();

			var task = new GenerateTasksFile
			{
				BuildEngine = engine,
				TasksName = Path.GetFileNameWithoutExtension(outputFile),
				OutputPath = Path.GetDirectoryName(outputFile),
				License = @"
	The MIT License (MIT)

	Copyright (c) 2015 Mobile Essentials
",
				References = XDocument.Load(@"..\..\..\TaskInliner\TaskInliner.csproj")
					.Root.Descendants(xmlns + "Reference")
					.Select(x => x.Attribute("Include").Value)
					.Select(x => new TaskItem(x)).ToArray(),
				SourceTasks = new ITaskItem[] { new TaskItem(@"..\..\..\TaskInliner\GenerateTasksFile.cs") },
			};

			File.Copy("MSBuilder.TaskInliner.dll", Path.ChangeExtension(outputFile, ".dll"));

			Assert.True(task.Execute());

			var xmlProject = ProjectRootElement.Create();
			xmlProject.DefaultTargets = "Build";
			xmlProject.AddImport(task.TasksFile);
			var taskXml = xmlProject.AddTarget("Build")
				.AddTask("GenerateTasksFile");

			var prjFile = Path.GetTempFileName();

			taskXml.SetParameter("TasksName", Path.GetFileNameWithoutExtension(prjFile));
			taskXml.SetParameter("OutputPath", Path.GetDirectoryName(prjFile));
			taskXml.SetParameter("References", "@(Reference)");
			taskXml.SetParameter("SourceTasks", "@(Compile)");

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
			var stdout = proc.StandardOutput.ReadToEnd().Trim();
			var errors = proc.StandardError.ReadToEnd().Trim();
			if (errors.Length > 0)
				Assert.True(false, errors);

			proc.WaitForExit();

			Assert.True(proc.ExitCode == 0, stdout);
		}
	}
}
