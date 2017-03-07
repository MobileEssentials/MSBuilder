MSBuilder: WriteItemsToFile
=========================================

Writes items with their full metadata to 
an MSBuild project "fragment". 

The resulting XML file can be directly imported 
by an MSBuild project, or read on-demand by 
the ReadItemsFromFile MSBuilder.

Usage:

<Target Name="BeforeBuild">
    <WriteItemsToFile File="AdditionalFileItems.items" 
                      Items="@(AdditionalFile)"
                      ItemName="AdditionalFile"
                      IncludeMetadata="true" />
</Target>