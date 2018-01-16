del *.nupkg
nuget pack
rd /q /s %userprofile%\.nuget\packages\msbuilder.vsixinstaller 2>nul
md "%temp%\packages" 2>nul
copy *.nupkg "%temp%\packages"