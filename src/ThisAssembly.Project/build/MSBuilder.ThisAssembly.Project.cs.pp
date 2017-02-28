#define $NamespaceDefine$
#pragma warning disable 0436

[assembly: System.Reflection.AssemblyMetadata("Project.AssemblyName", RootNamespace.ThisAssembly.Project.AssemblyName)]
[assembly: System.Reflection.AssemblyMetadata("Project.RootNamespace", RootNamespace.ThisAssembly.Project.RootNamespace)]
[assembly: System.Reflection.AssemblyMetadata("Project.TargetFrameworkVersion", RootNamespace.ThisAssembly.Project.TargetFrameworkVersion)]
[assembly: System.Reflection.AssemblyMetadata("Project.TargetFrameworkIdentifier", RootNamespace.ThisAssembly.Project.TargetFrameworkIdentifier)]
[assembly: System.Reflection.AssemblyMetadata("Project.TargetFrameworkMoniker", RootNamespace.ThisAssembly.Project.TargetFrameworkMoniker)]
[assembly: System.Reflection.AssemblyMetadata("Project.TargetPlatformVersion", RootNamespace.ThisAssembly.Project.TargetPlatformVersion)]
[assembly: System.Reflection.AssemblyMetadata("Project.TargetPlatformIdentifier", RootNamespace.ThisAssembly.Project.TargetPlatformIdentifier)]
[assembly: System.Reflection.AssemblyMetadata("Project.TargetPlatformMoniker", RootNamespace.ThisAssembly.Project.TargetPlatformMoniker)]

#if LOCALNAMESPACE
namespace _RootNamespace_
{
#endif
	partial class ThisAssembly
	{
		public static partial class Project
		{
			/// <summary>RootNamespace: $RootNamespace$</summary>
			public const string RootNamespace = "$RootNamespace$";

			/// <summary>AssemblyName: $AssemblyName$</summary>
			public const string AssemblyName = "$AssemblyName$";
					
			/// <summary>TargetFrameworkVersion: $TargetFrameworkVersion$</summary>
			public const string TargetFrameworkVersion = "$TargetFrameworkVersion$";
			
			/// <summary>TargetFrameworkIdentifier: $TargetFrameworkIdentifier$</summary>
			public const string TargetFrameworkIdentifier = "$TargetFrameworkIdentifier$";
			
			/// <summary>TargetFrameworkMoniker: $TargetFrameworkMoniker$</summary>
			public const string TargetFrameworkMoniker = "$TargetFrameworkMoniker$";
			
			/// <summary>TargetPlatformVersion: $TargetPlatformVersion$</summary>
			public const string TargetPlatformVersion = "$TargetPlatformVersion$";

			/// <summary>TargetPlatformIdentifier: $TargetPlatformIdentifier$</summary>
			public const string TargetPlatformIdentifier = "$TargetPlatformIdentifier$";

			/// <summary>TargetPlatformMoniker: $TargetPlatformMoniker$</summary>
			public const string TargetPlatformMoniker = "$TargetPlatformMoniker$";
		}
	}
#if LOCALNAMESPACE
}
#endif
#pragma warning restore 0436