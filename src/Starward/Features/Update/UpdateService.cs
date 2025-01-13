using Grpc.Core;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using Starward.Features.RPC;
using Starward.Frameworks;
using Starward.RPC.Update;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.Update;

internal class UpdateService
{

    private readonly ILogger<UpdateService> _logger;

    private readonly RpcService _rpcService;

    private readonly MetadataClient _metadataClient;


    public UpdateService(ILogger<UpdateService> logger, RpcService rpcService, MetadataClient metadataClient)
    {
        _logger = logger;
        _rpcService = rpcService;
        _metadataClient = metadataClient;
    }



    public async Task<ReleaseVersion?> CheckUpdateAsync(bool disableIgnore = false)
    {
        _ = NuGetVersion.TryParse(AppSetting.AppVersion, out var currentVersion);
        _ = NuGetVersion.TryParse(AppSetting.IgnoreVersion, out var ignoreVersion);
        var release = await _metadataClient.GetVersionAsync(AppSetting.EnablePreviewRelease, RuntimeInformation.OSArchitecture);
        _logger.LogInformation("Current version: {currentVersion}, latest version: {latestVersion}, ignore version: {ignoreVersion}.", AppSetting.AppVersion, release?.Version, ignoreVersion);
        _ = NuGetVersion.TryParse(release?.Version, out var newVersion);
        if (newVersion! > currentVersion!)
        {
            if (disableIgnore || newVersion! > ignoreVersion!)
            {
                return release;
            }
        }
        return null;
    }



    public static bool UpdateFinished { get; private set; }


    public UpdateState State { get; private set; }

    public int Progress_TotalFileCount { get; private set; }

    public int Progress_DownloadFileCount { get; private set; }

    public long Progress_TotalBytes { get; private set; }

    public long Progress_DownloadBytes { get; private set; }

    public string? ErrorMessage { get; private set; }



    private bool _isUpdating;

    private CancellationTokenSource? _cancellationTokenSource;


    public async Task StartUpdateAsync(ReleaseVersion release)
    {
        if (_isUpdating || UpdateFinished)
        {
            State = UpdateFinished ? UpdateState.Finish : State;
            return;
        }
        try
        {
            ClearState();
            _isUpdating = true;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            State = UpdateState.Pending;
            if (!AppSetting.IsPortable)
            {
                // 无法自动更新
                ErrorMessage = Lang.UpdateService_CannotUpdateAutomatically;
                State = UpdateState.NotSupport;
                return;
            }
            await StartInternalAsync(release, _cancellationTokenSource.Token);
            if (State is UpdateState.Finish)
            {
                UpdateFinished = true;
            }
            else if (State is not UpdateState.Finish and not UpdateState.Error)
            {
                _logger.LogWarning("Update stopped with unexpected state: {state}", State);
                State = UpdateState.Stop;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start update");
            State = UpdateState.Error;
            ErrorMessage = ex.Message;
        }
        finally
        {
            _isUpdating = false;
        }
    }




    private async Task StartInternalAsync(ReleaseVersion release, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _rpcService.EnsureRpcServerRunningAsync())
            {
                // 操作取消
                State = UpdateState.Stop;
                return;
            }
            var client = RpcService.CreateRpcClient<Updater.UpdaterClient>();
            var request = new UpdateRequest
            {
                Version = release.Version,
                Architecture = release.Architecture,
                TargetPath = Path.GetDirectoryName(AppSetting.StarwardLauncherExecutePath),
            };
            using var call = client.Update(request, cancellationToken: cancellationToken);
            await foreach (UpdateProgress progress in call.ResponseStream.ReadAllAsync(cancellationToken))
            {
                State = (UpdateState)progress.State;
                Progress_TotalFileCount = progress.TotalFile;
                Progress_DownloadFileCount = progress.DownloadFile;
                Progress_TotalBytes = progress.TotalBytes;
                Progress_DownloadBytes = progress.DownloadBytes;
                ErrorMessage = progress.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start update internal");
            State = UpdateState.Error;
            ErrorMessage = ex.Message;
        }
    }



    public void StopUpdate()
    {
        _cancellationTokenSource?.Cancel();
    }



    private void ClearState()
    {
        State = UpdateState.Stop;
        Progress_TotalFileCount = 0;
        Progress_DownloadFileCount = 0;
        Progress_TotalBytes = 0;
        Progress_DownloadBytes = 0;
        ErrorMessage = null;
    }



}
