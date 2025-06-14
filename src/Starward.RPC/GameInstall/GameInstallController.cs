using Grpc.Core;
using Microsoft.Extensions.Logging;
using Starward.Core.HoYoPlay;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.RPC.GameInstall;

internal class GameInstallController : GameInstaller.GameInstallerBase
{

    private readonly ILogger<GameInstallController> _logger;

    private readonly GameInstallService _gameInstallService;

    private readonly GameInstallHelper _gameInstallHelper;

    private readonly GameUninstallService _gameUninstallService;


    public GameInstallController(ILogger<GameInstallController> logger, GameInstallService gameInstallService, GameInstallHelper gameInstallHelper, GameUninstallService gameUninstallService)
    {
        _logger = logger;
        _gameInstallService = gameInstallService;
        _gameInstallHelper = gameInstallHelper;
        _gameUninstallService = gameUninstallService;
    }






    public override Task<GameInstallContextDTO> StartOrContinueTask(GameInstallRequest request, ServerCallContext context)
    {
        return Task.FromResult(_gameInstallService.StartOrContinueTask(request));
    }




    public override Task<GameInstallContextDTO> PauseTask(GameInstallRequest request, ServerCallContext context)
    {
        return Task.FromResult(_gameInstallService.PauseTask(request));
    }




    public override Task<GameInstallContextDTO> StopTask(GameInstallRequest request, ServerCallContext context)
    {
        return Task.FromResult(_gameInstallService.StopTask(request));
    }




    public override Task<RateLimiterMessage> SetRateLimiter(RateLimiterMessage request, ServerCallContext context)
    {
        int limit = _gameInstallHelper.SetRateLimiter(request.BytesPerSecond);
        return Task.FromResult(new RateLimiterMessage { BytesPerSecond = limit });
    }




    public override async Task<UninstallGameResponse> UninstallGame(UninstallGameRequest request, ServerCallContext context)
    {
        try
        {
            GameId gameId = new GameId { GameBiz = request.GameBiz, Id = request.GameId };
            if (_gameInstallService.TryGetTask(gameId, out GameInstallContext? task))
            {
                // 停止正在进行的安装任务
                task.Cancel(GameInstallState.Stop);
                await Task.Delay(3000);
            }
            await _gameUninstallService.UninstallGameAsync(request, context.CancellationToken);
            return new UninstallGameResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uninstall game ({gameBiz}): {path}", request.GameBiz, request.InstallPath);
            return new UninstallGameResponse { Success = false, ErrorMessage = ex.Message };
        }
    }




    #region GetTaskProgress



    private IServerStreamWriter<GameInstallContextDTO>? _taskProgressStream;


    public override async Task GetTaskProgress(EmptyMessage request, IServerStreamWriter<GameInstallContextDTO> responseStream, ServerCallContext context)
    {
        try
        {
            _taskProgressStream = responseStream;
            _gameInstallService.TaskStateChanged += _gameInstallService_TaskStateChanged;
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            while (await timer.WaitForNextTickAsync(context.CancellationToken))
            {
                if (_gameInstallService.CurrentTask is null)
                {
                    return;
                }
                _gameInstallService.CurrentTask.RefreshSpeed();
                await responseStream.WriteAsync(GameInstallContextDTO.FromTask(_gameInstallService.CurrentTask));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Report game install task progress");
        }
        finally
        {
            _gameInstallService.TaskStateChanged -= _gameInstallService_TaskStateChanged;
            _taskProgressStream = null;
        }
    }







    private async void _gameInstallService_TaskStateChanged(object? sender, GameInstallContext e)
    {
        if (_taskProgressStream is not null)
        {
            await _taskProgressStream.WriteAsync(GameInstallContextDTO.FromTask(e));
        }
    }


    #endregion





}
