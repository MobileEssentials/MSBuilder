MSBuilder: RegexReplace
=========================================

This NuGet package provides the RegexReplace task, which 
matches a pattern in files and applies a replacement in-place.

Usage:

<Target Name="BeforeBuild">
    <RegexReplace Files="@(Compile)" 
                  Pattern="/\* LICENSE \*/"
                  Replacement="$(LicenseHeader)" />
</Target>

Note: the file is not updated unless the the result of 
applying the replacements actually contains changes 
from the original contents.