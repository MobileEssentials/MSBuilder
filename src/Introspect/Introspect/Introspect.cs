using Microsoft.Build.Utilities;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Dynamic;
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
		public ITaskItem Properties { get; set; }

		/// <summary>
		/// Returns all current project targets being built as an item 
		/// list with metadata for their file, column and line information.
		/// </summary>
		[Output]
		public ITaskItem[] Targets { get; set; }

		/// <summary>
		/// Introspects the current project and retrieves its 
		/// properties and currently building targets.
		/// </summary>
		public override bool Execute()
		{
			var engine = BuildEngine.AsDynamicReflection();
			ProjectInstance project;
			IEnumerable<object> targets;

			try
			{
				// TODO: when the oss msbuild is used more frequently 
				// than the .NET one, swap these calls with the ones in the catch.
				var callback = engine.targetBuilderCallback;
				project = callback.projectInstance;
				targets = callback.targetsToBuild.target;
			}
			catch (RuntimeBinderException)
			{
				// Naming convention changed in the oss msbuild
				var callback = engine._targetBuilderCallback;
				project = callback._projectInstance;
				targets = callback._targetsToBuild.target;
			}

			var targetNames = ((IEnumerable<object>)targets)
				.Select(entry => entry.AsDynamicReflection())
				.Select(entry => new TaskItem((string)entry.Name, new Dictionary<string, string>
					{
						{ "File", (string)entry.ReferenceLocation.File },
						{ "Column", ((int)entry.ReferenceLocation.Column).ToString() },
						{ "Line", ((int)entry.ReferenceLocation.Line).ToString() },
						{ "Location", (string)entry.ReferenceLocation.LocationString },
					}))
				.ToArray();

			Targets = targetNames;
			Properties = new TaskItem(project.ProjectFileLocation.File, project.Properties.ToDictionary(
				prop => prop.Name, prop => prop.EvaluatedValue));

			return true;
		}
	}
}
