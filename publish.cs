#:property BuiltInComInteropSupport = true
#:package Microsoft.Extensions.Configuration.Binder@11.0.0
#:package Microsoft.Extensions.Configuration.CommandLine@11.0.0
#:package Polly@8.9.1
#:package SharpSevenZip@2.0.130
#:package System.IO.Hashing@11.0.0
#:project src/Starward.Setup.Core/Starward.Setup.Core.csproj

using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;
using SharpSevenZip;
using Starward.Setup.Core;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.IO.Hashing;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;


const string UrlPrefix = "https://starward-static.scighost.com/release";


var knownCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "res",
    "compile",
    "pack",
    "manifest",
    "merge",
    "full",
};

string command = "full";
string[] optionArgs = args;

if (args.Length > 0 && !args[0].StartsWith('-'))
{
    if (knownCommands.Contains(args[0]))
    {
        command = args[0].ToLowerInvariant();
        optionArgs = args[1..];
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: Unknown subcommand '{args[0]}'. Supported subcommands: res, compile, pack, manifest, merge, full.");
        Console.ResetColor();
        return 1;
    }
}

var config = new ConfigurationBuilder().AddCommandLine(optionArgs).Build();

if (string.Equals(command, "res", StringComparison.OrdinalIgnoreCase))
{
    bool noBuild = config.GetValue<bool>("no-build");
    string tag = config.GetValue<string>("tag") ?? DateTimeOffset.UtcNow.ToString("yyyy.MMdd.HHmm");

    if (File.Exists("src/Starward.Setup/Assets/Starward.7z"))
    {
        File.Delete("src/Starward.Setup/Assets/Starward.7z");
    }

    if (!noBuild)
    {
        Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + @";C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\");

        await Process.Start("dotnet", $"publish src/Starward.Setup -o publish/pub_res/ -r win-x64 -p:Version={tag}").EnsureExitSuccessAsync();
        File.Move("publish/pub_res/Starward.Setup.exe", $"publish/pub_res/Starward.Setup_x64_{tag}.exe", true);

        await Process.Start("dotnet", $"publish src/Starward.Setup -o publish/pub_res/ -r win-arm64 -p:Version={tag}").EnsureExitSuccessAsync();
        File.Move("publish/pub_res/Starward.Setup.exe", $"publish/pub_res/Starward.Setup_arm64_{tag}.exe", true);

        await Process.Start("msbuild", $"""
        src/Starward.Launcher -property:Configuration=Release;Platform=x64;Version={tag};OutDir={Path.GetFullPath("publish/pub_res/")}
        """).EnsureExitSuccessAsync();
        File.Move("publish/pub_res/Starward.exe", $"publish/pub_res/Starward_x64_{tag}.exe", true);

        await Process.Start("msbuild", $"""
        src/Starward.Launcher -property:Configuration=Release;Platform=arm64;Version={tag};OutDir={Path.GetFullPath("publish/pub_res/")}
        """).EnsureExitSuccessAsync();
        File.Move("publish/pub_res/Starward.exe", $"publish/pub_res/Starward_arm64_{tag}.exe", true);

        await Process.Start("upx", $"publish/pub_res/Starward.Setup_x64_{tag}.exe").EnsureExitSuccessAsync();
    }

    var buildRes = new BuildResource
    {
        Tag = tag,
        SetupX64 = new ReleaseSetup
        {
            FileName = "Starward.Setup.exe",
            Size = new FileInfo($"publish/pub_res/Starward.Setup_x64_{tag}.exe").Length,
            Hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes($"publish/pub_res/Starward.Setup_x64_{tag}.exe"))),
            Url = $"{UrlPrefix}/pub_res/Starward.Setup_x64_{tag}.exe",
        },
        SetupArm64 = new ReleaseSetup
        {
            FileName = "Starward.Setup.exe",
            Size = new FileInfo($"publish/pub_res/Starward.Setup_arm64_{tag}.exe").Length,
            Hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes($"publish/pub_res/Starward.Setup_arm64_{tag}.exe"))),
            Url = $"{UrlPrefix}/pub_res/Starward.Setup_arm64_{tag}.exe",
        },
        LauncherX64 = new ReleaseSetup
        {
            FileName = "Starward.exe",
            Size = new FileInfo($"publish/pub_res/Starward_x64_{tag}.exe").Length,
            Hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes($"publish/pub_res/Starward_x64_{tag}.exe"))),
            Url = $"{UrlPrefix}/pub_res/Starward_x64_{tag}.exe",
        },
        LauncherArm64 = new ReleaseSetup
        {
            FileName = "Starward.exe",
            Size = new FileInfo($"publish/pub_res/Starward_arm64_{tag}.exe").Length,
            Hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes($"publish/pub_res/Starward_arm64_{tag}.exe"))),
            Url = $"{UrlPrefix}/pub_res/Starward_arm64_{tag}.exe",
        }
    };

    byte[] buildResJsonBytes = JsonSerializer.SerializeToUtf8Bytes(buildRes, JsonContext.Default.BuildResource);
    await File.WriteAllBytesAsync("publish/pub_res/pub_res.json", buildResJsonBytes);
    await File.WriteAllBytesAsync($"publish/pub_res/pub_res_{tag}.json", buildResJsonBytes);

    return 0;
}

string? version = config.GetValue<string>("version");
bool rawEnableDiff = config.GetValue<bool>("diff");
string? rawArchOption = config.GetValue<string>("arch");

bool enableDiff = (string.Equals(command, "manifest", StringComparison.OrdinalIgnoreCase)
                || string.Equals(command, "full", StringComparison.OrdinalIgnoreCase))
                && rawEnableDiff;

string? archOption = command switch
{
    "compile" or "pack" or "manifest" or "full" => rawArchOption,
    _ => null,
};

List<Architecture> targetArchitectures = [];
if (string.IsNullOrWhiteSpace(archOption))
{
    targetArchitectures.Add(Architecture.X64);
    targetArchitectures.Add(Architecture.Arm64);
}
else
{
    if (!TryParseArchitecture(archOption, out Architecture selectedArch))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Error: Invalid --arch value. Use x64 or arm64.");
        Console.ResetColor();
        return 0;
    }
    targetArchitectures.Add(selectedArch);
}

if (string.IsNullOrWhiteSpace(version))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: Version is required. Use --version to specify the version.");
    Console.ResetColor();
    return 0;
}

string releaseUrl = $"{ReleaseClient.RepositoryUrl}/releases/download/v{version}";

bool doCompile = false;
bool doPackage = false;
bool doManifest = false;
bool doMerge = false;

switch (command)
{
    case "compile":
        doCompile = true;
        break;
    case "pack":
        doPackage = true;
        break;
    case "manifest":
        doManifest = true;
        break;
    case "merge":
        doMerge = true;
        break;
    case "full":
        doCompile = true;
        doPackage = true;
        doManifest = true;
        doMerge = true;
        break;
    default:
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: Unsupported execution mode '{command}'.");
        Console.ResetColor();
        return 0;
}

if (doMerge && !doCompile && !doPackage && !doManifest)
{
    await MergeReleaseInfoFilesAsync(version, targetArchitectures);
    return 0;
}


if (doCompile)
{
    foreach (var arch in targetArchitectures)
    {
        string archName = arch.ToLower();
        string archPath = $"publish/{archName}";
        if (Directory.Exists(archPath))
        {
            Directory.Delete(archPath, true);
        }

        Console.WriteLine($"Building {archName} release...");
        await Process.Start("dotnet", $"publish src/Starward -c Release -r win-{archName} -o {archPath}/Starward/app-{version} -p:Platform={archName} -p:Version={version}").EnsureExitSuccessAsync();
        await File.WriteAllTextAsync($"{archPath}/Starward/version.ini", $"version={version}");
    }
}


var _client = new BuildClient();

string hdiffz = string.Empty;
if (enableDiff)
{
    hdiffz = await _client.GetHdiffzAsync();
}

ConcurrentDictionary<string, (string XXHash, string SHA256, string CompressedHash)> _hashCache = new();


var release_info = new ReleaseInfo
{
    Version = version,
    Releases = new(),
};


foreach (var arch in targetArchitectures)
{
    if (Directory.Exists($"publish/{arch.ToLower()}"))
    {
        if (doPackage)
        {
            await BuildReleaseToolsAsync(version, arch);
        }
        List<ReleaseManifest> manifests = [];
        await CreatePackageAsync(version, arch, InstallType.Setup, manifests);
        await CreatePackageAsync(version, arch, InstallType.Portable, manifests);
        if (doManifest)
        {
            await WriteBundledManifestsAsync(arch, manifests);
        }
    }
}

if (doManifest)
{
    await WritePartialReleaseInfoFilesAsync(version, targetArchitectures, release_info);
    if (doMerge)
    {
        await MergeReleaseInfoFilesAsync(version, targetArchitectures);
    }
}


return 0;






async Task BuildReleaseToolsAsync(string version, Architecture arch)
{
    string platform = arch.ToLower();
    string toolsPath = Path.GetFullPath($"publish/pub_res/{platform}");
    Directory.CreateDirectory(toolsPath);
    Directory.CreateDirectory("publish/release/package/");
    Directory.CreateDirectory("src/Starward.Setup/Assets/");
    File.Delete("src/Starward.Setup/Assets/Starward.7z");

    await Process.Start("dotnet",
        $"publish src/Starward.Setup -c Release -o \"{toolsPath}\" -r win-{platform} -p:Version={version}").EnsureExitSuccessAsync();
    File.Copy(Path.Join(toolsPath, "Starward.Setup.exe"), $"publish/release/package/Starward_Updater_{version}_{platform}.exe", true);
    await Process.Start("msbuild", $"""
        src/Starward.Launcher "-property:Configuration=Release;Platform={platform};OutDir={toolsPath}"
        """).EnsureExitSuccessAsync();
}


async Task CreatePackageAsync(string version, Architecture arch, InstallType type, List<ReleaseManifest> architectureManifests)
{
    Console.WriteLine($"Creating package for ({version}, {arch}, {type})...");

    string rootPath = type is InstallType.Setup ? $"publish/{arch.ToLower()}/Starward/app-{version}/" : $"publish/{arch.ToLower()}/Starward/";

    // compress
    if (doPackage)
    {
        Directory.CreateDirectory("publish/release/package/");
        var compressor = new SharpSevenZipCompressor { CompressionLevel = SharpSevenZip.CompressionLevel.Ultra };
        if (type is InstallType.Setup)
        {
            Console.WriteLine("Compressing setup package...");
            const string embeddedArchive = "src/Starward.Setup/Assets/Starward.7z";
            try
            {
                compressor.CompressDirectory(rootPath, embeddedArchive);
                Console.WriteLine("Creating setup executable...");
                await Process.Start("dotnet", $"""
                    publish src/Starward.Setup -c Release -o publish/{arch.ToLower()}-setup/ -r win-{arch.ToLower()} -p:Version={version}
                    """).EnsureExitSuccessAsync();
                File.Move($"publish/{arch.ToLower()}-setup/Starward.Setup.exe", $"publish/release/package/Starward_Setup_{version}_{arch.ToLower()}.exe", true);
            }
            finally
            {
                File.Delete(embeddedArchive);
            }
        }
        else
        {
            Console.WriteLine("Compressing portable package...");
            File.Copy($"publish/pub_res/{arch.ToLower()}/Starward.exe", Path.Join(rootPath, "Starward.exe"), true);
            compressor.CompressDirectory(Path.GetDirectoryName(rootPath.TrimEnd('/', '\\'))!, $"publish/release/package/Starward_Portable_{version}_{arch.ToLower()}.7z");
        }
        Console.WriteLine("Compression completed.");
        Console.WriteLine("--------------------");
    }

    if (!doManifest)
    {
        return;
    }

    // manifest
    List<ReleaseManifest> manifests = [];
    Directory.CreateDirectory("publish/release/file/");
    Directory.CreateDirectory("publish/release/manifest/");

    manifests.Add(await CreateManifestAsync(arch, type, rootPath, version));

    if (enableDiff)
    {
        var release = await _client.GetReleaseInfoDetailAsync(arch, type, CancellationToken.None);
        if (release is not null)
        {
            var previousVersions = new[] { release.Version }.Concat(release.Diffs?.Keys.AsEnumerable() ?? [])
                .Distinct(StringComparer.OrdinalIgnoreCase).Where(previous => !string.Equals(previous, version, StringComparison.OrdinalIgnoreCase));
            foreach (string previous in previousVersions.Take(5))
            {
                manifests.Add(await CreateManifestAsync(arch, type, rootPath, version, previous));
            }
        }
    }
    architectureManifests.AddRange(manifests);

    string packageFileName = type is InstallType.Setup ? $"Starward_Setup_{version}_{arch.ToLower()}.exe" : $"Starward_Portable_{version}_{arch.ToLower()}.7z";
    string updaterName = $"Starward_Updater_{version}_{arch.ToLower()}.exe";
    string updaterPath = $"publish/release/package/{updaterName}";

    release_info.Releases.Add($"{arch}-{type}".ToLower(), new ReleaseInfoDetail
    {
        Version = version,
        Architecture = arch,
        InstallType = type,
        BuildTime = DateTimeOffset.UtcNow,
        ManifestUrl = $"{releaseUrl}/{GetManifestName(manifests[0])}",
        PackageUrl = $"{releaseUrl}/{packageFileName}",
        PackageSize = new FileInfo($"publish/release/package/{packageFileName}").Length,
        PackageHash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes($"publish/release/package/{packageFileName}"))),
        Diffs = manifests.Where(x => x.DiffVersion is not null)
                         .Select(x => new ReleaseInfoDiff
                         {
                             DiffVersion = x!.DiffVersion!,
                             DiffSize = x.DiffSize,
                             ManifestUrl = $"{releaseUrl}/{GetManifestName(x)}",
                         })
                         .ToDictionary(x => x.DiffVersion),
        Setup = type is InstallType.Setup ? new ReleaseSetup
        {
            FileName = "Starward.Setup.exe",
            Url = $"{releaseUrl}/{updaterName}",
            Size = new FileInfo(updaterPath).Length,
            Hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(updaterPath))),
        } : null,
    });
}


async Task<ReleaseManifest> CreateManifestAsync(Architecture arch, InstallType type, string rootPath, string version, string? diffVersion = null)
{
    Console.WriteLine($"Creating manifest for ({version}, {arch}, {type}, {diffVersion})...");
    ReleaseManifest manifest = new()
    {
        Architecture = arch,
        InstallType = type,
        Version = version,
        UrlPrefix = $"{releaseUrl}/",
        Files = [],
    };
    ReleaseManifest? oldManifest = null;
    if (!string.IsNullOrWhiteSpace(diffVersion))
    {
        Console.WriteLine($"Getting diff manifest for version {diffVersion}...");
        oldManifest = await _client.GetManifestAsync(diffVersion, arch, type, CancellationToken.None)
            ?? throw new InvalidDataException($"No manifest found for {diffVersion} ({arch}, {type}).");
    }

    string[] files = Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories);
    Console.WriteLine($"Found {files.Length} files to pack.");
    foreach (var item in files)
    {
        manifest.Files.Add(new ReleaseFile
        {
            Path = Path.GetRelativePath(rootPath, item),
        });
    }

    Console.ForegroundColor = ConsoleColor.DarkGray;
    await Parallel.ForEachAsync(manifest.Files, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(8, Environment.ProcessorCount * 2) }, async (item, _) =>
    {
        string file = Path.Join(rootPath, item.Path);
        string xxhash;
        string sha256;
        string compressedHash;
        string id;
        string idPath;
        if (!_hashCache.TryGetValue(file, out var cachedHash))
        {
            byte[] bytes = await File.ReadAllBytesAsync(file);
            sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes));
            xxhash = Convert.ToHexStringLower(XxHash3.Hash(bytes));
            id = $"{xxhash}_{sha256}";
            idPath = Path.Join("publish/release/file/", id);
            byte[] zstdBytes;
            using (var ms = new MemoryStream())
            {
                using (var zstd = new ZstandardStream(ms, new ZstandardCompressionOptions { Quality = 17 }, leaveOpen: true))
                {
                    await zstd.WriteAsync(bytes);
                }
                zstdBytes = ms.ToArray();
            }
            try
            {
                await File.WriteAllBytesAsync(idPath, zstdBytes);
            }
            catch
            {
                await Task.Delay(3000);
                await File.WriteAllBytesAsync(idPath, zstdBytes);
            }
            compressedHash = Convert.ToHexStringLower(SHA256.HashData(zstdBytes));
            _hashCache[file] = (xxhash, sha256, compressedHash);
        }
        else
        {
            xxhash = cachedHash.XXHash;
            sha256 = cachedHash.SHA256;
            compressedHash = cachedHash.CompressedHash;
            id = $"{xxhash}_{sha256}";
            idPath = Path.Join("publish/release/file/", id);
        }

        item.Id = id;
        item.Size = new FileInfo(file).Length;
        item.CompressedSize = new FileInfo(idPath).Length;
        item.Hash = sha256;
        item.CompressedHash = compressedHash;

        // diff
        if (oldManifest is not null && (type is InstallType.Setup || !string.IsNullOrEmpty(Path.GetDirectoryName(item.Path))))
        {
            if (oldManifest.Files.FirstOrDefault(x => x.Size == item.Size && string.Equals(x.Hash, item.Hash, StringComparison.OrdinalIgnoreCase)) is ReleaseFile oldItem)
            {
                item.Patch = new ReleaseFilePatch
                {
                    OldPath = oldItem.Path,
                    OldFileSize = oldItem.Size,
                    OldFileHash = oldItem.Hash,
                };
            }
            else if (MatchDiffFile(item.Path, oldManifest.Files) is ReleaseFile oldItem2)
            {
                string oldFilePath = Path.Join("publish/temp/", oldItem2.Id);
                await _client.DownloadZstdFileAndCheckHashAsync(oldManifest, oldItem2, oldFilePath);
                string diffPath = Path.Combine("publish/temp/", $"diff_{item.Id}_{oldItem2.Id}");
                if (!File.Exists(diffPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(diffPath)!);
                    var p = Process.Start(new ProcessStartInfo
                    {
                        FileName = hdiffz,
                        Arguments = $"""
                        "{oldFilePath}" "{file}" "{diffPath}" -c-zstd-17
                        """,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    });
                    if (p is not null)
                    {
                        await p.WaitForExitAsync();
                        if (p.ExitCode != 0)
                        {
                            throw new Exception($"hdiffz exited with code {p.ExitCode}: {p.StandardError.ReadToEnd()}");
                        }
                    }
                }
                if (File.Exists(diffPath) && new FileInfo(diffPath).Length < item.CompressedSize)
                {
                    long diffSize = new FileInfo(diffPath).Length;
                    item.Patch = new ReleaseFilePatch
                    {
                        Id = diffPath,
                        OldPath = oldItem2.Path,
                        OldFileSize = oldItem2.Size,
                        OldFileHash = oldItem2.Hash,
                        PatchSize = diffSize,
                        Length = diffSize,
                    };
                    // File.Move(diffPath, Path.Join("publish/release/file/", item.Patch.Id), true);
                }
            }
        }

    });

    manifest.FileCount = manifest.Files.Count;
    manifest.Size = manifest.Files.Sum(f => f.Size);
    manifest.CompressedSize = manifest.Files.Sum(f => f.CompressedSize);
    if (oldManifest is not null)
    {
        manifest.DiffVersion = oldManifest.Version;
        if (type is InstallType.Setup)
        {
            manifest.DeleteFiles = oldManifest.Files.Select(x => x.Path).Except(manifest.Files.Select(f => f.Path)).ToList();
        }


        var packages = PackDiffFiles(manifest.Files);

        foreach (var package in packages)
        {
            using var ms = new MemoryStream(package.Size > int.MaxValue ? 0 : (int)package.Size);
            long offset = 0;
            foreach (var file in package.Files)
            {
                string diffFilePath = file.Patch!.Id!; // 原id是路径
                byte[] diffBytes = await File.ReadAllBytesAsync(diffFilePath);
                file.Patch.Offset = offset;
                file.Patch.Length = diffBytes.Length;
                ms.Write(diffBytes);
                offset += diffBytes.Length;
            }

            byte[] packageBytes = ms.ToArray();
            string packageSha256 = Convert.ToHexStringLower(SHA256.HashData(packageBytes));
            string packageId = $"{Convert.ToHexStringLower(XxHash3.Hash(packageBytes))}_{packageSha256}";
            string packagePath = Path.Join("publish/release/file/", packageId);
            await File.WriteAllBytesAsync(packagePath, packageBytes);

            foreach (var file in package.Files)
            {
                file.Patch!.Id = packageId;
                file.Patch.PatchSize = packageBytes.LongLength;
                file.Patch.PatchHash = packageSha256;
            }
        }

        manifest.DiffFileCount = manifest.Files.Count(f => f.Patch == null) + packages.Count;
        manifest.DiffSize = manifest.Files.Where(x => x.Patch == null).Sum(x => x.CompressedSize) + packages.Sum(p => p.Size);
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Compressed {manifest.FileCount} files. Total size: {manifest.Size / 1024.0:N2} KB -> {manifest.CompressedSize / 1024.0:N2} KB");
    Console.ResetColor();
    Console.WriteLine("--------------------");

    return manifest;
}


static string GetManifestName(ReleaseManifest manifest)
{
    string diff = manifest.DiffVersion is null ? "" : $"_diff_{manifest.DiffVersion}";
    return $"manifest_{manifest.Version}_{manifest.Architecture}_{manifest.InstallType}{diff}.json".ToLowerInvariant();
}


async Task WriteBundledManifestsAsync(Architecture arch, List<ReleaseManifest> manifests)
{
    string name = $"update-files_{arch.ToLower()}.bin";
    const string assets = "publish/release/assets";
    const string compressedFiles = "publish/release/file";
    Directory.CreateDirectory(assets);
    var offsets = new Dictionary<string, long>(StringComparer.Ordinal);
    await using (var bundle = File.Create(Path.Join(assets, name)))
    {
        foreach (var file in manifests.Where(manifest => manifest.DiffVersion is null)
                     .SelectMany(manifest => manifest.Files).DistinctBy(file => file.Id).OrderBy(file => file.Id, StringComparer.Ordinal))
        {
            offsets.Add(file.Id, bundle.Position);
            await using var compressed = File.OpenRead(Path.Join(compressedFiles, file.Id));
            await compressed.CopyToAsync(bundle);
        }
    }

    foreach (var manifest in manifests)
    {
        manifest.FileBundleUrl = $"{releaseUrl}/{name}";
        foreach (var file in manifest.Files)
        {
            file.BundleOffset = offsets[file.Id];
        }
        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, JsonContext.Default.ReleaseManifest);
        await File.WriteAllBytesAsync(Path.Join("publish/release/manifest", GetManifestName(manifest)), jsonBytes);
    }

    foreach (string patchId in manifests.SelectMany(manifest => manifest.Files)
                 .Select(file => file.Patch?.Id).OfType<string>().Distinct(StringComparer.Ordinal))
    {
        File.Copy(Path.Join(compressedFiles, patchId), Path.Join(assets, patchId), true);
    }
}


static List<(List<ReleaseFile> Files, long Size)> PackDiffFiles(IEnumerable<ReleaseFile> files)
{
    const long TargetPackageSize = 2 * 1024 * 1024; // 2MB
    const long MinPackageSize = 18 * 1024 * 1024 / 10; // 1.8MB
    const long MaxPackageSize = 22 * 1024 * 1024 / 10; // 2.2MB

    List<ReleaseFile> orderedFiles = files
        .Where(x => x.Patch?.Id is not null)
        .OrderByDescending(x => x.Patch!.Length)
        .ThenBy(x => x.Path, StringComparer.Ordinal)
        .ToList();

    var oversizedPackages = new List<(List<ReleaseFile> Files, long Size)>();
    var normalFiles = new List<ReleaseFile>();
    foreach (var file in orderedFiles)
    {
        long fileSize = file.Patch!.Length;
        if (fileSize >= MaxPackageSize)
        {
            oversizedPackages.Add((new List<ReleaseFile> { file }, fileSize));
        }
        else
        {
            normalFiles.Add(file);
        }
    }

    long totalNormalSize = normalFiles.Sum(x => x.Patch!.Length);
    int initialBinCount = totalNormalSize == 0 ? 0 : (int)Math.Ceiling((double)totalNormalSize / MaxPackageSize);

    var packages = new List<(List<ReleaseFile> Files, long Size)>();
    for (int i = 0; i < initialBinCount; i++)
    {
        packages.Add((new List<ReleaseFile>(), 0));
    }

    foreach (var file in normalFiles)
    {
        long fileSize = file.Patch!.Length;

        int bestPackageIndex = -1;
        long bestDistance = long.MaxValue;
        long bestSize = long.MinValue;
        for (int i = 0; i < packages.Count; i++)
        {
            long nextSize = packages[i].Size + fileSize;
            if (nextSize > MaxPackageSize)
            {
                continue;
            }

            long distance = Math.Abs(TargetPackageSize - nextSize);
            if (distance < bestDistance || (distance == bestDistance && nextSize > bestSize))
            {
                bestDistance = distance;
                bestSize = nextSize;
                bestPackageIndex = i;
            }
        }

        if (bestPackageIndex >= 0)
        {
            packages[bestPackageIndex].Files.Add(file);
            packages[bestPackageIndex] = (packages[bestPackageIndex].Files, packages[bestPackageIndex].Size + fileSize);
        }
        else
        {
            packages.Add((new List<ReleaseFile> { file }, fileSize));
        }
    }

    MergeUndersizedPackages(packages, MinPackageSize, TargetPackageSize, MaxPackageSize);
    packages.AddRange(oversizedPackages);
    packages.RemoveAll(static x => x.Files.Count == 0);
    return packages;
}


static void MergeUndersizedPackages(List<(List<ReleaseFile> Files, long Size)> packages, long minPackageSize, long targetPackageSize, long maxPackageSize)
{
    while (true)
    {
        int undersizedIndex = -1;
        long undersizedSize = long.MaxValue;
        for (int i = 0; i < packages.Count; i++)
        {
            if (packages[i].Size < minPackageSize && packages[i].Size < undersizedSize)
            {
                undersizedIndex = i;
                undersizedSize = packages[i].Size;
            }
        }

        if (undersizedIndex < 0)
        {
            return;
        }

        int mergeIndex = -1;
        long bestDistance = long.MaxValue;
        long bestMergedSize = long.MinValue;
        for (int i = 0; i < packages.Count; i++)
        {
            if (i == undersizedIndex)
            {
                continue;
            }

            long mergedSize = packages[i].Size + undersizedSize;
            if (mergedSize > maxPackageSize)
            {
                continue;
            }

            long distance = Math.Abs(targetPackageSize - mergedSize);
            if (distance < bestDistance || (distance == bestDistance && mergedSize > bestMergedSize))
            {
                bestDistance = distance;
                bestMergedSize = mergedSize;
                mergeIndex = i;
            }
        }

        if (mergeIndex < 0)
        {
            if (!TryBorrowForUndersizedPackage(packages, undersizedIndex, minPackageSize, targetPackageSize, maxPackageSize))
            {
                return;
            }
            continue;
        }

        packages[mergeIndex].Files.AddRange(packages[undersizedIndex].Files);
        packages[mergeIndex] = (packages[mergeIndex].Files, packages[mergeIndex].Size + packages[undersizedIndex].Size);
        packages.RemoveAt(undersizedIndex);
    }
}


static bool TryBorrowForUndersizedPackage(List<(List<ReleaseFile> Files, long Size)> packages, int undersizedIndex, long minPackageSize, long targetPackageSize, long maxPackageSize)
{
    int donorIndex = -1;
    ReleaseFile? borrowedFile = null;
    long bestScore = long.MaxValue;

    for (int i = 0; i < packages.Count; i++)
    {
        if (i == undersizedIndex || packages[i].Files.Count == 0)
        {
            continue;
        }

        foreach (var file in packages[i].Files)
        {
            long fileSize = file.Patch!.Length;
            long nextUndersized = packages[undersizedIndex].Size + fileSize;
            long nextDonor = packages[i].Size - fileSize;

            if (nextUndersized > maxPackageSize)
            {
                continue;
            }

            if (nextDonor > 0 && nextDonor < minPackageSize)
            {
                continue;
            }

            long score = Math.Abs(targetPackageSize - nextUndersized) + Math.Abs(targetPackageSize - nextDonor);
            if (score < bestScore)
            {
                bestScore = score;
                donorIndex = i;
                borrowedFile = file;
            }
        }
    }

    if (donorIndex < 0 || borrowedFile is null)
    {
        return false;
    }

    long borrowedSize = borrowedFile.Patch!.Length;
    packages[donorIndex].Files.Remove(borrowedFile);
    packages[donorIndex] = (packages[donorIndex].Files, packages[donorIndex].Size - borrowedSize);
    packages[undersizedIndex].Files.Add(borrowedFile);
    packages[undersizedIndex] = (packages[undersizedIndex].Files, packages[undersizedIndex].Size + borrowedSize);
    return true;
}


static ReleaseFile? MatchDiffFile(string newFile, List<ReleaseFile> oldFiles)
{
    var fileName = Path.GetFileName(newFile.AsSpan());
    int splitCount = newFile.AsSpan().Count(Path.DirectorySeparatorChar);
    var span = newFile.AsSpan();
    while (true)
    {
        foreach (var item in oldFiles)
        {
            var pathSpan = item.Path.AsSpan();
            if (pathSpan.EndsWith(span) && pathSpan.Count(Path.DirectorySeparatorChar) == splitCount && Path.GetFileName(pathSpan).SequenceEqual(fileName))
            {
                return item;
            }
        }
        int index = span.IndexOf(Path.DirectorySeparatorChar);
        if (index >= 0)
        {
            span = span[(index + 1)..];
        }
        else
        {
            return null;
        }
    }
}


async Task WritePartialReleaseInfoFilesAsync(string version, List<Architecture> architectures, ReleaseInfo releaseInfo)
{
    Directory.CreateDirectory("publish/release/version/");

    foreach (var arch in architectures)
    {
        string archName = arch.ToLower();
        string outputDir = Path.Join("publish/release/version", archName);
        Directory.CreateDirectory(outputDir);

        ReleaseInfo partial = new()
        {
            Version = releaseInfo.Version,
            Releases = releaseInfo.Releases
                                  .Where(x => x.Value.Architecture == arch)
                                  .ToDictionary(x => x.Key, x => x.Value),
        };

        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(partial, JsonContext.Default.ReleaseInfo);
        await File.WriteAllBytesAsync(Path.Join(outputDir, $"release_info_{version}.json"), jsonBytes);
        File.Copy(Path.Join(outputDir, $"release_info_{version}.json"), Path.Join(outputDir, version), true);
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Release info partial files for version {version} created successfully.");
    Console.ResetColor();
}


async Task MergeReleaseInfoFilesAsync(string version, IEnumerable<Architecture> architectures)
{
    ReleaseInfo merged = new()
    {
        Version = version,
        Releases = new(),
    };

    foreach (var arch in architectures.Distinct())
    {
        string archName = arch.ToLower();
        string inputPath = Path.Join("publish/release/version", archName, $"release_info_{version}.json");
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException($"Release info file not found: {inputPath}");
        }

        byte[] jsonBytes = await File.ReadAllBytesAsync(inputPath);
        ReleaseInfo partial = JsonSerializer.Deserialize(jsonBytes, JsonContext.Default.ReleaseInfo)
                              ?? throw new Exception($"Failed to read release info: {inputPath}");

        foreach (var item in partial.Releases)
        {
            merged.Releases[item.Key] = item.Value;
        }
    }

    Directory.CreateDirectory("publish/release/version/");
    byte[] mergedJsonBytes = JsonSerializer.SerializeToUtf8Bytes(merged, JsonContext.Default.ReleaseInfo);
    await File.WriteAllBytesAsync($"publish/release/version/release_info_{version}.json", mergedJsonBytes);
    File.Copy($"publish/release/version/release_info_{version}.json", $"publish/release/version/{version}", true);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Release info for version {version} created successfully.");
    Console.ResetColor();
}


static bool TryParseArchitecture(string value, out Architecture architecture)
{
    architecture = default;
    if (string.Equals(value, "x64", StringComparison.OrdinalIgnoreCase))
    {
        architecture = Architecture.X64;
        return true;
    }
    if (string.Equals(value, "arm64", StringComparison.OrdinalIgnoreCase))
    {
        architecture = Architecture.Arm64;
        return true;
    }
    return false;
}


public class BuildClient
{

    private readonly HttpClient _httpClient;

    private readonly ReleaseClient _releaseClient;

    private readonly ResiliencePipeline _polly;


    public BuildClient()
    {
        _httpClient = new HttpClient(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All })
        {
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Starward Build Tool");
        _releaseClient = new ReleaseClient(_httpClient);

        _polly = new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Linear,
        }).Build();
    }


    public async Task<BuildResource> PrepareBuildResourceAsync()
    {
        Console.WriteLine("Preparing build resources...");
        BuildResource? res = await _httpClient.GetFromJsonAsync("https://starward-release.scighost.com/release/pub_res/pub_res.json", JsonContext.Default.BuildResource)
                           ?? throw new Exception("Failed to get build resource from server.");
        Directory.CreateDirectory("publish/pub_res/");
        await DownloadFileAndCheckHashAsync(res.SetupX64.Url, "publish/pub_res/Starward.Setup_x64.exe", res.SetupX64.Hash);
        await DownloadFileAndCheckHashAsync(res.SetupArm64.Url, "publish/pub_res/Starward.Setup_arm64.exe", res.SetupArm64.Hash);
        await DownloadFileAndCheckHashAsync(res.LauncherX64.Url, "publish/pub_res/Starward_x64.exe", res.LauncherX64.Hash);
        await DownloadFileAndCheckHashAsync(res.LauncherArm64.Url, "publish/pub_res/Starward_arm64.exe", res.LauncherArm64.Hash);
        return res;
    }



    public async Task DownloadFileAsync(string url, string path, CancellationToken cancellation = default)
    {
        await _polly.ExecuteAsync(async token =>
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            using var hs = await response.Content.ReadAsStreamAsync(token);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var fs = File.Create(path);
            await hs.CopyToAsync(fs, token);
        }, cancellation);
    }


    public async Task DownloadFileAndCheckHashAsync(string url, string path, string hash, CancellationToken cancellation = default)
    {
        await _polly.ExecuteAsync(async token =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            string fileHash = Convert.ToHexStringLower(await SHA256.HashDataAsync(fs, cancellation));
            if (string.Equals(fileHash, hash, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            fs.Position = 0;
            using HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            using var hs = await response.Content.ReadAsStreamAsync(token);
            await hs.CopyToAsync(fs, token);
            fs.SetLength(fs.Position);
            fs.Position = 0;
            fileHash = Convert.ToHexStringLower(await SHA256.HashDataAsync(fs, cancellation));
            if (!string.Equals(fileHash, hash, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Hash mismatch for downloaded file. Expected: {hash}, Actual: {fileHash}");
            }
        }, cancellation);
    }


    public async Task DownloadZstdFileAndCheckHashAsync(ReleaseManifest manifest, ReleaseFile file, string path, CancellationToken cancellation = default)
    {
        await _polly.ExecuteAsync(async token =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            string fileHash = Convert.ToHexStringLower(await SHA256.HashDataAsync(fs, cancellation));
            if (fs.Length == file.Size && string.Equals(fileHash, file.Hash, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            fs.Position = 0;
            using HttpResponseMessage response = await _releaseClient.DownloadReleaseFileAsync(manifest, file, token);
            using var hs = await response.Content.ReadAsStreamAsync(token);
            using var zstd = new ZstandardStream(hs, System.IO.Compression.CompressionMode.Decompress);
            await zstd.CopyToAsync(fs, token);
            fs.SetLength(fs.Position);
            fs.Position = 0;
            fileHash = Convert.ToHexStringLower(await SHA256.HashDataAsync(fs, cancellation));
            if (fs.Length != file.Size || !string.Equals(fileHash, file.Hash, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Hash mismatch for downloaded file. Expected: {file.Hash}, Actual: {fileHash}");
            }
        }, cancellation);
    }


    public async Task<string> GetHdiffzAsync(CancellationToken cancellation = default)
    {
        try
        {
            Process.Start("hdiffz", "-v").WaitForExit();
            return "hdiffz";
        }
        catch { }
        string exe = "publish/hdiffpatch/windows64/hdiffz.exe";
        if (File.Exists(exe))
        {
            return exe;
        }
        Console.WriteLine("Downloading hdiffz...");
        string zip = "publish/hdiffpatch/hdiffpatch.zip";
        const string url = "https://github.com/sisong/HDiffPatch/releases/download/v4.12.2/hdiffpatch_v4.12.2_bin_windows64.zip";
        Directory.CreateDirectory("publish/hdiffpatch");
        await DownloadFileAsync(url, zip, cancellation);
        ZipFile.ExtractToDirectory(zip, "publish/hdiffpatch/");
        return exe;
    }


    public async Task<ReleaseManifest?> GetManifestAsync(string version, Architecture arch, InstallType type, CancellationToken cancellation = default)
    {
        return await _polly.ExecuteAsync(async token =>
        {
            var info = await _releaseClient.GetReleaseInfoAsync(version, token);
            if (info.TryGetReleaseInfoDetail(arch, type, out ReleaseInfoDetail? detail))
            {
                return await _releaseClient.GetReleaseManifestAsync(detail.ManifestUrl, token);
            }
            else
            {
                return null;
            }
        }, cancellation);
    }



    public async Task<ReleaseInfoDetail?> GetReleaseInfoDetailAsync(Architecture arch, InstallType type, CancellationToken cancellation = default)
    {
        return await _polly.ExecuteAsync(async token =>
        {
            try
            {
                var info = await _releaseClient.GetLatestReleaseInfoAsync(true, "publish", token);
                return info.TryGetReleaseInfoDetail(arch, type, out ReleaseInfoDetail? detail) ? detail : null;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { return null; }
        }, cancellation);
    }



}


public class BuildResource
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; }

    [JsonPropertyName("launcher-x64")]
    public ReleaseSetup LauncherX64 { get; set; }

    [JsonPropertyName("launcher-arm64")]
    public ReleaseSetup LauncherArm64 { get; set; }

    [JsonPropertyName("setup-x64")]
    public ReleaseSetup SetupX64 { get; set; }

    [JsonPropertyName("setup-arm64")]
    public ReleaseSetup SetupArm64 { get; set; }

}


public static class Extension
{
    public static string ToLower(this Enum @enum) => @enum.ToString().ToLower();


    public static async Task EnsureExitSuccessAsync(this Process process)
    {
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new Exception($"Process exited with code {process.ExitCode}");
        }
    }
}


[JsonSerializable(typeof(BuildResource))]
[JsonSerializable(typeof(ReleaseInfo))]
[JsonSerializable(typeof(ReleaseManifest))]
[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
public partial class JsonContext : JsonSerializerContext { }

