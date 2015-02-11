using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace MSBuilder.TaskInliner
{
	public class TasksUpdater
	{
		const string xmlns = "{http://schemas.microsoft.com/developer/msbuild/2003}";

		/// <summary>
		/// Manually run this "test" using TD.NET if you need to re-generate the TaskInliner.tasks 
		/// file with an updated version of itself. 
		/// Optionally delete the TaskInliner.tasks file from the Tasks project root (so that the 
		/// library can build successfully without using the old version) and run this. 
		/// A new version of the file will only be overwritten if after generation to a temp file, 
		/// a project importing it can successfully import the tasks file and invoke the task.
		/// </summary>
		public void UpdateTasksFile()
		{
			var outputFile = Path.GetTempFileName();

			var task = new GenerateTasksFile
			{
				BuildEngine = new MockBuildEngine(),
				OutputFile = outputFile,
				References = XDocument.Load(@"..\..\..\TaskInliner\TaskInliner.Tasks.csproj")
					.Root.Descendants(xmlns + "Reference")
					.Select(x => x.Attribute("Include").Value)
					.Select(x => new TaskItem(x)).ToArray(),
				SourceTasks = new ITaskItem[] { new TaskItem(@"..\..\..\TaskInliner\GenerateTasksFile.cs") },
			};

			Assert.True(task.Execute());

			var xmlProject = ProjectRootElement.Create();
			xmlProject.DefaultTargets = "Build";
			xmlProject.AddImport(outputFile);
			var taskXml = xmlProject.AddTarget("Build")
				.AddTask("GenerateTasksFile");

			taskXml.SetParameter("OutputFile", Path.GetTempFileName());
			taskXml.SetParameter("References", "@(Reference)");
			taskXml.SetParameter("SourceTasks", "@(Compile)");

			var tempFile = Path.GetTempFileName();
			xmlProject.Save(tempFile);

			var psi = new ProcessStartInfo
			{
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				FileName = @"C:\Program Files (x86)\MSBuild\12.0\bin\MSBuild.exe",
				Arguments = tempFile
			};

			var proc = Process.Start(psi);
			var output = proc.StandardOutput.ReadToEnd().Trim();
			var errors = proc.StandardError.ReadToEnd().Trim();
			if (errors.Length > 0)
				Assert.True(false, errors);

			proc.WaitForExit();

			Assert.True(proc.ExitCode == 0, output);

			if (File.Exists(@"..\..\..\TaskInliner\build\MSBuilder.TaskInliner.tasks"))
				File.Delete(@"..\..\..\TaskInliner\build\MSBuilder.TaskInliner.tasks");

			File.Copy(outputFile, @"..\..\..\TaskInliner\build\MSBuilder.TaskInliner.tasks");
		}
	}
}
