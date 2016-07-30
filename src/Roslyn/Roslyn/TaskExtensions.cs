using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using MSBuilder;

namespace Microsoft.Build.Utilities
{
	/// <summary>
	/// Provides extensions for <see cref="ITask"/> that allows 
	/// to efficiently access and reuse a Roslyn <see cref="Workspace"/> 
	/// and loaded <see cref="Project"/>s across a build.
	/// </summary>
	public static class TaskExtensions
	{
		/// <summary>
		/// Gets the already loaded (and shared) instance of the current Roslyn 
		/// project being built when the task executes.
		/// </summary>
		/// <param name="task">The task that needs to access the project.</param>
		/// <param name="cancellation">Optional cancellation token to allow the calling task to cancel the operation.</param>
		/// <returns>The loaded Roslyn project or <see langword="null"/> if the 
		/// project language is not supported.</returns>
		public static Project GetProject(this ITask task, CancellationToken cancellation = default(CancellationToken))
		{
			ProjectInstance project;
			IEnumerable<object> targets;

			var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
			var engineType = task.BuildEngine.GetType();
			var callbackField = engineType.GetField("targetBuilderCallback", flags);

			if (callbackField != null)
			{
				// .NET field naming convention.
				var callback = callbackField.GetValue(task.BuildEngine);
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
				var callback = callbackField.GetValue(task.BuildEngine);
				var projectField = callback.GetType().GetField("_projectInstance", flags);
				project = (ProjectInstance)projectField.GetValue(callback);
				var targetsField = callback.GetType().GetField("_targetsToBuild", flags);
				targets = (IEnumerable<object>)targetsField.GetValue(callback);
			}

			return GetOrAddProject(task, project.ProjectFileLocation.File);
		}

		/// <summary>
		/// Gets the reusable <see cref="Workspace"/> for the 
		/// current build run.
		/// </summary>
		/// <param name="task">The task that needs to access the workspace.</param>
		/// <returns>An already initialized and reused <see cref="Workspace"/>, or a new one 
		/// if it is the first time it is accessed.</returns>
		public static Workspace GetWorkspace(this ITask task)
		{
			var engine = (IBuildEngine4)task.BuildEngine;

			// TODO: when file monitoring is added, we can add workspace reuse 
			// by using RegisteredTaskObjectLifetime.AppDomain when building inside VS.
			var lifetime = RegisteredTaskObjectLifetime.Build;

			var key = typeof(Workspace).FullName;
			var workspace = engine.GetRegisteredTaskObject(key, lifetime) as Workspace;
			if (workspace == null)
			{
				workspace = new SnapshotWorkspace();
				engine.RegisterTaskObject(key, workspace, lifetime, false);
			}

			return workspace;
		}

		/// <summary>
		/// Gets the already loaded (and shared) instance of the Roslyn project 
		/// for the given <paramref name="projectPath"/>, or loads and initializes 
		/// one.
		/// </summary>
		/// <param name="task">The task that needs to access the project.</param>
		/// <param name="projectPath">The project full path.</param>
		/// <param name="cancellation">Optional cancellation token to allow the calling task to cancel the operation.</param>
		/// <returns>The loaded Roslyn project.</returns>
		public static Project GetOrAddProject(this ITask task, string projectPath, CancellationToken cancellation = default(CancellationToken))
		{
			var workspace = (IWorkspace)GetWorkspace(task);

			return workspace.GetOrAddProject(task.BuildEngine, projectPath, cancellation);
		}
	}
}
