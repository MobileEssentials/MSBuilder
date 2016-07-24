using System;
using System.Xml.Linq;

namespace MSBuilder
{
    interface IProjectLoader : IDisposable
	{
		XElement LoadXml (string filePath);
	}
}
