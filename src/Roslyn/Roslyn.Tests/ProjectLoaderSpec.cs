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
using Microsoft.CodeAnalysis.MSBuild;
using Xunit;

namespace MSBuilder
{
	public class ProjectLoaderSpec : IDisposable
	{
        Lazy<AppDomain> appDomain = new Lazy<AppDomain>(() => AppDomain.CreateDomain(Guid.NewGuid().ToString(), null,
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName),
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName),
                false));

        public void Dispose()
        {
            if (appDomain.IsValueCreated)
                AppDomain.Unload(appDomain.Value);
        }

        [Fact]
		public void when_loading_project_then_can_retrieve_info()
		{
			var props = new Dictionary<string, string>
			{
				{ "Foo", "Bar" },
				{ "CurrentSolutionConfigurationContents",
					$@"<CurrentSolutionConfigurationContents xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
						<SolutionConfiguration xmlns=''>
							<ProjectConfiguration Project='{{F1DB354D-8DB4-476C-9308-08CDC0E411F7}}' AbsolutePath='{ModuleInitializer.BaseDirectory}\Content\CsLibrary\CsLibrary.csproj' BuildProjectInSolution='True'>Debug|AnyCPU</ProjectConfiguration>
							<ProjectConfiguration Project='{{3EDE89EC-A461-4E2C-BE95-05F63B96926C}}' AbsolutePath='{ModuleInitializer.BaseDirectory}\Content\PclLibrary\PclLibrary.csproj' BuildProjectInSolution='True'>Debug|AnyCPU</ProjectConfiguration>
						</SolutionConfiguration>
					</CurrentSolutionConfigurationContents>"
				}
			};

			var loader = new ProjectLoader(
                appDomain.Value,
                props);

			var xml = loader.LoadXml(Path.Combine(ModuleInitializer.BaseDirectory, @"Content\CsLibrary\CsLibrary.csproj"));
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
