This package provides the DumpItems task, which can be used in 
any MSBuild target, such as:

<Target Name="AfterBuild">
	<!-- Dumps to the console all the Compile items in the current project -->
	<!-- The ItemName is optional, but helpful to know what items you're dumping ;) -->
	<DumpItems Items="@(Compile)" ItemName="Compile" />
</Target>