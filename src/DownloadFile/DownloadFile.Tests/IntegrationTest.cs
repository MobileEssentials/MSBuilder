using Microsoft.Build.Construction;
using System;
using System.IO;
using Xunit;

namespace MSBuilder
{
	public class IntegrationTest : TaskInlinerTest, IDisposable
	{
		readonly string output = Path.Combine(Path.GetTempPath(), "favicon.ico");

		public IntegrationTest()
		{
			if (File.Exists(output))
				File.Delete(output);
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
			Build(useCompiledTasks);

			Assert.True(File.Exists(output), "Expected file to be downloaded to " + output + " but it wasn't.");
		}

		protected override string TargetsFile
		{
			get { return @"..\..\..\DownloadFile\bin\MSBuilder.DownloadFile.targets"; }
		}

		protected override void AddTask(ProjectTargetElement buildTarget)
		{
			var taskXml = buildTarget.AddTask("DownloadFile");
			taskXml.SetParameter("DestinationFolder", Path.GetTempPath());
			taskXml.SetParameter("SourceUrl", "https://www.nuget.org/favicon.ico");
		}
	}
}