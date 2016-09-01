MSBuilder: VsixDependency
=========================================

Allows <VsixDependency> items to be injected 
into the referencing project's VSIX manifest 
and automatically injects their %(VsixPath) 
payloads into the resulting VSIX.

Also automatically deploys the dependent VSIX 
to the VS experimental instance for seamless 
F5 experience.

Usage:

<ItemGroup>
	<VsixDependency Include="..\Clide\extension.vsixmanifest">
		<VsixPath>..\Clide\Clide.vsis</VsixPath>
	</VsixDependency>
</ItemGroup>