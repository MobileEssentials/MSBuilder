using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace MSBuilder.TaskInliner
{
	public class GenerateTasksFileTests : IDisposable
	{
		const string xmlns = "{http://schemas.microsoft.com/developer/msbuild/2003}";

		string sourceFile;
		string outputFile;
		MockBuildEngine buildEngine;

		public GenerateTasksFileTests()
		{
			sourceFile = Path.GetTempFileName();
			outputFile = Path.GetTempFileName();
			buildEngine = new MockBuildEngine();
		}

		[Fact]
		public void when_task_file_does_not_contain_public_class_then_logs_warning()
		{
			var content = @"
using System;

namespace Test
{
	internal class MyTask
	{
	}
}
";

			var task = CreateTask(content);
	
			Assert.True(task.Execute());
			Assert.Equal(1, buildEngine.LoggedWarningEvents.Count);
		}

		[Fact]
		public void when_task_file_does_not_contain_task_class_then_logs_error()
		{
			var content = @"
using System;

[assembly: AssemblyTitle(""Title"")]
";

			var task = CreateTask(content);
	
			Assert.True(task.Execute());
			Assert.Equal(1, buildEngine.LoggedWarningEvents.Count);
		}

		[Fact]
		public void when_extracing_usings_then_generates_usings_in_fragment()
		{
			var content = @"
using System;
using System.Collections.Generic;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Xunit;

namespace TaskInliner.Tests
{
	public class MyTask : Task
	{
		public override bool Execute ()
		{
			return true;
		}	
	}
}
";

			var task = CreateTask(content);
	
			Assert.True(task.Execute());
			Assert.Equal(1, task.OutputTasks.Length);

			var usings = task.XmlTasks[0]
				.Descendants(xmlns + "Using")
				.Select(x => x.Attribute("Namespace"))
				.Where(x => x != null)
				.Select(x => x.Value)
				.ToArray();

			Assert.Contains("System", usings);
			Assert.Contains("System.Collections.Generic", usings);
			Assert.Contains("Microsoft.Build.Utilities", usings);
			Assert.Contains("Microsoft.Build.Framework", usings);
			Assert.Contains("Xunit", usings);
		}

		[Fact]
		public void when_specifying_references_then_generated_xml_contains_references()
		{
			var content = @"
using System;
using System.Collections.Generic;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Xunit;

namespace TaskInliner.Tests
{
	public class MyTask : Task
	{
		public override bool Execute ()
		{
			return true;
		}	
	}
}
";

			var expectedRefs = new string[] 
			{
				"System",
				"System.Core",
				"Microsoft.CSharp",
				"System.Xml"
			};

			var task = CreateTask(content, expectedRefs);
	
			Assert.True(task.Execute());
			Assert.Equal(1, task.OutputTasks.Length);

			var actualRefs = task.XmlTasks[0]
				.Descendants(xmlns + "Reference")
				.Select(x => x.Attribute("Include"))
				.Where(x => x != null)
				.Select(x => x.Value)
				.ToArray();

			Assert.Equal(expectedRefs, actualRefs);
		}

		[Fact]
		public void when_specifying_references_then_generated_xml_skips_built_in_msbuild_references()
		{
			var content = @"
using System;
using System.Collections.Generic;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Xunit;

namespace TaskInliner.Tests
{
	public class MyTask : Task
	{
		public override bool Execute ()
		{
			return true;
		}	
	}
}
";

			var addedRefs = new string[] 
			{
				"Microsoft.Build.Framework",
				"Microsoft.Build.Utilities.v4.0",
				"System",
				"System.Core",
				"Microsoft.CSharp",
				"System.Xml"
			};
			var expectedRefs = new string[] 
			{
				"System",
				"System.Core",
				"Microsoft.CSharp",
				"System.Xml"
			};

			var task = CreateTask(content, addedRefs);
	
			Assert.True(task.Execute());
			Assert.Equal(1, task.OutputTasks.Length);

			var actualRefs = task.XmlTasks[0]
				.Descendants(xmlns + "Reference")
				.Select(x => x.Attribute("Include"))
				.Where(x => x != null)
				.Select(x => x.Value)
				.ToArray();

			Assert.Equal(expectedRefs, actualRefs);
		}

		[Fact]
		public void when_task_contains_output_property_then_adds_it_to_parameter_group()
		{
			var content = @"
namespace Test
{
	public class MyTask : Task
	{
		[Output]
		public System.String[] Results { get; set; }		

		public override bool Execute ()
		{
			return true;
		}	
	}
}
";

			var task = CreateTask(content);
	
			Assert.True(task.Execute());
			Assert.Equal(1, task.OutputTasks.Length);

			var parameters = task.XmlTasks[0]
				.Element(xmlns + "ParameterGroup")
				.Elements()
				.ToArray();

			Assert.Equal(1, parameters.Length);

			var parameter = parameters[0];

			Assert.Equal("Results", parameter.Name.LocalName);
			Assert.NotNull(parameter.Attribute("Output"));
			Assert.Equal("true", parameter.Attribute("Output").Value);
			Assert.Equal("System.String[]", parameter.Attribute("ParameterType").Value);
		}

		[Fact]
		public void when_task_contains_required_input_property_then_adds_it_to_parameter_group()
		{
			var content = @"
namespace Test
{
	public class MyTask : Task
	{
		[Required]
		public System.String File { get; set; }		

		public override bool Execute ()
		{
			return true;
		}	
	}
}
";

			var task = CreateTask(content);
	
			Assert.True(task.Execute());
			Assert.Equal(1, task.OutputTasks.Length);

			var parameters = task.XmlTasks[0]
				.Element(xmlns + "ParameterGroup")
				.Elements()
				.ToArray();

			Assert.Equal(1, parameters.Length);

			var parameter = parameters[0];

			Assert.Equal("File", parameter.Name.LocalName);
			Assert.NotNull(parameter.Attribute("Required"));
			Assert.Equal("true", parameter.Attribute("Required").Value);
			Assert.Equal("System.String", parameter.Attribute("ParameterType").Value);
		}

		[Fact]
		public void when_task_input_property_is_string_then_omits_parameter_type()
		{
			var content = @"
namespace Test
{
	public class MyTask : Task
	{
		public string File { get; set; }		

		public override bool Execute ()
		{
			return true;
		}	
	}
}
";

			var task = CreateTask(content);
	
			Assert.True(task.Execute());
			Assert.Equal(1, task.OutputTasks.Length);

			var parameters = task.XmlTasks[0]
				.Element(xmlns + "ParameterGroup")
				.Elements()
				.ToArray();

			Assert.Equal(1, parameters.Length);

			var parameter = parameters[0];

			Assert.Equal("File", parameter.Name.LocalName);
			Assert.Null(parameter.Attribute("ParameterType"));
		}

		[Fact]
		public void when_task_input_property_not_required_then_omits_required_attribute()
		{
			var content = @"
namespace Test
{
	public class MyTask : Task
	{
		public string File { get; set; }		

		public override bool Execute ()
		{
			return true;
		}	
	}
}
";

			var task = CreateTask(content);
	
			Assert.True(task.Execute());
			Assert.Equal(1, task.OutputTasks.Length);

			var parameters = task.XmlTasks[0]
				.Element(xmlns + "ParameterGroup")
				.Elements()
				.ToArray();

			Assert.Equal(1, parameters.Length);

			var parameter = parameters[0];

			Assert.Equal("File", parameter.Name.LocalName);
			Assert.Null(parameter.Attribute("Required"));
		}

		[Fact]
		public void when_task_contains_code_then_it_is_added_to_task_as_cdata()
		{
			var content = @"
using System;

namespace Test
{
	public class MyTask : Task
	{
		public string File { get; set; }		

		public override bool Execute ()
		{
			Console.WriteLine(""Hello World"");

			return true;
		}	
	}
}
";

			var task = CreateTask(content);
	
			Assert.True(task.Execute());
			Assert.Equal(1, task.OutputTasks.Length);

			var code = task.XmlTasks[0]
				.Element(xmlns + "Task")
				.Element(xmlns + "Code");

			Assert.Equal(XmlNodeType.CDATA, code.FirstNode.NodeType);
			Assert.Contains(@"Console.WriteLine(""Hello World"");", code.FirstNode.ToString());
		}

		[Fact]
		public void when_project_file_generated_then_can_load_it()
		{
			var content = @"
using System;

namespace Test
{
	public class MyTask : Task
	{
		[Required]
		public string File { get; set; }		

		[Output]
		public string Result { get; set; }		

		public override bool Execute ()
		{
			Result = File;

			Log.LogMessage(File);
			Log.LogMessage(Result);

			return true;
		}	
	}
}
";

			CreateTask(content).Execute();
	
			Assert.True(BuildProject());
		}

		[Fact]
		public void when_task_has_documentation_then_generates_header_information()
		{
			var content = @"
using System;

namespace Test
{
	/// <summary>
	/// Class summary
	/// over two lines.
	/// </summary>
	/// <remarks>A remark</remarks>
	public class MyTask : Task
	{
		/// <summary>
		/// Some property summary
		/// </summary>
		[Required]
		public string File { get; set; }		

		/// <summary>
		/// Output property summary
		/// </summary>
		[Output]
		public string Result { get; set; }		

		public override bool Execute ()
		{
			Result = File;

			Log.LogMessage(File);
			Log.LogMessage(Result);

			return true;
		}	
	}
}
";

			CreateTask(content).Execute();

			Assert.True(BuildProject());

			var contents = File.ReadAllText(outputFile);

			Assert.Contains("Class summary", contents);
			Assert.Contains("over two lines.", contents);
			Assert.Contains("Some property summary", contents);
			Assert.Contains("Output property summary", contents);
		}

		[Fact]
		public void when_task_has_generic_property_then_it_is_ignored()
		{
			var content = @"
using System;

namespace Test
{
	public class MyTask : Task
	{
		public KeyValue<string, string> Pair { get; set; }		

		public override bool Execute ()
		{
			Console.WriteLine(""Hello World"");

			return true;
		}	
	}
}
";

			var task = CreateTask(content);

			Assert.True(task.Execute());
		}


		private bool BuildProject(LoggerVerbosity verbosity = LoggerVerbosity.Quiet)
		{
			var xmlProject = ProjectRootElement.Create();
			xmlProject.AddImport(outputFile);
			xmlProject.AddTarget("Build")
				.AddTask("MyTask")
				.Condition = "'' != ''";

			var tempFile = Path.GetTempFileName();
			xmlProject.Save(tempFile);

			var buildProject = ProjectCollection.GlobalProjectCollection.LoadProject(tempFile);

			return buildProject.Build("Build", new [] { new ConsoleLogger(verbosity) });
		}

		private TestGenerateTasksFile CreateTask(string taskContent, params string[] references)
		{
			File.WriteAllText(sourceFile, taskContent);

			var task = new TestGenerateTasksFile
			{
				BuildEngine = buildEngine,
				OutputFile = outputFile,
				References = references.Select(x => new TaskItem(x)).ToArray(),
				SourceTasks = new ITaskItem[] { new TaskItem(sourceFile) },
			};

			return task;
		}

		void IDisposable.Dispose()
		{
			if (File.Exists(sourceFile))
				File.Delete(sourceFile);
			if (File.Exists(outputFile))
				File.Delete(outputFile);
		}

		class TestGenerateTasksFile : GenerateTasksFile
		{
			public XElement[] XmlTasks { get; private set; }			

			public override bool Execute()
			{
				var executed = base.Execute();
				if (executed)
					XmlTasks = OutputTasks.Select(x => XElement.Parse(x.GetMetadata("Xml"))).ToArray();

				return executed;
			}
		}
	}
}
