using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using MSBuilder;

namespace Microsoft.Build.Utilities
{
    /// <summary>
    /// Provides extensions for <see cref="Task"/> that allows 
    /// to efficiently access and reuse a Roslyn <see cref="Workspace"/> 
    /// and loaded <see cref="Project"/>s across a build.
    /// </summary>
    public static class TaskExtensions
	{
		/// <summary>
		/// Gets the reusable <see cref="Workspace"/> for the 
		/// current build run.
		/// </summary>
		/// <param name="task">The task that needs to access the workspace.</param>
		/// <returns>An already initialized and reused <see cref="Workspace"/>, or a new one 
		/// if it is the first time it is accessed.</returns>
		public static Workspace GetWorkspace(this Task task)
		{
			var engine = task.BuildEngine4;

			// TODO: when file monitoring is added, we can add workspace reuse 
			// by using RegisteredTaskObjectLifetime.AppDomain when building inside VS.
			var lifetime = RegisteredTaskObjectLifetime.Build;

			var key = typeof (Workspace).FullName;
			var workspace = engine.GetRegisteredTaskObject (key, lifetime) as Workspace;
			if (workspace == null) {
				workspace = new SnapshotWorkspace ();
				engine.RegisterTaskObject (key, workspace, lifetime, false);
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
		/// <returns>The loaded Roslyn project.</returns>
		public static Project GetOrAddProject (this Task task, string projectPath)
		{
			var workspace = (IWorkspace)GetWorkspace (task);
			
			return workspace.GetOrAddProject (task.BuildEngine, projectPath);
		}
	}
}
