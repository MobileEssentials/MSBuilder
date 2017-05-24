using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VsixExp;

[assembly: AssemblyTitle("VsixExp")]
[assembly: AssemblyDescription("Experimentalizes VSIXes")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("MobileEssentials")]
[assembly: AssemblyProduct("VsixExp")]
[assembly: AssemblyCopyright("Copyright © MobileEssentials 2017")]


[assembly: AssemblyVersion(ThisAssembly.SimpleVersion)]
[assembly: AssemblyFileVersion(ThisAssembly.FullVersion)]
[assembly: AssemblyInformationalVersion(ThisAssembly.InformationalVersion)]

partial class ThisAssembly
{
    /// <summary>
    /// Simple release-like version number, with just major, minor and ending up in '0'.
    /// </summary>
    public const string SimpleVersion = Git.SemVer.Major + "." + Git.SemVer.Minor + ".0";

    /// <summary>
    /// Full version, including commits since base version file, like 4.0.598
    /// </summary>
    public const string FullVersion = SimpleVersion + "." + Git.SemVer.Patch;

    /// <summary>
    /// Full version, plus branch and commit short sha.
    /// </summary>
    public const string InformationalVersion = FullVersion + "-" + Git.Branch + "+" + Git.Commit;
}