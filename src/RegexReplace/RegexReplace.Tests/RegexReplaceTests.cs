using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MSBuilder
{
    public class RegexReplaceTests
    {
		MockBuildEngine engine;

		public RegexReplaceTests(ITestOutputHelper output)
		{
			engine = new MockBuildEngine(output);
		}

		[Fact]
		public void when_parsing_options_then_can_combine_flags()
		{
			var values = "Singleline | Compiled";

			var options = values.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(value => (RegexOptions)Enum.Parse(typeof(RegexOptions), value))
				.Aggregate(RegexOptions.None, (current, value) => current |= value);

			Assert.Equal(RegexOptions.Singleline | RegexOptions.Compiled, options);
		}

		[Fact]
		public void when_no_replacements_applied_then_does_not_write_file()
		{
			var file = @"..\..\packages.config";
			var timestamp = File.GetLastWriteTimeUtc(file);
			var task = new RegexReplace
			{
				BuildEngine = engine,
				Files = new ITaskItem[] { new TaskItem(file) },
				Pattern = "/* LICENSE */",
				Replacement = "LICENSE"
			};

			var result = task.Execute();

			Assert.True(result);
			Assert.Equal(timestamp, File.GetLastWriteTimeUtc(file));
		}

		[Fact]
		public void when_replacements_applied_then_writes_file_in_place()
		{
			var file = @"Sample.txt";
			var original = File.ReadAllText(file);
			var task = new RegexReplace
			{
				BuildEngine = engine,
				Files = new ITaskItem[] { new TaskItem(file) },
				Pattern = @"/\* LICENSE \*/",
				Replacement = "LICENSE"
			};

			var result = task.Execute();
			var actual = File.ReadAllText(file);

			Assert.NotEqual(original, actual);
			Assert.True(actual.StartsWith("LICENSE"));
		}
	}
}
