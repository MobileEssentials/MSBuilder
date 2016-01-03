using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace MSBuilder
{
	class ProjectLoader : IProjectLoader
	{
		bool initialized;
		IDictionary<string,string> globalProperties;
		MSBuildWorkspace workspace;

		public ProjectLoader (IDictionary<string, string> globalProperties)
		{
			this.globalProperties = globalProperties;
		}

		public Project Initialize (string projectFilePath)
		{
			if (initialized)
				throw new InvalidOperationException ("Already initialized with a project.");

			// Detect the project configuration and platform 
			if (globalProperties.ContainsKey ("CurrentSolutionConfigurationContents")) {
				var xml = XElement.Parse (globalProperties["CurrentSolutionConfigurationContents"]);
				var config = xml.Elements ()
					.Where (x => x.Attribute ("AbsolutePath").Value.Equals (projectFilePath, StringComparison.OrdinalIgnoreCase))
					.Select (x => x.Value)
					.FirstOrDefault ();
				if (config != null) {
					// Debug|AnyCPU
					var configPlat = config.Split ('|');
					globalProperties["Configuration"] = configPlat[0];
					globalProperties["Platform"] = configPlat[1];
				}
			}

			workspace = MSBuildWorkspace.Create (globalProperties);
			initialized = true;

			return workspace.OpenProjectAsync (projectFilePath).Result;
		}

		public void Dispose ()
		{
			workspace.Dispose ();
		}
	}
}
