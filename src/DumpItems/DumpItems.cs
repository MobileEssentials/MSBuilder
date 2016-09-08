using System;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DumpItems
{
	/// <summary>
	/// Dumps items to the output log.
	/// </summary>
    public class DumpItems : Task
	{
		/// <summary>
		/// Items to dump with full metadata.
		/// </summary>
		[Required]
		public Microsoft.Build.Framework.ITaskItem[] Items { get; set; }

		/// <summary>
		/// Optional item name of the dumped items.
		/// </summary>
		public string ItemName { get; set; }

		/// <summary>
		/// Dumps items to the output log.
		/// </summary>
		public override bool Execute()
		{
			var itemName = ItemName ?? "Item";
			if (Items.Length == 0)
				Log.LogMessage(MessageImportance.High, "No {0} items received to dump.", ItemName ?? "");
			else
				Log.LogMessage(MessageImportance.High, "Dumping {0} {1} items.", Items.Length, ItemName ?? "");

			foreach (var item in Items.OrderBy(i => i.ItemSpec))
			{
				Log.LogMessage(MessageImportance.High, "{0}: {1}", itemName, item.ItemSpec);
				foreach (var name in item.MetadataNames.OfType<string>().OrderBy(_ => _))
				{
					try
					{
						Log.LogMessage(MessageImportance.High, "\t{0}={1}", name, item.GetMetadata(name));
					}
					catch
					{
					}
				}
			}

			return true;
		}
	}
}
