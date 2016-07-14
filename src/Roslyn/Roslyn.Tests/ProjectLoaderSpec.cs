using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace MSBuilder
{
	public class ProjectLoaderSpec
	{
		[Fact]
		public void when_loading_project_then_can_retrieve_info()
		{
			var props = new Dictionary<string, string>
			{
				{ "Foo", "Bar" },
				{ "CurrentSolutionConfigurationContents",
					$@"<CurrentSolutionConfigurationContents xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
						<SolutionConfiguration xmlns=''>
							<ProjectConfiguration Project='{{F1DB354D-8DB4-476C-9308-08CDC0E411F7}}' AbsolutePath='Content\CsLibrary\CsLibrary.csproj' BuildProjectInSolution='True'>Debug|AnyCPU</ProjectConfiguration>
							<ProjectConfiguration Project='{{3EDE89EC-A461-4E2C-BE95-05F63B96926C}}' AbsolutePath='Content\PclLibrary\PclLibrary.csproj' BuildProjectInSolution='True'>Debug|AnyCPU</ProjectConfiguration>
						</SolutionConfiguration>
					</CurrentSolutionConfigurationContents>"
				}
			};

			var loader = new ProjectLoader(props);

			var xml = loader.LoadXml(@"Content\CsLibrary\CsLibrary.csproj");
			var msbuildProject = XElement.Parse(xml).ToDynamic();

			var info = ProjectInfo.Create(
				ProjectId.CreateFromSerialized(new Guid((string)msbuildProject.Id)),
				VersionStamp.Default,
				(string)msbuildProject.Name,
				(string)msbuildProject.AssemblyName,
				(string)msbuildProject.Language,
				(string)msbuildProject.FilePath,
				outputFilePath: (string)msbuildProject.OutputFilePath,
				projectReferences: ((XElement)msbuildProject.ProjectReferences).Elements("Project").Select(e => new ProjectReference(ProjectId.CreateFromSerialized(new Guid(e.Attribute("Id").Value)))),
				metadataReferences: ((XElement)msbuildProject.MetadataReferences).Elements("FilePath").Select(e => MetadataReference.CreateFromFile(e.Value)));

			//File.WriteAllText(@"C:\Delete\roslyn.xml", xml);
			//Process.Start(@"C:\Delete\roslyn.xml");

			//Assert.Equal("1234", (string)msbuildProject["Id"]);
		}
	}

}
