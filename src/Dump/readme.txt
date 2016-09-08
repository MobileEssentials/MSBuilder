This package provides the DumpItems and DumpProperties targets, which can be 
invoked directly on the project passing properties to customize their behavior:

// Dump all Compile items
msbuild /t:DumpItems /p:ItemType=Compile

// Dump all BuiltProjectOutputGroupOutput items, but run AllProjectOutputGroups before doing so
msbuild /t:AllProjectOutputGroups,DumpItems /p:ItemType=Compile

// Dump all properties
msbuild /t:DumpProperties

// Dump OutputPath property 
msbuild /t:DumpProperty /p:PropertyName=OutputPath


(NOTE from above example that PropertyName is always evaluated as "Contains")