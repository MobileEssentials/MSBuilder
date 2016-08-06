using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace MSBuilder
{
	public class EndToEnd
	{
		ITestOutputHelper output;

		public EndToEnd(ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void when_building_then_succeeds()
		{
			var project = new Project(Path.Combine(ModuleInitializer.BaseDirectory, @"Content\CodeGen\CodeGen.csproj"));

			var built = project.Build("CodeGen", new[] { new TestOutputLogger(output) });

			Assert.True(built);
		}

		[Fact]
		public void when_reading_project_then_succeeds()
		{
			var project = ProjectReader.Read(
				Path.Combine(ModuleInitializer.BaseDirectory, @"Content\CodeGen\CodeGen.csproj"), 
				new Dictionary<string, string>());

			output.WriteLine(project.ToString());
		}
	}

	public class CodeGenTask : Task
	{
		[Required]
		public string ProjectFullPath { get; set; }

        [Output]
        public string ProjectId { get; set; }

        public override bool Execute()
		{
			var project = this.GetOrAddProject(ProjectFullPath);

            ProjectId = project.Id.Id.ToString();

			return true;
		}
	}
}
