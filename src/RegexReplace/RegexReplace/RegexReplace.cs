using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace MSBuilder
{
	/// <summary>
	/// Applies a regular expression pattern and a replacement 
	/// pattern in-place to the given files.
	/// </summary>
	public class RegexReplace : Task
	{
		/// <summary>
		/// The files to apply the replacement pattern to.
		/// </summary>
		[Required]
		public Microsoft.Build.Framework.ITaskItem[] Files { get; set; }

		/// <summary>
		/// Pipe-separated values from the RegexOptions enum, such as 
		/// "IgnoreCase | Compiled".
		/// </summary>
		public string Options { get; set; }

		/// <summary>
		/// The pattern to match against the Files contents.
		/// </summary>
		[Required]
		public string Pattern { get; set; }

		/// <summary>
		/// The replacement expression to apply to the 
		/// Files contents.
		/// </summary>
		[Required]
		public string Replacement { get; set; }

		/// <summary>
		/// Applies the replacements to the given files.
		/// </summary>
		public override bool Execute ()
		{
			var options = string.IsNullOrEmpty(Options) ? 
				RegexOptions.Compiled : 
				Options.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(value => (RegexOptions)Enum.Parse(typeof(RegexOptions), value))
					.Aggregate(RegexOptions.None, (current, value) => current |= value);
				
			var regex = new Regex (Pattern, options);

			foreach (var item in Files) {
				var content = File.ReadAllText (item.ItemSpec);
				var replaced = regex.Replace (content, Replacement);

				if (content != replaced) {
					Log.LogMessage (MessageImportance.Normal, "Updating contents of {0}.", item.ItemSpec);
					File.WriteAllText (item.ItemSpec, replaced);
					Log.LogMessage (MessageImportance.Low, @"Replaced: 
{0}

With:
{1}", content, replaced);

				}
			}

			return true;
		}
	}
}
