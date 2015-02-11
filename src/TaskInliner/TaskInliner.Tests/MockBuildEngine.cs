using Microsoft.Build.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Build.Framework
{
    public class MockBuildEngine : IBuildEngine
    {
        bool trace = false;

        public MockBuildEngine(bool trace = false)
        {
            this.trace = trace;
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
            if (trace)
                Console.WriteLine(e.Message);

            LoggedCustomEvents.Add(e);
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            if (trace)
                Console.WriteLine(e.Message);

            LoggedErrorEvents.Add(e);
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            if (trace)
                Console.WriteLine(e.Message);

            LoggedMessageEvents.Add(e);
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            if (trace)
                Console.WriteLine(e.Message);

            LoggedWarningEvents.Add(e);
        }

    }
}