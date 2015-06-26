using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Utilities;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Build.Framework;
using System.IO;

namespace MSBuilder
{
	public class EngineTests
	{
		TestOutputLogger logger;

		public EngineTests(ITestOutputHelper output)
		{
			this.logger = new TestOutputLogger(output);
		}

		[Fact]
		public void when_running_test_task_then_can_debug_it()
		{
			var project = BuildManager.DefaultBuildManager.GetProjectInstanceForBuild(new Project("EngineTests.targets"));
			 
			project.Build(new[] { logger });
		}
	}

	public class TestTask : Task
	{
		[Output]
		public ITaskItem Project { get; set; }

		[Output]
		public ITaskItem[] Targets { get; set; }

		public override bool Execute()
		{
			var engine = BuildEngine.AsDynamicReflection();
			ProjectInstance project;
			IEnumerable<object> targets;

			try
			{
				// TODO: when the oss msbuild is used more frequently 
				// than the .NET one, swap these calls with the ones in the catch.
				var callback = engine.targetBuilderCallback;
				project = callback.projectInstance;
				targets = callback.targetsToBuild.target;
			}
			catch (RuntimeBinderException)
			{
				// Naming convention changed in the oss msbuild
				var callback = engine._targetBuilderCallback;
				project = callback._projectInstance;
				targets = callback._targetsToBuild.target;
			}

			var targetNames = ((IEnumerable<object>)targets)
				.Select(entry => entry.AsDynamicReflection())
				.Select(entry => new TaskItem((string)entry.Name, new Dictionary<string, string>
					{
						{ "File", (string)entry.ReferenceLocation.File },
						{ "Column", ((int)entry.ReferenceLocation.Column).ToString() },
						{ "Line", ((int)entry.ReferenceLocation.Line).ToString() },
						{ "Location", (string)entry.ReferenceLocation.LocationString },
					}))
				.ToArray();

			Targets = targetNames;
			Project = new TaskItem(project.ProjectFileLocation.File, project.Properties.ToDictionary(
				prop => prop.Name, prop => prop.EvaluatedValue));

			return true;
		}
	}
}
