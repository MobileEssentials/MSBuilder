using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.MSBuild;

namespace MSBuilder
{
	class ProjectLoaderFactory : IProjectLoaderFactory
	{
		Lazy<AppDomain> appDomain = new Lazy<AppDomain>(() =>
			AppDomain.CreateDomain(Guid.NewGuid().ToString(), null, AppDomain.CurrentDomain.SetupInformation));

		public IProjectLoader Create(IBuildEngine buildEngine)
		{
			// Use introspection to determine the global properties to use for the 
			// workspace.
			var globalProperties = GetGlobalProperties(buildEngine);

			return new ProjectLoader(appDomain.Value, globalProperties);
		}

		public void Dispose()
		{
			if (appDomain.IsValueCreated)
				AppDomain.Unload(appDomain.Value);
		}

		Dictionary<string, string> GetGlobalProperties(IBuildEngine buildEngine)
		{
			var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
			var engineType = buildEngine.GetType();
			var callbackField = engineType.GetField("targetBuilderCallback", flags);

			IDictionary<string, string> properties;

			if (callbackField != null)
			{
				// .NET field naming convention.
				var callback = callbackField.GetValue(buildEngine);
				var projectField = callback.GetType().GetField("projectInstance", flags);
				var project = (ProjectInstance)projectField.GetValue(callback);
				properties = project.GlobalProperties;
			}
			else
			{
				callbackField = engineType.GetField("_targetBuilderCallback", flags);
				if (callbackField == null)
					throw new NotSupportedException("Failed to introspect current MSBuild Engine.");

				// OSS field naming convention.
				var callback = callbackField.GetValue(buildEngine);
				var projectField = callback.GetType().GetField("_projectInstance", flags);
				var project = (ProjectInstance)projectField.GetValue(callback);
				properties = project.GlobalProperties;
			}

			// Filter out internal/private properties, denoted by _
			return properties
				.Where(pair => !pair.Key.StartsWith("_", StringComparison.Ordinal))
				.ToDictionary(pair => pair.Key, pair => pair.Value);
		}
	}
}
