using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace MSBuilder
{
	public class ConsoleSpec
	{
		ITestOutputHelper output;

		public ConsoleSpec(ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void when_specifying_command_line_arg_then_does_not_wait_for_input_stream()
		{
			var projectFile = Path.Combine(ModuleInitializer.BaseDirectory, @"Content\PclLibrary\PclLibrary.csproj");
			var info = new ProcessStartInfo(
				Path.Combine(ModuleInitializer.BaseDirectory, "MSBuilder.Roslyn.ProjectReader.exe"),
				projectFile)
			{
				CreateNoWindow = true,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
			};

			var process = Process.Start(info);
			var output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();

			Assert.False(string.IsNullOrEmpty(output));
			var xml = XElement.Parse(output);

			this.output.WriteLine(xml.ToString());
		}

		[Fact]
		public void when_no_command_line_provided_then_reads_from_input_stream()
		{
			var projectFile = Path.Combine(ModuleInitializer.BaseDirectory, @"Content\PclLibrary\PclLibrary.csproj");
			var info = new ProcessStartInfo(
				Path.Combine(ModuleInitializer.BaseDirectory, "MSBuilder.Roslyn.ProjectReader.exe"))
			{
				CreateNoWindow = true,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
			};

			var process = Process.Start(info);
			var context = new XElement("Context",
				new XElement("Property",
					new XAttribute("Name", "ProjectFile"),
					new XText(projectFile)));

			this.output.WriteLine(context.ToString());

			process.StandardInput.WriteLine(context.ToString());
			process.StandardInput.Close();
			
			var output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();

			Assert.False(string.IsNullOrEmpty(output));
			var xml = XElement.Parse(output);

			this.output.WriteLine(xml.ToString());
		}
	}
}
