#Const $NamespaceDefine$ = 1

<Assembly: System.Reflection.AssemblyMetadata("Project.AssemblyName", Global.RootNamespace.ThisAssembly.Project.AssemblyName)>
<Assembly: System.Reflection.AssemblyMetadata("Project.RootNamespace", Global.RootNamespace.ThisAssembly.Project.RootNamespace)>
<Assembly: System.Reflection.AssemblyMetadata("Project.TargetFrameworkVersion", Global.RootNamespace.ThisAssembly.Project.TargetFrameworkVersion)>
<Assembly: System.Reflection.AssemblyMetadata("Project.TargetFrameworkIdentifier", Global.RootNamespace.ThisAssembly.Project.TargetFrameworkIdentifier)>
<Assembly: System.Reflection.AssemblyMetadata("Project.TargetFrameworkMoniker", Global.RootNamespace.ThisAssembly.Project.TargetFrameworkMoniker)>
<Assembly: System.Reflection.AssemblyMetadata("Project.TargetPlatformVersion", Global.RootNamespace.ThisAssembly.Project.TargetPlatformVersion)>
<Assembly: System.Reflection.AssemblyMetadata("Project.TargetPlatformIdentifier", Global.RootNamespace.ThisAssembly.Project.TargetPlatformIdentifier)>
<Assembly: System.Reflection.AssemblyMetadata("Project.TargetPlatformMoniker", Global.RootNamespace.ThisAssembly.Project.TargetPlatformMoniker)>

#If LOCALNAMESPACE
Namespace Global._RootNamespace_
#Else
Namespace Global
#End If
	Partial Class ThisAssembly
		Partial Public Class Project
			''' <summary>RootNamespace: $RootNamespace$</summary>
			Public Const RootNamespace = "$RootNamespace$"

			''' <summary>AssemblyName: $AssemblyName$</summary>
			Public Const AssemblyName = "$AssemblyName$"
					
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
End Namespace