using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;

namespace MSBuilder
{
	interface IProjectLoaderFactory
	{
		IProjectLoader Create (IBuildEngine buildEngine);
	}
}