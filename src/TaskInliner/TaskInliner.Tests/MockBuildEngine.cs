using Microsoft.Build.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskInliner.Tests
{
	public class MockBuildEngine : IBuildEngine
	{
		public MockBuildEngine()
		{
			LoggedCustomEvents = new List<CustomBuildEventArgs>();
			LoggedErrorEvents = new List<BuildErrorEventArgs>();
			LoggedMessageEvents = new List<BuildMessageEventArgs>();
			LoggedWarningEvents = new List<BuildWarningEventArgs>();
		}

		public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
		{
			throw new NotSupportedException();
		}

		public int ColumnNumberOfTaskNode { get; set; }

		public bool ContinueOnError { get; set; }

		public int LineNumberOfTaskNode { get; set; }

		public string ProjectFileOfTaskNode { get; set; }

		public List<CustomBuildEventArgs> LoggedCustomEvents { get; private set; }
		public List<BuildErrorEventArgs> LoggedErrorEvents { get; private set; }
		public List<BuildMessageEventArgs> LoggedMessageEvents { get; private set; }
		public List<BuildWarningEventArgs> LoggedWarningEvents { get; private set; }

		public void LogCustomEvent(CustomBuildEventArgs e)
		{
			LoggedCustomEvents.Add(e);
		}

		public void LogErrorEvent(BuildErrorEventArgs e)
		{
			LoggedErrorEvents.Add(e);
		}

		public void LogMessageEvent(BuildMessageEventArgs e)
		{
			LoggedMessageEvents.Add(e);
		}

		public void LogWarningEvent(BuildWarningEventArgs e)
		{
			LoggedWarningEvents.Add(e);
		}

	}
}
