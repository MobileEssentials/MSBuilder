MSBuilder: ReadItemFromFile
=========================================

Reads items and their metadata from an MSBuild project 
that contains an ItemGroup. For use together with the 
WriteItemsToFile task.


Usage:

<Target Name="BeforeBuild">
	<ReadItemsFromFile File="AdditionalItems.items">
		<Output TaskParameter="Items" ItemName="AdditionalItem" />
	</ReadItemsFromFile>
</Target>