param(
    [string] $Architecture = "x64",
    [string] $Version = "1.0.0",
    [string] $Output = "build/Starward",
    [int] $DiffCount = 5,
    [array] $DiffTags = @()
)

$ErrorActionPreference = "Stop";

.\build.ps1 -Version $Version -Architecture $Architecture -Output $Output;

if (!(Get-Module -Name 7Zip4Powershell -ListAvailable)) {
    Install-Module -Name 7Zip4Powershell -Force;
}

$package = "Starward_Portable_$($Version)_$($Architecture).7z";
Write-Host "Creating package $package ..." -ForegroundColor Green;
Compress-7Zip -ArchiveFileName $package -Path $Output -OutputPath 'build/release/package/' -CompressionLevel Ultra -PreserveDirectoryRoot;

Write-Host "Packing release ..." -ForegroundColor Green;
dotnet run --project 'src/Starward.Setup.Build' -c Release -p:Platform=x64 -- pack $Output -v $Version -a $Architecture -t portable;


if ($DiffTags.Count -eq 0 -and $DiffCount -gt 0) {
    if ($env:GITHUB_TOKEN) {
        $json = Invoke-WebRequest 'https://api.github.com/repos/Scighost/Starward/releases' -Headers @{ Authorization = "Bearer $env:GITHUB_TOKEN" } | ConvertFrom-Json;
    }
    else {
        $json = Invoke-WebRequest 'https://api.github.com/repos/Scighost/Starward/releases' | ConvertFrom-Json;
    }

    $pre = $true;
    $stableCount = 0;
    foreach ($r in $json) {
        if ($r.tag_name -eq $Version) {
            continue;
        }
        if ($pre -and $r.tag_name -like '*-*') {
            $DiffTags += $r.tag_name;
            Write-Host "Creating diff for $($r.tag_name) ..." -ForegroundColor Green;
            dotnet run --project 'src/Starward.Setup.Build' -c Release -p:Platform=x64 -- diff -np $Output -nv $Version -ov $r.tag_name -a $Architecture -t portable;
        }
        if ($r.tag_name -notlike '*-*') {
            $pre = $false;
            $stableCount += 1;
            $DiffTags += $r.tag_name;
            Write-Host "Creating diff for $($r.tag_name) ..." -ForegroundColor Green;
            dotnet run --project 'src/Starward.Setup.Build' -c Release -p:Platform=x64 -- diff -np $Output -nv $Version -ov $r.tag_name -a $Architecture -t portable;
        }
        if ($stableCount -ge $DiffCount) {
            break;
        }
    }
}
elseif ($DiffTags.Count -gt 0) {
    foreach ($tag in $DiffTags) {
        Write-Host "Creating diff for $tag ..." -ForegroundColor Green;
        dotnet run --project 'src/Starward.Setup.Build' -c Release -p:Platform=x64 -- diff -np $Output -nv $Version -ov $tag -a $Architecture -t portable;
    }
}

if ($DiffTags.Count -eq 0) {
    Write-Host "Creating release info for $($Version)_$($Architecture) ..." -ForegroundColor Green;
    dotnet run --project 'src/Starward.Setup.Build' -c Release -p:Platform=x64 -- release create "build/release_info_$($Version)_$($Architecture).json" -v $Version -a $Architecture -t portable -p "build/release/package/$package";
}
else {
    Write-Host "Creating release info for $($Version)_$($Architecture) with diffs $($DiffTags -join ', ') ..." -ForegroundColor Green;
    dotnet run --project 'src/Starward.Setup.Build' -c Release -p:Platform=x64 -- release create "build/release_info_$($Version)_$($Architecture).json" -v $Version -a $Architecture -t portable -p "build/release/package/$package" -d $DiffTags;
}

Remove-Item 'build/release/temp' -Recurse -Force -ErrorAction SilentlyContinue;
