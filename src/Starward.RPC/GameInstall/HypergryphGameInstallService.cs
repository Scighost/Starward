using Microsoft.Extensions.Logging;
using Snap.HPatch;
using Starward.Core.Hypergryph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace Starward.RPC.GameInstall;

internal sealed class HypergryphGameInstallService
{
    private const int DownloadConcurrency = 4;

    private const int HardLinkConcurrency = 2;

    private static readonly HashSet<string> V2ControlFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "patch.json",
        "delete_files.txt",
        "deletefiles.txt",
        "package_files",
        "verify_files.json",
    };

    private readonly ILogger<HypergryphGameInstallService> _logger;
    private readonly HypergryphLauncherClient _launcherClient;
    private readonly GameInstallHelper _gameInstallHelper;

    public HypergryphGameInstallService(
        ILogger<HypergryphGameInstallService> logger,
        HypergryphLauncherClient launcherClient,
        GameInstallHelper gameInstallHelper)
    {
        _logger = logger;
        _launcherClient = launcherClient;
        _gameInstallHelper = gameInstallHelper;
    }

    public async Task ExecuteAsync(GameInstallContext context, CancellationToken cancellationToken)
    {
        if (context.Operation is not GameInstallOperation.Predownload)
        {
            foreach (string file in Directory.EnumerateFiles(context.InstallPath, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                File.SetAttributes(file, FileAttributes.Normal);
            }
        }
        HypergryphGameProfile profile = HypergryphGameConstants.GetGameProfile(context.GameId.GameBiz);
        HypergryphInstallMetadata? metadata = await HypergryphInstallMetadata.ReadAsync(context.InstallPath, cancellationToken);
        if (metadata is not null && !metadata.IsFor(context.GameId.GameBiz))
        {
            metadata = null;
        }
        string localVersion = metadata?.Version ?? "";
        HypergryphLatestGame latest = await _launcherClient.GetLatestGameAsync(
            context.GameId.GameBiz,
            localVersion,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(localVersion) && File.Exists(Path.Combine(context.InstallPath, profile.ExeName)))
        {
            localVersion = await IsLatestOfficialInstallAsync(context, latest, cancellationToken) ? latest.Version : "0.0.0";
            if (localVersion == latest.Version)
            {
                metadata = new HypergryphInstallMetadata
                {
                    AppCode = profile.GameAppCode,
                    Version = latest.Version,
                    GameFilesMD5 = latest.Package.GameFilesMD5,
                };
                await metadata.WriteAsync(context.InstallPath, cancellationToken);
            }
        }

        context.LocalGameVersion = localVersion;
        context.LatestGameVersion = latest.Version;
        context.PredownloadVersion = latest.PrePatch?.TargetVersion;

        switch (context.Operation)
        {
            case GameInstallOperation.Predownload:
                await ExecutePredownloadAsync(context, latest, metadata, cancellationToken);
                break;
            case GameInstallOperation.Update when latest.Patch is not null && latest.Patch.DownloadParts.Count > 0:
                await ExecuteUpdateWithFallbackAsync(context, latest, cancellationToken);
                break;
            case GameInstallOperation.Install:
            case GameInstallOperation.Update:
            case GameInstallOperation.Repair:
                await ExecuteFullPackageAsync(context, latest, cancellationToken);
                break;
            default:
                throw new NotSupportedException($"Unsupported Hypergryph install operation: {context.Operation}.");
        }

        if (context.Operation is not GameInstallOperation.Predownload)
        {
            try
            {
                await HardLinkMatchingVfsFilesAsync(context, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to hard link matching Endfield VFS files.");
            }
        }
    }

    private async Task ExecuteUpdateWithFallbackAsync(GameInstallContext context, HypergryphLatestGame latest, CancellationToken cancellationToken)
    {
        try
        {
            if (latest.Patch!.IsV2)
            {
                await ExecuteV2PatchAsync(context, latest, latest.Patch, cancellationToken);
            }
            else
            {
                await ExecuteLegacyPatchAsync(context, latest, latest.Patch, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hypergryph game patch failed. Falling back to the full package.");
            string downloadDirectory = HypergryphInstallMetadata.GetDownloadsDirectory(context.InstallPath);
            DeleteDownloadedFiles(latest.Patch!.DownloadParts.Select(x => Path.Combine(downloadDirectory, x.GetFileName())));
            ResetProgress(context);
            await ExecuteFullPackageAsync(context, latest, cancellationToken);
        }
    }

    private async Task ExecutePredownloadAsync(
        GameInstallContext context,
        HypergryphLatestGame latest,
        HypergryphInstallMetadata? metadata,
        CancellationToken cancellationToken)
    {
        HypergryphGamePatch patch = latest.PrePatch
            ?? throw new InvalidOperationException("There is no Hypergryph pre-download package currently available.");
        IReadOnlyList<HypergryphPackagePart> parts = patch.DownloadParts;
        if (parts.Count == 0)
        {
            throw new InvalidOperationException("The Hypergryph pre-download package is empty.");
        }

        context.DownloadMode = GameInstallDownloadMode.CompressedPackage;
        await DownloadPartsAsync(context, parts, cancellationToken);
        metadata ??= new HypergryphInstallMetadata
        {
            AppCode = HypergryphGameConstants.GetGameProfile(context.GameId.GameBiz).GameAppCode,
            Version = context.LocalGameVersion ?? "",
        };
        metadata.PredownloadFingerprint = patch.GetFingerprint();
        await metadata.WriteAsync(context.InstallPath, cancellationToken);
    }

    private async Task ExecuteFullPackageAsync(GameInstallContext context, HypergryphLatestGame latest, CancellationToken cancellationToken)
    {
        IReadOnlyList<HypergryphPackagePart> parts = latest.Package.Packs;
        if (parts.Count == 0)
        {
            throw new InvalidDataException("The Hypergryph full package is empty.");
        }

        context.DownloadMode = GameInstallDownloadMode.CompressedPackage;
        IReadOnlyList<string> files = await DownloadPartsAsync(context, parts, cancellationToken);
        GameInstallFile package = CreateCompressedPackage(context.InstallPath, parts, latest.Package.TotalSize);

        await BreakHardLinksAsync(context, cancellationToken);
        context.State = GameInstallState.Decompressing;
        context.Progress_WriteTotalBytes = latest.Package.TotalSize;
        context.Progress_WriteFinishBytes = 0;
        context.Progress_Percent = 0;
        await _gameInstallHelper.ExtractCompressedPackageAsync(context, package, 1, cancellationToken);

        await InstallLauncherManifestsAsync(context.InstallPath, latest, null, cancellationToken);
        await VerifyInstalledGameAsync(context, latest, cancellationToken);
        context.Progress_WriteFinishBytes = context.Progress_WriteTotalBytes;
        await WriteInstalledMetadataAsync(context, latest, cancellationToken);
        DeleteDownloadedFiles(files);
    }

    private async Task ExecuteLegacyPatchAsync(
        GameInstallContext context,
        HypergryphLatestGame latest,
        HypergryphGamePatch patch,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<HypergryphPackagePart> parts = patch.DownloadParts;
        context.DownloadMode = GameInstallDownloadMode.CompressedPackage | GameInstallDownloadMode.Patch;
        IReadOnlyList<string> files = await DownloadPartsAsync(context, parts, cancellationToken);
        GameInstallFile package = CreateCompressedPackage(context.InstallPath, parts, patch.TotalSize);

        await BreakHardLinksAsync(context, cancellationToken);
        context.State = GameInstallState.Decompressing;
        context.Progress_WriteTotalBytes = patch.TotalSize;
        context.Progress_WriteFinishBytes = 0;
        context.Progress_Percent = 0;
        await _gameInstallHelper.ExtractCompressedPackageAsync(context, package, 1, cancellationToken);
        await DeleteListedFilesAsync(context.InstallPath, cancellationToken);
        await InstallLauncherManifestsAsync(context.InstallPath, latest, null, cancellationToken);
        await VerifyInstalledGameAsync(context, latest, cancellationToken);
        context.Progress_WriteFinishBytes = context.Progress_WriteTotalBytes;
        await WriteInstalledMetadataAsync(context, latest, cancellationToken);
        DeleteDownloadedFiles(files);
    }

    private async Task ExecuteV2PatchAsync(
        GameInstallContext context,
        HypergryphLatestGame latest,
        HypergryphGamePatch patch,
        CancellationToken cancellationToken)
    {
        context.DownloadMode = GameInstallDownloadMode.CompressedPackage | GameInstallDownloadMode.Patch;
        IReadOnlyList<string> files = await DownloadPartsAsync(context, patch.Patches, cancellationToken);
        GameInstallFile package = CreateCompressedPackage(context.InstallPath, patch.Patches, patch.TotalSize);
        string stagingPath = Path.Combine(HypergryphInstallMetadata.GetMetadataDirectory(context.InstallPath), "patch-staging");

        RecreateDirectory(stagingPath);
        try
        {
            context.State = GameInstallState.Decompressing;
            context.Progress_WriteTotalBytes = patch.TotalSize;
            context.Progress_WriteFinishBytes = 0;
            context.Progress_Percent = 0;
            await _gameInstallHelper.ExtractCompressedPackageAsync(
                context,
                package,
                0.45,
                cancellationToken,
                patch.CDKey,
                stagingPath,
                applyPackageDiff: false);

            HypergryphPatchManifest manifest = await ReadV2PatchManifestAsync(stagingPath, patch, cancellationToken);
            HypergryphV2VerifyManifest verifyManifest = await ReadV2VerifyManifestAsync(stagingPath, cancellationToken);
            context.State = GameInstallState.Merging;
            await ApplyV2ManifestAsync(context, stagingPath, manifest, cancellationToken);
            MoveRootOverlay(stagingPath, context.InstallPath);
            await DeleteListedFilesAsync(stagingPath, context.InstallPath, cancellationToken);
            File.Delete(GetSafePath(context.InstallPath, "verify_files.json"));
            await InstallLauncherManifestsAsync(context.InstallPath, latest, patch, cancellationToken);
            await VerifyV2FilesAsync(context, verifyManifest, cancellationToken);
            await VerifyInstalledGameAsync(context, latest, cancellationToken);
            context.Progress_Percent = 1;
            context.Progress_WriteFinishBytes = context.Progress_WriteTotalBytes;
            await WriteInstalledMetadataAsync(context, latest, cancellationToken);
            DeleteDownloadedFiles(files);
        }
        finally
        {
            if (Directory.Exists(stagingPath))
            {
                Directory.Delete(stagingPath, true);
            }
        }
    }

    private async Task ApplyV2ManifestAsync(
        GameInstallContext context,
        string stagingPath,
        HypergryphPatchManifest manifest,
        CancellationToken cancellationToken)
    {
        string vfsRoot = GetSafePath(context.InstallPath, manifest.VfsBasePath);
        List<string> obsoleteFiles = [];
        int processed = 0;

        foreach (HypergryphPatchFile item in manifest.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string relativeTarget = Path.Combine(item.NamePath, item.Name);
            string target = GetSafePath(vfsRoot, relativeTarget);

            if (!await MatchesAsync(context, target, item.Size, item.MD5, cancellationToken))
            {
                if (!string.IsNullOrWhiteSpace(item.LocalPath))
                {
                    string source = GetSafePath(stagingPath, item.LocalPath);
                    await ReplaceFileAsync(source, target, item.Size, item.MD5, context, cancellationToken);
                }
                else
                {
                    HypergryphPatchDiff? diff = await FindApplicablePatchAsync(context, vfsRoot, item.Patches, cancellationToken);
                    if (diff is null)
                    {
                        throw new InvalidDataException($"No applicable Endfield patch was found for {relativeTarget}.");
                    }
                    string source = GetSafePath(vfsRoot, Path.Combine(diff.BaseFilePath, diff.BaseFile));
                    string diffPath = GetSafePath(stagingPath, Path.Combine("vfs_files", "vfs_patch", diff.PatchPath, diff.Patch));
                    await PatchFileAsync(context, source, diffPath, target, item.Size, item.MD5, cancellationToken);
                    if (!string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
                    {
                        obsoleteFiles.Add(source);
                    }
                }
            }

            processed++;
            context.Progress_Percent = 0.45 + 0.5 * processed / Math.Max(1, manifest.Files.Count);
        }

        foreach (string path in obsoleteFiles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private async Task<HypergryphPatchDiff?> FindApplicablePatchAsync(
        GameInstallContext context,
        string vfsRoot,
        IEnumerable<HypergryphPatchDiff> patches,
        CancellationToken cancellationToken)
    {
        foreach (HypergryphPatchDiff patch in patches)
        {
            string source = GetSafePath(vfsRoot, Path.Combine(patch.BaseFilePath, patch.BaseFile));
            if (await MatchesAsync(context, source, patch.BaseSize, patch.BaseMD5, cancellationToken))
            {
                return patch;
            }
        }
        return null;
    }

    private async Task PatchFileAsync(
        GameInstallContext context,
        string source,
        string diff,
        string target,
        long size,
        string md5,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(diff))
        {
            throw new FileNotFoundException("The Endfield HDiff file is missing.", diff);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        string temporaryPath = target + ".starward_tmp";
        try
        {
            await using FileStream sourceStream = File.OpenRead(source);
            await using FileStream diffStream = File.OpenRead(diff);
            await using (FileStream targetStream = File.Create(temporaryPath))
            {
                if (!HPatch.PatchZstandard(sourceStream, diffStream, targetStream))
                {
                    throw new InvalidDataException($"Failed to apply Endfield HDiff patch to {target}.");
                }
            }
            if (!await MatchesAsync(context, temporaryPath, size, md5, cancellationToken))
            {
                throw new InvalidDataException($"The patched Endfield file does not match: {target}.");
            }
            File.Move(temporaryPath, target, true);
            Interlocked.Add(ref context.storageWriteBytes, size);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private async Task ReplaceFileAsync(
        string source,
        string target,
        long size,
        string md5,
        GameInstallContext context,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(source))
        {
            throw new FileNotFoundException("The Endfield replacement file is missing.", source);
        }
        if (!await MatchesAsync(context, source, size, md5, cancellationToken))
        {
            throw new InvalidDataException($"The Endfield replacement file does not match: {source}.");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Move(source, target, true);
    }

    private async Task<bool> MatchesAsync(GameInstallContext context, string path, long size, string md5, CancellationToken cancellationToken)
    {
        return await _gameInstallHelper.CheckFileMD5Async(context, path, size, md5, cancellationToken);
    }

    private async Task<IReadOnlyList<string>> DownloadPartsAsync(
        GameInstallContext context,
        IReadOnlyList<HypergryphPackagePart> parts,
        CancellationToken cancellationToken)
    {
        string directory = HypergryphInstallMetadata.GetDownloadsDirectory(context.InstallPath);
        Directory.CreateDirectory(directory);
        context.State = GameInstallState.Downloading;
        context.Progress_DownloadTotalBytes = parts.Sum(x => x.PackageSize);
        context.Progress_DownloadFinishBytes = 0;
        string[] files = parts.Select(x => Path.Combine(directory, x.GetFileName())).ToArray();

        await Parallel.ForEachAsync(
            parts,
            new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = DownloadConcurrency },
            async (part, token) =>
            {
                string path = Path.Combine(directory, part.GetFileName());
                Exception? lastException = null;
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    try
                    {
                        await _gameInstallHelper.DownloadToFileAsync(context, path, part.Url, part.PackageSize, part.MD5, token);
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        if (attempt < 3)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(attempt + 1), token);
                        }
                    }
                }
                throw lastException ?? new IOException($"Failed to download {part.Url}.");
            });
        return files;
    }

    private static GameInstallFile CreateCompressedPackage(
        string installPath,
        IReadOnlyList<HypergryphPackagePart> parts,
        long decompressedSize)
    {
        string directory = HypergryphInstallMetadata.GetDownloadsDirectory(installPath);
        return new GameInstallFile
        {
            DownloadMode = GameInstallDownloadMode.CompressedPackage,
            Size = parts.Sum(x => x.PackageSize),
            CompressedPackages = parts.Select(x => new GameInstallCompressedPackage
            {
                FullPath = Path.Combine(directory, x.GetFileName()),
                Url = x.Url,
                MD5 = x.MD5,
                Size = x.PackageSize,
                DecompressedSize = decompressedSize,
            }).ToList(),
        };
    }

    private async Task<bool> IsLatestOfficialInstallAsync(
        GameInstallContext context,
        HypergryphLatestGame latest,
        CancellationToken cancellationToken)
    {
        string path = Path.Combine(context.InstallPath, "game_files");
        if (!File.Exists(path) || string.IsNullOrWhiteSpace(latest.Package.GameFilesMD5))
        {
            return false;
        }
        await using FileStream stream = File.OpenRead(path);
        string md5 = Convert.ToHexStringLower(await MD5.HashDataAsync(stream, cancellationToken));
        return string.Equals(md5, latest.Package.GameFilesMD5, StringComparison.OrdinalIgnoreCase);
    }

    private async Task InstallLauncherManifestsAsync(
        string installPath,
        HypergryphLatestGame latest,
        HypergryphGamePatch? v2Patch,
        CancellationToken cancellationToken)
    {
        byte[] gameFiles = await _launcherClient.GetGameFilesAsync(latest.Package, cancellationToken);
        await ReplaceManifestAsync(Path.Combine(installPath, "game_files"), gameFiles, cancellationToken);

        if (v2Patch is not null && !string.IsNullOrWhiteSpace(v2Patch.V2VerifyFilesUrl))
        {
            byte[] verifyFiles = await _launcherClient.GetV2VerifyFilesAsync(v2Patch, cancellationToken);
            await ReplaceManifestAsync(Path.Combine(installPath, "verify_files"), verifyFiles, cancellationToken);
        }
    }

    private static async Task ReplaceManifestAsync(string path, byte[] bytes, CancellationToken cancellationToken)
    {
        string temporaryPath = path + ".starward_tmp";
        try
        {
            await File.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken);
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private async Task VerifyInstalledGameAsync(
        GameInstallContext context,
        HypergryphLatestGame latest,
        CancellationToken cancellationToken)
    {
        context.State = GameInstallState.Verifying;
        HypergryphGameProfile profile = HypergryphGameConstants.GetGameProfile(context.GameId.GameBiz);
        string exe = Path.Combine(context.InstallPath, profile.ExeName);
        if (!File.Exists(exe))
        {
            throw new FileNotFoundException($"{profile.ExeName} was not found after installation.", exe);
        }
        if (!await IsLatestOfficialInstallAsync(context, latest, cancellationToken))
        {
            throw new InvalidDataException("The installed Hypergryph game_files manifest does not match the latest package.");
        }
    }

    private static async Task WriteInstalledMetadataAsync(
        GameInstallContext context,
        HypergryphLatestGame latest,
        CancellationToken cancellationToken)
    {
        await new HypergryphInstallMetadata
        {
            AppCode = HypergryphGameConstants.GetGameProfile(context.GameId.GameBiz).GameAppCode,
            Version = latest.Version,
            GameFilesMD5 = latest.Package.GameFilesMD5,
            PredownloadFingerprint = "",
        }.WriteAsync(context.InstallPath, cancellationToken);
    }

    private static void MoveRootOverlay(string stagingPath, string installPath)
    {
        string vfsFiles = Path.Combine(stagingPath, "vfs_files");
        foreach (string file in Directory.EnumerateFiles(stagingPath, "*", SearchOption.AllDirectories).ToArray())
        {
            if (IsWithin(file, vfsFiles))
            {
                continue;
            }
            string relative = Path.GetRelativePath(stagingPath, file);
            if (V2ControlFiles.Contains(relative))
            {
                continue;
            }
            string target = GetSafePath(installPath, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Move(file, target, true);
        }
    }


    private static async Task<HypergryphV2VerifyManifest> ReadV2VerifyManifestAsync(string stagingPath, CancellationToken cancellationToken)
    {
        string path = GetSafePath(stagingPath, "verify_files.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The Hypergryph V2 verify manifest is missing.", path);
        }
        await using FileStream stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<HypergryphV2VerifyManifest>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidDataException("The Hypergryph V2 verify manifest is empty.");
    }


    private static async Task<HypergryphPatchManifest> ReadV2PatchManifestAsync(
        string stagingPath,
        HypergryphGamePatch patch,
        CancellationToken cancellationToken)
    {
        string path = GetSafePath(stagingPath, "patch.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The Hypergryph V2 patch manifest is missing.", path);
        }
        byte[] bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        if (patch.V2PatchInfoSize > 0 && bytes.LongLength != patch.V2PatchInfoSize)
        {
            throw new InvalidDataException("The Hypergryph V2 patch manifest size does not match.");
        }
        if (!string.IsNullOrWhiteSpace(patch.V2PatchInfoMD5))
        {
            string md5 = Convert.ToHexStringLower(MD5.HashData(bytes));
            if (!string.Equals(md5, patch.V2PatchInfoMD5, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The Hypergryph V2 patch manifest MD5 does not match.");
            }
        }
        return JsonSerializer.Deserialize<HypergryphPatchManifest>(bytes)
            ?? throw new InvalidDataException("The Hypergryph V2 patch manifest is empty.");
    }


    private async Task VerifyV2FilesAsync(
        GameInstallContext context,
        HypergryphV2VerifyManifest manifest,
        CancellationToken cancellationToken)
    {
        foreach (HypergryphVerifyFile file in manifest.Move.Concat(manifest.Patch))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(file.Path)
                || file.Size < 0
                || string.IsNullOrWhiteSpace(file.MD5))
            {
                throw new InvalidDataException("The Hypergryph V2 verify manifest contains an invalid file.");
            }
            string path = GetSafePath(context.InstallPath, file.Path);
            if (!await MatchesAsync(context, path, file.Size, file.MD5, cancellationToken))
            {
                throw new InvalidDataException($"The verified Endfield file does not match: {file.Path}.");
            }
        }
    }


    private async Task BreakHardLinksAsync(GameInstallContext context, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(context.InstallPath))
        {
            return;
        }

        string metadataRoot = HypergryphInstallMetadata.GetMetadataDirectory(context.InstallPath);
        string[] files = Directory
            .EnumerateFiles(context.InstallPath, "*", SearchOption.AllDirectories)
            .Where(x => !IsWithin(x, metadataRoot))
            .ToArray();
        int unlinkedFiles = 0;
        long unlinkedBytes = 0;
        context.State = GameInstallState.Merging;
        await Parallel.ForEachAsync(
            files,
            new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = HardLinkConcurrency },
            async (file, token) =>
            {
                if (!TryGetFileInformation(file, out Kernel32.BY_HANDLE_FILE_INFORMATION info) || info.nNumberOfLinks <= 1)
                {
                    return;
                }

                string temporaryPath = file + ".starward_unlink";
                try
                {
                    File.Delete(temporaryPath);
                    await using (FileStream source = File.OpenRead(file))
                    await using (FileStream target = File.Create(temporaryPath))
                    {
                        await source.CopyToAsync(target, token);
                    }
                    File.Move(temporaryPath, file, true);
                    Interlocked.Increment(ref unlinkedFiles);
                    Interlocked.Add(ref unlinkedBytes, new FileInfo(file).Length);
                }
                finally
                {
                    File.Delete(temporaryPath);
                }
            });
        if (unlinkedFiles > 0)
        {
            _logger.LogInformation(
                "Unlinked {Count} Hypergryph game files ({Bytes} bytes) before package extraction.",
                unlinkedFiles,
                unlinkedBytes);
        }
    }


    private async Task HardLinkMatchingVfsFilesAsync(GameInstallContext context, CancellationToken cancellationToken)
    {
        if (!CanUseHardLink(context))
        {
            return;
        }

        string relativeVfsPath = Path.Combine("Endfield_Data", "StreamingAssets", "VFS");
        string sourceRoot = GetSafePath(context.HardLinkPath!, relativeVfsPath);
        string targetRoot = GetSafePath(context.InstallPath, relativeVfsPath);
        if (!Directory.Exists(sourceRoot) || !Directory.Exists(targetRoot))
        {
            return;
        }

        int linkedFiles = 0;
        long linkedBytes = 0;
        context.State = GameInstallState.Merging;
        await Parallel.ForEachAsync(
            Directory.EnumerateFiles(targetRoot, "*", SearchOption.AllDirectories).ToArray(),
            new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = HardLinkConcurrency },
            async (target, token) =>
            {
                string relative = Path.GetRelativePath(targetRoot, target);
                string source = GetSafePath(sourceRoot, relative);
                if (AreSameFile(source, target) || !await FilesMatchAsync(context, source, target, token))
                {
                    return;
                }

                string temporaryPath = target + ".starward_link";
                try
                {
                    File.Delete(temporaryPath);
                    if (Kernel32.CreateHardLink(temporaryPath, source))
                    {
                        File.Move(temporaryPath, target, true);
                        Interlocked.Increment(ref linkedFiles);
                        Interlocked.Add(ref linkedBytes, new FileInfo(source).Length);
                    }
                }
                finally
                {
                    File.Delete(temporaryPath);
                }
            });
        _logger.LogInformation(
            "Hard linked {Count} Endfield VFS files ({Bytes} bytes) from {SourcePath}.",
            linkedFiles,
            linkedBytes,
            context.HardLinkPath);
    }


    private static bool CanUseHardLink(GameInstallContext context)
    {
        return Directory.Exists(context.HardLinkPath)
            && !string.Equals(Path.GetFullPath(context.InstallPath), Path.GetFullPath(context.HardLinkPath!), StringComparison.OrdinalIgnoreCase)
            && !IsWithin(context.InstallPath, context.HardLinkPath!)
            && !IsWithin(context.HardLinkPath!, context.InstallPath)
            && string.Equals(Path.GetPathRoot(context.InstallPath), Path.GetPathRoot(context.HardLinkPath), StringComparison.OrdinalIgnoreCase)
            && string.Equals(DriveHelper.GetDriveFormat(context.InstallPath), "NTFS", StringComparison.OrdinalIgnoreCase);
    }


    private static bool AreSameFile(string first, string second)
    {
        return TryGetFileInformation(first, out Kernel32.BY_HANDLE_FILE_INFORMATION firstInfo)
            && TryGetFileInformation(second, out Kernel32.BY_HANDLE_FILE_INFORMATION secondInfo)
            && firstInfo.dwVolumeSerialNumber == secondInfo.dwVolumeSerialNumber
            && firstInfo.nFileIndexHigh == secondInfo.nFileIndexHigh
            && firstInfo.nFileIndexLow == secondInfo.nFileIndexLow;
    }


    private static bool TryGetFileInformation(string path, out Kernel32.BY_HANDLE_FILE_INFORMATION info)
    {
        using Kernel32.SafeHFILE handle = Kernel32.CreateFile(
            path,
            0,
            FileShare.ReadWrite | FileShare.Delete,
            null,
            FileMode.Open,
            0,
            HFILE.NULL);
        if (handle.IsInvalid)
        {
            info = default;
            return false;
        }
        return Kernel32.GetFileInformationByHandle(handle, out info);
    }


    private static async Task<bool> FilesMatchAsync(
        GameInstallContext context,
        string source,
        string target,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(source) || !File.Exists(target))
        {
            return false;
        }
        FileInfo sourceInfo = new(source);
        FileInfo targetInfo = new(target);
        if (sourceInfo.Length != targetInfo.Length)
        {
            return false;
        }

        await using FileStream sourceStream = File.OpenRead(source);
        await using FileStream targetStream = File.OpenRead(target);
        byte[] sourceHash = await MD5.HashDataAsync(sourceStream, cancellationToken);
        byte[] targetHash = await MD5.HashDataAsync(targetStream, cancellationToken);
        Interlocked.Add(ref context.storageReadBytes, sourceInfo.Length + targetInfo.Length);
        return sourceHash.AsSpan().SequenceEqual(targetHash);
    }

    private static async Task DeleteListedFilesAsync(string installPath, CancellationToken cancellationToken)
    {
        await DeleteListedFilesAsync(installPath, installPath, cancellationToken);
    }

    private static async Task DeleteListedFilesAsync(string listRoot, string installPath, CancellationToken cancellationToken)
    {
        foreach (string name in new[] { "delete_files.txt", "deletefiles.txt" })
        {
            string path = Path.Combine(listRoot, name);
            if (!File.Exists(path))
            {
                continue;
            }
            string text = await File.ReadAllTextAsync(path, cancellationToken);
            IEnumerable<string> files;
            try
            {
                files = JsonSerializer.Deserialize<List<string>>(text) ?? [];
            }
            catch (JsonException)
            {
                files = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
            foreach (string file in files)
            {
                File.Delete(GetSafePath(installPath, file));
            }
            if (string.Equals(listRoot, installPath, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(path);
            }
        }
    }

    private static string GetSafePath(string root, string relativePath)
    {
        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"The Hypergryph package contains an invalid path: {relativePath}.");
        }
        return fullPath;
    }

    private static bool IsWithin(string path, string directory)
    {
        string fullDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static void RecreateDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
        Directory.CreateDirectory(path);
    }

    private static void DeleteDownloadedFiles(IEnumerable<string> files)
    {
        foreach (string file in files)
        {
            File.Delete(file);
            File.Delete(file + "_tmp");
        }
    }

    private static void ResetProgress(GameInstallContext context)
    {
        context.Progress_DownloadTotalBytes = 0;
        context.Progress_DownloadFinishBytes = 0;
        context.Progress_ReadTotalBytes = 0;
        context.Progress_ReadFinishBytes = 0;
        context.Progress_WriteTotalBytes = 0;
        context.Progress_WriteFinishBytes = 0;
        context.Progress_Percent = 0;
    }
}
