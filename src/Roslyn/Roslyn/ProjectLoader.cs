using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.MSBuild;

namespace MSBuilder
{
    class ProjectLoader : IProjectLoader
	{
		bool initialized;
		Dictionary<string, string> globalProperties;

		public ProjectLoader(Dictionary<string, string> globalProperties)
		{
			this.globalProperties = globalProperties;
		}

		public XElement LoadXml(string filePath)
		{
			if (initialized)
				throw new InvalidOperationException("Already initialized with a project.");

			initialized = true;

			var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName);
			var info = new ProcessStartInfo(Path.Combine(baseDir, "MSBuilder.Roslyn.ProjectReader.exe"))
			{
				CreateNoWindow = true,
				WorkingDirectory = baseDir,
				RedirectStandardInput = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
			};

			if (!File.Exists(info.FileName))
				throw new FileNotFoundException($"Failed to find the {Path.GetFileName(info.FileName)} executable at expected location {baseDir}.", info.FileName);

			var process = Process.Start(info);

			//Debug.WriteLine()
			var context = new XElement("Context",
				new XElement("Property",
					new XAttribute("Name", "ProjectFile"),
					new XText(filePath)),
				globalProperties.Select(prop =>
					new XElement("Property",
						new XAttribute("Name", prop.Key),
						new XText(prop.Value)
					)
				)
			);

			Debug.WriteLine("Requesting load with context: ");
			Debug.WriteLine(context.ToString());

			process.StandardInput.WriteLine(context.ToString());
			process.StandardInput.Close();

			var output = process.StandardOutput.ReadToEnd();
			var errors = process.StandardError.ReadToEnd();
			process.WaitForExit();

			if (process.ExitCode != 0)
				throw new ArgumentException(errors);

			return XElement.Parse(output);
		}

		public void Dispose()
		{
		}
	}
}
