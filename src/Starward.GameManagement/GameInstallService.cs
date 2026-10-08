using Microsoft.Extensions.Logging;
using Starward.Core.HoYoPlay;
using Starward.GameManagement.Step;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Starward.GameManagement;

public class GameInstallService
{

    private readonly ILogger<GameInstallService> _logger;

    private readonly IServiceProvider _serviceProvider;

    private readonly HoYoPlayService _hoYoPlayService;

    private readonly ConcurrentDictionary<GameId, GameInstallContext> _tasks = new();



    public GameInstallService(ILogger<GameInstallService> logger, HoYoPlayService hoYoPlayService, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _hoYoPlayService = hoYoPlayService;
        _serviceProvider = serviceProvider;

    }


    public bool TryGetContext(GameId gameId, [NotNullWhen(true)] out GameInstallContext? context)
    {
        return _tasks.TryGetValue(gameId, out context);
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
            if (context.Operation != request.Operation)
            {
                // 操作不一样，则取消上次任务
                _logger.LogInformation("Task operation changed from {previousOperation} to {newOperation}, cancel the previous task, GameBiz: {game_biz}", context.Operation, request.Operation, context.GameId.GameBiz);
                context.Cancel(GameInstallState.Stop);
                _tasks.TryRemove(context.GameId, out _);
                context = request.ToContext();
                await PrepareContextAsync(context, cancellation);
                _tasks.TryAdd(context.GameId, context);
            }
        }
        else
        {
            context = request.ToContext();
            _logger.LogInformation("Create game install task, GameBiz: {game_biz}, Operation: {operation}, path: {path}", context.GameId.GameBiz, context.Operation, context.InstallPath);
            await PrepareContextAsync(context, cancellation);
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
            context.State = GameInstallState.Running;
            context.Step = GameInstallStep.Pending;
            context.ErrorMessage = null;
            ExecuteContext(context);
        }
        else
        {
            context = request.ToContext();
            context.State = GameInstallState.Stop;
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
            context.Cancel(GameInstallState.Paused);
            context.State = GameInstallState.Paused;
        }
        else
        {
            context = request.ToContext();
            context.State = GameInstallState.Stop;
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
            context.Cancel(GameInstallState.Stop);
            context.State = GameInstallState.Stop;
        }
        else
        {
            context = request.ToContext();
            context.State = GameInstallState.Stop;
        }
        return GameInstallProgress.FromContext(context);
    }




    private async Task PrepareContextAsync(GameInstallContext context, CancellationToken cancellation = default)
    {
        if (context.GameConfig is null)
        {
            var config = await GameConfigService.GetGameConfigIniAsync(context.InstallPath, cancellation) ?? new();
            config.Channel = context.LauncherConfig.Channel;
            config.SubChannel = context.LauncherConfig.SubChannel;
            config.GameBiz = context.GameId.GameBiz;
            await _hoYoPlayService.InitializeClientsAsync([context.LauncherConfig], cancellation);
            context.GameConfig = (await _hoYoPlayService.GetGameConfigAsync(context.GameId, cancellation)).EnsureNotNull(context.GameId);
            if (context.GameConfig.DefaultDownloadMode is DownloadMode.DOWNLOAD_MODE_CHUNK)
            {
                (await _hoYoPlayService.GetGameBranchAsync(context.GameId, cancellation)).EnsureNotNull(context.GameId);
            }
            else
            {
                (await _hoYoPlayService.GetGamePackageAsync(context.GameId, cancellation)).EnsureNotNull(context.GameId);
            }
            context.ConfigIni = config;
        }
    }



    private async void ExecuteContext(GameInstallContext context)
    {
        await context.ExecuteLock.WaitAsync();
        using IDisposable? scope = _logger.BeginScope(new Dictionary<string, object?> { ["ExtraContext"] = $"[GameBiz={context.GameId.GameBiz}, Operation={context.Operation}]" });
        try
        {
            context.State = GameInstallState.Running;
            context.Step = GameInstallStep.Pending;
            _logger.LogInformation("GameInstallTask started.");
            CancellationToken cancellation = context.CancellationToken;
            await PrepareContextStepsAsync(context, cancellation);

            foreach (StepBase step in context.StepList)
            {
                context.CurrentStep++;
                await step.ExecuteAsync(cancellation);
            }

            context.State = GameInstallState.Finish;
            _logger.LogInformation("GameInstallTask finished.");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException te)
        {
            _logger.LogError(ex, "GameInstallTask timed out.");
            context.State = GameInstallState.Error;
            context.ErrorMessage = te.Message;
        }
        catch (OperationCanceledException) when (context.IsCancellationRequested)
        {
            _logger.LogInformation("GameInstallTask canceled, CancelState: {state}", context.CancelState);
            if (context.State is GameInstallState.Running)
            {
                context.State = context.CancelState;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GameInstallTask failed.");
            context.State = GameInstallState.Error;
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

        steps.Add(new PrepareStep(context, _serviceProvider));
        if (context.Operation is GameInstallOperation.Install)
        {
            if (chunkMode)
            {
                steps.Add(new ChunkInstallStep(context, _serviceProvider));
            }
            else
            {
                steps.Add(new PackageInstallStep(context, _serviceProvider));
            }
        }
        else if (context.Operation is GameInstallOperation.Predownload)
        {
            if (chunkMode)
            {
                steps.Add(new ChunkPredownloadStep(context, _serviceProvider));
            }
            else
            {
                steps.Add(new PackagePredownloadStep(context, _serviceProvider));
            }
        }
        else if (context.Operation is GameInstallOperation.Update)
        {
            if (chunkMode)
            {
                steps.Add(new ChunkUpdateStep(context, _serviceProvider));
                steps.Add(new ChunkRepairStep(context, _serviceProvider));
            }
            else
            {
                steps.Add(new PackageUpdateStep(context, _serviceProvider));
                steps.Add(new PackageRepairStep(context, _serviceProvider));
            }
        }
        else if (context.Operation is GameInstallOperation.Repair)
        {
            if (chunkMode)
            {
                steps.Add(new ChunkRepairStep(context, _serviceProvider));
            }
            else
            {
                steps.Add(new PackageRepairStep(context, _serviceProvider));
            }
        }
        else
        {
            _logger.LogWarning("Unsupported game install operation.");
        }
        if (context.Operation is not GameInstallOperation.Predownload)
        {
            steps.Add(new WpfPackageStep(context, _serviceProvider));
            steps.Add(new GameChannelSdkStep(context, _serviceProvider));
            steps.Add(new PluginStep(context, _serviceProvider));
            steps.Add(new DeprecatedFileStep(context, _serviceProvider));
        }
        steps.Add(new ConfigIniStep(context, _serviceProvider));

        await Parallel.ForEachAsync(steps, cancellation, async (step, token) =>
        {
            await step.PrepareAsync(token);
        });

        context.StepList = steps.Where(x => !x.SkipStep).ToList();
        context.TotalStep = context.StepList.Count;
    }
















}
