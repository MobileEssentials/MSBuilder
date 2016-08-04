MSBuilder: InstallVsix
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


Note: $(VSSDKTargetPlatformRegRootSuffix) usually matches 'Exp' in a 
VSIX project itself. You can also use the value 'Exp' directly, or 
no value at all to install to the normal hive.