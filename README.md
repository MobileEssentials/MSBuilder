MSBuilder : Reusable MSBuild Building Blocks
===

MSBuilder's goal is to provide fine-grained nuget packages that can be installed when only a certain MSBuild extension (task, property, target) is needed. 

This should enable smaller chunks of MSBuild to be reused without concern for bloating your build scripts or bring in unnecessary dependencies (if any). 

## Goals

 - No binary tasks assemblies: this makes it easier to upgrade/uninstall without restarting VS, and also makes the intended behavior of the tasks fully transparent.
 - No binary dependencies
 - Single-purpose components





