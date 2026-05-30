param(
    [string] $Architecture = "x64",
    [string] $Version = "1.0.0",
    [string] $Output = "build/Starward"
)

$ErrorActionPreference = "Stop";

$env:Path += ';C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\';

dotnet publish src/Starward -c Release -r "win-$Architecture" -o "$Output/app-$Version" -p:Platform=$Architecture -p:Version=$Version;

msbuild src/Starward.Launcher "-property:Configuration=Release;Platform=$Architecture;OutDir=$(Resolve-Path "$Output/")";

Set-Content "$Output/version.ini" -Value "version=$Version";

Remove-Item "$Output/Starward.pdb" -Force;
