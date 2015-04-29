MSBuilder: NuGet Package GetLatestVersion
=========================================

This NuGet package provides the GetLatestVersion task, which allows 
easy retrieval of the latest published version of a NuGet package on 
nuget.org given its package Id, as well as easily accessing all its SemVer
components.

Usage:

<Target Name="AfterBuild">
    <GetLatestVersion PackageId="Moq" IncludePreRelease="true">
        <!-- All Outputs are "optional" so you just use what you need -->

        <!-- Full version as published in nuget.org -->
        <Output TaskParameter="PackaverVersion" PropertyName="FullVersion" />
        <!-- Simplified MAJOR.MINOR.PATCH version, .NET compatible -->
        <Output TaskParameter="SimpleVersion" PropertyName="Version" />

        <!-- Access individual version components, as defined in SemVer.org -->
        <Output TaskParameter="Major" PropertyName="Major" />
        <Output TaskParameter="Minor" PropertyName="Minor" />
        <Output TaskParameter="Patch" PropertyName="Patch" />

        <!-- Release is optional, typically something like 'alpha', 'pre', etc. -->
        <Output TaskParameter="Release" PropertyName="Release" />

        <!-- Build metadata is optional, and typically not used since it's 
             part of SemVer v2 which isn't widely implemented yet. See SemVer.org
             for more info. -->
        <Output TaskParameter="Build" PropertyName="Build" />

    </GetLatestVersion>
</Target>