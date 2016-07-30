using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;

namespace MSBuilder
{
    interface IWorkspace
	{
		Project GetOrAddProject (IBuildEngine buildEngine, string projectPath, CancellationToken cancellation);
	}
}
