using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
		Dictionary<string, string> globalProperties;

		public ProjectLoader(Dictionary<string, string> globalProperties)
		{
			this.globalProperties = globalProperties;
		}

		public string LoadXml(string filePath)
		{
			if (initialized)
				throw new InvalidOperationException("Already initialized with a project.");

			initialized = true;

			//var domain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), null, 
			//	new AppDomainSetup
			//	{
			//		ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
			//		PrivateBinPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName),
			//		PrivateBinPathProbe = Path.GetDirectoryName(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName),
			//		ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
			//	});
			var domain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), null,
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName),
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName),
				false);

			var xmlFile = Path.GetTempFileName();
			try
			{
                var assemblies = typeof(MSBuildWorkspace).Assembly.GetReferencedAssemblies()
                    .Select(name => Assembly.Load(name))
                    .Concat(new[] { typeof(MSBuildWorkspace).Assembly })
                    .Select(asm => asm.ManifestModule.FullyQualifiedName)
                    .ToArray();

				domain.CreateInstance(typeof(IsolatedProjectReader).Assembly.FullName, typeof(IsolatedProjectReader).FullName,
					false, BindingFlags.Default, null, new object[] { assemblies, filePath, globalProperties, xmlFile }, null, null);

				return File.ReadAllText(xmlFile);
			}
			finally
			{
				AppDomain.Unload(domain);
				File.Delete(xmlFile);
			}
		}

		public void Dispose()
		{
		}
	}
}
