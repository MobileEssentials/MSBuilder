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

var File = new TaskItem(@"C:\Code\Xamarin\mobessen\MSBuilder\src\ReadItemsFromFile\ReadItemsFromFile.items");
ITaskItem[] Items;

XNamespace XmlNs = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");
XName ItemGroupElementName = XmlNs + "ItemGroup";
string IncludeAttributeName = "Include";

var document = XDocument.Load(File.GetMetadata("FullPath"));

Func<XElement, ITaskItem> itemFromElement = element =>
{
	var item = new TaskItem(element.Attribute(IncludeAttributeName).Value);
	foreach (var metadata in element.Elements())
		item.SetMetadata(metadata.Name.LocalName, metadata.Value);

	return item;
};

Items = document.Root
	.Elements(ItemGroupElementName)
	.SelectMany(element => element.Elements())
	.Select(element => itemFromElement(element))
	.ToArray();
				
Items.Dump();