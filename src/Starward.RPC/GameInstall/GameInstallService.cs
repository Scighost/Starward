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


    private readonly ConcurrentDictionary<GameId, GameInstallContext> _tasks = new();


    public event EventHandler<GameInstallContext>? TaskStateChanged;

    public GameInstallContext? CurrentTask { get; private set; }



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
            _logger.LogError(ex, "Cancel all install task");
        }
    }




    public bool TryGetTask(GameId gameId, [NotNullWhen(true)] out GameInstallContext? context)
    {
        return _tasks.TryGetValue(gameId, out context);
    }



    /// <summary>
    /// 开始或继续任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public GameInstallContextDTO StartOrContinueTask(GameInstallRequest request)
    {
        if (_tasks.TryGetValue(request.GetGameId(), out GameInstallContext? context))
        {
            if (context.Operation != (GameInstallOperation)request.Operation)
            {
                // 操作不一样，则取消上次任务
                _logger.LogInformation("The new task operation is different from the previous task, cancel the previous task, GameBiz: {game_biz}, Operation: {operation}", context.GameId.GameBiz, context.Operation);
                context.Cancel(GameInstallState.Stop);
                _tasks.TryRemove(context.GameId, out _);
                context = request.ToTask();
                _tasks.TryAdd(context.GameId, context);
            }
        }
        else
        {
            context = request.ToTask();
            _tasks.TryAdd(context.GameId, context);
        }
        return StartOrContinueTask(context);
    }



    /// <summary>
    /// 开始或继续任务
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private GameInstallContextDTO StartOrContinueTask(GameInstallContext context)
    {
        if (context.State is GameInstallState.Stop && CurrentTask != null && CurrentTask != context)
        {
            // 第一次开始时，如果有其他任务在执行，排队
            _logger.LogInformation("Queueing GameInstallTask, GameBiz: {game_biz}, Operation: {operation}", context.GameId.GameBiz, context.Operation);
            context.State = GameInstallState.Queueing;
            return GameInstallContextDTO.FromTask(context);
        }
        if (CurrentTask != null && CurrentTask != context)
        {
            CurrentTask.Cancel(GameInstallState.Queueing);
            TaskStateChanged?.Invoke(this, CurrentTask);
        }
        CurrentTask = context;
        if (context.State is GameInstallState.Finish)
        {
            ChangeToAnotherTask(context);
            return GameInstallContextDTO.FromTask(context);
        }
        else if (context.State is GameInstallState.Waiting or GameInstallState.Downloading or GameInstallState.Decompressing or GameInstallState.Verifying)
        {
            return GameInstallContextDTO.FromTask(context);
        }
        context.State = GameInstallState.Waiting;
        context.ErrorMessage = null;
        _ = PrepareGameInstallTaskAsync(context, context.CancellationToken);
        return GameInstallContextDTO.FromTask(context);
    }



    /// <summary>
    /// 暂停任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public GameInstallContextDTO PauseTask(GameInstallRequest request)
    {
        if (_tasks.TryGetValue(request.GetGameId(), out GameInstallContext? context))
        {
            context.Cancel(GameInstallState.Paused);
        }
        else
        {
            context = request.ToTask();
            context.State = GameInstallState.Stop;
        }
        return GameInstallContextDTO.FromTask(context);
    }


    /// <summary>
    /// 停止任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public GameInstallContextDTO StopTask(GameInstallRequest request)
    {
        if (_tasks.TryRemove(request.GetGameId(), out GameInstallContext? context))
        {
            context.Cancel(GameInstallState.Stop);
            context.State = GameInstallState.Stop;
        }
        else
        {
            context = request.ToTask();
            context.State = GameInstallState.Stop;
        }
        return GameInstallContextDTO.FromTask(context);
    }




    /// <summary>
    /// 准备游戏任务
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task PrepareGameInstallTaskAsync(GameInstallContext context, CancellationToken cancellationToken = default)
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
                """, context.Operation, context.GameId.Id, context.GameId.GameBiz, context.InstallPath, context.AudioLanguage, context.HardLinkPath);
            Directory.CreateDirectory(context.InstallPath);
            GamePackageService gamePackageService = _serviceProvider.GetRequiredService<GamePackageService>();
            if (context.AudioLanguage is not AudioLanguage.None)
            {
                await gamePackageService.SetAudioLanguageAsync(context.GameId, context.InstallPath, context.AudioLanguage, cancellationToken);
            }
            if (context.TaskFiles is null)
            {
                await gamePackageService.PrepareGamePackageAsync(context, cancellationToken);
            }

            foreach (string item in Directory.GetFiles(context.InstallPath, "*", SearchOption.AllDirectories))
            {
                // 设置所有文件为正常状态，防止遇到只读文件报错
                File.SetAttributes(item, FileAttributes.Normal);
            }

            if (context.Operation is GameInstallOperation.Install)
            {
                // 安装
                await ExecuteInstallTaskAsync(context, cancellationToken);
            }
            else if (context.Operation is GameInstallOperation.Update)
            {
                // 更新
                await ExecuteUpdateTaskAsnyc(context, cancellationToken);
            }
            else if (context.Operation is GameInstallOperation.Predownload)
            {
                // 预下载
                await ExecutePredownloadTaskAsync(context, cancellationToken);
            }
            else if (context.Operation is GameInstallOperation.Repair)
            {
                // 修复
                await ExecuteRepairTaskAsync(context, cancellationToken);
            }
            else
            {
                _logger.LogWarning("GameInstallTask ({GameBiz}): Unsupported Operation: {operation}", context.GameId.GameBiz, context.Operation);
            }

            ClearDeprecatedFiles(context);

            context.State = GameInstallState.Finish;
            _logger.LogInformation("GameInstallTask Finished, GameBiz: {game_biz}, Operation: {operation}", context.GameId.GameBiz, context.Operation);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "PrepareGameInstallTaskAsync");
            context.State = GameInstallState.Error;
            context.ErrorMessage = ex.InnerException.Message;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GameInstallTask canceled, GameBiz: {game_biz}, Operation: {operation}, CancelState: {state}", context.GameId.GameBiz, context.Operation, context.CancelState);
            context.State = context.CancelState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PrepareGameInstallTaskAsync");
            context.State = GameInstallState.Error;
            context.ErrorMessage = ex.Message;
        }
        ChangeToAnotherTask(context);
    }




    /// <summary>
    /// 切换到另一个任务
    /// </summary>
    /// <param name="context"></param>
    private void ChangeToAnotherTask(GameInstallContext context)
    {
        if (context.State is GameInstallState.Stop or GameInstallState.Finish)
        {
            _tasks.TryRemove(context.GameId, out _);
        }
        TaskStateChanged?.Invoke(this, context);
        if (_tasks.Values.FirstOrDefault(x => x.State is not GameInstallState.Paused and not GameInstallState.Error && x != context) is GameInstallContext anotherTask)
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
    /// <param name="context"></param>
    /// <returns></returns>
    private async Task ExecuteInstallTaskAsync(GameInstallContext context, CancellationToken cancellationToken = default)
    {
        if (context.DownloadMode is GameInstallDownloadMode.Chunk)
        {
            await ExecuteInstallTaskDownloadModeChunkAsync(context, cancellationToken);
        }
        else if (context.DownloadMode is GameInstallDownloadMode.CompressedPackage)
        {
            await ExecuteInstallTaskDownloadModePackageAsync(context, cancellationToken);
        }

        await DownloadGameChannelSDKAsync(context, cancellationToken);
        await SetGameConfigIniAsync(context);
    }



    /// <summary>
    /// 安装游戏，下载模式为 Chunk
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private async Task ExecuteInstallTaskDownloadModeChunkAsync(GameInstallContext context, CancellationToken cancellationToken = default)
    {
        long downloadBytes = 0, writeBytes = 0;
        foreach (GameInstallFile item in context.TaskFiles ?? [])
        {
            writeBytes += item.Size;
            foreach (GameInstallFileChunk chunk in item.Chunks ?? [])
            {
                downloadBytes += chunk.CompressedSize;
            }
        }
        context.Progress_DownloadTotalBytes = downloadBytes;
        context.Progress_DownloadFinishBytes = 0;
        context.Progress_WriteTotalBytes = writeBytes;
        context.Progress_WriteFinishBytes = 0;

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start downloading in mode chunk", context.GameId.GameBiz);
        context.State = GameInstallState.Downloading;
        await Parallel.ForEachAsync(context.TaskFiles ?? [], cancellationToken, async (GameInstallFile file, CancellationToken token) =>
        {
            await _polly.ExecuteAsync(async token => await _gameInstallHelper.DownloadChunksToFileAsync(context, file, false, token), token);
            file.IsFinished = true;
        });
    }



    /// <summary>
    /// 安装游戏，下载模式为 CompressedPackage
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private async Task ExecuteInstallTaskDownloadModePackageAsync(GameInstallContext context, CancellationToken cancellationToken = default)
    {
        var packages = context.TaskFiles!.SelectMany(x => x.CompressedPackages!).ToList();
        context.Progress_DownloadTotalBytes = packages.Sum(x => x.Size);
        context.Progress_DownloadFinishBytes = 0;

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start downloading in mode package", context.GameId.GameBiz);
        context.State = GameInstallState.Downloading;
        await Parallel.ForEachAsync(packages, cancellationToken, async (GameInstallCompressedPackage package, CancellationToken token) =>
        {
            await _polly.ExecuteAsync(async token => await _gameInstallHelper.DownloadToFileAsync(context, package.FullPath, package.Url, package.Size, package.MD5, token), token);
        });

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start decompressing in mode package", context.GameId.GameBiz);
        context.State = GameInstallState.Decompressing;
        context.Progress_Percent = 0;
        double totalSize = packages.Sum(x => x.Size);
        foreach (GameInstallFile item in context.TaskFiles ?? [])
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Decompress operation was canceled.", cancellationToken);
            }
            double ratio = item.Size / totalSize;
            if (item.IsFinished)
            {
                context.Progress_Percent += ratio;
                continue;
            }
            // SevenZipExtractor 解压被取消不会抛出异常
            await _gameInstallHelper.ExtractCompressedPackageAsync(context, item, ratio, cancellationToken);
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
    /// <param name="context"></param>
    /// <returns></returns>
    private async Task ExecuteUpdateTaskAsnyc(GameInstallContext context, CancellationToken cancellationToken = default)
    {
        // 如果音频包缓存目录和资源目录不一样，则移动缓存目录的文件到资源目录
        if (!string.IsNullOrWhiteSpace(context.GameConfig?.AudioPackageCacheDir)
            && !string.IsNullOrWhiteSpace(context.GameConfig?.AudioPackageResDir)
            && context.GameConfig.AudioPackageCacheDir != context.GameConfig.AudioPackageResDir)
        {
            string cache = Path.GetFullPath(Path.Combine(context.InstallPath, context.GameConfig.AudioPackageCacheDir));
            string res = Path.GetFullPath(Path.Combine(context.InstallPath, context.GameConfig.AudioPackageResDir));
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
                _logger.LogInformation("GameInstallTask ({GameBiz}): Move audio package cache files (Count: {count}) from {cacheDir} to res dir {resDir}", context.GameId.GameBiz, files.Length, cache, res);
            }
        }

        if (context.DownloadMode is GameInstallDownloadMode.Patch)
        {
            await ExecuteUpdateTaskDownloadPatchAsync(context, cancellationToken);
        }
        else if (context.DownloadMode is GameInstallDownloadMode.Chunk)
        {
            await ExecuteUpdateTaskDownloadModeChunkAsync(context, cancellationToken);
        }
        else if (context.DownloadMode is GameInstallDownloadMode.CompressedPackage)
        {
            await ExecuteUpdateTaskDownloadPackageAsync(context, cancellationToken);
        }

        await DownloadGameChannelSDKAsync(context, cancellationToken);
        await SetGameConfigIniAsync(context, ("predownload", null));
    }




    /// <summary>
    /// 开始更新，下载模式为 Patch
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private async Task ExecuteUpdateTaskDownloadPatchAsync(GameInstallContext context, CancellationToken cancellationToken = default)
    {
        List<PredownloadFile> files = GameInstallHelper.GetPredownloadFiles(context);

        context.Progress_DownloadTotalBytes = files.Sum(x => x.Size);
        context.Progress_DownloadFinishBytes = 0;
        context.State = GameInstallState.Downloading;

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start downloading in mode patch", context.GameId.GameBiz);
        await Parallel.ForEachAsync(files, cancellationToken, async (PredownloadFile item, CancellationToken token) =>
        {
            await _polly.ExecuteAsync(async token => await _gameInstallHelper.DownloadToFileAsync(context, item, token), token);
        });

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start merging in mode patch, file count: {count}", context.GameId.GameBiz, context.TaskFiles?.Count);
        context.State = GameInstallState.Merging;
        context.Progress_Percent = 0;
        double totalCount = context.TaskFiles?.Count ?? 1;
        double increase = 1 / totalCount;
        Lock _lock = new();
        await Parallel.ForEachAsync(context.TaskFiles ?? [], cancellationToken, async (GameInstallFile item, CancellationToken token) =>
        {
            if (item.IsFinished)
            {
                lock (_lock)
                {
                    context.Progress_Percent += increase;
                }
                return;
            }
            await _gameInstallHelper.PatchDiffFileAsync(context, item, cancellationToken);
            context.Progress_Percent += increase;
            item.IsFinished = true;
        });

        if (context.SophonPatchDeleteFiles is not null)
        {
            foreach (SophonPatchDeleteFile item in context.SophonPatchDeleteFiles)
            {
                string path = Path.Combine(context.InstallPath, item.File);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            _logger.LogInformation("GameInstallTask ({GameBiz}): Delete files by SophonPatchDeleteFiles, file count: {count}", context.GameId.GameBiz, context.SophonPatchDeleteFiles.Count);
        }

    }



    /// <summary>
    /// 开始更新，下载模式为 Chunk
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private async Task ExecuteUpdateTaskDownloadModeChunkAsync(GameInstallContext context, CancellationToken cancellationToken = default)
    {
        long downloadBytes = 0, writeBytes = 0;
        foreach (GameInstallFile item in context.TaskFiles ?? [])
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
        context.Progress_DownloadTotalBytes = downloadBytes;
        context.Progress_DownloadFinishBytes = 0;
        context.Progress_WriteTotalBytes = writeBytes;
        context.Progress_WriteFinishBytes = 0;

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start downloading in mode chunk", context.GameId.GameBiz);
        context.State = GameInstallState.Downloading;
        await Parallel.ForEachAsync(context.TaskFiles ?? [], cancellationToken, async (GameInstallFile file, CancellationToken token) =>
        {
            await _polly.ExecuteAsync(async token => await _gameInstallHelper.DownloadChunksToFileAsync(context, file, true, token), token);
            file.IsFinished = true;
        });
    }



    /// <summary>
    /// 开始更新，下载模式为 CompressedPackage
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="OperationCanceledException"></exception>
    private async Task ExecuteUpdateTaskDownloadPackageAsync(GameInstallContext context, CancellationToken cancellationToken = default)
    {
        List<PredownloadFile> files = GameInstallHelper.GetPredownloadFiles(context);

        context.Progress_DownloadTotalBytes = files.Sum(x => x.Size);
        context.Progress_DownloadFinishBytes = 0;
        context.State = GameInstallState.Downloading;

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start downloading in mode package", context.GameId.GameBiz);
        await Parallel.ForEachAsync(files, cancellationToken, async (PredownloadFile item, CancellationToken token) =>
        {
            await _polly.ExecuteAsync(async token => await _gameInstallHelper.DownloadToFileAsync(context, item, token), token);
        });

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start decompressing in mode package", context.GameId.GameBiz);
        context.State = GameInstallState.Decompressing;
        context.Progress_Percent = 0;
        double totalSize = files.Sum(x => x.Size);
        foreach (GameInstallFile item in context.TaskFiles ?? [])
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("The operation was canceled.", cancellationToken);
            }
            double ratio = item.Size / totalSize;
            if (item.IsFinished)
            {
                context.Progress_Percent += ratio;
                continue;
            }
            // SevenZipExtractor 解压被取消不会抛出异常
            await _gameInstallHelper.ExtractCompressedPackageAsync(context, item, ratio, cancellationToken);
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
    /// <param name="context"></param>
    /// <returns></returns>
    private async Task ExecutePredownloadTaskAsync(GameInstallContext context, CancellationToken cancellationToken = default)
    {
        List<PredownloadFile> files = GameInstallHelper.GetPredownloadFiles(context);

        context.Progress_DownloadTotalBytes = files.Sum(x => x.Size);
        context.Progress_DownloadFinishBytes = 0;
        context.State = GameInstallState.Downloading;

        _logger.LogInformation("GameInstallTask ({GameBiz}): Start downloading in mode predownload", context.GameId.GameBiz);
        await Parallel.ForEachAsync(files, cancellationToken, async (PredownloadFile item, CancellationToken token) =>
        {
            await _polly.ExecuteAsync(async token => await _gameInstallHelper.DownloadToFileAsync(context, item, token), token);
        });
        GamePackageService gamePackageService = _serviceProvider.GetRequiredService<GamePackageService>();
        string value = $"{context.LocalGameVersion},{context.PredownloadVersion},{context.AudioLanguage}";
        await SetGameConfigIniAsync(context, ("predownload", value));
    }



    /// <summary>
    /// 开始修复
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ExecuteRepairTaskAsync(GameInstallContext context, CancellationToken cancellationToken = default)
    {
        // 如果音频包缓存目录和资源目录不一样，则移动缓存目录的文件到资源目录
        if (!string.IsNullOrWhiteSpace(context.GameConfig?.AudioPackageCacheDir)
            && !string.IsNullOrWhiteSpace(context.GameConfig?.AudioPackageResDir)
            && context.GameConfig.AudioPackageCacheDir != context.GameConfig.AudioPackageResDir)
        {
            string cache = Path.GetFullPath(Path.Combine(context.InstallPath, context.GameConfig.AudioPackageCacheDir));
            string res = Path.GetFullPath(Path.Combine(context.InstallPath, context.GameConfig.AudioPackageResDir));
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
                _logger.LogInformation("GameInstallTask ({GameBiz}): Move audio package cache files (Count: {count}) from {cacheDir} to res dir {resDir}", context.GameId.GameBiz, files.Length, cache, res);
            }
        }

        if (context.DownloadMode is GameInstallDownloadMode.Chunk)
        {
            await ExecuteInstallTaskDownloadModeChunkAsync(context, cancellationToken);
        }
        else if (context.DownloadMode is GameInstallDownloadMode.SingleFile)
        {
            context.Progress_DownloadTotalBytes = context.TaskFiles!.Sum(x => x.Size);
            context.Progress_DownloadFinishBytes = 0;

            _logger.LogInformation("GameInstallTask ({GameBiz}): Start downloading in mode single file", context.GameId.GameBiz);
            context.State = GameInstallState.Downloading;
            await Parallel.ForEachAsync(context.TaskFiles!, cancellationToken, async (GameInstallFile file, CancellationToken token) =>
            {
                await _polly.ExecuteAsync(async token => await _gameInstallHelper.DownloadToFileAsync(context, file, token), token);
                file.IsFinished = true;
            });
        }

        // todo celar useless audio

        await DownloadGameChannelSDKAsync(context, cancellationToken);
        await SetGameConfigIniAsync(context);
    }



    /// <summary>
    /// 下载游戏渠道 SDK
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task DownloadGameChannelSDKAsync(GameInstallContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GameInstallTask ({GameBiz}): Start downloading GameChannelSDK", context.GameId.GameBiz);
        await _polly.ExecuteAsync(async token => await _gameInstallHelper.DownloadGameChannelSDKAsync(context, token), cancellationToken);
    }



    /// <summary>
    /// 清理文件
    /// </summary>
    /// <param name="context"></param>
    private void ClearDeprecatedFiles(GameInstallContext context)
    {
        if (context.Operation is not GameInstallOperation.Predownload)
        {
            int count = 0;
            foreach (GameInstallFile item in context.TaskFiles ?? [])
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
            foreach (GameDeprecatedFile item in context.DeprecatedFileConfig?.DeprecatedFiles ?? [])
            {
                string path = Path.Combine(context.InstallPath, item.Name);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    count++;
                }
            }
            if (context.PredownloadVersion is null)
            {
                foreach (string file in Directory.GetFiles(context.InstallPath, "*_tmp", SearchOption.AllDirectories))
                {
                    File.Delete(file);
                    count++;
                }
                foreach (string file in Directory.GetFiles(context.InstallPath, "*.hdiff", SearchOption.AllDirectories))
                {
                    File.Delete(file);
                    count++;
                }
                string chunk = Path.Combine(context.InstallPath, "chunk");
                if (Directory.Exists(chunk))
                {
                    Directory.Delete(chunk, true);
                }
                string ldiff = Path.Combine(context.InstallPath, "ldiff");
                if (Directory.Exists(ldiff))
                {
                    Directory.Delete(ldiff, true);
                }
                string staging = Path.Combine(context.InstallPath, "staging");
                if (Directory.Exists(staging))
                {
                    Directory.Delete(staging, true);
                }
            }
            _logger.LogInformation("GameInstallTask ({GameBiz}): Clear deprecated files, count: {count}", context.GameId.GameBiz, count);
        }
    }




    /// <summary>
    /// 设置游戏的 config.ini
    /// </summary>
    /// <param name="context"></param>
    /// <param name="keyValuePairs"></param>
    /// <returns></returns>
    private async Task SetGameConfigIniAsync(GameInstallContext context, params IEnumerable<(string Key, string? Value)> keyValuePairs)
    {
        string path = Path.Join(context.InstallPath, "config.ini");
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
        if (context.Operation is GameInstallOperation.Predownload)
        {
            config["game_version"] = context.LocalGameVersion;
        }
        else
        {
            config["game_version"] = context.LatestGameVersion;
        }
        if (context.GameId.GameBiz.Server is "cn")
        {
            config["channel"] = "1";
            config["sub_channel"] = "1";
            config["cps"] = "hyp_mihoyo";
        }
        else if (context.GameId.GameBiz.Server is "global")
        {
            config["channel"] = "1";
            config["sub_channel"] = "0";
            config["cps"] = "hyp_hoyoverse";
        }
        else if (context.GameId.GameBiz.Server is "bilibili")
        {
            config["channel"] = "14";
            config["sub_channel"] = "0";
            config["cps"] = "hyp_mihoyo";
        }
        config["sdk_version"] = context.GameChannelSDK?.Version ?? "";
        config["game_biz"] = context.GameId.GameBiz;

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
        Directory.CreateDirectory(context.InstallPath);
        await File.WriteAllTextAsync(path, sb.ToString());
        _logger.LogInformation("GameInstallTask ({GameBiz}): Set config.ini, path: {path}", context.GameId.GameBiz, path);
    }



}


