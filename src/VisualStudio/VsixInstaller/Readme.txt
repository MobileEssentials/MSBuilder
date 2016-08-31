MSBuilder: VsixInstaller
=========================================

This NuGet package provides the various tasks to handle all operations 
related to Visual Studio extensions via MSBuild, which goes beyond what 
VSIXInstaller.exe supports by allowing not only to work against arbitrary
Visual Studio versions but also experimental and custom instances (also 
known as -registry- hives).

Provided tasks usage:

<Target Name="InstallMyExtension">
    <InstallVsix VisualStudioVersion="$(VisualStudioVersion)" 
                 MessageImportance="high"
                 VsixPath="MyExtension.vsix"
                 RootSuffix="$(VSSDKTargetPlatformRegRootSuffix)" />
</Target>

<Target Name="UninstallMyExtension">
    <UninstallVsix VisualStudioVersion="$(VisualStudioVersion)" 
                   MessageImportance="normal"
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


Convenience targets are also provided that invoke the above tasks, as well 
as the ListVsix one, and can therefore be invoked directly on a project that 
consumes this nuget package. For example:

msbuild MyProject.csproj /t:ListVsix /p:VsixIdFilter=Windows /p:VsixNameFilter=SDK

Would render something like:

  Microsoft Windows Desktop SDK
        Id='Microsoft.Windows.DevelopmentKit.Desktop'
        InstalledByMsi=True
        InstalledPerMachine=True
        State=Enabled
        SystemComponent=True
        Version=8.1.0.0

You can inspect the targets file as well as the inline tasks file to learn more 
about usage and implementation.



