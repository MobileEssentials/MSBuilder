using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Construction;
using System.Xml;

namespace MSBuilder.TaskInliner
{
	/// <summary>
	/// Generates an MSBuild file containing an inline task version of the 
	/// specified compiled source tasks, as well as a base tasks file that 
	/// imports the inline version or compiled version depending on the 
	/// current operating system (inline for Windows/MSBuild, compiled 
	/// for Mono/Xbuild).
	/// </summary>
	public class GenerateTasksFile : Task
	{
		/// <summary>
		/// Base name of the tasks assembly and file to generate, without 
		/// a file extension.
		/// </summary>
		[Required]
		public string TasksName { get; set; }

		/// <summary>
		/// The output path where the generated files should be written to.
		/// </summary>
		[Required]
		public string OutputPath { get; set; }

		/// <summary>
		/// Assembly references required to compile the source tasks.
		/// </summary>
		[Required]
		public Microsoft.Build.Framework.ITaskItem[] References { get; set; }

		/// <summary>
		/// Source files for the tasks to embed as inline tasks.
		/// </summary>
		[Required]
		public Microsoft.Build.Framework.ITaskItem[] SourceTasks { get; set; }

		/// <summary>
		/// Optional license text to wrap in an XML comment at the top 
		/// of the generated files.
		/// </summary>
		public string License { get; set; }

		/// <summary>
		/// The generated tasks file importing both inline and compiled versions 
		/// of the tasks. This file is the one imported by targets that need to 
		/// use the resulting inline or compiled tasks.
		/// </summary>
		[Output]
		public string TasksFile { get; set; }

		/// <summary>
		/// The generated file containing the inline tasks.
		/// </summary>
		[Output]
		public string InlineFile { get; set; }

		/// <summary>
		/// The generated file containing a reference to the compiled tasks.
		/// </summary>
		[Output]
		public string CompiledFile { get; set; }

		/// <summary>
		/// An augmented version of <see cref="SourceTasks">SourceTasks</see> containing the 
		/// <c>Xml</c> metadata with the generated fragment for each source 
		/// task.
		/// </summary>
		[Output]
		public Microsoft.Build.Framework.ITaskItem[] OutputTasks { get; set; }

		/// <summary>
		/// Generates the inline tasks output file.
		/// </summary>
		public override bool Execute()
		{
			var xmlns = "{http://schemas.microsoft.com/developer/msbuild/2003}";
			var usingsExpr = new Regex(@"using (?<using>[\w\.]+);");
			var namespaceExpr = new Regex(@"namespace (?<ns>[\w\.]+)");
			var taskNameExpr = new Regex(@"public class (?<name>[\w]+) : Task");
			var propertyExpr = new Regex(@"(?<required>\[Required\].*?)?(?<output>\[Output\].*?)?public (?<type>[\w\.\[\]]+) (?<name>[\w]+) { get; set; }", RegexOptions.Singleline);
			var codeExpr = new Regex(@"public override bool Execute.+?{(?<code>.*)return true;.+}", RegexOptions.Singleline);
			var classSummaryExpr = new Regex(@"(?<summary>\t*\b*/// .*?)\s+public class", RegexOptions.Singleline);
			var propSummaryExpr = new Regex(@"(?<summary>t*\b*/// [^{]*?)\s+" + propertyExpr.ToString(), RegexOptions.Singleline);

			var project = ProjectRootElement.Create();
			project.ToolsVersion = "4.0";
			var properties = project.AddPropertyGroup();

			properties.AddProperty("CodeTaskAssembly", "$" + @"(MSBuildBinPath)\Microsoft.Build.Tasks.v4.0.dll")
				.Condition = "'$" + "(CodeTaskAssembly)' == '' And '$" + "(MSBuildAssemblyVersion)' == '' And Exists('$" + @"(MSBuildBinPath)\Microsoft.Build.Tasks.v4.0.dll')";
            properties.AddProperty("CodeTaskAssembly", "$" + @"(MSBuildFrameworkToolsPath)\Microsoft.Build.Tasks.v4.0.dll")
                .Condition = "'$" + "(CodeTaskAssembly)' == '' And '$" + "(MSBuildAssemblyVersion)' == '' And Exists('$" + @"(MSBuildFrameworkToolsPath)\Microsoft.Build.Tasks.v4.0.dll')";
            properties.AddProperty("CodeTaskAssembly", "$" + @"(MSBuildBinPath)\Microsoft.Build.Tasks.v12.0.dll")
				.Condition = "'$" + "(CodeTaskAssembly)' == '' And '$" + "(MSBuildAssemblyVersion)' == '' And Exists('$" + @"(MSBuildBinPath)\Microsoft.Build.Tasks.v12.0.dll')";
			properties.AddProperty("CodeTaskAssembly", "$" + @"(MSBuildToolsPath)\Microsoft.Build.Tasks.v12.0.dll")
				.Condition = "'$" + "(CodeTaskAssembly)' == '' And '$" + "(MSBuildAssemblyVersion)' == '12.0'";
			properties.AddProperty("CodeTaskAssembly", "$" + @"(MSBuildToolsPath)\Microsoft.Build.Tasks.Core.dll")
				.Condition = "'$" + "(CodeTaskAssembly)' == '' And '$" + "(MSBuildVersion)' != '' and '$" + "(MSBuildVersion)' >= '14.0'";

			var projectXml = XDocument.Parse(project.RawXml);
			var tasks = new List<ITaskItem>();

			projectXml.Root.AddFirst(new XComment(@" Typically provided by MSBuilder.CodeTaskAssembly already. "));

			// Helper function to get clean strings from XML doc comments.
			Func<string, string, string> indentedSummary = (indent, summary) =>
			{
				if (summary.Trim().Length == 0)
					return indent;

				var xml = string.Join(Environment.NewLine,
					summary.Trim().Split(new[] { Environment.NewLine }, StringSplitOptions.None)
						.Select(line => line.Trim().Substring(3).TrimStart()));

				var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment });
				var doc = new XDocument(new XElement("doc"));
				using (var writer = doc.Root.CreateWriter())
				{
					writer.WriteNode(reader, false);
				}

				return string.Join(Environment.NewLine,
					doc.Root.Value.Trim()
						.Split(new[] { Environment.NewLine, '\n'.ToString() }, StringSplitOptions.None)
						.Select(line => indent + line.Trim()));
			};

			var taskFullNames = new List<string>(SourceTasks.Length);

			foreach (var task in SourceTasks)
			{
				var content = File.ReadAllText(task.GetMetadata("FullPath"));
				var taskNameMatch = taskNameExpr.Match(content);
				if (!taskNameMatch.Success)
				{
					Log.LogWarning("Task source '{0}' does not contain a class declaration of the form: public class [TypeName] : Task", task.ItemSpec);
					continue;
				}

				var taskName = taskNameMatch.Groups["name"].Value;
				var taskNs = namespaceExpr.Match(content).Groups["ns"].Value;
				if (taskNs.Length > 0)
					taskFullNames.Add(taskNs + "." + taskName);
				else
					taskFullNames.Add(taskName);

				var usings = usingsExpr.Matches(content).Cast<Match>().Select(x => x.Groups["using"].Value)
					.OrderBy(x => x).ToArray();

				var taskSummary = indentedSummary("    ", classSummaryExpr.Match(content).Groups["summary"].Value) + @"

    Properties:";
				foreach (var propSummary in propSummaryExpr.Matches(content).Cast<Match>())
				{
					taskSummary += Environment.NewLine +
						"    - " + propSummary.Groups["name"].Value + ": " +
						propSummary.Groups["type"].Value + " (" +
						(propSummary.Groups["output"].Success ? "Output" : "Input") +
						(propSummary.Groups["required"].Success ? ", Required" : "") + ")" +
						Environment.NewLine +
						indentedSummary("        ", propSummary.Groups["summary"].Value) +
						Environment.NewLine;

				}

				var summaryXml = new XComment(string.Format(@"
    ============================================================
              {0} Task
	
{1}
	============================================================
  ", taskName, taskSummary));

				var taskXml = new XElement(xmlns + "UsingTask",
					new XAttribute("TaskName", taskName),
					new XAttribute("TaskFactory", "CodeTaskFactory"),
					new XAttribute("AssemblyFile", "$" + "(CodeTaskAssembly)"));

				var paramGroup = new XElement(xmlns + "ParameterGroup");
				taskXml.Add(paramGroup);

				foreach (var property in propertyExpr.Matches(content).Cast<Match>())
				{
					var propXml = new XElement(xmlns + property.Groups["name"].Value);
					// We only explicitly add parameter type when it's not the default value of "string"
					if (property.Groups["type"].Value != "string")
					{
						// Cover commonly used C# aliases
						var paramType = property.Groups["type"].Value;
						if (paramType == "bool")
							paramType = "System.Boolean";
						else if (paramType == "int")
							paramType = "System.Int32";
						else if (paramType == "long")
							paramType = "System.Int64";

						propXml.Add(new XAttribute("ParameterType", paramType));
					}

					// A property cannot be simultaneously a required input and an output.
					if (property.Groups["required"].Success)
						propXml.Add(new XAttribute("Required", "true"));
					else if (property.Groups["output"].Success)
						propXml.Add(new XAttribute("Output", true));

					paramGroup.Add(propXml);
				}

				var references = References
					.Select(x => x.ItemSpec)
					// Skip references that are already built-in, so that the 
					// runtime compilation takes precedence as to which version 
					// of these assemblies to use for compiling the task.
					.Where(x => !x.StartsWith("Microsoft.Build."))
					.OrderBy(x => x);

				var taskNode = new XElement(xmlns + "Task", references
					.Select(x => new XElement(xmlns + "Reference",
						new XAttribute("Include", x)))
					.Concat(
						usings.Select(x =>
							new XElement(xmlns + "Using",
								new XAttribute("Namespace", x)))
					));

				taskXml.Add(taskNode);

				taskNode.Add(new XElement(xmlns + "Code",
					new XAttribute("Type", "Fragment"),
					new XAttribute("Language", "cs"),
					new XCData(codeExpr.Match(content).Groups["code"].Value.TrimEnd() + Environment.NewLine + "      ")));

				// Just in case some other code needs to inspect the XML we're generating for 
				// each fragment, we emit these as output items.
				var taskItem = new TaskItem(task);
				taskItem.SetMetadata("Xml", taskXml.ToString());
				tasks.Add(taskItem);

				// We add in reverse order so that the PropertyGroup is at the bottom, 
				// avoiding clutter somewhat at the top of the file.
				projectXml.Root.AddFirst(taskXml);
				projectXml.Root.AddFirst(summaryXml);
			}

			OutputTasks = tasks.ToArray();

			if (!string.IsNullOrEmpty(License))
				projectXml.AddFirst(new XComment(License));

			TasksFile = Path.Combine(OutputPath, TasksName + ".tasks");
			InlineFile = Path.Combine(OutputPath, TasksName + ".Inline.tasks");
			CompiledFile = Path.Combine(OutputPath, TasksName + ".Compiled.tasks");

			projectXml.Save(InlineFile);

			// Generate .Compiled.tasks file defining the Using task using the compiled assembly.
			projectXml.Root.RemoveNodes();

			var tasksAssembly = TasksName + ".dll";
			foreach (var task in taskFullNames)
			{
				projectXml.Root.Add(new XElement(xmlns + "UsingTask",
					new XAttribute("TaskName", task),
					new XAttribute("AssemblyFile", tasksAssembly)));
			}

			projectXml.Save(CompiledFile);

			// Now generate .tasks file importing both inline and compiled tasks.
			projectXml.Root.RemoveNodes();
            projectXml.Root.Add(new XElement(xmlns + "Import",
                new XAttribute("Project", TasksName + ".Compiled.tasks"),
                new XAttribute("Condition", "'$" + "(UseCompiledTasks)' == 'true' Or '$" + "(CodeTaskAssembly)' == ''")));

            projectXml.Root.Add(new XElement(xmlns + "Import",
				new XAttribute("Project", TasksName + ".Inline.tasks"),
				new XAttribute("Condition", "'$" + "(UseCompiledTasks)' != 'true' And '$" + "(CodeTaskAssembly)' != ''")));

            projectXml.Save(TasksFile);

			return true;
		}
	}
}
