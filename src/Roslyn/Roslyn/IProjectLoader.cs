using System;

namespace MSBuilder
{
    interface IProjectLoader : IDisposable
	{
		string LoadXml (string filePath);
	}
}
