MSBuilder : Reusable MSBuild Building Blocks
===

MSBuilder's goal is to provide fine-grained nuget packages that can be installed when only a certain 
MSBuild extension (task, property, target) is needed. 

This should enable smaller chunks of MSBuild to be reused without concern for bloating your build 
scripts or bring in unnecessary dependencies (if any). 

## Goals

 - No binary tasks assemblies: this makes it easier to upgrade/uninstall without restarting VS, and
   also makes the intended behavior of the tasks fully transparent.
 - No binary dependencies
 - Single-purpose components


## Versioning

Each MSBuilder block has its own folder under `src` and contains its own `.nuspec` file of course. 
The way we determine the version on CI builds is as follows:

1. Read the .nuspec version attribute
2. Determine the commit of the last change to the spec
3. Determime the number of commits that happened since that commit, within the same block folder
4. If that count > 0, or if the last spec change is actually the current HEAD, then a new version
   of the nuget needs to be built.
5. Increment the patch (third component) of the version with the # of commits determined in step 2.

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
> it matches the HEAD), applying the commit count on top of the last change (zero, since this is 
> the actual commit that changes it!) to the patch component may result in a smaller version # than
> the last one.


## Contributing

You can use one of the existing blocks as inspiration, from the simplest CodeTaskAssembly to the 
complicated TaskInliner one (which you can use yourself to create blocks containing inline tasks too!).