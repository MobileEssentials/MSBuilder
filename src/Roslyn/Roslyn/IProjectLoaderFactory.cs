using System;
using Microsoft.Build.Framework;

namespace MSBuilder
{
    interface IProjectLoaderFactory : IDisposable
	{
		IProjectLoader Create (IBuildEngine buildEngine);
	}
}