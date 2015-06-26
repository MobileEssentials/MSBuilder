using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MSBuilder
{
    public class IntrospectTests : IDisposable
    {
		ITestOutputHelper output;
		TestOutputLogger logger;

		public IntrospectTests(ITestOutputHelper output)
		{
			this.output = output;
			this.logger = new TestOutputLogger(output);
		}

		[Fact]
		public void when_introspecting_then_retrieves_current_target()
		{
			var eval = new Project("IntrospectTests.targets");
			eval.SetGlobalProperty("UseCompiledTasks", "true");
			var project = BuildManager.DefaultBuildManager.GetProjectInstanceForBuild(eval);
			IDictionary<string, TargetResult> outputs; 

			var result = project.Build(new [] { "IntrospectTargets" }, new[] { logger }, out outputs);

			Assert.True(result);
			Assert.True(outputs.ContainsKey("IntrospectTargets"));
			Assert.Equal(1, outputs["IntrospectTargets"].Items.Length);

			var target = outputs["IntrospectTargets"].Items[0];

			Assert.Equal("IntrospectTargets", target.ItemSpec);
		}

		[Fact]
		public void when_introspecting_then_retrieves_properties()
		{
			var eval = new Project("IntrospectTests.targets");
			eval.SetGlobalProperty("UseCompiledTasks", "true");
			var project = BuildManager.DefaultBuildManager.GetProjectInstanceForBuild(eval);
			IDictionary<string, TargetResult> outputs;
			
			var result = project.Build(new [] { "IntrospectProperties" }, new[] { logger }, out outputs);

			Assert.True(result);
			Assert.True(outputs.ContainsKey("IntrospectProperties"));
			Assert.Equal(1, outputs["IntrospectProperties"].Items.Length);

			var target = outputs["IntrospectProperties"].Items[0];
			var metadata = new HashSet<string>(target.MetadataNames.OfType<string>());

			Assert.True(metadata.Contains("MSBuildBinPath"));
			Assert.Equal("Bar", target.GetMetadata("Foo"));
		}

		[Fact]
		public void when_introspecting_then_retrieves_dynamic_value()
		{
			var eval = new Project("IntrospectTests.targets");
			eval.SetGlobalProperty("UseCompiledTasks", "true");
			var project = BuildManager.DefaultBuildManager.GetProjectInstanceForBuild(eval);
			IDictionary<string, TargetResult> outputs;

			project.SetProperty("PropertyName", "MSBuildRuntimeVersion");
			var result = project.Build(new [] { "GetDynamicValue" }, new[] { logger }, out outputs);

			Assert.True(result); 
			Assert.True(outputs.ContainsKey("GetDynamicValue"));
			Assert.Equal(1, outputs["GetDynamicValue"].Items.Length);

			var target = outputs["GetDynamicValue"].Items[0];
			var expected = project.GetPropertyValue("MSBuildRuntimeVersion");

			Assert.Equal(expected, target.ItemSpec);
		}

		[Fact]
		public void when_getting_targets_then_does_not_include_initial_targets()
		{
			var eval = new Project("IntrospectTests.targets");
			eval.SetGlobalProperty("UseCompiledTasks", "true");
			var project = BuildManager.DefaultBuildManager.GetProjectInstanceForBuild(eval);
			IDictionary<string, TargetResult> outputs; 

			var result = project.Build(new string[0], new[] { logger }, out outputs);

			Assert.True(result);
			Assert.True(outputs.ContainsKey("Build"));
			Assert.True(!outputs["Build"].Items.Select(t => t.ItemSpec).Contains("Startup"));
		}

		public void Dispose()
		{
			ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
			BuildManager.DefaultBuildManager.ResetCaches();
		}
	}
}
