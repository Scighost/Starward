using SharpCompress.Compressors.ZStandard;
using Starward.Setup.Core;
using Starward.Setup.Snap.HPatch;
using System.Net;

namespace Starward.Setup.Services;

public class UpdateService : DownloadService
{

    public ReleaseManifest ReleaseManifest { get; set; }


    public string OldVersion { get; private set; }

    public string NewVersion { get; private set; }




    public async Task UpdateAsync(string installFolder, string? oldVersion, string? newVersion, bool preview = false, CancellationToken cancellation = default)
    {
        RecreateHttpClient();

        ReleaseManifest = await GetReleaseManifestAsync(oldVersion, newVersion, preview, cancellation).ConfigureAwait(false);

        OldVersion = oldVersion ?? string.Empty;
        NewVersion = ReleaseManifest.Version;

        await Task.Run(() =>
        {
            string[] files = Directory.GetFiles(installFolder, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
        }, cancellation).ConfigureAwait(false);

        await Task.Delay(1000, cancellation).ConfigureAwait(false);

        if (ReleaseManifest.DiffVersion is not null)
        {
            await DownloadDiffFilesAsync(installFolder, cancellation).ConfigureAwait(false);
        }

        await CheckFilesAsync(installFolder, cancellation).ConfigureAwait(false);

        foreach (var item in ReleaseManifest.DeleteFiles ?? [])
        {
            string path = Path.Combine(installFolder, item);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        CopySetupFile(installFolder);

        string updateFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Starward\update");
        if (Directory.Exists(updateFolder))
        {
            Directory.Delete(updateFolder, true);
        }

        RegistryHelper.WriteUninstallInfo(installFolder, ReleaseManifest.Version, ReleaseManifest.Size + new FileInfo(Environment.ProcessPath!).Length);
        RegistryHelper.WriteUrlProtocol(installFolder);

    }




    private async Task<ReleaseManifest> GetReleaseManifestAsync(string? oldVersion, string? newVersion, bool preview, CancellationToken cancellation = default)
    {
        ReleaseInfoDetail? info = null;

        if (!string.IsNullOrWhiteSpace(newVersion))
        {
            try
            {
                info = await GetReleaseInfoAsync(newVersion, cancellation).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }
        }

        info ??= await GetReleaseInfoAsync(preview, cancellation).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(oldVersion) && info.Diffs.TryGetValue(oldVersion, out var diff))
        {
            return await GetReleaseManifestAsync(diff.ManifestUrl, cancellation).ConfigureAwait(false);
        }
        else
        {
            return await GetReleaseManifestAsync(info.ManifestUrl, cancellation).ConfigureAwait(false);
        }
    }



    private async Task DownloadDiffFilesAsync(string installFolder, CancellationToken cancellation = default)
    {
        string updateFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Starward\update");
        Directory.CreateDirectory(updateFolder);

        TotalBytes = ReleaseManifest.DiffSize;
        DownloadBytes = 0;

        List<(string Id, long Size, string Hash, string? Suffix)> list = new();
        foreach (var item in ReleaseManifest.Files)
        {
            if (item.Patch is null)
            {
                list.Add((item.Id, item.CompressedSize, item.CompressedHash, item.UrlSuffix));
            }
            else if (item.Patch?.Id is not null && item.Patch?.PatchHash is not null)
            {
                list.Add((item.Patch.Id, item.Patch.PatchSize, item.Patch.PatchHash, item.Patch.UrlSuffix));
            }
        }
        list = list.DistinctBy(x => x.Id).ToList();

        await Parallel.ForEachAsync(list, cancellation, async (item, token) =>
        {
            string url = ReleaseManifest.UrlPrefix + item.Id + (item.Suffix ?? ReleaseManifest.UrlSuffix);
            string path = Path.Combine(updateFolder, item.Id);

            await RetryHelper.ExecuteAsync(async retryToken => await DownloadFileAndCheckFileHashAsync(url, path, item.Size, item.Hash, retryToken).ConfigureAwait(false), token).ConfigureAwait(false);

        }).ConfigureAwait(false);

        TotalBytes = 0;
        DownloadBytes = 0;

        await Parallel.ForEachAsync(ReleaseManifest.Files, cancellation, async (item, token) =>
        {
            string path = Path.Combine(installFolder, item.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string idPath = Path.Combine(updateFolder, item.Patch?.Id ?? item.Id);
            if (item.Patch is null && File.Exists(idPath))
            {
                using var fs = File.OpenRead(idPath);
                using var zstd = new DecompressionStream(fs);
                using var output = File.Create(path);
                await zstd.CopyToAsync(output, cancellation).ConfigureAwait(false);
            }
            else if (item.Patch?.Id is not null && File.Exists(idPath) && File.Exists(Path.Combine(installFolder, item.Patch.OldPath)))
            {
                if (await CheckFileHashAsync(path, item.Patch.OldFileHash, item.Patch.OldFileSize, cancellation).ConfigureAwait(false))
                {
                    using var fs_diff = new FileSliceStream(idPath, item.Patch.Offset, item.Patch.Length);
                    string temp = path + ".tmp";
                    using var fs_source = File.OpenRead(Path.Combine(installFolder, item.Patch.OldPath));
                    using var fs_target = File.Create(temp);
                    bool success = HPatch.PatchZstandard(fs_source, fs_diff, fs_target);
                    if (success)
                    {
                        fs_source.Dispose();
                        fs_target.Dispose();
                        File.Move(temp, path, true);
                    }
                }
            }
        }).ConfigureAwait(false);
    }



    private async Task CheckFilesAsync(string installFolder, CancellationToken cancellation = default)
    {
        TotalBytes = ReleaseManifest.Size;
        DownloadBytes = 0;

        await Parallel.ForEachAsync(ReleaseManifest.Files, cancellation, async (item, token) =>
        {
            string path = Path.Combine(installFolder, item.Path);
            string url = ReleaseManifest.UrlPrefix + item.Id + (item.UrlSuffix ?? ReleaseManifest.UrlSuffix);
            await RetryHelper.ExecuteAsync(async retryToken =>
            {
                if (await CheckFileHashAsync(path, item.Hash, item.Size, retryToken).ConfigureAwait(false))
                {
                    Interlocked.Add(ref _downloadBytes, item.Size);
                }
                else
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    await DownloadZstdFileAsync(url, path, item.Size, retryToken).ConfigureAwait(false);
                    if (!await CheckFileHashAsync(path, item.Hash, item.Size, retryToken).ConfigureAwait(false))
                    {
                        File.Delete(path);
                        throw new Exception($"File hash mismatch after download: {path}");
                    }
                }
            }, cancellation).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }




}