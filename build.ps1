param(
    [string] $Architecture = $null,
    [string] $Version = $null
)

$ErrorActionPreference = "Stop";

dotnet publish Starward -c Release -r "win10-$Architecture" -o "build/Starward/app_$Version" -p:Platform=$Architecture -p:PublishTrimmed=true -p:TrimMode=partial -p:Version=$Version;

msbuild Starward.Launcher "-property:Configuration=Release;Platform=$Architecture;OutDir=$(Resolve-Path "build/Starward/")";

Add-Content "build/Starward/config.ini" -Value "app_folder=app_$Version`r`nexe_name=Starward.exe";

Remove-Item "build/Starward/Starward.pdb" -Force;
