using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Services.InstallGame;
using Starward.Services.Launcher;
using Starward.SevenZip;
using System;
using System.Buffers;
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
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace Starward.Services.Download;

internal class InstallGameService
{


    protected readonly ILogger<InstallGameService> _logger;

    protected readonly HttpClient _httpClient;

    protected readonly GameLauncherService _launcherService;

    protected readonly GamePackageService _packageService;

    protected readonly HoYoPlayService _hoYoPlayService;



    public InstallGameService(ILogger<InstallGameService> logger, HttpClient httpClient, GameLauncherService launcherService, GamePackageService packageService, HoYoPlayService hoYoPlayService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _launcherService = launcherService;
        _packageService = packageService;
        _hoYoPlayService = hoYoPlayService;
    }




    public static InstallGameService FromGameBiz(GameBiz gameBiz)
    {
        return gameBiz.ToGame().Value switch
        {
            GameBiz.bh3 => AppConfig.GetService<InstallGameService>(),
            GameBiz.hk4e => AppConfig.GetService<GenshinInstallGameService>(),
            GameBiz.hkrpg => AppConfig.GetService<StarRailInstallGameService>(),
            GameBiz.nap => AppConfig.GetService<ZZZInstallGameService>(),
            _ => throw new ArgumentOutOfRangeException(nameof(gameBiz), $"Game ({gameBiz}) is not supported."),
        };
    }




    public GameBiz CurrentGameBiz { get; protected set; }


    public InstallGameTask InstallTask { get; protected set; }


    private InstallGameState _state;
    public InstallGameState State
    {
        get => _state;
        set
        {
            _state = value;
            StateChanged?.Invoke(this, value);
        }
    }


    public event EventHandler<InstallGameState> StateChanged;


    public event EventHandler<Exception> InstallFailed;


    protected void OnInstallFailed(Exception ex)
    {
        if (State != InstallGameState.Error)
        {
            _pausedState = State;
            State = InstallGameState.Error;
            _cancellationTokenSource?.Cancel();
            InstallFailed?.Invoke(this, ex);
        }
    }







    #region Internal Property



    protected bool _initialized;


    protected string _installPath;


    protected string? _hardLinkPath;


    protected GameBiz _hardLinkGameBiz;


    protected InstallGameState _pausedState;


    protected GamePackage _gamePackage;


    protected GameChannelSDK? _channelSDK;


    protected List<InstallGameItem> _gamePackageItems;


    protected List<InstallGameItem> _audioPackageItems;


    protected List<InstallGameItem> _gameFileItems;


    protected InstallGameItem? _gameSDKItem;


    protected ConcurrentQueue<InstallGameItem> _verifyFailedItems = new();


    protected CancellationTokenSource? _cancellationTokenSource;



    #endregion






    #region Initialize & Start






    public bool CheckAccessPermission(string installPath)
    {
        try
        {
            var temp = Path.Combine(installPath, Random.Shared.Next(1000_0000, int.MaxValue).ToString());
            File.Create(temp).Dispose();
            File.Delete(temp);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }





    public async Task<long> GetGamePackageDecompressedSizeAsync(GameBiz gameBiz, string? installPath = null)
    {
        long size = 0;
        string lang = "";
        var package = await _packageService.GetGamePackageAsync(gameBiz);
        size += package.Main.Major!.GamePackages.Sum(x => x.DecompressedSize);
        if (!string.IsNullOrWhiteSpace(installPath))
        {
            var sb = new StringBuilder();
            var config = await _hoYoPlayService.GetGameConfigAsync(CurrentGameBiz);
            if (!string.IsNullOrWhiteSpace(config?.AudioPackageScanDir))
            {
                string file = Path.Join(installPath, config.AudioPackageScanDir);
                if (File.Exists(file))
                {
                    var lines = await File.ReadAllLinesAsync(file);
                    if (lines.Any(x => x.Contains("Chinese"))) { sb.Append("zh-cn"); }
                    if (lines.Any(x => x.Contains("English(US)"))) { sb.Append("en-us"); }
                    if (lines.Any(x => x.Contains("Japanese"))) { sb.Append("ja-jp"); }
                    if (lines.Any(x => x.Contains("Korean"))) { sb.Append("ko-kr"); }
                }
            }
            lang = sb.ToString();
        }
        if (string.IsNullOrWhiteSpace(lang))
        {
            lang = LanguageUtil.FilterLanguage(CultureInfo.CurrentUICulture.Name);
        }
        foreach (var item in package.Main.Major.AudioPackages ?? [])
        {
            if (!string.IsNullOrWhiteSpace(item.Language) && lang.Contains(item.Language))
            {
                size += item.DecompressedSize;
            }
        }
        return size;
    }





    public async Task InitializeAsync(GameBiz gameBiz, string installPath)
    {
        if (!gameBiz.IsKnown())
        {
            throw new ArgumentOutOfRangeException(nameof(gameBiz), gameBiz, $"GameBiz ({gameBiz}) is invalid.");
        }
        await Task.Run(() =>
        {
            Directory.CreateDirectory(installPath);
            var temp = Path.Combine(installPath, Random.Shared.Next(1000_0000, int.MaxValue).ToString());
            File.Create(temp).Dispose();
            File.Delete(temp);
            var files = Directory.GetFiles(installPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
        }).ConfigureAwait(false);
        CurrentGameBiz = gameBiz;
        _installPath = installPath;
        _initialized = true;
    }




    public virtual async Task StartInstallGameAsync(CancellationToken cancellationToken = default)
    {
        _gamePackage = await _packageService.GetGamePackageAsync(CurrentGameBiz);
        await PrepareDownloadGamePackageResourceAsync(_gamePackage.Main.Major!);
        if (CurrentGameBiz.IsBilibili())
        {
            await PrepareBilibiliChannelSDKAsync(InstallGameItemType.Download);
        }
        InstallTask = InstallGameTask.Install;
        StartTask(InstallGameState.Download);
    }




    public virtual async Task StartRepairGameAsync(CancellationToken cancellationToken = default)
    {
        _gamePackage = await _packageService.GetGamePackageAsync(CurrentGameBiz);
        var prefix = _gamePackage.Main.Major!.ResListUrl;
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new NotSupportedException($"Repairing game ({CurrentGameBiz}) is not supported.");
        }
        _gameFileItems = await GetPkgVersionsAsync(prefix, "pkg_version");
        foreach (var item in _gameFileItems)
        {
            _installItemQueue.Enqueue(item);
        }
        if (CurrentGameBiz.IsBilibili())
        {
            await PrepareBilibiliChannelSDKAsync(InstallGameItemType.Verify);
        }
        InstallTask = InstallGameTask.Repair;
        StartTask(InstallGameState.Verify);
    }




    public virtual async Task StartPredownloadAsync(CancellationToken cancellationToken = default)
    {
        _gamePackage = await _packageService.GetGamePackageAsync(CurrentGameBiz);
        GamePackageResource resource;
        var localVersion = await _launcherService.GetLocalGameVersionAsync(CurrentGameBiz, _installPath);
        if (_gamePackage.PreDownload is null)
        {
            throw new InvalidOperationException($"Predownload of ({CurrentGameBiz}) is not enabled.");
        }
        if (_gamePackage.PreDownload.Patches.FirstOrDefault(x => x.Version == localVersion?.ToString()) is GamePackageResource _resource_temp)
        {
            resource = _resource_temp;
        }
        else
        {
            resource = _gamePackage.PreDownload.Major!;
        }
        await PrepareDownloadGamePackageResourceAsync(resource);
        InstallTask = InstallGameTask.Predownload;
        StartTask(InstallGameState.Download);
    }




    public virtual async Task StartUpdateGameAsync(CancellationToken cancellationToken = default)
    {
        _gamePackage = await _packageService.GetGamePackageAsync(CurrentGameBiz);
        GamePackageResource resource;
        var localVersion = await _launcherService.GetLocalGameVersionAsync(CurrentGameBiz, _installPath);
        if (_gamePackage.Main.Patches.FirstOrDefault(x => x.Version == localVersion?.ToString()) is GamePackageResource _resource_tmp)
        {
            resource = _resource_tmp;
        }
        else
        {
            resource = _gamePackage.Main.Major!;
        }
        await PrepareDownloadGamePackageResourceAsync(resource);
        if (CurrentGameBiz.IsBilibili())
        {
            await PrepareBilibiliChannelSDKAsync(InstallGameItemType.Download);
        }
        InstallTask = InstallGameTask.Update;
        StartTask(InstallGameState.Download);
    }



    public virtual async Task StartHardLinkAsync(GameBiz linkGameBiz, CancellationToken cancellationToken = default)
    {
        _gamePackage = await _packageService.GetGamePackageAsync(CurrentGameBiz);
        var linkPackage = await _packageService.GetGamePackageAsync(linkGameBiz);
        string? linkInstallPath = _launcherService.GetGameInstallPath(linkGameBiz);
        if (!Directory.Exists(linkInstallPath))
        {
            throw new DirectoryNotFoundException($"Cannot find installation path of game ({linkGameBiz}).");
        }
        if (Path.GetPathRoot(_installPath) != Path.GetPathRoot(linkInstallPath))
        {
            throw new NotSupportedException("Hard linking between different drives is not supported.");
        }
        _hardLinkPath = linkInstallPath;
        _hardLinkGameBiz = linkGameBiz;
        var prefix = _gamePackage.Main.Major!.ResListUrl;
        var linkPrefix = linkPackage.Main.Major!.ResListUrl;
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new NotSupportedException($"Hard linking game ({CurrentGameBiz}) is not supported.");
        }
        _gameFileItems = await GetPkgVersionsAsync(prefix, "pkg_version");
        var linkGameFilesItem = await GetPkgVersionsAsync(linkPrefix, "pkg_version");
        var same = _gameFileItems.IntersectBy(linkGameFilesItem.Select(x => (x.Path, x.MD5)), x => (x.Path, x.MD5)).ToList();
        var diff = _gameFileItems.ExceptBy(linkGameFilesItem.Select(x => (x.Path, x.MD5)), x => (x.Path, x.MD5)).ToList();
        foreach (var item in same)
        {
            item.Type = InstallGameItemType.HardLink;
            item.HardLinkSource = Path.Combine(linkInstallPath, Path.GetRelativePath(_installPath, item.Path));
            _installItemQueue.Enqueue(item);
        }
        foreach (var item in diff)
        {
            item.Type = InstallGameItemType.Verify;
            _installItemQueue.Enqueue(item);
        }
        if (CurrentGameBiz.IsBilibili())
        {
            await PrepareBilibiliChannelSDKAsync(InstallGameItemType.Verify);
        }
        InstallTask = InstallGameTask.HardLink;
        StartTask(InstallGameState.Verify);
    }



    protected async Task PrepareDownloadGamePackageResourceAsync(GamePackageResource resource)
    {
        _gamePackageItems = new List<InstallGameItem>();
        foreach (var item in resource.GamePackages)
        {
            _gamePackageItems.Add(GamePackageFileToInstallGameItem(item));
        }
        _audioPackageItems = new List<InstallGameItem>();
        foreach (var item in await GetAudioPackageFilesFromGameResourceAsync(resource))
        {
            _audioPackageItems.Add(GamePackageFileToInstallGameItem(item));
        }
        foreach (var item in _gamePackageItems)
        {
            _installItemQueue.Enqueue(item);
        }
        foreach (var item in _audioPackageItems)
        {
            _installItemQueue.Enqueue(item);
        }
    }



    protected async Task PrepareBilibiliChannelSDKAsync(InstallGameItemType type)
    {
        _channelSDK = await _hoYoPlayService.GetGameChannelSDKAsync(CurrentGameBiz);
        if (_channelSDK is not null)
        {
            string name = Path.GetFileName(_channelSDK.ChannelSDKPackage.Url);
            _gameSDKItem = new InstallGameItem
            {
                Type = type,
                FileName = name,
                Path = Path.Combine(_installPath, name),
                MD5 = _channelSDK.ChannelSDKPackage.MD5,
                Size = _channelSDK.ChannelSDKPackage.Size,
                DecompressedSize = _channelSDK.ChannelSDKPackage.DecompressedSize,
                Url = _channelSDK.ChannelSDKPackage.Url,
                WriteAsTempFile = false,
            };
            _installItemQueue.Enqueue(_gameSDKItem);
        }
    }



    protected async Task<List<GamePackageFile>> GetAudioPackageFilesFromGameResourceAsync(GamePackageResource resource)
    {
        string lang = await GetAudioLanguageAsync();
        if (string.IsNullOrWhiteSpace(lang))
        {
            lang = LanguageUtil.FilterAudioLanguage(CultureInfo.CurrentUICulture.Name);
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
        if (!string.IsNullOrWhiteSpace(config?.AudioPackageScanDir))
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
        if (!string.IsNullOrWhiteSpace(config?.AudioPackageScanDir))
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



    protected InstallGameItem GamePackageFileToInstallGameItem(GamePackageFile file)
    {
        return new InstallGameItem
        {
            Type = InstallGameItemType.Download,
            FileName = Path.GetFileName(file.Url),
            Path = Path.Combine(_installPath, Path.GetFileName(file.Url)),
            Url = file.Url,
            MD5 = file.MD5,
            Size = file.Size,
            DecompressedSize = file.DecompressedSize,
            WriteAsTempFile = true,
        };
    }



    protected async Task<List<InstallGameItem>> GetPkgVersionsAsync(string prefix, string pkgName)
    {
        prefix = prefix.TrimEnd('/') + '/';
        var list = new List<InstallGameItem>();
        var str = await _httpClient.GetStringAsync(prefix + pkgName).ConfigureAwait(false);
        var lines = str.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            var node = JsonNode.Parse(line.Trim());
            var remoteName = node?["remoteName"]?.ToString()?.TrimStart('/')!;
            list.Add(new InstallGameItem
            {
                Type = InstallGameItemType.Verify,
                FileName = Path.GetFileName(remoteName),
                Path = Path.Combine(_installPath, remoteName),
                MD5 = node?["md5"]?.ToString()!,
                Size = (long)(node?["fileSize"] ?? 0),
                Url = prefix + remoteName,
            });
        }
        return list;
    }




    #endregion






    #region Control Method




    public void Continue()
    {
        try
        {
            if (State != InstallGameState.None && State != InstallGameState.Error && State != InstallGameState.Finish)
            {
                return;
            }
            StartTask(_pausedState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(Continue));
        }
    }




    public void Pause()
    {
        try
        {
            if (State is InstallGameState.None)
            {
                return;
            }
            _pausedState = State;
            State = InstallGameState.None;
            _cancellationTokenSource?.Cancel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(Pause));
        }
    }





    public void ClearState()
    {
        try
        {

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(ClearState));
        }
    }




    #endregion






    #region State Convert




    protected void StartTask(InstallGameState state)
    {
        if (_concurrentExecuteThreadCount > 0) return;

        if (state is InstallGameState.Download)
        {
            if (InstallTask is InstallGameTask.HardLink)
            {
                _totalCount = _installItemQueue.Count;
                _finishCount = 0;
                _totalBytes = _installItemQueue.Sum(x => x.Size);
                _finishBytes = 0;
            }
            else
            {
                _totalCount = 0;
                _finishCount = 0;
                _totalBytes = 0;
                _finishBytes = 0;
                foreach (var item in _gamePackageItems ?? [])
                {
                    _totalCount++;
                    _totalBytes += item.Size;
                    long length = GetFileLength(item);
                    _finishBytes += length;
                    if (length == item.Size)
                    {
                        _finishCount++;
                    }
                }
                foreach (var item in _audioPackageItems ?? [])
                {
                    _totalCount++;
                    _totalBytes += item.Size;
                    long length = GetFileLength(item);
                    _finishBytes += length;
                    if (length == item.Size)
                    {
                        _finishCount++;
                    }
                }
                foreach (var item in _gameFileItems ?? [])
                {
                    _totalCount++;
                    _totalBytes += item.Size;
                    long length = GetFileLength(item);
                    _finishBytes += length;
                    if (length == item.Size)
                    {
                        _finishCount++;
                    }
                }
                if (_gameSDKItem is not null)
                {
                    _totalCount++;
                    _totalBytes += _gameSDKItem.Size;
                    long length = GetFileLength(_gameSDKItem);
                    _finishBytes += length;
                    if (length == _gameSDKItem.Size)
                    {
                        _finishCount++;
                    }
                }
            }
        }
        else if (state is InstallGameState.Verify)
        {
            _totalCount = _installItemQueue.Count;
            _finishCount = 0;
            _totalBytes = _installItemQueue.Sum(x => x.Size);
            _finishBytes = 0;
        }
        else if (state is InstallGameState.Decompress)
        {
            _totalCount = _installItemQueue.Count;
            _finishCount = 0;
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

        _ = RunTasksAsync(); //不需要ConfigureAwait，因为返回值丢弃，且无需调用“.GetAwaiter().OnCompleted()”
        return;

        async Task RunTasksAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var tasks = new Task[Environment.ProcessorCount];
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                tasks[i] = ExecuteTaskItemAsync(_cancellationTokenSource.Token);
            }
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            finally
            {
                CurrentTaskFinished();
            }
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




    private void CurrentTaskFinished()
    {
        try
        {
            if (InstallTask is InstallGameTask.Install or InstallGameTask.Update)
            {
                OnInstallOrUpdateTaskFinished();
            }
            else if (InstallTask is InstallGameTask.Repair)
            {
                OnRepairTaskFinished();
            }
            else if (InstallTask is InstallGameTask.Predownload)
            {
                OnPredownloadTaskFinished();
            }
            else if (InstallTask is InstallGameTask.HardLink)
            {
                OnHardLinkTaskFinished();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(CurrentTaskFinished));
            OnInstallFailed(ex);
        }
    }




    protected void OnInstallOrUpdateTaskFinished()
    {
        // download -> verify -> decompress -> clean
        if (State is InstallGameState.Download)
        {
            FromDownloadToVerify();
        }
        else if (State is InstallGameState.Verify)
        {
            if (_verifyFailedItems.IsEmpty)
            {
                FromVerifyToDecompress();
            }
            else
            {
                FromVerifyToDownload();
            }
        }
        else if (State is InstallGameState.Decompress)
        {
            _ = CleanGameDeprecatedFilesAsync();
        }
        else if (State is InstallGameState.Clean)
        {
            Finish();
        }
    }



    protected void OnRepairTaskFinished()
    {
        Debug.WriteLine($"OnRepairTaskFinished - {State}");
        // verify -> download -> decompress (SDK) -> clean
        if (State is InstallGameState.Verify)
        {
            if (!_verifyFailedItems.IsEmpty)
            {
                FromVerifyToDownload();
            }
            else
            {
                if (_gameSDKItem is not null)
                {
                    _gameSDKItem.Type = InstallGameItemType.Decompress;
                    _gameSDKItem.DecompressPackageFiles = [_gameSDKItem.Path];
                    _gameSDKItem.DecompressPath = _installPath;
                    _installItemQueue.Enqueue(_gameSDKItem);
                }
                StartTask(InstallGameState.Decompress);
            }
        }
        else if (State is InstallGameState.Download)
        {
            if (_gameSDKItem is not null)
            {
                _gameSDKItem.Type = InstallGameItemType.Decompress;
                _gameSDKItem.DecompressPackageFiles = [_gameSDKItem.Path];
                _gameSDKItem.DecompressPath = _installPath;
                _installItemQueue.Enqueue(_gameSDKItem);
            }
            StartTask(InstallGameState.Decompress);
        }
        else if (State is InstallGameState.Decompress)
        {
            _ = CleanGameDeprecatedFilesAsync();
        }
        else if (State is InstallGameState.Clean)
        {
            Finish();
        }
    }



    protected void OnPredownloadTaskFinished()
    {
        // download -> verify
        if (State is InstallGameState.Download)
        {
            FromDownloadToVerify();
        }
        else if (State is InstallGameState.Verify)
        {
            if (!_verifyFailedItems.IsEmpty)
            {
                FromVerifyToDownload();
            }
            else
            {
                Finish();
            }
        }
    }



    protected void OnHardLinkTaskFinished()
    {
        // verify -> download -> decompress (SDK) -> clean
        if (State is InstallGameState.Verify)
        {
            if (!_verifyFailedItems.IsEmpty)
            {
                FromVerifyToDownload();
            }
            else
            {
                if (_gameSDKItem is not null)
                {
                    _gameSDKItem.Type = InstallGameItemType.Decompress;
                    _gameSDKItem.DecompressPackageFiles = [_gameSDKItem.Path];
                    _gameSDKItem.DecompressPath = _installPath;
                    _installItemQueue.Enqueue(_gameSDKItem);
                }
                StartTask(InstallGameState.Decompress);
            }
        }
        else if (State is InstallGameState.Download)
        {
            if (_gameSDKItem is not null)
            {
                _gameSDKItem.Type = InstallGameItemType.Decompress;
                _gameSDKItem.DecompressPackageFiles = [_gameSDKItem.Path];
                _gameSDKItem.DecompressPath = _installPath;
                _installItemQueue.Enqueue(_gameSDKItem);
            }
            StartTask(InstallGameState.Decompress);
        }
        else if (State is InstallGameState.Decompress)
        {
            _ = CleanGameDeprecatedFilesAsync();
        }
        else if (State is InstallGameState.Clean)
        {
            Finish();
        }
    }



    protected void FromDownloadToVerify()
    {
        foreach (var item in _gamePackageItems)
        {
            item.Type = InstallGameItemType.Verify;
            _installItemQueue.Enqueue(item);
        }
        foreach (var item in _audioPackageItems)
        {
            item.Type = InstallGameItemType.Verify;
            _installItemQueue.Enqueue(item);
        }
        if (_gameSDKItem is not null)
        {
            _gameSDKItem.Type = InstallGameItemType.Verify;
            _installItemQueue.Enqueue(_gameSDKItem);
        }
        StartTask(InstallGameState.Verify);
    }



    protected void FromVerifyToDownload()
    {
        while (_verifyFailedItems.TryDequeue(out InstallGameItem? item))
        {
            item.Type = InstallGameItemType.Download;
            _installItemQueue.Enqueue(item);
        }
        StartTask(InstallGameState.Download);
    }



    protected void FromVerifyToDecompress()
    {
        var game = new InstallGameItem
        {
            Type = InstallGameItemType.Decompress,
            DecompressPath = _installPath,
            DecompressPackageFiles = _gamePackageItems.Select(x => x.Path).ToList(),
            Size = _gamePackageItems.Sum(x => x.Size),
            DecompressedSize = _gamePackageItems.Sum(x => x.DecompressedSize),
        };
        _installItemQueue.Enqueue(game);
        foreach (var item in _audioPackageItems)
        {
            item.Type = InstallGameItemType.Decompress;
            item.DecompressPackageFiles = [item.Path];
            item.DecompressPath = _installPath;
            _installItemQueue.Enqueue(item);
        }
        if (_gameSDKItem is not null)
        {
            _gameSDKItem.Type = InstallGameItemType.Decompress;
            _gameSDKItem.DecompressPackageFiles = [_gameSDKItem.Path];
            _gameSDKItem.DecompressPath = _installPath;
            _installItemQueue.Enqueue(_gameSDKItem);
        }
        StartTask(InstallGameState.Decompress);
    }



    protected async Task CleanGameDeprecatedFilesAsync()
    {
        State = InstallGameState.Clean;
        foreach (var file in Directory.GetFiles(_installPath, "*_tmp", SearchOption.AllDirectories))
        {
            File.Delete(file);
        }
        foreach (var file in await _hoYoPlayService.GetGameDeprecatedFilesAsync(CurrentGameBiz).ConfigureAwait(false))
        {
            var path = Path.Combine(_installPath, file.Name);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        await WriteConfigFileAsync().ConfigureAwait(false);
        CurrentTaskFinished();
    }



    protected async Task WriteConfigFileAsync()
    {
        string version = _gamePackage.Main.Major?.Version ?? "";
        string sdk_version = _channelSDK?.Version ?? "";
        string cps = "", channel = "1", sub_channel = "1";
        if (CurrentGameBiz.IsBilibili())
        {
            cps = "bilibili";
            channel = "14";
            sub_channel = "0";
        }
        else if (CurrentGameBiz.IsChinaServer())
        {
            cps = "mihoyo";
        }
        else if (CurrentGameBiz.IsGlobalServer())
        {
            cps = "hyp_hoyoverse";
        }
        string config = $"""
            [General]
            channel={channel}
            cps={cps}
            game_version={version}
            sub_channel={sub_channel}
            sdk_version={sdk_version}
            game_biz={CurrentGameBiz}
            """;
        if (!string.IsNullOrWhiteSpace(_hardLinkPath))
        {
            config = $"""
                {config}
                hardlink_gamebiz={_hardLinkGameBiz}
                hardlink_path={_hardLinkPath}
                """;
        }
        _logger.LogInformation("Write config.ini (game_version={version})", version);
        await File.WriteAllTextAsync(Path.Combine(_installPath, "config.ini"), config).ConfigureAwait(false);
    }



    protected void Finish()
    {
        if (State != InstallGameState.Finish)
        {
            State = InstallGameState.Finish;
            StateChanged?.Invoke(this, InstallGameState.Finish);
        }
    }




    #endregion






    #region Execute Item (Download, Verify, Decompress, Patch)





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



    private static SemaphoreSlim _verifyGlobalSemaphore = new SemaphoreSlim(Environment.ProcessorCount);


    private SemaphoreSlim _decompressSemaphore = new SemaphoreSlim(1);



    protected async Task ExecuteTaskItemAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Interlocked.Increment(ref _concurrentExecuteThreadCount);
            while (_installItemQueue.TryDequeue(out InstallGameItem? item))
            {
                try
                {
                    switch (item.Type)
                    {
                        case InstallGameItemType.None:
                            break;
                        case InstallGameItemType.Download:
                            await DownloadItemAsync(item, cancellationToken).ConfigureAwait(false);
                            Interlocked.Increment(ref _finishCount);
                            break;
                        case InstallGameItemType.Verify:
                            await VerifyItemAsync(item, cancellationToken).ConfigureAwait(false);
                            Interlocked.Increment(ref _finishCount);
                            break;
                        case InstallGameItemType.Decompress:
                            await DecompressItemAsync(item, cancellationToken).ConfigureAwait(false);
                            Interlocked.Increment(ref _finishCount);
                            break;
                        case InstallGameItemType.HardLink:
                            await HardLinkItemAsync(item, cancellationToken).ConfigureAwait(false);
                            Interlocked.Increment(ref _finishCount);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or SocketException or HttpIOException or IOException { InnerException: SocketException } or IOException { Message: "Received an unexpected EOF or 0 bytes from the transport stream." })
                {
                    // network error
                    _installItemQueue.Enqueue(item);
                    await Task.Delay(1000, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
                {
                    // cacel
                    _installItemQueue.Enqueue(item);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(ExecuteTaskItemAsync));
            OnInstallFailed(ex);
        }
        finally
        {
            Interlocked.Decrement(ref _concurrentExecuteThreadCount);
        }
    }




    protected async Task DownloadItemAsync(InstallGameItem item, CancellationToken cancellationToken = default)
    {
        const int BUFFER_SIZE = 1 << 10;
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
        var buffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE);
        try
        {
            using var fs = File.Open(file_target, FileMode.OpenOrCreate);
            if (fs.Length < item.Size)
            {
                fs.Position = fs.Length;
                var request = new HttpRequestMessage(HttpMethod.Get, item.Url) { Version = HttpVersion.Version11 };
                request.Headers.Range = new RangeHeaderValue(fs.Length, null);
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                using var hs = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                int length;
                while ((length = await hs.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
                {
                    RateLimitLease lease = await InstallGameManager.RateLimiter.AcquireAsync(length, cancellationToken).ConfigureAwait(false);
                    while (!lease.IsAcquired)
                    {
                        await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                        lease = await InstallGameManager.RateLimiter.AcquireAsync(length, cancellationToken).ConfigureAwait(false);
                    }
                    await fs.WriteAsync(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
                    Interlocked.Add(ref _finishBytes, length);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }





    protected async Task VerifyItemAsync(InstallGameItem item, CancellationToken cancellationToken = default)
    {
        const int BUFFER_SIZE = 1 << 20;
        string file = item.Path;
        string file_tmp = item.Path + "_tmp";
        string file_target;
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
            _verifyFailedItems.Enqueue(item);
            return;
        }
        var buffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE);
        try
        {
            await _verifyGlobalSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            using var fs = File.OpenRead(file_target);
            if (fs.Length != item.Size)
            {
                fs.Dispose();
                File.Delete(file_target);
                _verifyFailedItems.Enqueue(item);
                return;
            }
            int length = 0;
            var hashProvider = MD5.Create();
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
                if (file_target.EndsWith("_tmp", StringComparison.OrdinalIgnoreCase))
                {
                    File.Move(file_target, file, true);
                }
            }
            else
            {
                File.Delete(file_target);
                _verifyFailedItems.Enqueue(item);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            _verifyGlobalSemaphore.Release();
        }
    }





    protected async Task DecompressItemAsync(InstallGameItem item, CancellationToken cancellationToken = default)
    {
        try
        {
            await _decompressSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            using var fs = new FileSliceStream(item.DecompressPackageFiles);
            if (item.DecompressPackageFiles[0].Contains(".7z", StringComparison.CurrentCultureIgnoreCase))
            {
                await Task.Run(async () =>
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
                    extra.Extract(item.DecompressPath, true);
                    _finishBytes += fs.Length - sum;
                    await ApplyDiffFilesAsync(item.DecompressPath).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
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
                            var target = Path.Combine(item.DecompressPath, entry.FullName);
                            Directory.CreateDirectory(target);
                        }
                        else
                        {
                            var target = Path.Combine(item.DecompressPath, entry.FullName);
                            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                            entry.ExtractToFile(target, true);
                            _finishBytes += entry.CompressedLength;
                            sum += entry.CompressedLength;
                        }
                    }
                    _finishBytes += fs.Length - sum;
                    await ApplyDiffFilesAsync(item.DecompressPath).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }
            fs.Dispose();
            foreach (var file in item.DecompressPackageFiles)
            {
                File.Delete(file);
            }
        }
        finally
        {
            _decompressSemaphore.Release();

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




    protected async Task HardLinkItemAsync(InstallGameItem item, CancellationToken cancellationToken = default)
    {
        const int BUFFER_SIZE = 1 << 20;
        string file_source = item.HardLinkSource;
        string file_target = item.Path;

        if (item.HardLinkSkipVerify)
        {
            if (File.Exists(file_source))
            {
                if (File.Exists(file_target))
                {
                    File.Delete(file_target);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(file_target)!);
                Kernel32.CreateHardLink(file_target, file_source);
            }
            return;
        }
        else
        {
            if (!File.Exists(file_source))
            {
                _verifyFailedItems.Enqueue(item);
                return;
            }
        }
        var buffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE);
        try
        {
            await _verifyGlobalSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            using var fs = File.OpenRead(file_source);
            if (fs.Length != item.Size)
            {
                _verifyFailedItems.Enqueue(item);
                return;
            }
            int length = 0;
            var hashProvider = MD5.Create();
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
                if (File.Exists(file_target))
                {
                    File.Delete(file_target);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(file_target)!);
                Kernel32.CreateHardLink(file_target, file_source);
            }
            else
            {
                _verifyFailedItems.Enqueue(item);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            _verifyGlobalSemaphore.Release();
        }
    }




    #endregion





}
