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
	/// Provides a MBRO that can be used to read an MSBuild project 
	/// in an isolated AppDomain.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class IsolatedProjectReader : MarshalByRefObject
	{
		/// <summary>
		/// 
		/// </summary>
		public IsolatedProjectReader(string filePath, Dictionary<string, string> properties, string xmlFile)
		{
			File.WriteAllText(xmlFile, ReadXml(filePath, properties));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="properties"></param>
		/// <returns></returns>
		string ReadXml(string filePath, Dictionary<string, string> properties)
		{
			// Detect the project configuration and platform 
			if (properties.ContainsKey("CurrentSolutionConfigurationContents"))
			{
				var xml = XElement.Parse(properties["CurrentSolutionConfigurationContents"]);
				var config = xml.Descendants("ProjectConfiguration")
					.Where(x => x.Attribute("AbsolutePath").Value.Equals(filePath, StringComparison.OrdinalIgnoreCase))
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
			var project = workspace.OpenProjectAsync(filePath).Result;
			var references = project.MetadataReferences.OfType<PortableExecutableReference>().ToList();

			if (references.Count == 0) {
				var msbproj = new Microsoft.Build.Evaluation.Project(filePath);
				var result = BuildManager.DefaultBuildManager.Build(new BuildParameters(),
					new BuildRequestData(filePath, new Dictionary<string, string>(), null, new[] { "ResolveAssemblyReferences" }, null));

				if (result.HasResultsForTarget("ResolveAssemblyReferences"))
					references = result.ResultsByTarget["ResolveAssemblyReferences"].Items
						.Select(i => MetadataReference.CreateFromFile(i.GetMetadata("FullPath")))
						.ToList();
			}

			return new XElement("Project",
				new XElement("Id", project.Id.Id),
				new XElement("Name", project.Name),
				new XElement("AssemblyName", project.AssemblyName),
				new XElement("Language", project.Language),
				new XElement("FilePath", project.FilePath),
				new XElement("OutputFilePath", project.OutputFilePath),
				new XElement("ProjectReferences", project.ProjectReferences
					.Where(x => workspace.CurrentSolution.Projects.Any(p => p.Id == x.ProjectId))
					.Select(x => new XElement("FilePath", workspace.CurrentSolution.Projects.First(p => p.Id == x.ProjectId).FilePath))),
				new XElement("MetadataReferences", references.Select(x => new XElement("FilePath", x.FilePath))),
				new XElement("Documents", project.Documents.Select(x => new XElement("FilePath", x.FilePath))),
				new XElement("AdditionalDocuments", project.AdditionalDocuments.Select(x => new XElement("FilePath", x.FilePath)))
			).ToString();
		}
	}
}