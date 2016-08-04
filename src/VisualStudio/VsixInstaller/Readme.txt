MSBuilder: VsixInstaller
=========================================

This NuGet package provides the InstallVsix task, which allows 
installing a Visual Studio Extension (VSIX) to arbitrary Visual 
Studio versions and registry hives.

Usage:
<Target Name="InstallMyExtension">
    <InstallVsix VisualStudioVersion="$(VisualStudioVersion)" 
                 VsixPath="MyExtension.vsix"
                 RootSuffix="$(VSSDKTargetPlatformRegRootSuffix)" />
</Target>

<Target Name="UninstallMyExtension">
    <UninstallVsix VisualStudioVersion="$(VisualStudioVersion)" 
                   VsixId="MyExtension"
                   RootSuffix="Exp" />
</Target>

<Target Name="EnableMyExtension">
    <EnableVsix VisualStudioVersion="$(VisualStudioVersion)" 
                VsixId="MyExtension"
	            FailIfNotInstalled="true" />
</Target>

<Target Name="InstallMyExtension">
    <DisableVsix VisualStudioVersion="$(VisualStudioVersion)" 
                 VsixId="MyExtension"
	             FailIfNotInstalled="false"
                 RootSuffix="Exp" />
</Target>


Note: $(VSSDKTargetPlatformRegRootSuffix) usually matches 'Exp' in a 
VSIX project itself unless expliticly overriden by the user. 

You can also use the value 'Exp' directly, or no value at all to install 
to the normal hive.