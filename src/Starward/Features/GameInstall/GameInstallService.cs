using Starward.Core.Hypergryph;
using Starward.Features.GameLauncher;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Core.Passport;
using Starward.Features.RPC;
using Starward.GameInstall;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.GameInstall;

internal class GameInstallService
{

    private readonly ILogger<GameInstallService> _logger;

    private readonly GameInstaller.GameInstallerClient _gameInstallerClient;

    private readonly HoYoPlayService _hoYoPlayService;

    private readonly SemaphoreSlim _getProgressSemaphore = new(1);


    public GameInstallService(ILogger<GameInstallService> logger, HoYoPlayService hoYoPlayService)
    {
        _logger = logger;
        _hoYoPlayService = hoYoPlayService;
        _gameInstallerClient = RpcService.CreateRpcClient<GameInstaller.GameInstallerClient>();
    }



    private ConcurrentDictionary<GameId, GameInstallProgress> _tasks = new();


    public GameInstallProgress? GetGameInstallTask(GameId gameId)
    {
        return _tasks.GetValueOrDefault(gameId);
    }


    public async void StartUpdateTaskProgress()
    {
        if (_getProgressSemaphore.CurrentCount == 0)
        {
            return;
        }
        try
        {
            await _getProgressSemaphore.WaitAsync().ConfigureAwait(false);
            if (RpcService.CheckRpcServerRunning())
            {
                using var call = _gameInstallerClient.GetProgress(new EmptyMessage());
                await foreach (GameInstallProgressList? list in call.ResponseStream.ReadAllAsync().ConfigureAwait(false))
                {
                    foreach (GameInstallProgressDTO? item in list.List)
                    {
                        AddOrUpdateTask(item);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTaskProgress");
        }
        finally
        {
            _getProgressSemaphore.Release();
            if (!_tasks.IsEmpty)
            {
                if (RpcService.CheckRpcServerRunning())
                {
                    await Task.Delay(1000);
                    StartUpdateTaskProgress();
                }
                else
                {
                    _logger.LogWarning("RPC server is not running, set {count} running install tasks state to error.", _tasks.Count);
                    foreach (GameInstallProgress item in _tasks.Values)
                    {
                        item.State = RunnerState.Error;
                        item.ErrorMessage = Lang.RPCServiceExitedUnexpectedly;
                        item.NetworkDownloadSpeed = 0;
                        item.RemainTimeSeconds = 0;
                    }
                }
            }
        }
    }


    private GameInstallProgress AddOrUpdateTask(GameInstallProgressDTO dto)
    {
        GameId gameId = new GameId { Id = dto.GameId.GameId, GameBiz = dto.GameId.GameBiz };
        if (_tasks.TryGetValue(gameId, out GameInstallProgress? task))
        {
            GameInstallProgress value = GameInstallProgress.FromDTO(dto);
            task.LauncherConfig = value.LauncherConfig;
            task.GameId = value.GameId;
            task.InstallPath = value.InstallPath;
            task.Operation = value.Operation;
            task.AudioLanguage = value.AudioLanguage;
            task.CategoryScenario = value.CategoryScenario;
            task.Timestamp = value.Timestamp;
            task.CurrentStep = value.CurrentStep;
            task.TotalStep = value.TotalStep;
            task.Step = value.Step;
            task.State = value.State;
            task.ErrorMessage = value.ErrorMessage;
            task.PercentProgress = value.PercentProgress;
            task.ProgressDisplay = value.ProgressDisplay;
            task.NetworkTotalBytes = value.NetworkTotalBytes;
            task.NetworkFinishedBytes = value.NetworkFinishedBytes;
            task.StorageTotalBytes = value.StorageTotalBytes;
            task.StorageFinishedBytes = value.StorageFinishedBytes;
            task.HardLinkBytes = value.HardLinkBytes;
            task.NetworkDownloadSpeed = value.NetworkDownloadSpeed;
            task.RemainTimeSeconds = value.RemainTimeSeconds;
        }
        else
        {
            task = GameInstallProgress.FromDTO(dto);
            _tasks.TryAdd(gameId, task);
        }
        if (task.State is RunnerState.Stop or RunnerState.Finish)
        {
            _tasks.TryRemove(gameId, out _);
        }
        return task;
    }


    private GameInstallRequestDTO ToRequestDTO(GameInstallProgress task)
    {
        return new GameInstallRequestDTO
        {
            GameId = new GameIdDTO
            {
                GameBiz = task.GameId.GameBiz,
                GameId = task.GameId.Id,
                LauncherId = task.LauncherConfig.Id,
                Channel = task.LauncherConfig.Channel,
                SubChannel = task.LauncherConfig.SubChannel,
                Region = (int)task.LauncherConfig.Region,
                IsBeta = task.LauncherConfig.IsBeta,
            },
            InstallPath = task.InstallPath,
            Operation = (int)task.Operation,
            AudioLanguage = (int)task.AudioLanguage,
            CategoryScenario = task.CategoryScenario,
            Passport = task.LauncherConfig.IsBeta ? ToPassportDTO(_hoYoPlayService.PassportUserInfo) : null,
        };
    }


    private static PassportUserInfoDTO? ToPassportDTO(PassportUserInfo? passportUserInfo)
    {
        return passportUserInfo is null ? null : new PassportUserInfoDTO
        {
            Aid = passportUserInfo.Aid,
            Mid = passportUserInfo.Mid,
            Stoken = passportUserInfo.Stoken,
        };
    }


    public async Task<GameInstallProgress?> StartInstallAsync(GameId gameId, string installPath, string categoryScenario, AudioLanguage audioLanguage, CancellationToken cancellation)
    {
        return await StartOrContinueTaskAsync(GameInstallOperation.Install, gameId, installPath, categoryScenario, audioLanguage, cancellation);
    }


    public async Task<GameInstallProgress?> ResumeDownloadAsync(GameId gameId, string installPath, CancellationToken cancellation)
    {
        return await StartOrContinueTaskAsync(GameInstallOperation.Install, gameId, installPath, null, AudioLanguage.None, cancellation);
    }


    public async Task<GameInstallProgress?> StartPredownloadAsync(GameId gameId, string installPath, CancellationToken cancellation)
    {
        return await StartOrContinueTaskAsync(GameInstallOperation.Predownload, gameId, installPath, null, AudioLanguage.None, cancellation);
    }


    public async Task<GameInstallProgress?> StartUpdateAsync(GameId gameId, string installPath, CancellationToken cancellation)
    {
        return await StartOrContinueTaskAsync(GameInstallOperation.Update, gameId, installPath, null, AudioLanguage.None, cancellation);
    }


    public async Task<GameInstallProgress?> StartRepairAsync(GameId gameId, string installPath, CancellationToken cancellation)
    {
        return await StartOrContinueTaskAsync(GameInstallOperation.Repair, gameId, installPath, null, AudioLanguage.None, cancellation);
    }


    public async Task<GameInstallProgress?> StarUpdatePluginAsync(GameId gameId, string installPath, CancellationToken cancellation)
    {
        return await StartOrContinueTaskAsync(GameInstallOperation.UpdatePlugin, gameId, installPath, null, AudioLanguage.None, cancellation);
    }


    public async Task<GameInstallProgress?> StarHotUpdateAsync(GameId gameId, string installPath, CancellationToken cancellation)
    {
        if (gameId.GameBiz.Game is GameBiz.hk4e)
        {
            return await StartOrContinueTaskAsync(GameInstallOperation.HotUpdate, gameId, installPath, null, AudioLanguage.None, cancellation);
        }
        else
        {
            throw new NotSupportedException($"Hot update operation is not support for ({gameId})");
        }
    }


    public async Task<GameInstallProgress?> UpdateCutsceneAsync(GameId gameId, string installPath, List<string> blockList, CancellationToken cancellation)
    {
        if (gameId.GameBiz.Game is GameBiz.hk4e)
        {
            return await StartOrContinueTaskAsync(GameInstallOperation.UpdateCutscene, gameId, installPath, null, AudioLanguage.None, cancellation, blockList);
        }
        else
        {
            throw new NotSupportedException($"Update cutscene operation is not support for ({gameId})");
        }
    }


    private async Task<GameInstallProgress?> StartOrContinueTaskAsync(GameInstallOperation operation, GameId gameId, string installPath, string? categoryScenario, AudioLanguage audioLanguage, CancellationToken cancellation, List<string>? blockList = null)
    {
        LauncherConfig? launcher = _hoYoPlayService.GetLauncherConfig(gameId);
        if (launcher is null)
        {
            throw new ArgumentNullException($"LauncherConfig of game ({gameId}) is null");
        }
        var request = new GameInstallRequestDTO
        {
            GameId = new GameIdDTO
            {
                GameBiz = gameId.GameBiz,
                GameId = gameId.Id,
                LauncherId = launcher.Id,
                Channel = launcher.Channel,
                SubChannel = launcher.SubChannel,
                Region = (int)launcher.Region,
                IsBeta = launcher.IsBeta,
            },
            HardLinkPath = AppConfig.EnableHardLink && HypergryphGameConstants.IsEndfield(gameId.GameBiz)
                ? GameLauncherService.GetEndfieldHardLinkSource(gameId.GameBiz, installPath) : null,
            InstallPath = installPath,
            Operation = (int)operation,
            CategoryScenario = categoryScenario,
            AudioLanguage = (int)audioLanguage,
            Passport = launcher.IsBeta ? ToPassportDTO(_hoYoPlayService.PassportUserInfo) : null,
        };
        if (blockList?.Count > 0)
        {
            request.CutsceneBlockList.AddRange(blockList);
        }
        if (await RpcService.EnsureRpcServerRunningAsync())
        {
            _logger.LogInformation("""
                Start game install task: 
                Operation: {operation}
                GameId: {gameId} {gameBiz}
                InstallPath: {installPath}
                AudioLanguage: {audioLanguage}
                """, operation, gameId.Id, gameId.GameBiz, installPath, audioLanguage);
            int limit = Math.Clamp(AppConfig.SpeedLimitKBPerSecond * 1024, 0, int.MaxValue);
            await _gameInstallerClient.SetRateLimiterAsync(new RateLimiterMessage { Enable = limit > 0, BytesPerSecond = limit }, cancellationToken: cancellation);
            await _gameInstallerClient.CreateContextAsync(request, cancellationToken: cancellation);
            var dto = await _gameInstallerClient.StartOrContinueTaskAsync(request, deadline: DateTime.UtcNow.AddSeconds(5), cancellationToken: cancellation);
            StartUpdateTaskProgress();
            return AddOrUpdateTask(dto);
        }
        else
        {
            _logger.LogInformation("RPC server is not running, can't start game install task ({GameBiz}, {Operation}).", gameId.GameBiz, operation);
            return null;
        }
    }


    public async Task<GameInstallProgress> PauseTaskAsync(GameInstallProgress task)
    {
        if (RpcService.CheckRpcServerRunning())
        {
            await RpcService.EnsureRpcServerRunningAsync();
            _logger.LogInformation("Pause game install task: {gameId} {gameBiz}", task.GameId.Id, task.GameId.GameBiz);
            var dto = await _gameInstallerClient.PauseTaskAsync(ToRequestDTO(task), deadline: DateTime.UtcNow.AddSeconds(3));
            task = AddOrUpdateTask(dto);
            StartUpdateTaskProgress();
        }
        else
        {
            task.State = RunnerState.Stop;
            task.ErrorMessage = Lang.RPCServiceExitedUnexpectedly;
        }
        return task;
    }


    public async Task<GameInstallProgress> ContinueTaskAsync(GameInstallProgress task)
    {
        if (await RpcService.EnsureRpcServerRunningAsync())
        {
            _logger.LogInformation("Continue game install task: {gameId} {gameBiz}", task.GameId.Id, task.GameId.GameBiz);
            var dto = await _gameInstallerClient.StartOrContinueTaskAsync(ToRequestDTO(task), deadline: DateTime.UtcNow.AddSeconds(3));
            task = AddOrUpdateTask(dto);
            StartUpdateTaskProgress();
        }
        return task;
    }


    public async Task<GameInstallProgress> StopTaskAsync(GameInstallProgress task)
    {
        if (RpcService.CheckRpcServerRunning())
        {
            await RpcService.EnsureRpcServerRunningAsync();
            _logger.LogInformation("Stop game install task: {gameId} {gameBiz}", task.GameId.Id, task.GameId.GameBiz);
            var dto = await _gameInstallerClient.StopTaskAsync(ToRequestDTO(task), deadline: DateTime.UtcNow.AddSeconds(3));
            task = AddOrUpdateTask(dto);
            StartUpdateTaskProgress();
        }
        else
        {
            task.State = RunnerState.Stop;
            task.ErrorMessage = Lang.RPCServiceExitedUnexpectedly;
        }
        return task;
    }


    public async Task<bool> StartUninstallAsync(GameId gameId, string installPath, CancellationToken cancellation)
    {
        if (await RpcService.EnsureRpcServerRunningAsync())
        {
            _logger.LogInformation("""
                Start uninstall game:
                GameId: {gameId}
                InstallPath: {installPath}
                """, gameId, installPath);
            var request = new UninstallGameRequestDTO
            {
                GameId = new GameIdDTO { GameBiz = gameId.GameBiz, GameId = gameId.Id },
                InstallPath = installPath,
            };
            try
            {
                GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(gameId, cancellation);
                if (!string.IsNullOrWhiteSpace(config?.GameScreenshotDir))
                {
                    request.ScreenshotPath = Path.GetFullPath(Path.Combine(installPath, config.GameScreenshotDir));
                    if (Directory.Exists(AppConfig.ScreenshotFolder))
                    {
                        request.ScreenshotBackupPath = Path.Combine(AppConfig.ScreenshotFolder, Path.GetFileNameWithoutExtension(config.ExeFileName));
                    }
                    else if (Directory.Exists(AppConfig.UserDataFolder))
                    {
                        request.ScreenshotBackupPath = Path.Combine(AppConfig.UserDataFolder, "Screenshots", Path.GetFileNameWithoutExtension(config.ExeFileName));
                    }
                }
            }
            catch { }
            var response = await _gameInstallerClient.UninstallGameAsync(request);
            if (response.Success)
            {
                _tasks.TryRemove(gameId, out _);
                return true;
            }
            else
            {
                throw new Exception(response.ErrorMessage);
            }
        }
        else
        {
            return false;
        }
    }


}
