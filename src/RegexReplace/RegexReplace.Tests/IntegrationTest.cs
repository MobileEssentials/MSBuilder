using Microsoft.Build.Construction;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace MSBuilder
{
	public class IntegrationTest : TaskInlinerTest, IDisposable
	{
		readonly string output = Path.GetTempFileName();
		readonly ITestOutputHelper writer;

		public IntegrationTest(ITestOutputHelper writer)
		{
			if (File.Exists(output))
				File.Delete(output);

			this.writer = writer;
		}

		public void Dispose()
		{
			if (File.Exists(output))
				File.Delete(output);
		}

		[InlineData(true)]
		[InlineData(false)]
		[Theory]
		public void when_building_then_succeeds(bool useCompiledTasks)
		{
			File.WriteAllText(output, "public const string Bar = \"Bar\"");

			Build(useCompiledTasks, buildTarget => 
			{
				var taskXml = buildTarget.AddTask("RegexReplace");
				taskXml.SetParameter("Files", output);
				taskXml.SetParameter("Pattern", "\"$");
				taskXml.SetParameter("Replacement", "\";");
				taskXml.SetParameter("Options", "Compiled | IgnoreCase | ExplicitCapture");
			}, 
			@"..\..\..\RegexReplace\bin\MSBuilder.RegexReplace.targets");

			var content = File.ReadAllText(output);

			writer.WriteLine(content);

			Assert.True(content.EndsWith("\";"));
		}
	}
}