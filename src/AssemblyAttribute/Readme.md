# MSBuilder.AssemblyAttribute

This package brings compatibility with the `<AssemblyAttribute ...>` items from SDK-style 
projects to classic ones.

You can declare items as follows to get them generated as assembly-level metadata:

```xml
  <ItemGroup>
    <AssemblyAttribute Include="System.Reflection.AssemblyMetadataAttribute">
      <_Parameter1>Foo</_Parameter1>
      <_Parameter2>Bar</_Parameter2>
    </AssemblyAttribute>
  </ItemGroup>
```

The `Include` value should contain the full type name of the assembly attribute, and the
inner `_Parameter` are the positional constructor arguments for the attribute type. 

In SDK-style projects, items declared this way in the project file itself do not show up 
in the solution explorer. In classic projects, they do. In order to hide them from the 
solution explorer, the item group must be moved to a targets file imported from the main 
project file, like so:

```xml
    <Import Project="$(MSBuildProjectName).targets" />
```

Then create a .targets file alongside the main project, containing the items.