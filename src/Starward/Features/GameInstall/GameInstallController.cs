using Grpc.Core;
using Microsoft.Extensions.Logging;
using Starward.GameInstall;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.GameInstall;

public class GameInstallController : GameInstaller.GameInstallerBase
{
    private readonly ILogger<GameInstallController> _logger;

    private readonly GameInstallRunner _gameInstallRunner;


    public GameInstallController(ILogger<GameInstallController> logger, GameInstallRunner gameInstallRunner)
    {
        _logger = logger;
        _gameInstallRunner = gameInstallRunner;
    }



    public override Task<RateLimiterMessage> GetRateLimiter(EmptyMessage request, ServerCallContext context)
    {
        (bool enable, int bytesPerSecond) = _gameInstallRunner.GetRateLimiter();
        return Task.FromResult(new RateLimiterMessage { Enable = enable, BytesPerSecond = bytesPerSecond });
    }



    public override Task<RateLimiterMessage> SetRateLimiter(RateLimiterMessage request, ServerCallContext context)
    {
        int bytesPerSecond = _gameInstallRunner.SetRateLimiter(request.Enable, request.BytesPerSecond);
        return Task.FromResult(new RateLimiterMessage { Enable = request.Enable, BytesPerSecond = bytesPerSecond });
    }



    public override async Task<GameInstallProgressDTO> CreateContext(GameInstallRequestDTO request, ServerCallContext context)
    {
        var progress = await _gameInstallRunner.CreateContextAsync(GameInstallRequest.FromDTO(request), context.CancellationToken).ConfigureAwait(false);
        return progress.ToDTO();
    }



    public override Task<GameInstallProgressDTO> StartOrContinueTask(GameInstallRequestDTO request, ServerCallContext context)
    {
        return Task.FromResult(_gameInstallRunner.StartOrContinueTask(GameInstallRequest.FromDTO(request)).ToDTO());
    }


    public override Task<GameInstallProgressDTO> PauseTask(GameInstallRequestDTO request, ServerCallContext context)
    {
        return Task.FromResult(_gameInstallRunner.PauseTask(GameInstallRequest.FromDTO(request)).ToDTO());
    }



    public override Task<GameInstallProgressDTO> StopTask(GameInstallRequestDTO request, ServerCallContext context)
    {
        return Task.FromResult(_gameInstallRunner.StopTask(GameInstallRequest.FromDTO(request)).ToDTO());
    }


    public override async Task GetProgress(EmptyMessage request, IServerStreamWriter<GameInstallProgressList> responseStream, ServerCallContext context)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        while (await timer.WaitForNextTickAsync(context.CancellationToken).ConfigureAwait(false))
        {
            var list = new GameInstallProgressList();
            list.List.AddRange(_gameInstallRunner.GetAllProgress());
            await responseStream.WriteAsync(list).ConfigureAwait(false);
        }
    }


    public override async Task<UninstallGameResponse> UninstallGame(UninstallGameRequestDTO request, ServerCallContext context)
    {
        return await _gameInstallRunner.UninstallGameAsync(UninstallGameRequest.FromDTO(request), context.CancellationToken).ConfigureAwait(false);
    }


}