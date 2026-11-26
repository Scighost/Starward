using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Starward.GameInstall.Service;
using Starward.Core.Hypergryph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace Starward.GameInstall.Step;

internal sealed class HypergryphInstallStep : PackageStepBase
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

    private readonly HypergryphLauncherClient _launcherClient;
    private string? _localVersion;

    public HypergryphInstallStep(GameInstallContext context, IServiceProvider services) : base(context, services)
    {
        _launcherClient = new HypergryphLauncherClient(services.GetRequiredService<HttpClient>());
    }

    protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
    {
        GameInstallContext context = _context;
        bool isEndfield = HypergryphGameConstants.IsEndfield(context.GameId.GameBiz);
        bool hasDonor = isEndfield && CanUseHardLink(context)
            && File.Exists(Path.Combine(context.HardLinkPath!, HypergryphGameConstants.EndfieldExeName))
            && File.Exists(Path.Combine(context.HardLinkPath!, "game_files"));
        bool useGameFiles = isEndfield && (context.Operation is GameInstallOperation.Repair
            || (context.Operation is GameInstallOperation.Install && hasDonor));
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
            if (localVersion == latest.Version && !useGameFiles)
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

        _localVersion = localVersion;

        if (useGameFiles)
        {
            await ExecuteGameFilesAsync(context, latest, hasDonor ? context.HardLinkPath : null, cancellationToken);
            return;
        }

        if (hasDonor && context.Operation is GameInstallOperation.Update)
        {
            string downloadDirectory = HypergryphInstallMetadata.GetDownloadsDirectory(context.InstallPath);
            IReadOnlyList<HypergryphPackagePart> parts = latest.Patch is { DownloadParts.Count: > 0 } patch
                ? patch.DownloadParts : latest.Package.Packs;
            long downloadLimit = parts.Count == 0 ? long.MaxValue : parts.Sum(part =>
                GetRemainingDownloadBytes(Path.Combine(downloadDirectory, part.GetFileName()), part.PackageSize));
            if (downloadLimit > 0 && await ExecuteGameFilesAsync(context, latest, context.HardLinkPath, cancellationToken, downloadLimit))
            {
                return;
            }
            ResetProgress(context);
        }

        if (context.Operation is not GameInstallOperation.Predownload)
        {
            foreach (string file in Directory.EnumerateFiles(context.InstallPath, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                File.SetAttributes(file, FileAttributes.Normal);
            }
        }

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
                _stepLogger.LogWarning(ex, "Failed to hard link matching Endfield VFS files.");
            }
        }
    }

    private async Task<bool> ExecuteGameFilesAsync(
        GameInstallContext context,
        HypergryphLatestGame latest,
        string? donorPath,
        CancellationToken cancellationToken,
        long? downloadLimit = null)
    {
        ResetProgress(context);
        context.Step = RunnerStep.Verifying;
        context.ProgressDisplay = ProgressDisplay.Progress;
        byte[] encrypted;
        IReadOnlyList<HypergryphVerifyFile> files;
        string manifestPath = GetGameFilePath(context.InstallPath, "game_files");
        string[] obsolete = [];
        try
        {
            encrypted = await _launcherClient.GetGameFilesAsync(latest.Package, cancellationToken);
            files = ReadGameFiles(encrypted);
            if (downloadLimit.HasValue && File.Exists(manifestPath))
            {
                byte[] previous = await File.ReadAllBytesAsync(manifestPath, cancellationToken);
                var currentPaths = files.Select(file => file.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
                obsolete = ReadGameFiles(previous).Where(file => !currentPaths.Contains(file.Path))
                    .Select(file => GetGameFilePath(context.InstallPath, file.Path)).ToArray();
            }
        }
        catch (Exception ex) when (downloadLimit.HasValue && !cancellationToken.IsCancellationRequested)
        {
            _stepLogger.LogWarning(ex, "Could not read Endfield game file manifests. Using the official update package.");
            return false;
        }

        string stagingPath = GetGameFilePath(context.InstallPath, ".starward/downloads/game-files");
        var targets = files.Select(file => GetGameFilePath(context.InstallPath, file.Path)).ToArray();
        var links = new List<(string Staged, string Target)>();
        var pending = new List<(HypergryphVerifyFile File, string Target, string Staged)>();
        context.StorageTotalBytes = files.Sum(file => file.Size);
        long linkedBytes = 0;
        long remainingBytes = 0;
        try
        {
            for (int i = 0; i < files.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                HypergryphVerifyFile file = files[i];
                string target = targets[i];
                if (!await MatchesAsync(context, target, file.Size, file.MD5, cancellationToken))
                {
                    string? link = donorPath is null ? null
                        : await TryStageGameFileLinkAsync(context, donorPath, file, stagingPath, cancellationToken);
                    if (link is not null)
                    {
                        links.Add((link, target));
                        linkedBytes += file.Size;
                    }
                    else
                    {
                        string staged = Path.Combine(stagingPath, $"{i}-{file.MD5}");
                        pending.Add((file, target, staged));
                        if (downloadLimit is long limit)
                        {
                            remainingBytes += GetRemainingDownloadBytes(staged, file.Size);
                            if (remainingBytes >= limit)
                            {
                                _stepLogger.LogInformation("Using the Endfield update package with {Bytes} estimated download bytes remaining.", limit);
                                return false;
                            }
                        }
                    }
                }
                context.StorageFinishedBytes += file.Size;
                context.PercentProgress = (double)(i + 1) / files.Count;
            }

            if (pending.Count > 0)
            {
                context.NetworkTotalBytes = pending.Sum(item => item.File.Size);
                context.Step = RunnerStep.Downloading;
                context.ProgressDisplay = ProgressDisplay.NetworkBytes | ProgressDisplay.NetworkSpeed;
                await Parallel.ForEachAsync(
                    pending,
                    new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = DownloadConcurrency },
                    async (item, token) =>
                    {
                        string relativeUrl = string.Join('/', item.File.Path.Split('/').Select(Uri.EscapeDataString));
                        string url = $"{latest.Package.FilePath.TrimEnd('/')}/{relativeUrl}";
                        await DownloadWithRetryAsync(context, item.Staged, url, item.File.Size, item.File.MD5, token);
                    });
            }

            byte[]? verifyFiles = null;
            if (downloadLimit.HasValue && latest.Patch is { IsV2: true } v2Patch
                && !string.IsNullOrWhiteSpace(v2Patch.V2VerifyFilesUrl))
            {
                verifyFiles = await _launcherClient.GetV2VerifyFilesAsync(v2Patch, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            context.Step = RunnerStep.Verifying;
            context.ProgressDisplay = ProgressDisplay.Progress;
            foreach (var (staged, target) in links.Concat(pending.Select(item => (item.Staged, item.Target))))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Move(staged, target, true);
            }
            foreach (string path in obsolete)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            // Once files are published, finish updating their metadata even if the user pauses.
            if (verifyFiles is not null)
            {
                await ReplaceManifestAsync(GetGameFilePath(context.InstallPath, "verify_files"), verifyFiles, CancellationToken.None);
            }
            await ReplaceManifestAsync(manifestPath, encrypted, CancellationToken.None);
            await WriteInstalledMetadataAsync(context, latest, CancellationToken.None);
            context.AddHardLinkBytes(linkedBytes);
            _stepLogger.LogInformation("Hard linked {Count} Endfield files ({Bytes} bytes).", links.Count, linkedBytes);
            return true;
        }
        finally
        {
            foreach (var link in links)
            {
                File.Delete(link.Staged);
            }
        }
    }

    private static long GetRemainingDownloadBytes(string path, long size)
    {
        FileInfo complete = new(path);
        if (complete.Exists && complete.Length == size)
        {
            return 0;
        }
        FileInfo partial = new(path + "_tmp");
        return partial.Exists && partial.Length <= size ? size - partial.Length : size;
    }

    private async Task<string?> TryStageGameFileLinkAsync(
        GameInstallContext context,
        string donorPath,
        HypergryphVerifyFile file,
        string stagingPath,
        CancellationToken cancellationToken)
    {
        string temporaryPath = Path.Combine(stagingPath, Guid.NewGuid().ToString("N") + ".link");
        bool verified = false;
        try
        {
            string source = GetGameFilePath(donorPath, file.Path);
            if (!File.Exists(source) || new FileInfo(source).Length != file.Size)
            {
                return null;
            }
            Directory.CreateDirectory(stagingPath);
            if (!Kernel32.CreateHardLink(temporaryPath, source))
            {
                return null;
            }
            verified = await MatchesAsync(context, temporaryPath, file.Size, file.MD5, cancellationToken);
            return verified ? temporaryPath : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _stepLogger.LogDebug(ex, "Could not hard link Endfield file {Path}.", file.Path);
            return null;
        }
        finally
        {
            if (!verified && File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static IReadOnlyList<HypergryphVerifyFile> ReadGameFiles(byte[] encrypted)
    {
        // The launcher uses the same AES-CBC format for both Endfield regions.
        using Aes aes = Aes.Create();
        aes.Key = Convert.FromHexString("C0F30E1CE763BBC21CC355A34303AC50399444BFF68C4A22AF398C0A166EE143");
        byte[] decrypted = aes.DecryptCbc(encrypted, Convert.FromHexString("33467861192750649501937264608400"));
        using StreamReader reader = new(new MemoryStream(decrypted), Encoding.UTF8);
        var files = new List<HypergryphVerifyFile>();
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.ReadLine() is string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            HypergryphVerifyFile file = JsonSerializer.Deserialize<HypergryphVerifyFile>(line)
                ?? throw new InvalidDataException("The Endfield game_files manifest contains an empty entry.");
            file.Path = file.Path?.Replace('\\', '/') ?? "";
            string[] segments = file.Path.Split('/');
            if (file.Size < 0 || file.MD5 is not { Length: 32 } || !file.MD5.All(Uri.IsHexDigit)
                || segments.Any(x => x.Length == 0 || x.EndsWith('.') || x.EndsWith(' ') || x.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                || segments[0].Equals(".starward", StringComparison.OrdinalIgnoreCase)
                || file.Path.Equals("game_files", StringComparison.OrdinalIgnoreCase)
                || !paths.Add(file.Path))
            {
                throw new InvalidDataException($"The Endfield game_files manifest contains an invalid entry: {file.Path}.");
            }
            files.Add(file);
        }
        if (!paths.Contains(HypergryphGameConstants.EndfieldExeName))
        {
            throw new InvalidDataException("The Endfield game_files manifest does not include the game executable.");
        }
        return files;
    }

    private static string GetGameFilePath(string root, string relativePath)
    {
        string path = GetSafePath(root, relativePath);
        string directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        for (string? item = path; item is not null; item = Path.GetDirectoryName(item))
        {
            if ((File.Exists(item) || Directory.Exists(item)) && File.GetAttributes(item).HasFlag(FileAttributes.ReparsePoint))
            {
                throw new IOException($"The Endfield file path contains a symbolic link or junction: {item}.");
            }
            if (string.Equals(item, directory, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }
        return path;
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
            _stepLogger.LogWarning(ex, "Hypergryph game patch failed. Falling back to the full package.");
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

        await DownloadPartsAsync(context, parts, cancellationToken);
        metadata ??= new HypergryphInstallMetadata
        {
            AppCode = HypergryphGameConstants.GetGameProfile(context.GameId.GameBiz).GameAppCode,
            Version = _localVersion ?? "",
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

        IReadOnlyList<string> files = await DownloadPartsAsync(context, parts, cancellationToken);

        await BreakHardLinksAsync(context, cancellationToken);
        context.Step = RunnerStep.Decompressing;
        context.ProgressDisplay = ProgressDisplay.Progress;
        context.StorageTotalBytes = latest.Package.TotalSize;
        context.StorageFinishedBytes = 0;
        context.PercentProgress = 0;
        await ExtractPackagesAsync(files, context.InstallPath, null, 0.5, cancellationToken);
        await PatchDiffFilesAsync(0.5, cancellationToken);

        await InstallLauncherManifestsAsync(context.InstallPath, latest, null, cancellationToken);
        await VerifyInstalledGameAsync(context, latest, cancellationToken);
        context.StorageFinishedBytes = context.StorageTotalBytes;
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
        IReadOnlyList<string> files = await DownloadPartsAsync(context, parts, cancellationToken);

        await BreakHardLinksAsync(context, cancellationToken);
        context.Step = RunnerStep.Decompressing;
        context.ProgressDisplay = ProgressDisplay.Progress;
        context.StorageTotalBytes = patch.TotalSize;
        context.StorageFinishedBytes = 0;
        context.PercentProgress = 0;
        await ExtractPackagesAsync(files, context.InstallPath, null, 0.5, cancellationToken);
        await PatchDiffFilesAsync(0.5, cancellationToken);
        await DeleteListedFilesAsync(context.InstallPath, cancellationToken);
        await InstallLauncherManifestsAsync(context.InstallPath, latest, null, cancellationToken);
        await VerifyInstalledGameAsync(context, latest, cancellationToken);
        context.StorageFinishedBytes = context.StorageTotalBytes;
        await WriteInstalledMetadataAsync(context, latest, cancellationToken);
        DeleteDownloadedFiles(files);
    }

    private async Task ExecuteV2PatchAsync(
        GameInstallContext context,
        HypergryphLatestGame latest,
        HypergryphGamePatch patch,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string> files = await DownloadPartsAsync(context, patch.Patches, cancellationToken);
        string stagingPath = Path.Combine(HypergryphInstallMetadata.GetMetadataDirectory(context.InstallPath), "patch-staging");

        RecreateDirectory(stagingPath);
        try
        {
            context.Step = RunnerStep.Decompressing;
            context.ProgressDisplay = ProgressDisplay.Progress;
            context.StorageTotalBytes = patch.TotalSize;
            context.StorageFinishedBytes = 0;
            context.PercentProgress = 0;
            await ExtractPackagesAsync(files, stagingPath, patch.CDKey, 0.45, cancellationToken);

            HypergryphPatchManifest manifest = await ReadV2PatchManifestAsync(stagingPath, patch, cancellationToken);
            HypergryphV2VerifyManifest verifyManifest = await ReadV2VerifyManifestAsync(stagingPath, cancellationToken);
            context.Step = RunnerStep.Patching;
            context.ProgressDisplay = ProgressDisplay.Progress;
            await ApplyV2ManifestAsync(context, stagingPath, manifest, cancellationToken);
            MoveRootOverlay(stagingPath, context.InstallPath);
            await DeleteListedFilesAsync(stagingPath, context.InstallPath, cancellationToken);
            File.Delete(GetSafePath(context.InstallPath, "verify_files.json"));
            await InstallLauncherManifestsAsync(context.InstallPath, latest, patch, cancellationToken);
            await VerifyV2FilesAsync(context, verifyManifest, cancellationToken);
            await VerifyInstalledGameAsync(context, latest, cancellationToken);
            context.PercentProgress = 1;
            context.StorageFinishedBytes = context.StorageTotalBytes;
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
            context.PercentProgress = 0.45 + 0.5 * processed / Math.Max(1, manifest.Files.Count);
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
                await HPatch.HPatch.PatchAsync(sourceStream, diffStream, targetStream, cancellationToken: cancellationToken);
            }
            if (!await MatchesAsync(context, temporaryPath, size, md5, cancellationToken))
            {
                throw new InvalidDataException($"The patched Endfield file does not match: {target}.");
            }
            File.Move(temporaryPath, target, true);
            context.AddStorageBytes(size);
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
        return await HashService.CheckFileMD5Async(path, size, md5, cancellationToken);
    }

    private async Task<IReadOnlyList<string>> DownloadPartsAsync(
        GameInstallContext context,
        IReadOnlyList<HypergryphPackagePart> parts,
        CancellationToken cancellationToken)
    {
        string directory = HypergryphInstallMetadata.GetDownloadsDirectory(context.InstallPath);
        Directory.CreateDirectory(directory);
        context.Step = RunnerStep.Downloading;
        context.ProgressDisplay = ProgressDisplay.NetworkBytes | ProgressDisplay.NetworkSpeed;
        context.NetworkTotalBytes = parts.Sum(x => x.PackageSize);
        context.NetworkFinishedBytes = 0;
        string[] files = parts.Select(x => Path.Combine(directory, x.GetFileName())).ToArray();

        await Parallel.ForEachAsync(
            parts,
            new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = DownloadConcurrency },
            (part, token) => new ValueTask(DownloadWithRetryAsync(
                context, Path.Combine(directory, part.GetFileName()), part.Url, part.PackageSize, part.MD5, token)));
        return files;
    }

    private async Task DownloadWithRetryAsync(
        GameInstallContext context, string path, string url, long size, string md5, CancellationToken cancellationToken)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                if (await HashService.CheckFileMD5Async(path, size, md5, cancellationToken))
                {
                    context.AddNetworkBytes(size);
                    return;
                }
                string temporaryPath = path + "_tmp";
                await _downloadService.DownloadToFileAsync(temporaryPath, url, size, context, cancellationToken);
                if (!await HashService.CheckFileMD5Async(temporaryPath, size, md5, cancellationToken))
                {
                    File.Delete(temporaryPath);
                    context.AddNetworkBytes(-size);
                    throw new FileMD5FailedException(temporaryPath, md5);
                }
                File.Move(temporaryPath, path, true);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException && attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt + 1), cancellationToken);
            }
        }
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
        context.Step = RunnerStep.Verifying;
        context.ProgressDisplay = ProgressDisplay.Progress;
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
        context.Step = RunnerStep.Patching;
        context.ProgressDisplay = ProgressDisplay.Progress;
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
            _stepLogger.LogInformation(
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
        context.Step = RunnerStep.Patching;
        context.ProgressDisplay = ProgressDisplay.Progress;
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
        _stepLogger.LogInformation(
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
            && string.Equals(new DriveInfo(context.InstallPath).DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase);
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
        context.ClearProgress();
    }
}
