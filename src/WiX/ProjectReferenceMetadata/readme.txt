Augments the current project with an item named after each project reference, containing the metadata 
that would be added as project define constants for WiX. i.e. for a project reference PclProfiles, 
WiX will add $(var.PclProfiles.TargetDir), which this target exposes to MSBuild as %(PclProfiles.TargetDir).

The conversion from a property to an item (group of 1) is necessary because properties cannot contain 
dots, and the item notation for accessing it looks much better than (say) underscores on properties.