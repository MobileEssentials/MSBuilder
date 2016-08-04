MSBuilder: VsixInstaller
=========================================

This NuGet package provides the various tasks to handle all operations 
related to Visual Studio extensions via MSBuild, which goes beyond what 
VSIXInstaller.exe supports by allowing not only to work against arbitrary
Visual Studio versions but also experimental and custom instances (also 
known as -registry- hives).

Usage:
<Target Name="InstallMyExtension">
    <InstallVsix VisualStudioVersion="$(VisualStudioVersion)" 
                 MessageImportance="high"
                 VsixPath="MyExtension.vsix"
                 RootSuffix="$(VSSDKTargetPlatformRegRootSuffix)" />
</Target>

<Target Name="UninstallMyExtension">
    <UninstallVsix VisualStudioVersion="$(VisualStudioVersion)" 
                   MessageImportance="high"
                   VsixId="MyExtension"
                   RootSuffix="Exp" />
</Target>

<Target Name="EnableMyExtension">
    <EnableVsix VisualStudioVersion="$(VisualStudioVersion)" 
                MessageImportance="normal"
                VsixId="MyExtension"
	            FailIfNotInstalled="true" />
</Target>

<Target Name="InstallMyExtension">
    <DisableVsix VisualStudioVersion="$(VisualStudioVersion)" 
                 MessageImportance="low"
                 VsixId="MyExtension"
	             FailIfNotInstalled="false"
                 RootSuffix="Exp" />
</Target>


Note: $(VSSDKTargetPlatformRegRootSuffix) usually matches 'Exp' in a 
VSIX project itself unless expliticly overriden by the user. 

You can also use the value 'Exp' directly, or no value at all to install 
to the normal hive.