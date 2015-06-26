using Microsoft.Build.Utilities;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Build.Framework;
using System.IO;
using Microsoft.Build.Execution;

namespace MSBuilder
{
	/// <summary>
	/// Introspects the current project properties and targets being built.
	/// </summary>
	public class Introspect : Task
	{
		/// <summary>
		/// Returns all current project properties as an item, with 
		/// each property as an item metadata with its evaluated value.
		/// </summary>
		[Output]
		public Microsoft.Build.Framework.ITaskItem Properties { get; set; }

		/// <summary>
		/// Returns all current project targets being built as an item list.
		/// </summary>
		[Output]
		public Microsoft.Build.Framework.ITaskItem[] Targets { get; set; }

		/// <summary>
		/// Introspects the current project and retrieves its 
		/// properties and currently building targets.
		/// </summary>
		public override bool Execute()
		{
			ProjectInstance project;
			IEnumerable<object> targets;

			var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
			var engineType = BuildEngine.GetType();
			var callbackField = engineType.GetField("targetBuilderCallback", flags);

			if (callbackField != null)
			{
				// .NET field naming convention.
				var callback = callbackField.GetValue(BuildEngine);
				var projectField = callback.GetType().GetField("projectInstance", flags);
				project = (ProjectInstance)projectField.GetValue(callback);
				var targetsField = callback.GetType().GetField("targetsToBuild", flags);
				targets = (IEnumerable<object>)targetsField.GetValue(callback);
			}
			else
			{
				callbackField = engineType.GetField("_targetBuilderCallback", flags);
				if (callbackField == null)
					throw new NotSupportedException("Failed to introspect current MSBuild Engine.");

				// OSS field naming convention.
				var callback = callbackField.GetValue(BuildEngine);
				var projectField = callback.GetType().GetField("_projectInstance", flags);
				project = (ProjectInstance)projectField.GetValue(callback);
				var targetsField = callback.GetType().GetField("_targetsToBuild", flags);
				targets = (IEnumerable<object>)targetsField.GetValue(callback);
			}

			Properties = new TaskItem(project.ProjectFileLocation.File, project.Properties.ToDictionary(
				prop => prop.Name, prop => prop.EvaluatedValue));

			if (targets.Any())
			{
				var entryType = targets.First().GetType();
				var nameField = entryType.GetProperty("Name", flags);
				Targets = targets
					.Select(entry => (string)nameField.GetValue(entry))
					.Where(target => !project.InitialTargets.Contains(target))
					.Select(target => new TaskItem(target))
					.ToArray();
			}
			else
			{
				Targets = new ITaskItem[0];
			}

			return true;
		}
	}
}
