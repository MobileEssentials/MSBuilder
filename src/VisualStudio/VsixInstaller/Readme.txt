MSBuilder: VsixInstaller
=========================================

Provides automated installation of VSIXes to the VSSDK-supported target 
Visual Studio version and instance (a.k.a hives).

On a project that references the Microsoft.VSSDK.BuildTools 
package, enables installing extra VSIXes to the targetted Visual Studio 
version and instance by simply declaring them as @(Vsix) items:

<ItemGroup>
  <Vsix Include="MyOtherExtension.vsix" />
</ItemGroup>

The items will be installed only if needed so that incremental 
builds are fast, and only if the current project's $(DeployExtension) 
is set to 'true'.

The installation happens automatically before the DeployVsixExtensionFiles 
runs, but can also be manually invoked by running the InstallVsix target.

Note: $(VSSDKTargetPlatformRegRootSuffix) usually matches 'Exp' in a 
VSIX project itself unless expliticly overriden by the user. A shorthand 
$(RootSuffix) property is also supported to set the VSSDK property.