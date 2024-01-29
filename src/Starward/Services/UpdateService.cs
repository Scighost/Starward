using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using Starward.Core.Metadata;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services;

internal class UpdateService
{

    private readonly ILogger<UpdateService> _logger;

    private readonly HttpClient _httpClient;

    private readonly MetadataClient _metadataClient;


    public UpdateService(ILogger<UpdateService> logger, HttpClient httpClient, MetadataClient metadataClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _metadataClient = metadataClient;
    }



    public async Task<ReleaseVersion?> CheckUpdateAsync(bool disableIgnore = false)
    {
        _logger.LogInformation("Start to check update (Preview: {preview}, Arch: {arch})", AppConfig.EnablePreviewRelease, RuntimeInformation.OSArchitecture);
        NuGetVersion.TryParse(AppConfig.AppVersion, out var currentVersion);
        NuGetVersion.TryParse(AppConfig.IgnoreVersion, out var ignoreVersion);
        var release = await _metadataClient.GetVersionAsync(AppConfig.EnablePreviewRelease, RuntimeInformation.OSArchitecture);
        _logger.LogInformation("Current version: {0}, latest version: {1}, ignore version: {2}", AppConfig.AppVersion, release?.Version, ignoreVersion);
        NuGetVersion.TryParse(release?.Version, out var newVersion);
        if (newVersion! > currentVersion!)
        {
            if (disableIgnore || newVersion! > ignoreVersion!)
            {
                return release;
            }
        }
        return null;
    }





    private string updateFolder;


    private ReleaseVersion releaseVersion;


    private List<ReleaseFile> localFiles;


    private List<ReleaseFile> downloadFiles;


    private List<UpdateFile> updateFiles = new();




    public UpdateState State { get; private set; }

    public int Progress_FileCountToDownload { get; private set; }

    private int progress_FileCountDownloaded;
    public int Progress_FileCountDownloaded => progress_FileCountDownloaded;

    public long Progress_BytesToDownload { get; private set; }


    private long progress_BytesDownloaded;
    public long Progress_BytesDownloaded => progress_BytesDownloaded;


    public string ErrorMessage { get; set; }




    #region Prepare




    public async Task PrepareForUpdateAsync(ReleaseVersion release)
    {
        try
        {
            cancelSource?.Cancel();
            ErrorMessage = string.Empty;
            updateFiles.Clear();
            releaseVersion = release;
            State = UpdateState.Preparing;
            var baseFolder = new DirectoryInfo(AppContext.BaseDirectory).Parent?.FullName;
            if (baseFolder == null)
            {
                // 无法自动更新
                ErrorMessage = Lang.UpdateService_CannotUpdateAutomatically;
                State = UpdateState.NotSupport;
                return;
            }
            var exe = Path.Join(baseFolder, "Starward.exe");
            if (!File.Exists(exe))
            {
                // 无法自动更新
                ErrorMessage = Lang.UpdateService_CannotUpdateAutomatically;
                State = UpdateState.NotSupport;
                return;
            }
            foreach (var file in release.SeparateFiles)
            {
                file.Path = Path.GetFullPath(file.Path, baseFolder);
            }
            updateFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward\\update");
            Directory.CreateDirectory(updateFolder);
            await Task.Run(() =>
            {
                GetLocalFilesHash();
                GetExistFiles();
                GetDownloadFiles();
            });
            progress_BytesDownloaded = 0;
            progress_FileCountDownloaded = 0;
            Progress_BytesToDownload = downloadFiles.Sum(x => x.Size);
            Progress_FileCountToDownload = downloadFiles.Count;
            State = UpdateState.Pending;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Prepare for update");
            State = UpdateState.Stop;
        }
    }



    public void GetLocalFilesHash()
    {
        if (localFiles is null)
        {
            var files = Directory.GetFiles(AppContext.BaseDirectory, "*", SearchOption.AllDirectories);
            var releaseFiles = new List<ReleaseFile>(files.Length);
            foreach (var file in files)
            {
                using var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var len = (int)fs.Length;
                var bytes = ArrayPool<byte>.Shared.Rent(len);
                fs.Read(bytes, 0, len);
                var span = bytes.AsSpan(0, len);
                var sha256 = SHA256.HashData(span);
                ArrayPool<byte>.Shared.Return(bytes);
                releaseFiles.Add(new ReleaseFile
                {
                    Path = file,
                    Size = len,
                    Hash = Convert.ToHexString(sha256)
                });
            }
            localFiles = releaseFiles;
        }
    }



    private void GetExistFiles()
    {
        List<(ReleaseFile from, ReleaseFile to)> exists = localFiles.Join(releaseVersion.SeparateFiles, x => x.Hash, x => x.Hash, (x, y) => (x, y)).ToList();
        foreach ((ReleaseFile from, ReleaseFile to) in exists)
        {
            updateFiles.Add(new UpdateFile
            {
                From = from.Path,
                To = to.Path,
                Size = to.Size,
                Hash = to.Hash
            });
        }
    }



    private void GetDownloadFiles()
    {
        var files = releaseVersion.SeparateFiles.ExceptBy(localFiles.Select(x => x.Hash), x => x.Hash).ToList();
        var download = new List<ReleaseFile>();
        foreach (var file in files)
        {
            var target = Path.Combine(updateFolder, file.Hash);
            if (File.Exists(target))
            {
                updateFiles.Add(new UpdateFile
                {
                    From = target,
                    To = file.Path,
                    Size = file.Size,
                    Hash = file.Hash,
                    IsMoving = true,
                });
            }
            else
            {
                download.Add(file);
            }
        }
        downloadFiles = download;
    }



    #endregion




    #region Update



    private CancellationTokenSource? cancelSource;



    public void Start()
    {
        new Thread(async () => await UpdateAsync()).Start();
    }



    public void Stop()
    {
        cancelSource?.Cancel();
        State = UpdateState.Stop;
        progress_BytesDownloaded = 0;
        progress_FileCountDownloaded = 0;
        Progress_BytesToDownload = 0;
        Progress_FileCountToDownload = 0;
    }





    public async Task UpdateAsync()
    {
        try
        {
            cancelSource?.Cancel();
            cancelSource = new();
            var source = cancelSource;
            State = UpdateState.Downloading;
            await DownloadFilesAsync(source.Token);
            if (source.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }
            var check = CheckDownloadFiles();
            if (!check)
            {
                throw new Exception("File verification failed");
            }
            if (source.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }
            State = UpdateState.Moving;
            await Task.Delay(1000);
            MovingFiles();
            State = UpdateState.Finish;
        }
        catch (TaskCanceledException)
        {
            State = UpdateState.Stop;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update failed");
            State = UpdateState.Error;
            ErrorMessage = ex.Message;
        }

    }







    private async Task DownloadFilesAsync(CancellationToken cancellationToken = default)
    {
        progress_BytesDownloaded = 0;
        progress_FileCountDownloaded = 0;
        Progress_BytesToDownload = downloadFiles.Sum(x => x.Size);
        Progress_FileCountToDownload = downloadFiles.Count;
        await Parallel.ForEachAsync(downloadFiles, new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
            CancellationToken = cancellationToken
        }, async (releaseFile, token) =>
        {
            await DownloadFileAsync(releaseFile, token);
        });
        updateFiles.AddRange(downloadFiles.Select(x => new UpdateFile
        {
            From = Path.Combine(updateFolder, x.Hash),
            To = x.Path,
            Size = x.Size,
            Hash = x.Hash,
            IsMoving = true,
        }));
    }



    private async Task DownloadFileAsync(ReleaseFile releaseFile, CancellationToken cancellationToken = default)
    {
        var readLength = 0;
        for (int i = 0; i < 3; i++)
        {
            try
            {
                readLength = 0;
                var file = Path.Combine(updateFolder, releaseFile.Hash);
                if (!File.Exists(file))
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, releaseFile.Url) { Version = HttpVersion.Version11 };
                    var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    var ms = new MemoryStream();
                    var buffer = new byte[1 << 16];
                    int length;
                    while ((length = await stream.ReadAsync(buffer, cancellationToken)) != 0)
                    {
                        ms.Write(buffer, 0, length);
                        readLength += length;
                        Interlocked.Add(ref progress_BytesDownloaded, length);
                    }
                    await File.WriteAllBytesAsync(file, ms.ToArray(), cancellationToken);
                    Interlocked.Increment(ref progress_FileCountDownloaded);
                }
                using var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                string hash = Convert.ToHexString(await SHA256.HashDataAsync(fs, cancellationToken));
                if (!string.Equals(hash, releaseFile.Hash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Checksum failed, path: {path}, actual hash: {hash}, true hash: {truehash}", releaseFile.Path, hash, releaseFile.Hash);
                    throw new Exception($"Checksum failed: {releaseFile.Path}");
                }
                break;
            }
            catch (TaskCanceledException)
            {
                Interlocked.Add(ref progress_BytesDownloaded, -readLength);
                break;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("Download failed: {error}\r\n{url}", ex.Message, releaseFile.Url);
                Interlocked.Add(ref progress_BytesDownloaded, -readLength);
                if (i == 2)
                {
                    throw;
                }
            }
        }
    }



    private bool CheckDownloadFiles()
    {
        foreach (var releaseFile in downloadFiles)
        {
            var file = Path.Combine(updateFolder, releaseFile.Hash);
            if (!File.Exists(file))
            {
                return false;
            }
        }
        return true;
    }




    private void MovingFiles()
    {
        foreach (var file in updateFiles)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file.To)!);
            if (file.IsMoving)
            {
                File.Move(file.From, file.To, true);
            }
            else
            {
                File.Copy(file.From, file.To, true);
            }
        }
    }


    #endregion







    public enum UpdateState
    {
        Stop,

        Preparing,

        Pending,

        Downloading,

        Moving,

        Checking,

        Finish,

        Error,

        NotSupport,
    }



    public class UpdateFile
    {

        public string From { get; set; }

        public string To { get; set; }

        public long Size { get; set; }

        public string Hash { get; set; }

        public bool IsMoving { get; set; }

    }




}
