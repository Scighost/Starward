param(
    [string] $Architecture = "x64",
    [string] $Version = "1.0.0",
    [switch] $Dev
)

$ErrorActionPreference = "Stop";

$build = "build";
$starward = "$build/Starward";
if ($Dev) {
    $metadata = "$build/metadata/dev";
    $package = "$build/release/package/dev";
    $separate = "$build/release/separate_files/dev";
}
else {
    $metadata = "$build/metadata/v1";
    $package = "$build/release/package";
    $separate = "$build/release/separate_files";
}

$null = New-Item -Path $package -ItemType Directory -Force;
$null = New-Item -Path $separate -ItemType Directory -Force;
$null = New-Item -Path $metadata -ItemType Directory -Force;

if (!(Get-Module -Name 7Zip4Powershell -ListAvailable)) {
    Install-Module -Name 7Zip4Powershell -Force;
}

$portableName = "Starward_Portable_$($Version)_$($Architecture).7z";
$portableFile = "$package/$portableName";

if (!(Test-Path $portableFile)) {
    Compress-7Zip -ArchiveFileName $portableName -Path $starward -OutputPath $package -CompressionLevel Ultra -PreserveDirectoryRoot;
}

$release = @{
    Version           = $Version
    Architecture      = $Architecture
    BuildTime         = Get-Date
    DisableAutoUpdate = $false
    Install           = $null
    InstallSize       = 0
    InstallHash       = $null
    Portable          = "https://starward.scighost.com/release/package/$portableName"
    PortableSize      = (Get-Item $portableFile).Length
    PortableHash      = (Get-FileHash $portableFile).Hash
    SeparatePrefix    = "https://starward.scighost.com/release/separate_files/"
};

if ($Dev) {
    $release.Portable = "https://starward.scighost.com/release/package/dev/$portableName";
    $release.SeparatePrefix = "https://starward.scighost.com/release/separate_files/dev/";
}

Out-File -Path "$metadata/version_preview_$Architecture.json" -InputObject (ConvertTo-Json $release);

$path = @{l = "Path"; e = { [System.IO.Path]::GetRelativePath($starward, $_.FullName) } };
$size = @{l = "Size"; e = { $_.Length } };
$hash = @{l = "Hash"; e = { (Get-FileHash $_).Hash } };

$release.SeparateFiles = Get-ChildItem -Path $starward -File -Recurse | Select-Object -Property $path, $size, $hash;

Out-File -Path "$metadata/release_preview_$Architecture.json" -InputObject (ConvertTo-Json $release);

foreach ($file in $release.SeparateFiles) {
    Move-Item -Path "$starward/$($file.Path)" -Destination "$separate/$($file.Hash)" -Force;
}
