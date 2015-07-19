MSBuilder : Reusable Build Blocks
===

MSBuilder's goal is to provide fine-grained nuget packages that can be installed when only a certain 
MSBuild extension (task, property, target) is needed. 

This should enable smaller chunks of MSBuild to be reused without concern for bloating your build 
scripts or bring in unnecessary dependencies (if any). 

## Goals

 - No binary tasks assemblies: this makes it easier to upgrade/uninstall without restarting VS, and
   also makes the intended behavior of the tasks fully transparent, "view-source" style.
 - No binary dependencies
 - Single-purpose components


## Versioning

Each MSBuilder block has its own folder under `src` and contains its own `.nuspec` file of course. 
On each push, AppVeyor CI will automatically build and push a new version of the MSBuilder block 
as needed. The way we determine the version on CI builds is as follows:

1. Read the .nuspec version attribute
2. Determine the commit of the last change to the .nuspec file (typically, when its version was 
   updated)
3. Determime the number of commits that happened since that commit, within the same block folder
4. If that count > 0, or if the last spec change is actually the current HEAD, then a new version
   of the nuget needs to be built.
5. Increment the patch (third component) of the version with the # of commits determined in step 3.
6. Grab the lastest published version of the corresponding nuget package, and if the verion 
   determined in step 5. is greater than than, a new version is published.

This ensures that by default, whenever changes are made to any artifacts related to the block, a 
new version # will automatically be calculated, the corresponding nuget package will be built by 
the CI server, and it will be automatically published to nuget.org. 

Pull requests with fixes and improvements can therefore trivially accepted and a new build will 
show up in nuget.org right-away, making for a truly interactive and almost immediate process for 
contributors. 

> DEV NOTE: If changes to the manifest itself are done, make sure to make the version attribute equal
> to the last successful build (or published nuget) +1 in the patch component, so that the next 
> build can successfully build and publish it. 
> Otherwise, even if the build will be triggered (since the condition on step 4. will detect
> it matches the HEAD), applying the commit count on top of the lastest change (zero, since this is 
> the actual commit that changes it!) to the patch component may result in a smaller version # than
> the lastest published nuget package for the block. This causes a build error with a clear message, 
> so it's easy to catch, though. Just make sure you build locally at least once to ensure everything 
> is OK.


## Contributing

You can use one of the existing blocks as inspiration, from the simplest CodeTaskAssembly to the 
complicated TaskInliner one (which you can use yourself to create blocks containing inline tasks too!).


[![Build status](https://img.shields.io/appveyor/ci/MobileEssentials/MSBuilder.svg)](https://ci.appveyor.com/project/MobileEssentials/msbuilder)

Package | Stats
--- | ---
MSBuilder.CodeTaskAssembly | [![NuGet downloads](https://img.shields.io/nuget/dt/MSBuilder.CodeTaskAssembly.svg)](https://www.nuget.org/packages/MSBuilder.CodeTaskAssembly) [![Version](https://img.shields.io/nuget/v/MSBuilder.CodeTaskAssembly.svg)](https://www.nuget.org/packages/MSBuilder.CodeTaskAssembly)
MSBuilder.DownloadFile | [![NuGet downloads](https://img.shields.io/nuget/dt/MSBuilder.DownloadFile.svg)](https://www.nuget.org/packages/MSBuilder.DownloadFile) [![Version](https://img.shields.io/nuget/v/MSBuilder.DownloadFile.svg)](https://www.nuget.org/packages/MSBuilder.DownloadFile)
MSBuilder.DumpItems | [![NuGet downloads](https://img.shields.io/nuget/dt/MSBuilder.DumpItems.svg)](https://www.nuget.org/packages/MSBuilder.DumpItems) [![Version](https://img.shields.io/nuget/v/MSBuilder.DumpItems.svg)](https://www.nuget.org/packages/MSBuilder.DumpItems)
MSBuilder.Git | [![NuGet downloads](https://img.shields.io/nuget/dt/MSBuilder.Git.svg)](https://www.nuget.org/packages/MSBuilder.Git) [![Version](https://img.shields.io/nuget/v/MSBuilder.Git.svg)](https://www.nuget.org/packages/MSBuilder.Git)
MSBuilder.Introspect | [![NuGet downloads](https://img.shields.io/nuget/dt/MSBuilder.Introspect.svg)](https://www.nuget.org/packages/MSBuilder.Introspect) [![Version](https://img.shields.io/nuget/v/MSBuilder.Introspect.svg)](https://www.nuget.org/packages/MSBuilder.Introspect)
MSBuilder.IsXBuild | [![NuGet downloads](https://img.shields.io/nuget/dt/MSBuilder.IsXBuild.svg)](https://www.nuget.org/packages/MSBuilder.IsXBuild) [![Version](https://img.shields.io/nuget/v/MSBuilder.IsXBuild.svg)](https://www.nuget.org/packages/MSBuilder.IsXBuild)
MSBuilder.NuGet.GetLatestVersion | [![NuGet downloads](https://img.shields.io/nuget/dt/MSBuilder.NuGet.GetLatestVersion.svg)](https://www.nuget.org/packages/MSBuilder.NuGet.GetLatestVersion) [![Version](https://img.shields.io/nuget/v/MSBuilder.NuGet.GetLatestVersion.svg)](https://www.nuget.org/packages/MSBuilder.NuGet.GetLatestVersion)
MSBuilder.RegexReplace | [![NuGet downloads](https://img.shields.io/nuget/dt/MSBuilder.RegexReplace.svg)](https://www.nuget.org/packages/MSBuilder.RegexReplace) [![Version](https://img.shields.io/nuget/v/MSBuilder.RegexReplace.svg)](https://www.nuget.org/packages/MSBuilder.RegexReplace)
MSBuilder.Run | [![NuGet downloads](https://img.shields.io/nuget/dt/MSBuilder.Run.svg)](https://www.nuget.org/packages/MSBuilder.Run) [![Version](https://img.shields.io/nuget/v/MSBuilder.Run.svg)](https://www.nuget.org/packages/MSBuilder.Run)
MSBuilder.TaskInliner | [![NuGet downloads](https://img.shields.io/nuget/dt/MSBuilder.TaskInliner.svg)](https://www.nuget.org/packages/MSBuilder.TaskInliner) [![Version](https://img.shields.io/nuget/v/MSBuilder.TaskInliner.svg)](https://www.nuget.org/packages/MSBuilder.TaskInliner)
MSBuilder.ThisAssembly.Project | [![NuGet downloads](https://img.shields.io/nuget/dt/MSBuilder.ThisAssembly.Project.svg)](https://www.nuget.org/packages/MSBuilder.ThisAssembly.Project) [![Version](https://img.shields.io/nuget/v/MSBuilder.ThisAssembly.Project.svg)](https://www.nuget.org/packages/MSBuilder.ThisAssembly.Project)


