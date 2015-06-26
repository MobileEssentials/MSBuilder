using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace MSBuilder
{
	public class TestOutputLogger : ILogger
	{
		ITestOutputHelper output;
		List<BuildMessageEventArgs> messages = new List<BuildMessageEventArgs>();
		List<BuildErrorEventArgs> errors = new List<BuildErrorEventArgs>();
		List<BuildWarningEventArgs> warnings = new List<BuildWarningEventArgs>();

		public TestOutputLogger(ITestOutputHelper output)
		{
			this.output = output;
			this.Verbosity = LoggerVerbosity.Normal;
		}

		public void Initialize(IEventSource eventSource)
		{
			eventSource.MessageRaised += (sender, e) =>
			{
				if (e.Importance <= MessageImportance.Normal)
				{
					output.WriteLine(e.Message);
					messages.Add(e);
				}
			};

			eventSource.ErrorRaised += (sender, e) =>
			{
				output.WriteLine(e.Message);
				errors.Add(e);
			};

			eventSource.WarningRaised += (sender, e) =>
			{
				output.WriteLine(e.Message);
				warnings.Add(e);
			};
		}

		public string Parameters { get; set; }

		public void Shutdown()
		{
		}

		public LoggerVerbosity Verbosity { get; set; }

		public IEnumerable<BuildMessageEventArgs> Messages { get { return this.messages; } }

		public IEnumerable<BuildWarningEventArgs> Warnings { get { return this.warnings; } }

		public IEnumerable<BuildErrorEventArgs> Errors { get { return this.errors; } }
	}
}
