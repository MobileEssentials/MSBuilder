#Const $NamespaceDefine$ = 1

<Assembly: System.Reflection.AssemblyMetadata("Project.AssemblyName", RootNamespace.ThisAssembly.Project.AssemblyName)>
<Assembly: System.Reflection.AssemblyMetadata("Project.RootNamespace", RootNamespace.ThisAssembly.Project.RootNamespace)>
<Assembly: System.Reflection.AssemblyMetadata("Project.ProjectGuid", RootNamespace.ThisAssembly.Project.ProjectGuid)>
<Assembly: System.Reflection.AssemblyMetadata("Project.TargetFrameworkVersion", RootNamespace.ThisAssembly.Project.TargetFrameworkVersion)>
<Assembly: System.Reflection.AssemblyMetadata("Project.TargetFrameworkIdentifier", RootNamespace.ThisAssembly.Project.TargetFrameworkIdentifier)>
<Assembly: System.Reflection.AssemblyMetadata("Project.TargetFrameworkMoniker", RootNamespace.ThisAssembly.Project.TargetFrameworkMoniker)>
<Assembly: System.Reflection.AssemblyMetadata("Project.TargetPlatformVersion", RootNamespace.ThisAssembly.Project.TargetPlatformVersion)>
<Assembly: System.Reflection.AssemblyMetadata("Project.TargetPlatformIdentifier", RootNamespace.ThisAssembly.Project.TargetPlatformIdentifier)>
<Assembly: System.Reflection.AssemblyMetadata("Project.TargetPlatformMoniker", RootNamespace.ThisAssembly.Project.TargetPlatformMoniker)>

#If LOCALNAMESPACE
Namespace _RootNamespace_
#End If
	Partial Class ThisAssembly
		Partial Public Class Project
			''' <summary>RootNamespace: $RootNamespace$</summary>
			Public Const RootNamespace = "$RootNamespace$"

			''' <summary>AssemblyName: $AssemblyName$</summary>
			Public Const AssemblyName = "$AssemblyName$"
		
			''' <summary>ProjectGuid: $ProjectGuid$</summary>
			Public Const ProjectGuid = "$ProjectGuid$"
			
			''' <summary>TargetFrameworkVersion: $TargetFrameworkVersion$</summary>
			Public Const TargetFrameworkVersion = "$TargetFrameworkVersion$"
			
			''' <summary>TargetFrameworkIdentifier: $TargetFrameworkIdentifier$</summary>
			Public Const TargetFrameworkIdentifier = "$TargetFrameworkIdentifier$"
			
			''' <summary>TargetFrameworkMoniker: $TargetFrameworkMoniker$</summary>
			Public Const TargetFrameworkMoniker = "$TargetFrameworkMoniker$"
			
			''' <summary>TargetPlatformVersion: $TargetPlatformVersion$</summary>
			Public Const TargetPlatformVersion = "$TargetPlatformVersion$"

			''' <summary>TargetPlatformIdentifier: $TargetPlatformIdentifier$</summary>
			Public Const TargetPlatformIdentifier = "$TargetPlatformIdentifier$"

			''' <summary>TargetPlatformMoniker: $TargetPlatformMoniker$</summary>
			Public Const TargetPlatformMoniker = "$TargetPlatformMoniker$"
		End Class
	End Class
#If LOCALNAMESPACE
End Namespace
#End If