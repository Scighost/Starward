using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Services.InstallGame;
using Starward.Services.Launcher;
using Starward.SevenZip;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services.Download;

internal abstract class InstallGameService
{


    protected readonly ILogger<InstallGameService> _logger;

    protected readonly HttpClient _httpClient;

    protected readonly GameLauncherService _launcherService;

    protected readonly GamePackageService _packageService;

    protected readonly HoYoPlayService _hoYoPlayService;



    public GameBiz CurrentGameBiz { get; protected set; }



    public abstract bool CanRepairGameFiles { get; }




    public abstract bool CanRepairAudioFiles { get; }




    public InstallGameState State { get; protected set; }


    protected string _installPath;


    protected bool _initialized;


    protected InstallGameTask _installTask;


    protected List<InstallGameItem> _gamePackageItems;


    protected List<InstallGameItem> _audioPackageItems;


    private List<InstallGameItem> _gameFileItems;


    private ConcurrentQueue<InstallGameItem> _verifyFailedItems = new();





    public void Initialize(GameBiz gameBiz, string installPath)
    {
        if (gameBiz.ToGame() is GameBiz.None)
        {
            throw new ArgumentOutOfRangeException(nameof(gameBiz), gameBiz, $"GameBiz ({gameBiz}) is invalid.");
        }
        Directory.CreateDirectory(installPath);
        var temp = Path.Combine(installPath, Random.Shared.Next(1000_0000, int.MaxValue).ToString());
        File.Create(temp).Dispose();
        File.Delete(temp);
        CurrentGameBiz = gameBiz;
        _installPath = installPath;
        _initialized = true;
    }




    public virtual async Task StartInstallGameAsync(CancellationToken cancellationToken = default)
    {
        var package = await _packageService.GetGamePackageAsync(CurrentGameBiz);
        var files = await GetAudioPackageFilesFromGameResourceAsync(package.Main.Major!);
        List<InstallGameItem> list = [];
        foreach (var item in _packageResouce.GamePackages)
        {
            list.Add(new InstallGameItem
            {
                Type = InstallGameItemType.Download,
                FileName = Path.GetFileName(item.Url),
                Path = Path.Combine(_installPath, Path.GetFileName(item.Url)),
                Url = item.Url,
                MD5 = item.MD5,
                Size = item.Size,
                DecompressedSize = item.DecompressedSize,
                WriteAsTempFile = true,
            });
        }
        foreach (var item in await GetAudioPackageFilesFromGameResourceAsync(package.Main.Major!))
        {
            list.Add(new InstallGameItem
            {
                Type = InstallGameItemType.Download,
                FileName = Path.GetFileName(item.Url),
                Path = Path.Combine(_installPath, Path.GetFileName(item.Url)),
                Url = item.Url,
                MD5 = item.MD5,
                Size = item.Size,
                DecompressedSize = item.DecompressedSize,
                WriteAsTempFile = true,
            });
        }
        _packageItems = list;
        _installTask = InstallGameTask.Install;
        foreach (var item in _packageItems)
        {
            _installItemQueue.Enqueue(item);
        }
        StartTask(InstallGameState.Download);
    }





    public virtual Task StartRepairGameAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }




    public virtual async Task StartPredownloadAsync(CancellationToken cancellationToken = default)
    {
        var package = await _packageService.GetGamePackageAsync(CurrentGameBiz);
        List<InstallGameItem> list = [];
        GamePackageResource resource;
        var localVersion = await _launcherService.GetLocalGameVersionAsync(CurrentGameBiz, _installPath);
        if (package.PreDownload is null)
        {
            throw new InvalidOperationException($"Predownload of ({CurrentGameBiz}) is not enabled.");
        }
        if (package.PreDownload.Patches.FirstOrDefault(x => x.Version == localVersion?.ToString()) is GamePackageResource resource1)
        {
            resource = resource1;
        }
        else
        {
            resource = package.PreDownload.Major!;
        }
        _packageResouce = resource;
        foreach (var item in _packageResouce.GamePackages)
        {
            list.Add(new InstallGameItem
            {
                Type = InstallGameItemType.Download,
                FileName = Path.GetFileName(item.Url),
                Path = Path.Combine(_installPath, Path.GetFileName(item.Url)),
                Url = item.Url,
                MD5 = item.MD5,
                Size = item.Size,
                DecompressedSize = item.DecompressedSize,
                WriteAsTempFile = true,
            });
        }
        foreach (var item in await GetAudioPackageFilesFromGameResourceAsync(resource))
        {
            list.Add(new InstallGameItem
            {
                Type = InstallGameItemType.Download,
                FileName = Path.GetFileName(item.Url),
                Path = Path.Combine(_installPath, Path.GetFileName(item.Url)),
                Url = item.Url,
                MD5 = item.MD5,
                Size = item.Size,
                DecompressedSize = item.DecompressedSize,
                WriteAsTempFile = true,
            });
        }
        _packageItems = list;
        _installTask = InstallGameTask.Predownload;
        foreach (var item in _packageItems)
        {
            _installItemQueue.Enqueue(item);
        }
        StartTask(InstallGameState.Download);
    }




    public virtual async Task StartUpdateGameAsync(CancellationToken cancellationToken = default)
    {
        var package = await _packageService.GetGamePackageAsync(CurrentGameBiz);
        List<InstallGameItem> list = new();
        GamePackageResource resource;
        var localVersion = await _launcherService.GetLocalGameVersionAsync(CurrentGameBiz, _installPath);
        if (package.Main.Patches.FirstOrDefault(x => x.Version == localVersion?.ToString()) is GamePackageResource resource1)
        {
            resource = resource1;
        }
        else
        {
            resource = package.Main.Major!;
        }
        _packageResouce = resource;
        foreach (var item in _packageResouce.GamePackages)
        {
            list.Add(new InstallGameItem
            {
                Type = InstallGameItemType.Download,
                FileName = Path.GetFileName(item.Url),
                Path = Path.Combine(_installPath, Path.GetFileName(item.Url)),
                Url = item.Url,
                MD5 = item.MD5,
                Size = item.Size,
                DecompressedSize = item.DecompressedSize,
                WriteAsTempFile = true,
            });
        }
        foreach (var item in await GetAudioPackageFilesFromGameResourceAsync(resource))
        {
            list.Add(new InstallGameItem
            {
                Type = InstallGameItemType.Download,
                FileName = Path.GetFileName(item.Url),
                Path = Path.Combine(_installPath, Path.GetFileName(item.Url)),
                Url = item.Url,
                MD5 = item.MD5,
                Size = item.Size,
                DecompressedSize = item.DecompressedSize,
                WriteAsTempFile = true,
            });
        }
        _packageItems = list;
        _installTask = InstallGameTask.Update;
        foreach (var item in _packageItems)
        {
            _installItemQueue.Enqueue(item);
        }
        StartTask(InstallGameState.Download);
    }





    protected async Task<List<GamePackageFile>> GetAudioPackageFilesFromGameResourceAsync(GamePackageResource resource)
    {
        string lang = await GetAudioLanguageAsync();
        if (string.IsNullOrWhiteSpace(lang))
        {
            lang = LanguageUtil.FilterLanguage(CultureInfo.CurrentUICulture.Name);
            await SetAudioLanguageAsync(lang);
        }
        List<GamePackageFile> list = [];
        foreach (var item in resource.AudioPackages ?? [])
        {
            if (!string.IsNullOrWhiteSpace(item.Language) && lang.Contains(item.Language))
            {
                list.Add(item);
            }
        }
        return list;
    }





    protected async Task<string> GetAudioLanguageAsync()
    {
        var sb = new StringBuilder();
        var config = await _hoYoPlayService.GetGameConfigAsync(CurrentGameBiz);
        if (config is not null && !string.IsNullOrWhiteSpace(config.AudioPackageScanDir))
        {
            string file = Path.Join(_installPath, config.AudioPackageScanDir);
            if (File.Exists(file))
            {
                var lines = await File.ReadAllLinesAsync(file);
                if (lines.Any(x => x.Contains("Chinese"))) { sb.Append("zh-cn"); }
                if (lines.Any(x => x.Contains("English(US)"))) { sb.Append("en-us"); }
                if (lines.Any(x => x.Contains("Japanese"))) { sb.Append("ja-jp"); }
                if (lines.Any(x => x.Contains("Korean"))) { sb.Append("ko-kr"); }
            }
        }
        return sb.ToString();
    }




    protected async Task SetAudioLanguageAsync(string lang)
    {
        var config = await _hoYoPlayService.GetGameConfigAsync(CurrentGameBiz);
        if (config is not null && !string.IsNullOrWhiteSpace(config.AudioPackageScanDir))
        {
            string file = Path.Join(_installPath, config.AudioPackageScanDir);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var lines = new List<string>(4);
            if (lang.Contains("zh-cn") || lang.Contains("zh-tw")) { lines.Add("Chinese"); }
            if (lang.Contains("en-us")) { lines.Add("English(US)"); }
            if (lang.Contains("ja-jp")) { lines.Add("Japanese"); }
            if (lang.Contains("ko-kr")) { lines.Add("Korean"); }
            await File.WriteAllLinesAsync(file, lines);
        }
    }




    protected async Task CleanGameDeprecatedFilesAsync()
    {
        State = InstallGameState.Clean;
        var files = await _hoYoPlayService.GetGameDeprecatedFilesAsync(CurrentGameBiz);
        foreach (var file in files)
        {
            var path = Path.Combine(_installPath, file.Name);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        CurrentTaskFinished();
    }



    protected void Finish()
    {

    }



    protected void StartTask(InstallGameState state)
    {
        if (state is InstallGameState.Download)
        {
            _totalBytes = _installItemQueue.Sum(x => x.Size);
            _finishBytes = _installItemQueue.Sum(GetFileLength);
        }
        else if (state is InstallGameState.Verify)
        {
            _totalBytes = _installItemQueue.Sum(x => x.Size);
            _finishBytes = 0;
        }
        else if (state is InstallGameState.Decompress)
        {
            _totalBytes = _installItemQueue.Sum(x => x.Size);
            _finishBytes = 0;
        }
        else if (state is InstallGameState.Clean)
        {
            _totalBytes = 0;
            _finishBytes = 0;
        }
        else if (state is InstallGameState.Finish)
        {
            _totalBytes = 0;
            _finishBytes = 0;
        }
        State = state;
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            _ = ExecuteTaskAsync();
        }
    }



    private static long GetFileLength(InstallGameItem item)
    {
        string file = item.Path;
        string file_tmp = item.Path + "_tmp";
        if (File.Exists(file))
        {
            return new FileInfo(file).Length;
        }
        else if (File.Exists(file_tmp))
        {
            return new FileInfo(file_tmp).Length;
        }
        return 0;
    }


    public async Task Continue()
    {
        try
        {

        }
        catch (Exception ex)
        {

        }
    }




    public async Task Pause()
    {
        try
        {

        }
        catch (Exception ex)
        {

        }
    }





    public void ClearState()
    {
        try
        {

        }
        catch (Exception ex)
        {

        }
    }





    private void CurrentTaskFinished()
    {
        try
        {
            if (_installTask is InstallGameTask.Install or InstallGameTask.Update)
            {
                // download -> verify -> decompress -> clean
                if (State is InstallGameState.Download)
                {
                    foreach (var item in _packageItems)
                    {
                        item.Type = InstallGameItemType.Verify;
                        _installItemQueue.Enqueue(item);
                    }
                    StartTask(InstallGameState.Verify);
                }
                if (State is InstallGameState.Verify)
                {
                    if (_verifyFailedItems.IsEmpty)
                    {

                        foreach (var item in _packageItems)
                        {
                            item.Type = InstallGameItemType.Decompress;
                            _installItemQueue.Enqueue(item);
                        }
                        StartTask(InstallGameState.Decompress);
                    }
                    else
                    {
                        while (_verifyFailedItems.TryDequeue(out InstallGameItem? item))
                        {
                            item.Type = InstallGameItemType.Download;
                            _installItemQueue.Enqueue(item);
                        }
                        StartTask(InstallGameState.Download);
                    }
                }
                if (State is InstallGameState.Decompress)
                {
                    _ = CleanGameDeprecatedFilesAsync();
                }
                if (State is InstallGameState.Clean)
                {
                    Finish();
                }
            }

            if (_installTask is InstallGameTask.Repair)
            {
                // verify -> download -> clean
                if (State is InstallGameState.Verify)
                {
                    while (_verifyFailedItems.TryDequeue(out InstallGameItem? item))
                    {
                        item.Type = InstallGameItemType.Download;
                        _installItemQueue.Enqueue(item);
                    }
                    StartTask(InstallGameState.Download);
                }
                if (State is InstallGameState.Download)
                {
                    foreach (var item in _packageItems)
                    {
                        item.Type = InstallGameItemType.Verify;
                        _installItemQueue.Enqueue(item);
                    }
                    StartTask(InstallGameState.Verify);
                }
                if (State is InstallGameState.Decompress)
                {
                    _ = CleanGameDeprecatedFilesAsync();
                }
                if (State is InstallGameState.Clean)
                {
                    Finish();
                }
            }

            if (_installTask is InstallGameTask.Predownload)
            {
                // download -> verify
                if (State is InstallGameState.Download)
                {
                    foreach (var item in _packageItems)
                    {
                        item.Type = InstallGameItemType.Verify;
                        _installItemQueue.Enqueue(item);
                    }
                    StartTask(InstallGameState.Verify);
                }
                if (State is InstallGameState.Verify)
                {
                    if (_verifyFailedItems.IsEmpty)
                    {
                        foreach (var item in _packageItems)
                        {
                            item.Type = InstallGameItemType.Decompress;
                            _installItemQueue.Enqueue(item);
                        }
                        StartTask(InstallGameState.Decompress);
                    }
                    else
                    {
                        while (_verifyFailedItems.TryDequeue(out InstallGameItem? item))
                        {
                            item.Type = InstallGameItemType.Download;
                            _installItemQueue.Enqueue(item);
                        }
                        StartTask(InstallGameState.Download);
                    }
                }
                if (State is InstallGameState.Decompress)
                {
                    _ = CleanGameDeprecatedFilesAsync();
                }
                if (State is InstallGameState.Clean)
                {
                    Finish();
                }
            }

            if (_installTask is InstallGameTask.Update)
            {
                // download -> verify -> decompress -> clean
            }
        }
        catch (Exception ex)
        {

        }
    }




    protected async Task<List<InstallGameItem>> GetDecompressInstallGameItemsAsync()
    {
        List<InstallGameItem> list = [];
        if (_packageResouce is not null)
        {
            var game = new InstallGameItem
            {
                Type = InstallGameItemType.Decompress,
                TargetPath = _installPath,
                PackageFiles = _packageResouce.GamePackages.Select(x => Path.Combine(_installPath, Path.GetFileName(x.Url))).ToList(),
                Size = _packageResouce.GamePackages.Sum(x => x.Size),
                DecompressedSize = _packageResouce.GamePackages.Sum(x => x.DecompressedSize),
            };
            list.Add(game);
            var audios = await GetAudioPackageFilesFromGameResourceAsync(_packageResouce);
            foreach (var audio in audios)
            {
                list.Add(new InstallGameItem
                {
                    Type = InstallGameItemType.Decompress,
                    TargetPath = _installPath,
                    PackageFiles = [Path.Combine(_installPath, Path.GetFileName(audio.Url))],
                    Size = audio.Size,
                    DecompressedSize = audio.DecompressedSize
                });
            }
        }
        else
        {
            while (_installItemQueue.TryDequeue(out InstallGameItem? item))
            {
                item.Type = InstallGameItemType.Decompress;
                item.TargetPath = _installPath;
                item.PackageFiles = [item.Path];
                list.Add(item);
            };
        }
        return list;
    }



    #region Install Internal





    protected ConcurrentQueue<InstallGameItem> _installItemQueue = new();




    protected int _totalCount;
    public int TotalCount => _totalCount;



    protected int _finishCount;
    public int FinishCount => _finishCount;



    protected long _totalBytes;
    public long TotalBytes => _totalBytes;



    protected long _finishBytes;
    public long FinishBytes => _finishBytes;



    protected int _concurrentExecuteThreadCount;
    public int ConcurrentExecuteThreadCount => _concurrentExecuteThreadCount;







    protected async Task ExecuteTaskAsync()
    {
        try
        {
            Interlocked.Increment(ref _concurrentExecuteThreadCount);
            if (_concurrentExecuteThreadCount > Environment.ProcessorCount)
            {
                return;
            }

            while (_installItemQueue.TryDequeue(out InstallGameItem? item))
            {
                try
                {
                    switch (item.Type)
                    {
                        case InstallGameItemType.None:
                            break;
                        case InstallGameItemType.Download:
                            await DownloadItemAsync(item);
                            break;
                        case InstallGameItemType.Verify:
                            await VerifyItemAsync(item);
                            break;
                        case InstallGameItemType.Decompress:
                            await DecompressItemAsync(item);
                            break;
                        default:
                            break;
                    }
                }
                catch (HttpRequestException ex)
                {

                }
                catch (SocketException ex)
                {

                }
                catch (HttpIOException ex)
                {

                }
                catch (IOException ex)
                {
                    // Received an unexpected EOF or 0 bytes from the transport stream.
                }
            }
        }
        catch (Exception ex)
        {

        }
        finally
        {
            Interlocked.Decrement(ref _concurrentExecuteThreadCount);
            if (_concurrentExecuteThreadCount == 0)
            {
                CurrentTaskFinished();
            }
        }
    }




    protected async Task DownloadItemAsync(InstallGameItem item, CancellationToken cancellationToken = default)
    {
        const int BUFFER_SIZE = 1 << 14;
        string file = item.Path;
        string file_tmp = item.Path + "_tmp";
        string file_target;
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        if (File.Exists(file))
        {
            file_target = file;
        }
        else if (File.Exists(file_tmp))
        {
            file_target = file_tmp;
        }
        else
        {
            file_target = item.WriteAsTempFile ? file_tmp : file;
        }
        using var fs = File.Open(file_target, FileMode.OpenOrCreate);
        if (fs.Length < item.Size)
        {
            _logger.LogInformation("Download: FileName {name}, Url {url}", item.FileName, item.Url);
            fs.Position = fs.Length;
            var request = new HttpRequestMessage(HttpMethod.Get, item.Url) { Version = HttpVersion.Version11 };
            request.Headers.Range = new RangeHeaderValue(fs.Length, null);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var hs = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var buffer = new byte[BUFFER_SIZE];
            int length;
            while ((length = await hs.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
                Interlocked.Add(ref _finishBytes, length);
            }
            _logger.LogInformation("Download Successfully: FileName {name}", item.FileName);
        }
    }





    protected async Task VerifyItemAsync(InstallGameItem item, CancellationToken cancellationToken = default)
    {
        const int BUFFER_SIZE = 1 << 20;
        string file = item.WriteAsTempFile ? item.Path + "_tmp" : item.Path;
        using var fs = File.OpenRead(file);
        if (fs.Length != item.Size)
        {
            return false;
        }
        int length = 0;
        var hashProvider = MD5.Create();
        var buffer = new byte[BUFFER_SIZE];
        while ((length = await fs.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
        {
            hashProvider.TransformBlock(buffer, 0, length, buffer, 0);
            Interlocked.Add(ref _finishBytes, length);
        }
        hashProvider.TransformFinalBlock(buffer, 0, length);
        var hash = hashProvider.Hash;
        fs.Dispose();
        if (string.Equals(Convert.ToHexString(hash!), item.MD5, StringComparison.OrdinalIgnoreCase))
        {
            if (item.WriteAsTempFile)
            {
                File.Move(file, item.Path, true);
            }
        }
        else
        {
            File.Delete(file);
            _verifyFailedItems.Enqueue(item);
        }
    }





    protected async Task DecompressItemAsync(InstallGameItem item, CancellationToken cancellationToken = default)
    {
        using var fs = new FileSliceStream(item.PackageFiles);
        if (item.PackageFiles[0].Contains(".7z", StringComparison.CurrentCultureIgnoreCase))
        {
            await Task.Run(() =>
            {
                using var extra = new ArchiveFile(fs);
                double ratio = (double)fs.Length / extra.Entries.Sum(x => (long)x.Size);
                long sum = 0;
                extra.ExtractProgress += (_, e) =>
                {
                    long size = (long)(e.Read * ratio);
                    _finishBytes += size;
                    sum += size;
                };
                extra.Extract(item.TargetPath, true);
                _finishBytes += fs.Length - sum;
            }).ConfigureAwait(false);
        }
        else
        {
            await Task.Run(async () =>
            {
                long sum = 0;
                using var zip = new ZipArchive(fs, ZipArchiveMode.Read, true);
                foreach (var entry in zip.Entries)
                {
                    if ((entry.ExternalAttributes & 0x10) > 0)
                    {
                        var target = Path.Combine(item.TargetPath, entry.FullName);
                        Directory.CreateDirectory(target);
                    }
                    else
                    {
                        var target = Path.Combine(item.TargetPath, entry.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                        entry.ExtractToFile(target, true);
                        _finishBytes += entry.CompressedLength;
                        sum += entry.CompressedLength;
                    }
                }
                _finishBytes += fs.Length - sum;
                await ApplyDiffFilesAsync(item.TargetPath);
            }).ConfigureAwait(false);
        }
        fs.Dispose();
        foreach (var file in item.PackageFiles)
        {
            File.Delete(file);
        }
    }





    protected async Task ApplyDiffFilesAsync(string installPath)
    {
        var delete = Path.Combine(installPath, "deletefiles.txt");
        if (File.Exists(delete))
        {
            var deleteFiles = await File.ReadAllLinesAsync(delete).ConfigureAwait(false);
            foreach (var file in deleteFiles)
            {
                var target = Path.Combine(installPath, file);
                if (File.Exists(target))
                {
                    File.Delete(target);
                }
            }
            File.Delete(delete);
        }

        var hdifffiles = Path.Combine(installPath, "hdifffiles.txt");
        if (File.Exists(hdifffiles))
        {
            var hpatch = Path.Combine(AppContext.BaseDirectory, "hpatchz.exe");
            var lines = await File.ReadAllLinesAsync(hdifffiles).ConfigureAwait(false);
            foreach (var line in lines)
            {
                var json = JsonNode.Parse(line);
                var name = json?["remoteName"]?.ToString();
                var target = Path.Join(installPath, name);
                var diff = $"{target}.hdiff";
                if (File.Exists(target) && File.Exists(diff))
                {
                    File.SetAttributes(target, FileAttributes.Archive);
                    using var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = hpatch,
                        Arguments = $"""-f "{target}" "{diff}" "{target}"  """,
                        CreateNoWindow = true,
                    });
                    if (process != null)
                    {
                        await process.WaitForExitAsync().ConfigureAwait(false);
                    }
                    if (File.Exists(diff))
                    {
                        File.Delete(diff);
                    }
                }
            }
            File.Delete(hdifffiles);
        }
    }




    #endregion





}
