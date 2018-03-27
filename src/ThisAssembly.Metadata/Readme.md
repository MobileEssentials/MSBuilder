# MSBuilder.ThisAssembly.Metadata

This package extends MSBuilder.GenerateAssemblyInfo to also generate a static 
`ThisAssembly.Metadata` class with the `@(AssemblyAttribute)` attributes that 
have `Include="System.Reflection.AssemblyMetadataAttribute"`. 

So for an attribute like:

```csharp
[assembly: System.Reflection.AssemblyMetadataAttribute("Foo", "Bar")]
```

You get a corresponding `ThisAssembly.Metadata.Foo` constant with the value `Bar`.


Example:

```xml
   <ItemGroup>
     <AssemblyAttribute Include="System.Reflection.AssemblyMetadataAttribute">
       <_Parameter1>Foo</_Parameter1>
       <_Parameter2>Bar</_Parameter2>
     </AssemblyAttribute>
   </ItemGroup>
```

Generates (C#):

```csharp
partial class ThisAssembly
{
    public static partial class Metadata
    {
        public const string Foo = "Bar";
    }
}
```

or (VB):

```vb
Namespace Global
  Partial Class ThisAssembly
        Partial Class Metadata
            Public Const Foo = "Bar"
        End Class
    End Class
End Namespace
```


NOTE: In SDK-style projects, items declared this way in the project file itself do not show up 
in the solution explorer. In classic projects, they do. In order to hide them from the 
solution explorer, the item group must be moved to a targets file imported from the main 
project file, like so:

```xml
    <Import Project="$(MSBuildProjectName).targets" />
```

Then create a .targets file alongside the main project, containing the items.

KNOWN ISSUES:
Also, note that by default, after installing this package in a classic project, you will 
typically get build errors because of duplicate assembly level attributes, which are now generated 
by default as specified by the following property defaults (imported from the dependency 
on https://www.nuget.org/packages/MSBuilder.GenerateAssemblyInfo):

```xml
  <PropertyGroup Condition="'$(GenerateAssemblyInfo)' == 'true'">
    <GenerateAssemblyCompanyAttribute Condition="'$(GenerateAssemblyCompanyAttribute)' == ''">true</GenerateAssemblyCompanyAttribute>
    <GenerateAssemblyConfigurationAttribute Condition="'$(GenerateAssemblyConfigurationAttribute)' == ''">true</GenerateAssemblyConfigurationAttribute>
    <GenerateAssemblyCopyrightAttribute Condition="'$(GenerateAssemblyCopyrightAttribute)' == ''">true</GenerateAssemblyCopyrightAttribute>
    <GenerateAssemblyDescriptionAttribute Condition="'$(GenerateAssemblyDescriptionAttribute)' == ''">true</GenerateAssemblyDescriptionAttribute>
    <GenerateAssemblyFileVersionAttribute Condition="'$(GenerateAssemblyFileVersionAttribute)' == ''">true</GenerateAssemblyFileVersionAttribute>
    <GenerateAssemblyInformationalVersionAttribute Condition="'$(GenerateAssemblyInformationalVersionAttribute)' == ''">true</GenerateAssemblyInformationalVersionAttribute>
    <GenerateAssemblyProductAttribute Condition="'$(GenerateAssemblyProductAttribute)' == ''">true</GenerateAssemblyProductAttribute>
    <GenerateAssemblyTitleAttribute Condition="'$(GenerateAssemblyTitleAttribute)' == ''">true</GenerateAssemblyTitleAttribute>
    <GenerateAssemblyVersionAttribute Condition="'$(GenerateAssemblyVersionAttribute)' == ''">true</GenerateAssemblyVersionAttribute>
    <GenerateNeutralResourcesLanguageAttribute Condition="'$(GenerateNeutralResourcesLanguageAttribute)' == ''">true</GenerateNeutralResourcesLanguageAttribute>
  </PropertyGroup>
```

You can turn off `GenerateAssemblyInfo` entirely still get the `ThisAssembly.Metadata`, but 
you won't get any assembly-level attributes in that case, which might be undesirable.

This is the mapping from assembly attributes to MSBuild properties used by the GenerateAssemblyInfo 
target:

  * AssemblyCompanyAttribute: $(Company)
  * AssemblyConfigurationAttribute: $(Configuration)
  * AssemblyCopyrightAttribute: $(Copyright)
  * AssemblyDescriptionAttribute: $(Description)
  * AssemblyFileVersionAttribute: $(FileVersion)
  * AssemblyInformationalVersionAttribute: $(InformationalVersion)
  * AssemblyProductAttribute: $(Product)
  * AssemblyTitleAttribute: $(AssemblyTitle)
  * AssemblyVersionAttribute: $(AssemblyVersion)
  * NeutralResourcesLanguageAttribute: $(NeutralLanguage)

You can typically just remove the legacy `AssemblyInfo.cs` entirely from your project.

See https://www.nuget.org/packages/MSBuilder.GenerateAssemblyInfo for more information.
