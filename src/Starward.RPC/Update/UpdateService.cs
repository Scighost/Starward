using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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


    private ReleaseVersion releaseVersion;


    private List<ReleaseFile> currentVersionFiles;


    private List<UpdateFile> sameFiles = new();


    private List<UpdateFile> downloadFiles = new();




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




    public async Task PrepareForUpdateAsync(ReleaseVersion release, string targetPath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Prepare for update starward");
            State = UpdateState.Pending;
            this.targetPath = targetPath;
            releaseVersion = release;
            foreach (var file in release.SeparateFiles)
            {
                file.Path = Path.GetFullPath(file.Path, targetPath);
            }
            updateCacheFolder = Path.Combine(AppConfig.CacheFolder, "Starward\\update");
            Directory.CreateDirectory(updateCacheFolder);
            await GetCurrentVersionFilesHashAsync(cancellationToken);
            GetSameAndDownloadFiles();
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
        if (currentVersionFiles is null)
        {
            var files = Directory.GetFiles(AppContext.BaseDirectory, "*", SearchOption.AllDirectories);
            var releaseFiles = new List<ReleaseFile>(files.Length);
            await Parallel.ForEachAsync(files, cancellationToken, async (file, token) =>
            {
                using var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var sha256 = await SHA256.HashDataAsync(fs, cancellationToken);
                lock (releaseFiles)
                {
                    releaseFiles.Add(new ReleaseFile
                    {
                        Path = file,
                        Size = fs.Length,
                        Hash = Convert.ToHexString(sha256)
                    });
                }
            });
            currentVersionFiles = releaseFiles;
        }
    }




    private void GetSameAndDownloadFiles()
    {
        sameFiles.Clear();
        List<(ReleaseFile from, ReleaseFile to)> same = currentVersionFiles.Join(releaseVersion.SeparateFiles, x => x.Hash, x => x.Hash, (x, y) => (x, y)).ToList();
        foreach ((ReleaseFile from, ReleaseFile to) in same)
        {
            sameFiles.Add(new UpdateFile
            {
                From = from.Path,
                To = to.Path,
                Size = to.Size,
                Hash = to.Hash,
                Url = to.Url,
            });
        }

        downloadFiles.Clear();
        var files = releaseVersion.SeparateFiles.ExceptBy(currentVersionFiles.Select(x => x.Hash), x => x.Hash).ToList();
        foreach (var file in files)
        {
            var cache = Path.Combine(updateCacheFolder, file.Hash);
            downloadFiles.Add(new UpdateFile
            {
                From = cache,
                To = file.Path,
                Size = file.Size,
                Hash = file.Hash,
                Url = file.Url,
            });
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
            await MoveAndCheckFilesAsync(cancellationToken);
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
        Progress_TotalBytes = downloadFiles.Sum(x => x.Size);
        Progress_TotalFileCount = downloadFiles.Count;
        _logger.LogInformation("Update Starward, downloading {count} files", Progress_TotalFileCount);
        await Parallel.ForEachAsync(downloadFiles, cancellationToken, async (updateFile, token) =>
        {
            await _polly.ExecuteAsync(async (pollyToken) => await DownloadFileAsync(updateFile.From, updateFile.Url, updateFile.Size, updateFile.Hash, false, pollyToken), token);
        });
    }



    private async Task DownloadFileAsync(string file, string url, long size, string trueHash, bool noProgress, CancellationToken cancellationToken = default)
    {
        using var fs = File.Open(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
        if (!noProgress)
        {
            Interlocked.Add(ref progress_DownloadBytes, fs.Length);
        }
        if (fs.Length < size)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url) { Version = HttpVersion.Version11 };
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
                if (!noProgress)
                {
                    Interlocked.Add(ref progress_DownloadBytes, length);
                }
            }
        }
        await fs.FlushAsync(cancellationToken);
        if (!noProgress)
        {
            Interlocked.Increment(ref progress_DownloadFileCount);
        }

        fs.Position = 0;
        var sha256 = await SHA256.HashDataAsync(fs, cancellationToken);
        string hash = Convert.ToHexString(sha256);
        if (string.Equals(hash, trueHash, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        _logger.LogWarning("""
                    File verification failed: {file}
                    Expected Hash: {ExpectedHash}
                    Actual Hash: {ActualHash}
                    """, file, trueHash, hash);
        fs.Dispose();
        File.Delete(file);
        if (!noProgress)
        {
            Interlocked.Add(ref progress_DownloadBytes, -size);
            Interlocked.Decrement(ref progress_DownloadFileCount);
        }
        throw new Exception("File verification failed");
    }



    private async Task MoveAndCheckFilesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var item in sameFiles.Concat(downloadFiles))
        {
            if (File.Exists(item.From))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(item.To)!);
                File.Copy(item.From, item.To, true);
            }
        }
        await Parallel.ForEachAsync(releaseVersion.SeparateFiles, cancellationToken, async (releaseFiles, token) =>
        {
            await _polly.ExecuteAsync(async (pollyToken) => await DownloadFileAsync(releaseFiles.Path, releaseFiles.Url, releaseFiles.Size, releaseFiles.Hash, true, pollyToken), token);
        });
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




    private class UpdateFile
    {

        public string From { get; set; }

        public string To { get; set; }

        public long Size { get; set; }

        public string Hash { get; set; }

        public string Url { get; set; }

    }




}
