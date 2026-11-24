using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.GameInstall.HotUpdate;
using Starward.GameInstall.Model;
using Starward.GameInstall.Service;
using Starward.GameInstall.Step;
using System.Collections.Concurrent;
using Vanara.PInvoke;

namespace Starward.GameInstall;

public class GameInstallRunner
{

    private readonly ILogger<GameInstallRunner> _logger;

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ConcurrentDictionary<GameId, GameInstallContext> _tasks = new();



    public GameInstallRunner(ILogger<GameInstallRunner> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;

    }


    /// <summary>
    /// 获取限速
    /// </summary>
    /// <returns></returns>
    public (bool Enable, int BytePerSecond) GetRateLimiter()
    {
        return (RateLimiter.EnableLimit, RateLimiter.BytesPerSecond);
    }


    /// <summary>
    /// 设置限速
    /// </summary>
    /// <param name="enable"></param>
    /// <param name="bytesPerSecond"></param>
    /// <returns></returns>
    public int SetRateLimiter(bool enable, int bytesPerSecond)
    {
        RateLimiter.EnableLimit = enable;
        RateLimiter.SetLimit(bytesPerSecond);
        return RateLimiter.BytesPerSecond;
    }


    /// <summary>
    /// 创建任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<GameInstallProgress> CreateContextAsync(GameInstallRequest request, CancellationToken cancellation = default)
    {
        if (_tasks.TryGetValue(request.GameId, out GameInstallContext? context))
        {
            if (context.State is RunnerState.Finish or RunnerState.Stop || context.Operation != request.Operation || context.InstallPath != request.InstallPath)
            {
                // 操作不一样，则取消上次任务
                _logger.LogInformation("Task state is {state} or operation changed from {previousOperation} to {newOperation}, cancel the previous task, GameBiz: {game_biz}", context.State, context.Operation, request.Operation, context.GameId.GameBiz);
                context.Cancel(RunnerState.Stop);
                context.ServiceScope.Dispose();
                _tasks.TryRemove(context.GameId, out _);
                context = await PrepareContextAsync(request, cancellation).ConfigureAwait(false);
                _tasks.TryAdd(context.GameId, context);
            }
        }
        else
        {
            context = await PrepareContextAsync(request, cancellation).ConfigureAwait(false);
            _logger.LogInformation("Create game install task, GameBiz: {game_biz}, Operation: {operation}, path: {path}", context.GameId.GameBiz, context.Operation, context.InstallPath);
            context.State = RunnerState.Queue;
            _tasks.TryAdd(context.GameId, context);
        }
        return GameInstallProgress.FromContext(context);
    }


    /// <summary>
    /// 开始或继续任务
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public GameInstallProgress StartOrContinueTask(GameInstallRequest request)
    {
        if (_tasks.TryGetValue(request.GameId, out GameInstallContext? context))
        {
            context.State = RunnerState.Running;
            context.Step = RunnerStep.Pending;
            context.ErrorMessage = null;
            ExecuteContext(context);
        }
        else
        {
            context = request.ToContext();
            context.State = RunnerState.Stop;
        }
        return GameInstallProgress.FromContext(context);
    }


    /// <summary>
    /// 暂停任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public GameInstallProgress PauseTask(GameInstallRequest request)
    {
        if (_tasks.TryGetValue(request.GameId, out GameInstallContext? context))
        {
            _logger.LogInformation("Pause game install task, GameBiz: {game_biz}, Operation: {operation}", context.GameId.GameBiz, context.Operation);
            context.RefreshSpeed();
            context.Cancel(RunnerState.Paused);
            context.State = RunnerState.Paused;
        }
        else
        {
            context = request.ToContext();
            context.State = RunnerState.Stop;
        }
        return GameInstallProgress.FromContext(context);
    }


    /// <summary>
    /// 停止任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public GameInstallProgress StopTask(GameInstallRequest request)
    {
        if (_tasks.TryRemove(request.GameId, out GameInstallContext? context))
        {
            _logger.LogInformation("Stop game install task, GameBiz: {game_biz}, Operation: {operation}", context.GameId.GameBiz, context.Operation);
            context.Cancel(RunnerState.Stop);
            context.State = RunnerState.Stop;
            context.ServiceScope.Dispose();
        }
        else
        {
            context = request.ToContext();
            context.State = RunnerState.Stop;
        }
        return GameInstallProgress.FromContext(context);
    }


    private async Task<GameInstallContext> PrepareContextAsync(GameInstallRequest request, CancellationToken cancellation = default)
    {
        IServiceScope serviceScope = _scopeFactory.CreateScope();
        try
        {
            HoYoPlayService hoYoPlayService = serviceScope.ServiceProvider.GetRequiredService<HoYoPlayService>();
            hoYoPlayService.PassportUserInfo = request.PassportUserInfo;
            GameInstallContext context = request.ToContext();
            context.ServiceScope = serviceScope;

            GameConfigIni config = await GameConfigService.GetGameConfigIniAsync(context.InstallPath, cancellation).ConfigureAwait(false) ?? new();
            config.Channel = context.LauncherConfig.Channel;
            config.SubChannel = context.LauncherConfig.SubChannel;
            config.GameBiz = context.GameId.GameBiz;

            await hoYoPlayService.GetGameInfosAsync(context.LauncherConfig, cancellation).ConfigureAwait(false);
            context.GameConfig = (await hoYoPlayService.GetGameConfigAsync(context.GameId, cancellation).ConfigureAwait(false)).EnsureNotNull(context.GameId);
            if (context.GameConfig.DefaultDownloadMode is DownloadMode.DOWNLOAD_MODE_CHUNK)
            {
                GameBranch branch = (await hoYoPlayService.GetGameBranchAsync(context.GameId, cancellation).ConfigureAwait(false)).EnsureNotNull(context.GameId);
                (await hoYoPlayService.GetGameSophonChunkBuildAsync(context.GameId, branch.Main, null, cancellation).ConfigureAwait(false)).EnsureNotNull(context.GameId);
            }
            else
            {
                (await hoYoPlayService.GetGamePackageAsync(context.GameId, cancellation).ConfigureAwait(false)).EnsureNotNull(context.GameId);
            }
            context.ConfigIni = config;
            var gameConfigService = serviceScope.ServiceProvider.GetRequiredService<GameConfigService>();
            if (context.AudioLanguage is Core.AudioLanguage.None)
            {
                context.AudioLanguage = await gameConfigService.GetAudioLanguageAsync(context.GameId, context.InstallPath, cancellation);
            }
            if (context.CategoryScenario is null)
            {
                context.CategoryScenario = await gameConfigService.GetCategoryScenarioAsync(context.GameId, context.InstallPath, cancellation);
            }
            Directory.CreateDirectory(request.InstallPath);
            return context;
        }
        catch
        {
            serviceScope.Dispose();
            throw;
        }
    }


    private async void ExecuteContext(GameInstallContext context)
    {
        await context.ExecuteLock.WaitAsync().ConfigureAwait(false);
        using IDisposable? scope = _logger.BeginScope(new Dictionary<string, object?> { ["ExtraContext"] = $"[GameBiz={context.GameId.GameBiz}, Operation={context.Operation}]" });
        try
        {
            context.State = RunnerState.Running;
            context.Step = RunnerStep.Pending;
            context.CurrentStep = 0;
            _logger.LogInformation("GameInstallTask started.");
            CancellationToken cancellation = context.CancellationToken;
            await PrepareContextStepsAsync(context, cancellation).ConfigureAwait(false);

            foreach (StepBase step in context.StepList)
            {
                context.CurrentStep++;
                await step.ExecuteAsync(cancellation).ConfigureAwait(false);
            }

            context.State = RunnerState.Finish;
            _logger.LogInformation("GameInstallTask finished.");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException te)
        {
            _logger.LogError(ex, "GameInstallTask timed out.");
            context.State = RunnerState.Error;
            context.ErrorMessage = te.Message;
        }
        catch (OperationCanceledException) when (context.IsCancellationRequested)
        {
            _logger.LogInformation("GameInstallTask canceled, CancelState: {state}", context.CancelState);
            if (context.State is RunnerState.Running)
            {
                context.State = context.CancelState;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GameInstallTask failed.");
            context.State = RunnerState.Error;
            context.ErrorMessage = ex.Message;
        }
        finally
        {
            context.ExecuteLock.Release();
        }
    }


    private async Task PrepareContextStepsAsync(GameInstallContext context, CancellationToken cancellation = default)
    {
        if (context.StepList is not null)
        {
            return;
        }
        List<StepBase> steps = new();
        bool chunkMode = context.GameConfig.DefaultDownloadMode is DownloadMode.DOWNLOAD_MODE_CHUNK;

        steps.Add(new PrepareStep(context, context.ServiceScope.ServiceProvider));
        if (context.GameId.GameBiz.Game is GameBiz.hk4e && context.Operation is GameInstallOperation.HotUpdate)
        {
            steps.Add(new GenshinHotUpdateStep(context, context.ServiceScope.ServiceProvider));
        }
        else if (context.GameId.GameBiz.Game is GameBiz.hk4e && context.Operation is GameInstallOperation.UpdateCutscene)
        {
            steps.Add(new CutsceneManageStep(context, context.ServiceScope.ServiceProvider));
        }
        else
        {
            if (context.Operation is GameInstallOperation.Install)
            {
                if (chunkMode)
                {
                    steps.Add(new ChunkInstallStep(context, context.ServiceScope.ServiceProvider));
                }
                else
                {
                    steps.Add(new PackageInstallStep(context, context.ServiceScope.ServiceProvider));
                }
            }
            else if (context.Operation is GameInstallOperation.Predownload)
            {
                if (chunkMode)
                {
                    steps.Add(new ChunkPredownloadStep(context, context.ServiceScope.ServiceProvider));
                }
                else
                {
                    steps.Add(new PackagePredownloadStep(context, context.ServiceScope.ServiceProvider));
                }
            }
            else if (context.Operation is GameInstallOperation.Update)
            {
                if (chunkMode)
                {
                    steps.Add(new ChunkUpdateStep(context, context.ServiceScope.ServiceProvider));
                    steps.Add(new ChunkRepairStep(context, context.ServiceScope.ServiceProvider));
                }
                else
                {
                    steps.Add(new PackageUpdateStep(context, context.ServiceScope.ServiceProvider));
                    steps.Add(new PackageRepairStep(context, context.ServiceScope.ServiceProvider));
                }
            }
            else if (context.Operation is GameInstallOperation.Repair)
            {
                if (chunkMode)
                {
                    steps.Add(new ChunkRepairStep(context, context.ServiceScope.ServiceProvider));
                }
                else
                {
                    steps.Add(new PackageRepairStep(context, context.ServiceScope.ServiceProvider));
                }
            }
            else
            {
                _logger.LogWarning("Unsupported game install operation.");
            }
            if (context.Operation is not GameInstallOperation.Predownload)
            {
                steps.Add(new WpfPackageStep(context, context.ServiceScope.ServiceProvider));
                steps.Add(new GameChannelSdkStep(context, context.ServiceScope.ServiceProvider));
                steps.Add(new PluginStep(context, context.ServiceScope.ServiceProvider));
                steps.Add(new DeprecatedFileStep(context, context.ServiceScope.ServiceProvider));
            }
            steps.Add(new ConfigIniStep(context, context.ServiceScope.ServiceProvider));
        }

        await Parallel.ForEachAsync(steps, cancellation, async (step, token) =>
        {
            await step.PrepareAsync(token).ConfigureAwait(false);
        }).ConfigureAwait(false);

        context.StepList = steps.Where(x => !x.SkipStep).ToList();
        context.TotalStep = context.StepList.Count;
    }


    public List<GameInstallProgressDTO> GetAllProgress()
    {
        List<GameInstallProgressDTO> list = new();
        foreach (GameInstallContext context in _tasks.Values)
        {
            context.RefreshSpeed();
            list.Add(GameInstallProgress.FromContext(context).ToDTO());
            if (context.State is RunnerState.Stop or RunnerState.Finish)
            {
                context.ServiceScope.Dispose();
                _tasks.TryRemove(context.GameId, out _);
            }
        }
        return list;
    }


    public async Task<UninstallGameResponse> UninstallGameAsync(UninstallGameRequest request, CancellationToken cancellation = default)
    {
        try
        {
            string installPath = request.InstallPath;
            if (!Directory.Exists(installPath))
            {
                _logger.LogWarning("Game folder does not exist: {installPath}", installPath);
            }
            if (Path.GetPathRoot(installPath) == installPath)
            {
                _logger.LogError("Game folder is the root of drive.");
                throw new InvalidOperationException("Game folder is the root of drive.");
            }
            _logger.LogInformation("Start to uninstall game ({GameId}): {installPath}", request.GameId, installPath);
            if (_tasks.TryGetValue(request.GameId, out GameInstallContext? context))
            {
                context.Cancel(RunnerState.Stop);
            }
            // todo 更多防御措施
            string[] files = Directory.GetFiles(installPath, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            cancellation.ThrowIfCancellationRequested();
            BackupScreenshot(request.ScreenshotPath, request.ScreenshotBackupPath);
            cancellation.ThrowIfCancellationRequested();
            _logger.LogInformation("Deleting folder {installPath} ({count} files).", installPath, files.Length);
            Directory.Delete(installPath, true);
            _logger.LogInformation("Finished uninstall game ({GameId}): {installPath}", request.GameId, installPath);
            HardLinkService.DeleteGameFileItem(request.GameId);
            return new UninstallGameResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uninstall game failed");
            return new UninstallGameResponse { Success = false, ErrorMessage = ex.Message };
        }
    }


    private void BackupScreenshot(string? screenshotPath, string? screenshotBackupPath)
    {
        if (!Directory.Exists(screenshotPath) || string.IsNullOrWhiteSpace(screenshotBackupPath))
        {
            return;
        }
        string[] files = Directory.GetFiles(screenshotPath);
        if (files.Length is 0)
        {
            return;
        }
        Directory.CreateDirectory(screenshotBackupPath);

        bool canHardLink = false;
        if (Path.GetPathRoot(screenshotPath) == Path.GetPathRoot(screenshotBackupPath) && new DriveInfo(screenshotBackupPath).DriveFormat is "NTFS" or "ReFS")
        {
            canHardLink = true;
        }

        int count = 0;
        foreach (string file in files)
        {
            string target = Path.Join(screenshotBackupPath, Path.GetFileName(file));
            if (!File.Exists(target))
            {
                bool result = false;
                if (canHardLink)
                {
                    result = Kernel32.CreateHardLink(target, file);
                }
                if (!result)
                {
                    File.Copy(file, target, true);
                }
                count++;
            }
        }
        _logger.LogInformation("Backed up {count} screenshots.", count);
    }

}
