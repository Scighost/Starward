using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Snap.HPatch;
using Starward.RPC.GameInstall;
using Starward.RPC.Update.Metadata;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.RPC.Update;

internal class UpdateService
{

    private readonly ILogger<UpdateService> _logger;

    private readonly HttpClient _httpClient;

    private readonly ResiliencePipeline _polly;



    public UpdateService(ILogger<UpdateService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _polly = new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 5,
            BackoffType = DelayBackoffType.Linear
        }).Build();
    }



    private string targetPath;

    private string updateCacheFolder;

    private ReleaseManifest releaseManifest;

    private ConcurrentDictionary<string, string> currentVersionFilesHash;



    public UpdateState State { get; private set; }

    public int Progress_TotalFileCount { get; private set; }

    private int progress_DownloadFileCount;
    public int Progress_DownloadFileCount => progress_DownloadFileCount;

    public long Progress_TotalBytes { get; private set; }


    private long progress_DownloadBytes;
    public long Progress_DownloadBytes => progress_DownloadBytes;


    public string ErrorMessage { get; set; }




    public UpdateProgress GetUpdateProgress()
    {
        return new UpdateProgress
        {
            State = (int)State,
            TotalFile = Progress_TotalFileCount,
            DownloadFile = Progress_DownloadFileCount,
            TotalBytes = Progress_TotalBytes,
            DownloadBytes = Progress_DownloadBytes,
            ErrorMessage = ErrorMessage,
        };
    }




    #region Prepare




    public async Task PrepareForUpdateAsync(ReleaseManifest manifest, string targetPath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Prepare for update starward");
            State = UpdateState.Pending;
            this.targetPath = targetPath;
            releaseManifest = manifest;
            updateCacheFolder = Path.Combine(AppConfig.CacheFolder, "Starward\\update");
            Directory.CreateDirectory(updateCacheFolder);
            await GetCurrentVersionFilesHashAsync(cancellationToken);
            await UpdateAsync(cancellationToken);
            _logger.LogInformation("Update finished");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Prepare for update");
            State = UpdateState.Stop;
        }
    }



    public async Task GetCurrentVersionFilesHashAsync(CancellationToken cancellationToken = default)
    {
        if (currentVersionFilesHash is null)
        {
            var files = Directory.GetFiles(AppContext.BaseDirectory, "*", SearchOption.AllDirectories);
            var releaseFiles = new ConcurrentDictionary<string, string>();
            await Parallel.ForEachAsync(files, cancellationToken, async (file, token) =>
            {
                using var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var sha256 = await SHA256.HashDataAsync(fs, cancellationToken);
                lock (releaseFiles)
                {
                    releaseFiles[Convert.ToHexStringLower(sha256)] = file;
                }
            });
            currentVersionFilesHash = releaseFiles;
        }
    }





    #endregion




    #region Update





    public async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            BackupVersionIni();
            State = UpdateState.Downloading;
            await DownloadFilesAsync(cancellationToken);
            State = UpdateState.Pending;
            await PatchAndVerifyAsync(cancellationToken);
            DeleteOldFiles();
            DeleteUpdateCacheFolder();
            await Task.Delay(1000, cancellationToken);
            State = UpdateState.Finish;
            _logger.LogInformation("Update Starward finished");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Update Starward canceled, restore version.ini");
            State = UpdateState.Stop;
            RestoreVersionIni();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update Starward failed, restore version.ini");
            State = UpdateState.Error;
            ErrorMessage = ex.Message;
            RestoreVersionIni();
        }
    }



    private void BackupVersionIni()
    {
        var ini = Path.Combine(targetPath, "version.ini");
        var backup = Path.Combine(updateCacheFolder, "version.ini");
        if (File.Exists(backup))
        {
            File.Delete(backup);
        }
        if (File.Exists(ini))
        {
            File.Copy(ini, Path.Combine(updateCacheFolder, "version.ini"), true);
        }
    }



    private void RestoreVersionIni()
    {
        var ini = Path.Combine(updateCacheFolder, "version.ini");
        if (File.Exists(ini))
        {
            File.Copy(ini, Path.Combine(targetPath, "version.ini"), true);
        }
    }



    private async Task DownloadFilesAsync(CancellationToken cancellationToken = default)
    {
        progress_DownloadBytes = 0;
        progress_DownloadFileCount = 0;
        Progress_TotalBytes = 0;
        Progress_TotalFileCount = 0;
        if (releaseManifest.DiffVersion is null)
        {
            foreach (var item in releaseManifest.Files)
            {
                if (!(currentVersionFilesHash?.TryGetValue(item.Hash, out var value) ?? false))
                {
                    Progress_TotalBytes += item.CompressedSize;
                    Progress_TotalFileCount += 1;
                }
            }
        }
        else
        {
            Progress_TotalBytes = releaseManifest.DiffSize;
            Progress_TotalFileCount = releaseManifest.DiffFileCount;
        }

        List<(string Id, long Size, string Hash)> list = new();
        foreach (var item in releaseManifest.Files)
        {
            if (item.Patch is null && !currentVersionFilesHash!.ContainsKey(item.Hash))
            {
                list.Add((item.Id, item.CompressedSize, item.CompressedHash));
            }
            else if (item.Patch?.Id is not null && item.Patch?.PatchHash is not null)
            {
                list.Add((item.Patch.Id, item.Patch.PatchSize, item.Patch.PatchHash));
            }
        }
        list = list.DistinctBy(x => x.Id).ToList();

        _logger.LogInformation("Update Starward, downloading {count} files", Progress_TotalFileCount);
        await Parallel.ForEachAsync(list, cancellationToken, async (item, token) =>
        {
            await _polly.ExecuteAsync(async (pollyToken) => await DownloadReleaseFileAsync(item, pollyToken), token);
        });
    }



    private async Task PatchAndVerifyAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Update Starward, patching and verifying files");
        await Parallel.ForEachAsync(releaseManifest.Files, cancellationToken, async (releaseFile, token) =>
        {
            try
            {
                await DecompressOrPatchFileAsync(releaseFile, token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Decompress or patch file failed: {file}", releaseFile.Path);
            }
            await _polly.ExecuteAsync(async (pollyToken) => await VerifyAndDownloadFileAsync(releaseFile, pollyToken), token);
        });
    }



    private async Task DownloadReleaseFileAsync((string Id, long Size, string Hash) item, CancellationToken cancellationToken = default)
    {
        string url = releaseManifest.UrlPrefix + item.Id;
        string path = Path.Combine(updateCacheFolder, item.Id);

        using var fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
        Interlocked.Add(ref progress_DownloadBytes, fs.Length);
        if (fs.Length < item.Size)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url) { VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher };
            request.Headers.Range = new RangeHeaderValue(fs.Length, null);
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentRange?.From is not null)
            {
                fs.Position = response.Content.Headers.ContentRange.From.Value;
            }
            using var hs = await response.Content.ReadAsStreamAsync(cancellationToken);
            var buffer = new byte[1 << 16];
            int length;
            while ((length = await hs.ReadAsync(buffer, cancellationToken)) != 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, length), cancellationToken);
                Interlocked.Add(ref progress_DownloadBytes, length);
            }
        }
        await fs.FlushAsync(cancellationToken);
        Interlocked.Increment(ref progress_DownloadFileCount);

        fs.Position = 0;
        var sha256 = await SHA256.HashDataAsync(fs, cancellationToken);
        if (string.Equals(Convert.ToHexString(sha256), item.Hash, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        _logger.LogWarning("Checksum failed: {path}", path);
        fs.Dispose();
        File.Delete(path);
        Interlocked.Add(ref progress_DownloadBytes, -item.Size);
        Interlocked.Decrement(ref progress_DownloadFileCount);
        throw new Exception($"Checksum failed: {path}");
    }


    private async Task DecompressOrPatchFileAsync(ReleaseFile releaseFile, CancellationToken cancellationToken = default)
    {
        string path = Path.Combine(targetPath, releaseFile.Path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (releaseFile.Patch is null && currentVersionFilesHash.TryGetValue(releaseFile.Hash, out string? oldPath))
        {
            File.Copy(oldPath, path, true);
        }
        else if (releaseFile.Patch is null)
        {
            string downloadPath = Path.Combine(updateCacheFolder, releaseFile.Id);
            if (File.Exists(downloadPath))
            {
                using var zstdFs = File.OpenRead(downloadPath);
                using var fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                using var decompressionStream = new ZstdSharp.DecompressionStream(zstdFs);
                await decompressionStream.CopyToAsync(fs, cancellationToken);
            }
        }
        else if (currentVersionFilesHash.TryGetValue(releaseFile.Patch.OldFileHash, out string? oldPath2) && new FileInfo(oldPath2).Length == releaseFile.Patch.OldFileSize)
        {
            if (releaseFile.Patch.Id is null)
            {
                File.Copy(oldPath2, path, true);
            }
            else
            {
                string patchPath = Path.Combine(updateCacheFolder, releaseFile.Patch.Id);
                if (File.Exists(patchPath))
                {
                    using var oldFs = File.Open(oldPath2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var newFs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                    using var patchFs = new FileSliceStream(patchPath, releaseFile.Patch.Offset, releaseFile.Patch.Length == 0 ? releaseFile.Patch.PatchSize : releaseFile.Patch.Length);
                    HPatch.PatchZstandard(oldFs, patchFs, newFs);
                }
            }
        }
    }


    private async Task VerifyAndDownloadFileAsync(ReleaseFile releaseFile, CancellationToken cancellationToken = default)
    {
        string path = Path.Combine(targetPath, releaseFile.Path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
        if (fs.Length != releaseFile.Size)
        {
            string url = releaseManifest.UrlPrefix + releaseFile.Id;
            using var hs = await _httpClient.GetStreamAsync(url, cancellationToken);
            fs.SetLength(0);
            using var zstdStream = new ZstdSharp.DecompressionStream(hs);
            await zstdStream.CopyToAsync(fs, cancellationToken);
            fs.Position = 0;
        }
        var sha256 = await SHA256.HashDataAsync(fs, cancellationToken);
        if (!string.Equals(Convert.ToHexStringLower(sha256), releaseFile.Hash, StringComparison.OrdinalIgnoreCase))
        {
            fs.Dispose();
            File.Delete(path);
            throw new Exception($"Checksum failed: {path}");
        }
    }



    private void DeleteOldFiles()
    {
        try
        {
            if (releaseManifest.DeleteFiles is null)
            {
                return;
            }
            foreach (var item in releaseManifest.DeleteFiles)
            {
                string path = Path.Combine(targetPath, item);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete old files");
        }
    }


    private void DeleteUpdateCacheFolder()
    {
        try
        {
            Directory.Delete(updateCacheFolder, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete update cache folder");
        }
    }




    #endregion



}
