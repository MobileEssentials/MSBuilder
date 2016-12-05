MSBuilder: XmlPoke
=========================================

Drop-in placement for the built-in XmlPoke 
task, which preserves XML whitespace and
formatting when saving the modified XML 
document. 

Usage:

<Target Name="BeforeBuild">
    <XmlPoke XmlInputPath="project.nuspec" Query="/package/metadata/version" Value="$(Version)" />
</Target>

Note: the file is updated only if replacements are applied.