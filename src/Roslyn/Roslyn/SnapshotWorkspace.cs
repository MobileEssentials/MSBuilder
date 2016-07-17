using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace MSBuilder
{
    internal class SnapshotWorkspace : Workspace, IWorkspace
	{
		readonly IProjectLoaderFactory loaderFactory;

		public SnapshotWorkspace ()
			: this (new ProjectLoaderFactory ())
		{
		}

		public SnapshotWorkspace (IProjectLoaderFactory loaderFactory)
			: base (MefHostServices.DefaultHost, "SnapshotWorkspace")
		{
			this.loaderFactory = loaderFactory;
		}

        protected override void Dispose(bool finalize)
        {
            loaderFactory.Dispose();
        }

        public override bool CanApplyChange (ApplyChangesKind feature)
		{
			switch (feature) {
				case ApplyChangesKind.AddProject:
				case ApplyChangesKind.AddProjectReference:
					return true;
			}

			return false;
		}

		public Project GetOrAddProject (IBuildEngine buildEngine, string projectPath)
		{
			// Ensure full project paths always.
			var fullPath = new FileInfo (projectPath).FullName;
			if (!File.Exists (fullPath))
				throw new FileNotFoundException ("Project file not found.", fullPath);

			// We load projects only once.
			var project = FindProjectByPath (fullPath);
			if (project != null)
				return project;

			project = AddProject (buildEngine, fullPath);

			TryApplyChanges (CurrentSolution);

			return project;
		}

		Project AddProject (IBuildEngine buildEngine, string projectPath)
		{
			// NOTE: we use a different workspace for each project added 
			// because we need to set the current configuration/platform 
			// as a global property for each, according to the current 
			// solution configuration as detected by the current global 
			// properties. This is automatically done by the workspace 
			// factory, which looks up the project by ID and determines 
			// the configuration/platform to load.
			using (var loader = loaderFactory.Create (buildEngine)) {
				var msbuildProject = XElement.Parse (loader.LoadXml (projectPath)).ToDynamic();

				// Use the msbuild project to add a new project to the current solution of the workspace
				OnProjectAdded(
					ProjectInfo.Create(
						ProjectId.CreateFromSerialized(new Guid((string)msbuildProject.Id)),
						VersionStamp.Default,
						(string)msbuildProject.Name,
						(string)msbuildProject.AssemblyName,
						(string)msbuildProject.Language,
						(string)msbuildProject.FilePath,
						outputFilePath: (string)msbuildProject.OutputFilePath,
						metadataReferences: ((XElement)msbuildProject.MetadataReferences).Elements("FilePath").Select(e => MetadataReference.CreateFromFile(e.Value)),
						compilationOptions: new CSharpCompilationOptions(
							(OutputKind)(Enum.Parse(typeof(OutputKind), (string)msbuildProject.CompilationOptions["OutputKind"])),
							platform: (Platform)(Enum.Parse(typeof(Platform), (string)msbuildProject.CompilationOptions["Platform"])))));
				
				// Add the documents to the workspace
				foreach (var document in ((XElement)msbuildProject.Documents).Elements("FilePath").Select(e => e.Value))
					AddDocument ((string)msbuildProject.FilePath, document, false);
				foreach (var document in ((XElement)msbuildProject.AdditionalDocuments).Elements("FilePath").Select(e => e.Value))
					AddDocument ((string)msbuildProject.FilePath, document, true);

				// Fix references
				// Iterate the references of the msbuild project
				var referencesToAdd = new List<ProjectReference> ();
				foreach (var referencePath in ((XElement)msbuildProject.ProjectReferences).Elements("FilePath").Select(e => e.Value)) {
					var referencedProject = GetOrAddProject(buildEngine, referencePath);
					referencesToAdd.Add (new ProjectReference (referencedProject.Id));
				}

				if (referencesToAdd.Count > 0) {
					var addedProject = FindProjectByPath ((string)msbuildProject.FilePath);

					TryApplyChanges (CurrentSolution.WithProjectReferences (addedProject.Id, referencesToAdd));
				}

				return FindProjectByPath (projectPath);
			}
		}

		void AddDocument (string projectPath, string documentPath, bool isAdditionalDocument)
		{
			var project = FindProjectByPath (projectPath);
			SourceText text;
			using (var reader = new StreamReader (documentPath)) {
				text = SourceText.From (reader.BaseStream);
			}

			var documentInfo = DocumentInfo.Create (
				DocumentId.CreateNewId (project.Id),
				Path.GetFileName (documentPath),
				loader: TextLoader.From (TextAndVersion.Create (text, VersionStamp.Create (), documentPath)),
				filePath: documentPath);

			if (isAdditionalDocument)
				OnAdditionalDocumentAdded (documentInfo);
			else
				OnDocumentAdded (documentInfo);
		}

		Project FindProjectByPath (string projectPath)
		{
			return CurrentSolution.Projects.Where (x =>
				string.Equals (x.FilePath, projectPath, StringComparison.InvariantCultureIgnoreCase))
				.FirstOrDefault ();
		}
	}
}
