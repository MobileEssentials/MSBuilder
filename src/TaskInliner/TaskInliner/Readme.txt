MSBuilder: Inline Code Tasks Generator
======================================

This NuGet package will automatically generate an .Inline.tasks file in your output directory 
containing an inline code task version of your Task-derived classes in the project.

It will also generate a .Compiled.tasks file declaring the tasks in the compiled assembly, 
as well as a .tasks file that conditionally imports both depending on the whether compiled 
tasks are enabled or not, like:

    <Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
      <Import Project="MyTasks.Inline.tasks" Condition="'$(UseCompiledTasks)' == 'false' Or '$(UseCompiledTasks)' == ''" />
      <Import Project="MyTasks.Compiled.tasks" Condition="'$(UseCompiledTasks)' == 'true'" />
    </Project>

This allows this nuget to provide an enhanced experience for Windows/MSBuild/Visual Studio
users without breaking support for Mac/Xbuild/Xamarin Studio. By default, the UseCompiledTasks 
uses the IsXBuild MSBuilder props to default to compiled only on XBuild.

The generated file can be customized by modifying the following extensibility points 
directly via your MSBuild project file:
	
	$(TasksName): this property controls the base file name generated 
	              by the GenerateInlineTasks target after build. 
				  Defaults to $(AssemblyName).

	$(TasksOutputPath): determines the output path of the three files 
                        to be generated. Defaults to $(OutputPath).
	
	@(SourceTask): item group with source files to process for inline 
	               task generation. Defaults to @(Compile)
	

The TaskInliner task is also provided as an inline task, so you can inspect in detail 
how it works by just exploring the targets file and tasks source included in this package.


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
