using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.RPC.Env;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.RPC.GameInstall;

internal class GameInstallService
{


    private readonly ILogger<GameInstallService> _logger;


    private readonly IServiceProvider _serviceProvider;


    private readonly ResiliencePipeline _polly;


    private readonly GameInstallHelper _gameInstallHelper;


    private readonly ConcurrentDictionary<GameId, GameInstallTask> _tasks = new();


    public event EventHandler<GameInstallTask>? TaskStateChanged;

    public GameInstallTask? CurrentTask { get; private set; }



    public GameInstallService(ILogger<GameInstallService> logger, IServiceProvider serviceProvider, GameInstallHelper gameInstallHelper)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _gameInstallHelper = gameInstallHelper;
        _gameInstallHelper = gameInstallHelper;
        _polly = new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 5,
            BackoffType = DelayBackoffType.Linear
        }).Build();
        LifecycleManager.ParentProcessExited += LifecycleManager_ParentProcessExited;
    }



    /// <summary>
    /// 停止所有任务
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void LifecycleManager_ParentProcessExited(object? sender, Process e)
    {
        try
        {
            _logger.LogInformation("Parent process {name} ({pid}) exited, stop all game install tasks.", e.ProcessName, e.Id);
            foreach (var item in _tasks)
            {
                item.Value.Cancel(GameInstallState.Stop);
            }
        }
        catch (Exception ex)
        {

        }
    }




    public bool TryGetTask(GameId gameId, [NotNullWhen(true)] out GameInstallTask? task)
    {
        return _tasks.TryGetValue(gameId, out task);
    }



    /// <summary>
    /// 开始或继续任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public GameInstallTaskDTO StartOrContinueTask(GameInstallTaskRequest request)
    {
        if (_tasks.TryGetValue(request.GetGameId(), out GameInstallTask? task))
        {
            if (task.Operation != (GameInstallOperation)request.Operation)
            {
                // 操作不一样，则取消上次任务
                _logger.LogInformation("The new task operation is different from the previous task, cancel the previous task, GameBiz: {game_biz}, Operation: {operation}", task.GameId.GameBiz, task.Operation);
                task.Cancel(GameInstallState.Stop);
                _tasks.TryRemove(task.GameId, out _);
                task = request.ToTask();
                _tasks.TryAdd(task.GameId, task);
            }
        }
        else
        {
            task = request.ToTask();
            _tasks.TryAdd(task.GameId, task);
        }
        return StartOrContinueTask(task);
    }



    /// <summary>
    /// 开始或继续任务
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    private GameInstallTaskDTO StartOrContinueTask(GameInstallTask task)
    {
        if (task.State is GameInstallState.Stop && CurrentTask != null && CurrentTask != task)
        {
            // 第一次开始时，如果有其他任务在执行，排队
            _logger.LogInformation("Queueing GameInstallTask, GameBiz: {game_biz}, Operation: {operation}", task.GameId.GameBiz, task.Operation);
            task.State = GameInstallState.Queueing;
            return GameInstallTaskDTO.FromTask(task);
        }
        if (CurrentTask != null && CurrentTask != task)
        {
            CurrentTask.Cancel(GameInstallState.Queueing);
            TaskStateChanged?.Invoke(this, CurrentTask);
        }
        CurrentTask = task;
        if (task.State is GameInstallState.Finish)
        {
            ChangeToAnotherTask(task);
            return GameInstallTaskDTO.FromTask(task);
        }
        else if (task.State is GameInstallState.Waiting or GameInstallState.Downloading or GameInstallState.Decompressing or GameInstallState.Verifying)
        {
            return GameInstallTaskDTO.FromTask(task);
        }
        task.State = GameInstallState.Waiting;
        task.ErrorMessage = null;
        _ = PrepareGameInstallTaskAsync(task, task.CancellationToken);
        return GameInstallTaskDTO.FromTask(task);
    }



    /// <summary>
    /// 暂停任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public GameInstallTaskDTO PauseTask(GameInstallTaskRequest request)
    {
        if (_tasks.TryGetValue(request.GetGameId(), out GameInstallTask? task))
        {
            task.Cancel(GameInstallState.Paused);
        }
        else
        {
            task = request.ToTask();
            task.State = GameInstallState.Stop;
        }
        return GameInstallTaskDTO.FromTask(task);
    }


    /// <summary>
    /// 停止任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public GameInstallTaskDTO StopTask(GameInstallTaskRequest request)
    {
        if (_tasks.TryRemove(request.GetGameId(), out GameInstallTask? task))
        {
            task.Cancel(GameInstallState.Stop);
            task.State = GameInstallState.Stop;
        }
        else
        {
            task = request.ToTask();
            task.State = GameInstallState.Stop;
        }
        return GameInstallTaskDTO.FromTask(task);
    }




    /// <summary>
    /// 准备游戏任务
    /// </summary>
    /// <param name="task"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task PrepareGameInstallTaskAsync(GameInstallTask task, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("""
            Start game install task: 
            Operation: {operation}
            GameId: {gameId} {gameBiz}
            InstallPath: {installPath}
            AudioLanguage: {audioLanguage}
            HardLinkPath: {hardLinkPath}
            """, task.Operation, task.GameId.Id, task.GameId.GameBiz, task.InstallPath, task.AudioLanguage, task.HardLinkPath);
            Directory.CreateDirectory(task.InstallPath);
            GamePackageService gamePackageService = _serviceProvider.GetRequiredService<GamePackageService>();
            if (task.AudioLanguage is not AudioLanguage.None)
            {
                await gamePackageService.SetAudioLanguageAsync(task.GameId, task.InstallPath, task.AudioLanguage, cancellationToken);
            }
            if (task.TaskFiles is null)
            {
                await gamePackageService.PrepareGamePackageAsync(task, cancellationToken);
            }

            foreach (string item in Directory.GetFiles(task.InstallPath, "*", SearchOption.AllDirectories))
            {
                // 设置所有文件为正常状态，防止遇到只读文件报错
                File.SetAttributes(item, FileAttributes.Normal);
            }

            if (task.Operation is GameInstallOperation.Install)
            {
                // 安装
                await ExecuteInstallTaskAsync(task, cancellationToken);
            }
            else if (task.Operation is GameInstallOperation.Update)
            {
                // 更新
                await ExecuteUpdateTaskAsnyc(task, cancellationToken);
            }
            else if (task.Operation is GameInstallOperation.Predownload)
            {
                // 预下载
                await ExecutePredownloadTaskAsync(task, cancellationToken);
            }
            else if (task.Operation is GameInstallOperation.Repair)
            {
                // 修复
                await ExecuteRepairTaskAsync(task, cancellationToken);
            }
            else
            {
                _logger.LogWarning("GameInstallTask ({GameBiz}): Unsupported Operation: {operation}", task.GameId.GameBiz, task.Operation);
            }

            ClearDeprecatedFiles(task);

            task.State = GameInstallState.Finish;
            _logger.LogInformation("GameInstallTask Finished, GameBiz: {game_biz}, Operation: {operation}", task.GameId.GameBiz, task.Operation);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation("GameInstallTask canceled, GameBiz: {game_biz}, Operation: {operation}, CancelState: {state}", task.GameId.GameBiz, task.Operation, task.CancelState);
            task.State = task.CancelState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PrepareGameInstallTaskAsync");
            task.State = GameInstallState.Error;
            task.ErrorMessage = ex.Message;
        }
        ChangeToAnotherTask(task);
    }




    /// <summary>
    /// 切换到另一个任务
    /// </summary>
    /// <param name="task"></param>
    private void ChangeToAnotherTask(GameInstallTask task)
    {
        if (task.State is GameInstallState.Stop or GameInstallState.Finish)
        {
            _tasks.TryRemove(task.GameId, out _);
        }
        TaskStateChanged?.Invoke(this, task);
        if (_tasks.Values.FirstOrDefault(x => x.State is not GameInstallState.Paused and not GameInstallState.Error && x != task) is GameInstallTask anotherTask)
        {
            CurrentTask = anotherTask;
            StartOrContinueTask(anotherTask);
        }
        else
        {
            CurrentTask = null;
        }
    }




    /// <summary>
    /// 开始安装
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    private async Task ExecuteInstallTaskAsync(GameInstallTask task, CancellationToken cancellationToken = default)
    {
        if (task.DownloadMode is GameInstallDownloadMode.Chunk)
        {
            await ExecuteInstallTaskDownloadModeChunkAsync(task, cancellationToken);
        }
        else if (task.DownloadMode is GameInstallDownloadMode.CompressedPackage)
        {
            await ExecuteInstallTaskDownloadModePackageAsync(task, cancellationToken);
        }

        await DownloadGameChannelSDKAsync(task, cancellationToken);
        await SetGameConfigIniAsync(task);
    }



    /// <summary>
    /// 安装游戏，下载模式为 Chunk
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    private async Task ExecuteInstallTaskDownloadModeChunkAsync(GameInstallTask task, CancellationToken cancellationToken = default)
    {
        long downloadBytes = 0, writeBytes = 0;
        foreach (GameInstallFile item in task.TaskFiles ?? [])
        {
            writeBytes += item.Size;
            foreach (GameInstallFileChunk chunk in item.Chunks ?? [])
            {
                downloadBytes += chunk.CompressedSize;
            }
        }
        task.Progress_DownloadTotalBytes = downloadBytes;
        task.Progress_DownloadFinishBytes = 0;
        task.Progress_WriteTotalBytes = writeBytes;
        task.Progress_WriteFinishBytes = 0;

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start downloading in mode chunk", task.GameId.GameBiz);
        task.State = GameInstallState.Downloading;
        await Parallel.ForEachAsync(task.TaskFiles ?? [], cancellationToken, async (GameInstallFile file, CancellationToken token) =>
        {
            await _polly.ExecuteAsync(async token => await _gameInstallHelper.DownloadChunksToFileAsync(task, file, false, token), token);
            file.IsFinished = true;
        });
    }



    /// <summary>
    /// 安装游戏，下载模式为 CompressedPackage
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    private async Task ExecuteInstallTaskDownloadModePackageAsync(GameInstallTask task, CancellationToken cancellationToken = default)
    {
        var packages = task.TaskFiles!.SelectMany(x => x.CompressedPackages!).ToList();
        task.Progress_DownloadTotalBytes = packages.Sum(x => x.Size);
        task.Progress_DownloadFinishBytes = 0;

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start downloading in mode package", task.GameId.GameBiz);
        task.State = GameInstallState.Downloading;
        await Parallel.ForEachAsync(packages, cancellationToken, async (GameInstallCompressedPackage package, CancellationToken token) =>
        {
            await _polly.ExecuteAsync(async token => await _gameInstallHelper.DownloadToFileAsync(task, package.FullPath, package.Url, package.Size, package.MD5, token), token);
        });

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start decompressing in mode package", task.GameId.GameBiz);
        task.State = GameInstallState.Decompressing;
        task.Progress_Percent = 0;
        double totalSize = packages.Sum(x => x.Size);
        foreach (GameInstallFile item in task.TaskFiles ?? [])
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Decompress operation was canceled.", cancellationToken);
            }
            double ratio = item.Size / totalSize;
            if (item.IsFinished)
            {
                task.Progress_Percent += ratio;
                continue;
            }
            // SevenZipExtractor 解压被取消不会抛出异常
            await _gameInstallHelper.ExtractCompressedPackageAsync(task, item, ratio, cancellationToken);
            item.IsFinished = true;
        }
        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Decompress operation was canceled.", cancellationToken);
        }

    }




    /// <summary>
    /// 开始更新
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    private async Task ExecuteUpdateTaskAsnyc(GameInstallTask task, CancellationToken cancellationToken = default)
    {
        // 如果音频包缓存目录和资源目录不一样，则移动缓存目录的文件到资源目录
        if (!string.IsNullOrWhiteSpace(task.GameConfig?.AudioPackageCacheDir)
            && !string.IsNullOrWhiteSpace(task.GameConfig?.AudioPackageResDir)
            && task.GameConfig.AudioPackageCacheDir != task.GameConfig.AudioPackageResDir)
        {
            string cache = Path.GetFullPath(Path.Combine(task.InstallPath, task.GameConfig.AudioPackageCacheDir));
            string res = Path.GetFullPath(Path.Combine(task.InstallPath, task.GameConfig.AudioPackageResDir));
            if (Directory.Exists(cache))
            {
                string[] files = Directory.GetFiles(cache, "*", SearchOption.AllDirectories);
                foreach (string source in files)
                {
                    string relative = Path.GetRelativePath(cache, source);
                    string target = Path.GetFullPath(relative, res);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Move(source, target, true);
                }
                _logger.LogInformation("GameInstallTask ({GameBiz}): Move audio package cache files (Count: {count}) from {cacheDir} to res dir {resDir}", task.GameId.GameBiz, files.Length, cache, res);
            }
        }

        if (task.DownloadMode is GameInstallDownloadMode.Patch)
        {
            await ExecuteUpdateTaskDownloadPatchAsync(task, cancellationToken);
        }
        else if (task.DownloadMode is GameInstallDownloadMode.Chunk)
        {
            await ExecuteUpdateTaskDownloadModeChunkAsync(task, cancellationToken);
        }
        else if (task.DownloadMode is GameInstallDownloadMode.CompressedPackage)
        {
            await ExecuteUpdateTaskDownloadPackageAsync(task, cancellationToken);
        }

        await DownloadGameChannelSDKAsync(task, cancellationToken);
        await SetGameConfigIniAsync(task, ("predownload", null));
    }




    /// <summary>
    /// 开始更新，下载模式为 Patch
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    private async Task ExecuteUpdateTaskDownloadPatchAsync(GameInstallTask task, CancellationToken cancellationToken = default)
    {
        List<PredownloadFile> files = GameInstallHelper.GetPredownloadFiles(task);

        task.Progress_DownloadTotalBytes = files.Sum(x => x.Size);
        task.Progress_DownloadFinishBytes = 0;
        task.State = GameInstallState.Downloading;

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start downloading in mode patch", task.GameId.GameBiz);
        await Parallel.ForEachAsync(files, cancellationToken, async (PredownloadFile item, CancellationToken token) =>
        {
            await _polly.ExecuteAsync(async token => await _gameInstallHelper.DownloadToFileAsync(task, item, token), token);
        });

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start merging in mode patch, file count: {count}", task.GameId.GameBiz, task.TaskFiles?.Count);
        task.State = GameInstallState.Merging;
        task.Progress_Percent = 0;
        double totalCount = task.TaskFiles?.Count ?? 1;
        double increase = 1 / totalCount;
        Lock _lock = new();
        await Parallel.ForEachAsync(task.TaskFiles ?? [], cancellationToken, async (GameInstallFile item, CancellationToken token) =>
        {
            if (item.IsFinished)
            {
                lock (_lock)
                {
                    task.Progress_Percent += increase;
                }
                return;
            }
            await _gameInstallHelper.PatchDiffFileAsync(task, item, cancellationToken);
            task.Progress_Percent += increase;
            item.IsFinished = true;
        });

        if (task.SophonPatchDeleteFiles is not null)
        {
            foreach (SophonPatchDeleteFile item in task.SophonPatchDeleteFiles)
            {
                string path = Path.Combine(task.InstallPath, item.File);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            _logger.LogInformation("GameInstallTask ({GameBiz}): Delete files by SophonPatchDeleteFiles, file count: {count}", task.GameId.GameBiz, task.SophonPatchDeleteFiles.Count);
        }

    }



    /// <summary>
    /// 开始更新，下载模式为 Chunk
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    private async Task ExecuteUpdateTaskDownloadModeChunkAsync(GameInstallTask task, CancellationToken cancellationToken = default)
    {
        long downloadBytes = 0, writeBytes = 0;
        foreach (GameInstallFile item in task.TaskFiles ?? [])
        {
            writeBytes += item.Size;
            foreach (GameInstallFileChunk chunk in item.Chunks ?? [])
            {
                if (string.IsNullOrWhiteSpace(chunk.OriginalFileFullPath))
                {
                    downloadBytes += chunk.CompressedSize;
                }
            }
        }
        task.Progress_DownloadTotalBytes = downloadBytes;
        task.Progress_DownloadFinishBytes = 0;
        task.Progress_WriteTotalBytes = writeBytes;
        task.Progress_WriteFinishBytes = 0;

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start downloading in mode chunk", task.GameId.GameBiz);
        task.State = GameInstallState.Downloading;
        await Parallel.ForEachAsync(task.TaskFiles ?? [], cancellationToken, async (GameInstallFile file, CancellationToken token) =>
        {
            await _polly.ExecuteAsync(async token => await _gameInstallHelper.DownloadChunksToFileAsync(task, file, true, token), token);
            file.IsFinished = true;
        });
    }



    /// <summary>
    /// 开始更新，下载模式为 CompressedPackage
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    /// <exception cref="OperationCanceledException"></exception>
    private async Task ExecuteUpdateTaskDownloadPackageAsync(GameInstallTask task, CancellationToken cancellationToken = default)
    {
        List<PredownloadFile> files = GameInstallHelper.GetPredownloadFiles(task);

        task.Progress_DownloadTotalBytes = files.Sum(x => x.Size);
        task.Progress_DownloadFinishBytes = 0;
        task.State = GameInstallState.Downloading;

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start downloading in mode package", task.GameId.GameBiz);
        await Parallel.ForEachAsync(files, cancellationToken, async (PredownloadFile item, CancellationToken token) =>
        {
            await _polly.ExecuteAsync(async token => await _gameInstallHelper.DownloadToFileAsync(task, item, token), token);
        });

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start decompressing in mode package", task.GameId.GameBiz);
        task.State = GameInstallState.Decompressing;
        task.Progress_Percent = 0;
        double totalSize = files.Sum(x => x.Size);
        foreach (GameInstallFile item in task.TaskFiles ?? [])
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("The operation was canceled.", cancellationToken);
            }
            double ratio = item.Size / totalSize;
            if (item.IsFinished)
            {
                task.Progress_Percent += ratio;
                continue;
            }
            // SevenZipExtractor 解压被取消不会抛出异常
            await _gameInstallHelper.ExtractCompressedPackageAsync(task, item, ratio, cancellationToken);
            item.IsFinished = true;
        }
        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("The operation was canceled.", cancellationToken);
        }
    }




    /// <summary>
    /// 开始预下载
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    private async Task ExecutePredownloadTaskAsync(GameInstallTask task, CancellationToken cancellationToken = default)
    {
        List<PredownloadFile> files = GameInstallHelper.GetPredownloadFiles(task);

        task.Progress_DownloadTotalBytes = files.Sum(x => x.Size);
        task.Progress_DownloadFinishBytes = 0;
        task.State = GameInstallState.Downloading;

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start downloading in mode predownload", task.GameId.GameBiz);
        await Parallel.ForEachAsync(files, cancellationToken, async (PredownloadFile item, CancellationToken token) =>
        {
            await _polly.ExecuteAsync(async token => await _gameInstallHelper.DownloadToFileAsync(task, item, token), token);
        });
        GamePackageService gamePackageService = _serviceProvider.GetRequiredService<GamePackageService>();
        string value = $"{task.LocalGameVersion},{task.PredownloadVersion},{task.AudioLanguage}";
        await SetGameConfigIniAsync(task, ("predownload", value));
    }



    /// <summary>
    /// 开始修复
    /// </summary>
    /// <param name="task"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ExecuteRepairTaskAsync(GameInstallTask task, CancellationToken cancellationToken = default)
    {
        // 如果音频包缓存目录和资源目录不一样，则移动缓存目录的文件到资源目录
        if (!string.IsNullOrWhiteSpace(task.GameConfig?.AudioPackageCacheDir)
            && !string.IsNullOrWhiteSpace(task.GameConfig?.AudioPackageResDir)
            && task.GameConfig.AudioPackageCacheDir != task.GameConfig.AudioPackageResDir)
        {
            string cache = Path.GetFullPath(Path.Combine(task.InstallPath, task.GameConfig.AudioPackageCacheDir));
            string res = Path.GetFullPath(Path.Combine(task.InstallPath, task.GameConfig.AudioPackageResDir));
            if (Directory.Exists(cache))
            {
                string[] files = Directory.GetFiles(cache, "*", SearchOption.AllDirectories);
                foreach (string source in files)
                {
                    string relative = Path.GetRelativePath(cache, source);
                    string target = Path.GetFullPath(relative, res);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Move(source, target, true);
                }
                _logger.LogInformation("GameInstallTask ({GameBiz}): Move audio package cache files (Count: {count}) from {cacheDir} to res dir {resDir}", task.GameId.GameBiz, files.Length, cache, res);
            }
        }

        if (task.DownloadMode is GameInstallDownloadMode.Chunk)
        {
            await ExecuteInstallTaskDownloadModeChunkAsync(task, cancellationToken);
        }
        else if (task.DownloadMode is GameInstallDownloadMode.SingleFile)
        {
            task.Progress_DownloadTotalBytes = task.TaskFiles!.Sum(x => x.Size);
            task.Progress_DownloadFinishBytes = 0;

            _logger.LogInformation("GameInstallTask ({GameBiz}): Start downloading in mode single file", task.GameId.GameBiz);
            task.State = GameInstallState.Downloading;
            await Parallel.ForEachAsync(task.TaskFiles!, cancellationToken, async (GameInstallFile file, CancellationToken token) =>
            {
                await _polly.ExecuteAsync(async token => await _gameInstallHelper.DownloadToFileAsync(task, file, token), token);
                file.IsFinished = true;
            });
        }

        // todo celar useless audio

        await DownloadGameChannelSDKAsync(task, cancellationToken);
        await SetGameConfigIniAsync(task);
    }



    /// <summary>
    /// 下载游戏渠道 SDK
    /// </summary>
    /// <param name="task"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task DownloadGameChannelSDKAsync(GameInstallTask task, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GameInstallTask ({GameBiz}): Start downloading GameChannelSDK", task.GameId.GameBiz);
        await _polly.ExecuteAsync(async token => await _gameInstallHelper.DownloadGameChannelSDKAsync(task, token), cancellationToken);
    }



    /// <summary>
    /// 清理文件
    /// </summary>
    /// <param name="task"></param>
    private void ClearDeprecatedFiles(GameInstallTask task)
    {
        if (task.Operation is not GameInstallOperation.Predownload)
        {
            int count = 0;
            foreach (GameInstallFile item in task.TaskFiles ?? [])
            {
                foreach (GameInstallCompressedPackage package in item.CompressedPackages ?? [])
                {
                    if (File.Exists(package.FullPath))
                    {
                        File.Delete(package.FullPath);
                        count++;
                    }
                }
            }
            foreach (GameDeprecatedFile item in task.DeprecatedFileConfig?.DeprecatedFiles ?? [])
            {
                string path = Path.Combine(task.InstallPath, item.Name);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    count++;
                }
            }
            if (task.PredownloadVersion is null)
            {
                foreach (string file in Directory.GetFiles(task.InstallPath, "*_tmp", SearchOption.AllDirectories))
                {
                    File.Delete(file);
                    count++;
                }
                foreach (string file in Directory.GetFiles(task.InstallPath, "*.hdiff", SearchOption.AllDirectories))
                {
                    File.Delete(file);
                    count++;
                }
                string chunk = Path.Combine(task.InstallPath, "chunk");
                if (Directory.Exists(chunk))
                {
                    Directory.Delete(chunk, true);
                }
                string staging = Path.Combine(task.InstallPath, "staging");
                if (Directory.Exists(staging))
                {
                    Directory.Delete(staging, true);
                }
            }
            _logger.LogInformation("GameInstallTask ({GameBiz}): Clear deprecated files, count: {count}", task.GameId.GameBiz, count);
        }
    }




    /// <summary>
    /// 设置游戏的 config.ini
    /// </summary>
    /// <param name="task"></param>
    /// <param name="keyValuePairs"></param>
    /// <returns></returns>
    private async Task SetGameConfigIniAsync(GameInstallTask task, params IEnumerable<(string Key, string? Value)> keyValuePairs)
    {
        string path = Path.Join(task.InstallPath, "config.ini");
        using MemoryStream ms = new MemoryStream();
        if (File.Exists(path))
        {
            using StreamWriter sw = new StreamWriter(ms, leaveOpen: true);
            foreach (string line in await File.ReadAllLinesAsync(path))
            {
                if (!line.Contains("[General]", StringComparison.OrdinalIgnoreCase))
                {
                    sw.WriteLine(line);
                }
            }
        }
        IConfigurationRoot config = new ConfigurationBuilder().AddIniStream(ms).Build();
        if (task.Operation is GameInstallOperation.Predownload)
        {
            config["game_version"] = task.LocalGameVersion;
        }
        else
        {
            config["game_version"] = task.LatestGameVersion;
        }
        if (task.GameId.GameBiz.Server is "cn")
        {
            config["channel"] = "1";
            config["sub_channel"] = "1";
            config["cps"] = "hyp_mihoyo";
        }
        else if (task.GameId.GameBiz.Server is "global")
        {
            config["channel"] = "1";
            config["sub_channel"] = "0";
            config["cps"] = "hyp_hoyoverse";
        }
        else if (task.GameId.GameBiz.Server is "bilibili")
        {
            config["channel"] = "14";
            config["sub_channel"] = "0";
            config["cps"] = "hyp_mihoyo";
        }
        config["sdk_version"] = task.GameChannelSDK?.Version ?? "";
        config["game_biz"] = task.GameId.GameBiz;

        foreach ((string key, string? value) in keyValuePairs)
        {
            config[key] = value;
        }
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[General]");
        foreach (KeyValuePair<string, string?> item in config.AsEnumerable())
        {
            sb.AppendLine($"{item.Key}={item.Value}");
        }
        Directory.CreateDirectory(task.InstallPath);
        await File.WriteAllTextAsync(path, sb.ToString());
        _logger.LogInformation("GameInstallTask ({GameBiz}): Set config.ini, path: {path}", task.GameId.GameBiz, path);
    }



}


