MSBuilder: VisualStudioVersions
=========================================

Discovers installed Visual Studio versions, 
their install and VSSDK directories.

The provided GetVisualStudioVersions is set 
to run before the Build target, so in your 
project you can leverage the provided 
@(InstalledVisualStudioVersion) item group 
with the found Visual Studio installations.

Provided item metadata for each installed 
Visual Studio item is:

- DisplayName: i.e. VS2015, VS2017, etc.
- Dev: the short number of the VS version, 
  like 15 instead of 15.0
- InstallDir: the root directory of the VS 
  installation
- IsSdkInstalled: whether the VSSDK was 
  found for the given VS installation
- SdkDir: the install location of the VSSDK 
  if installed. Always present, may be empty.
