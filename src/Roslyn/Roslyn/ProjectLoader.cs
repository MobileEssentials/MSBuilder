using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.MSBuild;

namespace MSBuilder
{
    class ProjectLoader : IProjectLoader
	{
        AppDomain appDomain;
        string[] assemblyFiles;
		bool initialized;
		Dictionary<string, string> globalProperties;

		public ProjectLoader(AppDomain appDomain, string[] assemblyFiles, Dictionary<string, string> globalProperties)
		{
            this.appDomain = appDomain;
            this.assemblyFiles = assemblyFiles;
			this.globalProperties = globalProperties;
		}

		public string LoadXml(string filePath)
		{
			if (initialized)
				throw new InvalidOperationException("Already initialized with a project.");

			initialized = true;

			var xmlFile = Path.GetTempFileName();
			try
			{
				appDomain.CreateInstance(typeof(IsolatedProjectReader).Assembly.FullName, typeof(IsolatedProjectReader).FullName,
					false, BindingFlags.Default, null, new object[] { assemblyFiles, filePath, globalProperties, xmlFile }, null, null);

				return File.ReadAllText(xmlFile);
			}
			finally
			{
				File.Delete(xmlFile);
			}
		}

        public void Dispose()
		{
		}
	}
}
