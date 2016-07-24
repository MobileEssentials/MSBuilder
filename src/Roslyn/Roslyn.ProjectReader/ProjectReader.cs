using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.Build.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace MSBuilder
{
	/// <summary>
	/// Reads a project's information and returns it as XML.
	/// </summary>
	public static class ProjectReader
	{
		/// <summary>
		/// Read the given project file with the specified global 
		/// properties.
		/// </summary>
		public static XElement Read(string projectFile, Dictionary<string, string> properties)
		{
			// Detect the project configuration and platform 
			if (properties.ContainsKey("CurrentSolutionConfigurationContents"))
			{
				var xml = XElement.Parse(properties["CurrentSolutionConfigurationContents"]);
				var config = xml.Descendants("ProjectConfiguration")
					.Where(x => x.Attribute("AbsolutePath").Value.Equals(projectFile, StringComparison.OrdinalIgnoreCase))
					.Select(x => x.Value)
					.FirstOrDefault();
				if (config != null)
				{
					// Debug|AnyCPU
					var configPlat = config.Split('|');
					properties["Configuration"] = configPlat[0];
					properties["Platform"] = configPlat[1];
				}
			}

			var workspace = MSBuildWorkspace.Create(properties);
			var project = workspace.OpenProjectAsync(projectFile).Result;
			var references = project.MetadataReferences.OfType<PortableExecutableReference>().ToList();

			// Sometimes Roslyn fails to read the metadata, so we fallback to invoking 
			// msbuild's ResolveAssemblyReferences target instead.
			if (references.Count == 0) {
				var msbproj = new Microsoft.Build.Evaluation.Project(projectFile);
				var result = BuildManager.DefaultBuildManager.Build(new BuildParameters(),
					new BuildRequestData(projectFile, new Dictionary<string, string>(), null, new[] { "ResolveAssemblyReferences" }, null));

				// This is a best-case effort anyway, we could still fail if the project 
				// doesn't have the target or whatever.
				if (result.HasResultsForTarget("ResolveAssemblyReferences") && 
					result["ResolveAssemblyReferences"].ResultCode == TargetResultCode.Success)
					references = result.ResultsByTarget["ResolveAssemblyReferences"].Items
						.Select(i => MetadataReference.CreateFromFile(i.GetMetadata("FullPath")))
						.ToList();
			}

			return new XElement("Project",
				new XAttribute("Id", project.Id.Id),
				new XAttribute("Name", project.Name),
				new XAttribute("AssemblyName", project.AssemblyName),
				// We may support other languages than C# in our visitors down the road.
				new XAttribute("Language", project.Language),
				new XAttribute("FilePath", project.FilePath),
				new XAttribute("OutputFilePath", project.OutputFilePath),
				new XElement("CompilationOptions",
					new XAttribute("OutputKind", project.CompilationOptions.OutputKind.ToString()),
					new XAttribute("Platform", project.CompilationOptions.Platform.ToString())),
				new XElement("ProjectReferences", project.ProjectReferences
					.Where(x => workspace.CurrentSolution.Projects.Any(p => p.Id == x.ProjectId))
					.Select(x => new XElement("ProjectReference",
						new XAttribute("FilePath", workspace.CurrentSolution.Projects.First(p => p.Id == x.ProjectId).FilePath)))),
				new XElement("MetadataReferences", references.Select(x =>
					new XElement("MetadataReference", new XAttribute("FilePath", x.FilePath)))),
				new XElement("Documents", project.Documents.Select(x =>
					new XElement("Document",
						new XAttribute("FilePath", x.FilePath),
						new XAttribute("Folders", string.Join(Path.DirectorySeparatorChar.ToString(), x.Folders))))),
				new XElement("AdditionalDocuments", project.AdditionalDocuments.Select(x =>
					new XElement("Document",
						new XAttribute("FilePath", x.FilePath),
						new XAttribute("Folders", string.Join(Path.DirectorySeparatorChar.ToString(), x.Folders)))))
			);
		}
	}
}