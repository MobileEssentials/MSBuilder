<Query Kind="Statements">
  <NuGetReference>Microsoft.Build</NuGetReference>
  <NuGetReference>Microsoft.Build.Framework</NuGetReference>
  <NuGetReference>Microsoft.Build.Tasks.Core</NuGetReference>
  <NuGetReference>Microsoft.Build.Utilities.Core</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>System.Net.Http</NuGetReference>
  <Namespace>Microsoft.Build</Namespace>
  <Namespace>Microsoft.Build.Framework</Namespace>
  <Namespace>Microsoft.Build.Utilities</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>System</Namespace>
  <Namespace>System.Linq</Namespace>
  <Namespace>System.Net.Http</Namespace>
</Query>

var ItemName = "None";
var Items = new ITaskItem[] {
	new TaskItem(@"C:\Code\Xamarin\mobessen\MSBuilder\src\WriteItemsToFile\Readme.txt")
};

Items[0].SetMetadata("CopyToOutputDirectory", "PreserveNewest");

bool? IncludeMetadata = true;
var File = new TaskItem(@"C:\Code\Xamarin\mobessen\MSBuilder\src\WriteItemsToFile\WriteItemsToFile.items");


XNamespace XmlNs = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");
XName ProjectElementName = XmlNs + "Project";
XName ItemGroupElementName = XmlNs + "ItemGroup";
string IncludeAttributeName = "Include";

var itemName = ItemName ?? "Item";
var includeMetadata = IncludeMetadata ?? true;

var items = Items;
if (items == null)
	items = new ITaskItem[0];

Func<ITaskItem, IEnumerable<XElement>> metadataFromItem;
if (includeMetadata)
	metadataFromItem = item => item.CloneCustomMetadata()
		.OfType<KeyValuePair<string, string>>()
		.Select(entry => new XElement(XmlNs + entry.Key, entry.Value));
else
	metadataFromItem = item => Enumerable.Empty<XElement>();

Func<ITaskItem, XElement> itemFromElement = item => new XElement(XmlNs + ItemName,
	new XAttribute(IncludeAttributeName, item.ItemSpec), metadataFromItem(item));

var document = new XDocument(
	new XElement(ProjectElementName,
		new XElement(ItemGroupElementName,
			items.Select(item => itemFromElement(item)))));

var filePath = File.GetMetadata("FullPath");
if (System.IO.File.Exists(filePath))
	System.IO.File.Delete(filePath);

if (!Directory.Exists(Path.GetDirectoryName(filePath)))
	Directory.CreateDirectory(Path.GetDirectoryName(filePath));

document.Save(filePath);

document.Dump();