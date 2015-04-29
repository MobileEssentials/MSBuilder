using Microsoft.Build.Construction;
using System.IO;
using Xunit;

namespace MSBuilder
{
	public class IntegrationTest : TaskInlinerTest
	{
		[InlineData(true)]
		[InlineData(false)]
        [Theory]
		public void when_building_then_succeeds(bool useCompiledTasks)
		{
			Build(useCompiledTasks);
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