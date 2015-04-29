MSBuilder: NuGet Package DownloadFile
=========================================

This NuGet package provides the DownloadFile task, which downloads 
a file from a URL to a destination directory or file path.

Usage:

<Target Name="AfterBuild">
    <DownloadFile SourceUrl="https://www.nuget.org/api/v2/package/MSBuilder/0.1.0" 
                  DestinationFolder="$(MSBuildProjectDirectory)">
                  <!-- ^ alternatively, the DestinationFile attribute can be used instead. -->
        <!-- The downloaded file is available as an optional output property. -->
        <Output TaskParameter="DownloadedFile" PropertyName="PackageFile" />
    </DownloadFile>
</Target>