param(
    [string] $Architecture = "x64",
    [string] $Version = "1.0.0",
    [string] $Output = "build/Starward",
    [switch] $Dev
)

$ErrorActionPreference = "Stop";

if ($Dev) {
    dotnet publish src/Starward -c Release -r "win-$Architecture" -o "$Output/app-$Version" -p:Platform=$Architecture -p:DefineConstants=DEV -p:Version=$Version;
}
else {
    dotnet publish src/Starward -c Release -r "win-$Architecture" -o "$Output/app-$Version" -p:Platform=$Architecture -p:Version=$Version;
}

$env:Path += ';C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\';
$env:Path += ';C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\';

msbuild src/Starward.Launcher "-property:Configuration=Release;Platform=$Architecture;OutDir=$(Resolve-Path "$Output/")";

Add-Content "$Output/version.ini" -Value "exe_path=app-$Version\Starward.exe";

Remove-Item "$Output/Starward.pdb" -Force;
