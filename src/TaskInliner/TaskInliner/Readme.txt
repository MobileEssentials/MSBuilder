MSBuilder: Inline Code Tasks Generator
======================================

This NuGet package will automatically generate a .tasks file in your output directory 
containing an inline code task version of your Task-derived classes in the project.

The generated file can be customized by modifying the following extensibility points 
directly via your MSBuild project file:
	
	$(TasksFile): this property controls the output file name generated 
	              by the GenerateInlineTasks target after build. 
				  Defaults to $(OutputPath)$(AssemblyName).tasks
	
	@(SourceTask): item group with source files to process for inline 
	               task generation. Defaults to @(Compile)


Limitations
=====================================

Inline code tasks must satisfy the requirement that any state outside of the 
Execute method override must be kept as properties, and no additional helper
methods can be defined in the task class, or in any other class anywhere in 
the project. 

The code in the inline task has to be fully self-contained, and can only 
invoke other libraries that are properly referenced by the project. Almost 
by definition, inline tasks shouldn't be overly complicated, so that there's 
actual value in being able to inspect its source directly via the MSBuild 
file. 

One advanced technique for creating reusable functions that are still 
within the limitations of inline code tasks is to create an anonymous function
in the Execute override and invoke it as needed. Such a technique was used 
in the MSBuilder.TaskInliner.tasks file itself (which is itself generated 
by itself, how cool is that? ;)).
