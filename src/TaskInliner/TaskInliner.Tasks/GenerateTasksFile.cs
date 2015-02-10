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

namespace TaskInliner.Tasks
{
	public class GenerateTasksFile : Task
	{
		[Required]
		public string OutputFile { get; set; }

		[Required]
		public ITaskItem[] References { get; set; }

		[Required]
		public ITaskItem[] SourceTasks { get; set; }

		[Output]
		public ITaskItem[] OutputTasks { get; set; }

		public override bool Execute()
		{
			var xmlns = "{http://schemas.microsoft.com/developer/msbuild/2003}";
			var usingsExpr = new Regex(@"using (?<using>[^;\s]+);");
			var taskNameExpr = new Regex(@"public class (?<name>[^\s]+)");
       	    var propertyExpr = new Regex(@"(?<required>\[Required\].*?)?(?<output>\[Output\].*?)?public (?<type>[^\s]+) (?<name>[^\s]+) { get; set; }", RegexOptions.Singleline);
			var codeExpr = new Regex(@"public override bool Execute.+{(?<code>.*)return true;.+}", RegexOptions.Singleline);

			var project = ProjectRootElement.Create();
			var properties = project.AddPropertyGroup();

			properties.AddProperty("CodeTaskAssembly", @"$(MSBuildBinPath)\Microsoft.Build.Tasks.v4.0.dll")
				.Condition = "'$(MSBuildAssemblyVersion)' == ''";
			properties.AddProperty("CodeTaskAssembly", @"$(MSBuildToolsPath)\Microsoft.Build.Tasks.v12.0.dll")
				.Condition = "'$(MSBuildAssemblyVersion)' == '12.0'";
			properties.AddProperty("CodeTaskAssembly", @"$(MSBuildToolsPath)\Microsoft.Build.Tasks.Core.dll")
				.Condition = "'$(MSBuildAssemblyVersion)' != '' and '$(MSBuildAssemblyVersion)' >= '14.0'";

			var projectXml = XDocument.Parse(project.RawXml);
			var tasks = new List<ITaskItem>();

			foreach (var task in SourceTasks)
			{
				var content = File.ReadAllText(task.GetMetadata("FullPath"));
				var taskNameMatch = taskNameExpr.Match(content);
				if (!taskNameMatch.Success)
				{
					Log.LogError("Task source '{0}' does not contain a class declaration of the form: public class [TypeName] : Task", task.ItemSpec);
					return false;
				}

				var taskName = taskNameMatch.Groups["name"].Value;
				var usings = usingsExpr.Matches(content).Cast<Match>().Select(x => x.Groups["using"].Value).ToArray();

				var taskXml = new XElement(xmlns + "UsingTask",
					new XAttribute("TaskName", taskName),
					new XAttribute("TaskFactory", "CodeTaskFactory"),
					new XAttribute("AssemblyFile", "$(CodeTaskAssembly)"));

				var paramGroup = new XElement(xmlns + "ParameterGroup");
				taskXml.Add(paramGroup);

				foreach (var property in propertyExpr.Matches(content).Cast<Match>())
				{
					var propXml = new XElement(xmlns + property.Groups["name"].Value);
					// We only explicitly add parameter type when it's not the default value of "string"
					if (property.Groups["type"].Value != "string")
						propXml.Add(new XAttribute("ParameterType", property.Groups["type"].Value));

					if (property.Groups["required"].Success)
						propXml.Add(new XAttribute("Required", "true"));
					else if (property.Groups["output"].Success)
						propXml.Add(new XAttribute("Output", true));

					paramGroup.Add(propXml);
				}

				var taskNode = new XElement(xmlns + "Task",
					References.Select(x =>
						new XElement(xmlns + "Reference",
							new XAttribute("Include", x.ItemSpec)))
					.Concat(
						usings.Select(x => 
							new XElement(xmlns + "Using", 
								new XAttribute("Namespace", x)))
					));

				taskXml.Add(taskNode);

				taskNode.Add(new XElement(xmlns + "Code",
					new XAttribute("Type", "Fragment"),
					new XAttribute("Language", "cs"),
					new XCData(codeExpr.Match(content).Groups["code"].Value.TrimEnd() + Environment.NewLine)));

				var taskItem = new TaskItem(task);
				taskItem.SetMetadata("Xml", taskXml.ToString());
				tasks.Add(taskItem);

				projectXml.Root.Add(taskXml);
			}

			OutputTasks = tasks.ToArray();
			projectXml.Save(OutputFile);

			return true;
		}
	}
}
