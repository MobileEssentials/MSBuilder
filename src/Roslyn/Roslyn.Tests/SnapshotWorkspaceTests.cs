using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MSBuilder
{
    public class SnapshotWorkspaceTests : IDisposable
	{
        Lazy<AppDomain> appDomain = new Lazy<AppDomain>(() => AppDomain.CreateDomain(Guid.NewGuid().ToString(), null,
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName),
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName),
                false));
        ITestOutputHelper output;

		public SnapshotWorkspaceTests (ITestOutputHelper output)
		{
			this.output = output;
		}

        public void Dispose()
        {
            if (appDomain.IsValueCreated)
                AppDomain.Unload(appDomain.Value);
        }

		[Fact]
		public void when_loading_projects_then_loads_proper_configuration_from_solution ()
		{
			var properties = new Dictionary<string, string> {
				{ "CurrentSolutionConfigurationContents", @"<SolutionConfiguration>
<ProjectConfiguration Project=""{f1db354d-8db4-476c-9308-08cdc0e411f7}"" AbsolutePath=""" + ModuleInitializer.BaseDirectory + @"\Content\CsLibrary\CsLibrary.csproj"">Debug|AnyCPU</ProjectConfiguration>
<ProjectConfiguration Project=""{a8ea2d18-4125-4cfd-a9de-6112f38df636}"" AbsolutePath=""" + ModuleInitializer.BaseDirectory + @"\Content\VbLibrary\VbLibrary.vbproj"">Debug|AnyCPU</ProjectConfiguration>
<ProjectConfiguration Project=""{3ede89ec-a461-4e2c-be95-05f63b96926c}"" AbsolutePath=""" + ModuleInitializer.BaseDirectory + @"\Content\PclLibrary\PclLibrary.csproj"">Release|AnyCPU</ProjectConfiguration>
<ProjectConfiguration Project=""{b7009850-92bd-4926-a2a6-1208f1dcd645}"" AbsolutePath=""" + ModuleInitializer.BaseDirectory + @"\Content\FsLibrary\FsLibrary.fsproj"">Debug|AnyCPU</ProjectConfiguration>
</SolutionConfiguration>" 
				}, 
			};

			var factory = new Mock<IProjectLoaderFactory> ();
			factory.Setup (x => x.Create (It.IsAny<IBuildEngine> ()))
				.Returns (() => new ProjectLoader (
                    appDomain.Value,
                    properties));
			var workspace = new SnapshotWorkspace (factory.Object);

			var project = workspace.GetOrAddProject (Mock.Of<IBuildEngine> (), Path.Combine(ModuleInitializer.BaseDirectory, @"Content\CsLibrary\CsLibrary.csproj"));

			// Configuration for the main project is Debug in the selected solution configuration.
			Assert.Equal ("Debug", new DirectoryInfo (Path.GetDirectoryName (project.OutputFilePath)).Name);

			// We have a single project reference
			Assert.Equal (1, project.ProjectReferences.Count ());

			var reference = project.Solution.GetProject (project.ProjectReferences.First ().ProjectId);
			// The reference exists in the project solution
			Assert.NotNull (reference);

			// Configuration for the referenced project is Release in the selected solution configuration.
			Assert.Equal ("Release", new DirectoryInfo (Path.GetDirectoryName (reference.OutputFilePath)).Name);
			// Because solution configuration is release for the PclLibrary, the Release.cs should be included
			Assert.True (reference.Documents.Any (doc => Path.GetFileName (doc.FilePath) == "Release.cs"));
		}

		[Fact]
		public void when_loading_without_solution_configuration_then_loads_default_configuration ()
		{
			var factory = new Mock<IProjectLoaderFactory> ();
			factory.Setup (x => x.Create (It.IsAny<IBuildEngine> ()))
				.Returns (() => new ProjectLoader (
                    appDomain.Value,
                    new Dictionary<string, string> ()));
			var workspace = new SnapshotWorkspace (factory.Object);

			var project = workspace.GetOrAddProject (Mock.Of<IBuildEngine> (), Path.Combine(ModuleInitializer.BaseDirectory, @"Content\CsLibrary\CsLibrary.csproj"));
			var reference = project.Solution.GetProject (project.ProjectReferences.First ().ProjectId);

			// Because there is no solution configuration, PclLibrary defaults to Debug configuration, and the 
			// Release.cs should be not included since it has a condition on Configuration=Release
			Assert.False (reference.Documents.Any (doc => Path.GetFileName (doc.FilePath) == "Release.cs"));
		}

		[Fact]
		public void when_loading_projects_then_loads_documents ()
		{
			var properties = new Dictionary<string, string> {
				{ "CurrentSolutionConfigurationContents", @"<SolutionConfiguration>
<ProjectConfiguration Project=""{f1db354d-8db4-476c-9308-08cdc0e411f7}"" AbsolutePath=""" + ModuleInitializer.BaseDirectory + @"\Content\CsLibrary\CsLibrary.csproj"">Debug|AnyCPU</ProjectConfiguration>
<ProjectConfiguration Project=""{a8ea2d18-4125-4cfd-a9de-6112f38df636}"" AbsolutePath=""" + ModuleInitializer.BaseDirectory + @"\Content\VbLibrary\VbLibrary.vbproj"">Debug|AnyCPU</ProjectConfiguration>
<ProjectConfiguration Project=""{3ede89ec-a461-4e2c-be95-05f63b96926c}"" AbsolutePath=""" + ModuleInitializer.BaseDirectory + @"\Content\PclLibrary\PclLibrary.csproj"">Release|AnyCPU</ProjectConfiguration>
<ProjectConfiguration Project=""{b7009850-92bd-4926-a2a6-1208f1dcd645}"" AbsolutePath=""" + ModuleInitializer.BaseDirectory + @"\Content\FsLibrary\FsLibrary.fsproj"">Debug|AnyCPU</ProjectConfiguration>
</SolutionConfiguration>" 
				}, 
			};

			var factory = new Mock<IProjectLoaderFactory> ();
			factory.Setup (x => x.Create (It.IsAny<IBuildEngine> ()))
				.Returns (() => new ProjectLoader (
                    appDomain.Value,
                    properties));
			var workspace = new SnapshotWorkspace (factory.Object);

			var project = workspace.GetOrAddProject (Mock.Of<IBuildEngine> (), Path.Combine(ModuleInitializer.BaseDirectory, @"Content\CsLibrary\CsLibrary.csproj"));
			var reference = project.Solution.GetProject (project.ProjectReferences.First ().ProjectId);

			Assert.True (project.Documents.Any (doc => Path.GetFileName (doc.FilePath) == "Class1.cs"));
			Assert.True (project.Documents.Any (doc => Path.GetFileName (doc.FilePath) == "AssemblyInfo.cs"));
			Assert.True (reference.Documents.Any (doc => Path.GetFileName (doc.FilePath) == "Class1.cs"));
			Assert.True (reference.Documents.Any (doc => Path.GetFileName (doc.FilePath) == "AssemblyInfo.cs"));

			// CsLibrary has AdditionalFileItemNames=None
			Assert.True (project.AdditionalDocuments.Any (doc => Path.GetFileName (doc.FilePath) == "CsTextInFolder.txt"));
		}
	}
}