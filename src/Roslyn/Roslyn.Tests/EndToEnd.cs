using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			var project = new Project(@"Content\CodeGen\CodeGen.csproj");
			var built = project.Build("CodeGen", new[] { new TestOutputLogger(output) });

			Assert.True(built);

			// Ensure we tear down to avoid locking assemblies.
			//Process.GetCurrentProcess().Kill();
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
