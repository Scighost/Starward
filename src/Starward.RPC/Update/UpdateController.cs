using Grpc.Core;
using Microsoft.Extensions.Logging;
using Starward.Setup.Core;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.RPC.Update;

internal class UpdateController : Updater.UpdaterBase
{


    private readonly ILogger<UpdateController> _logger;


    private readonly ReleaseClient _releaseClient;


    private readonly UpdateService _updateService;


    public UpdateController(ILogger<UpdateController> logger, ReleaseClient releaseClient, UpdateService updateService)
    {
        _logger = logger;
        _releaseClient = releaseClient;
        _updateService = updateService;
    }




    public override async Task Update(UpdateRequest request, IServerStreamWriter<UpdateProgress> responseStream, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Start to update ({Version}), target path: {path}.", request.Version, request.TargetPath);
            var info = await _releaseClient.GetReleaseInfoAsync(request.Version, context.CancellationToken);
            if (info.TryGetReleaseInfoDetail((Architecture)request.Architecture, (InstallType)request.InstallType, out var release))
            {
                string manifestUrl = release.ManifestUrl;
                if (release.Diffs?.TryGetValue(request.CurrentVersion, out var diff) ?? false)
                {
                    manifestUrl = diff.ManifestUrl;
                }
                var manifest = await _releaseClient.GetReleaseManifestAsync(manifestUrl, context.CancellationToken);
                _ = _updateService.PrepareForUpdateAsync(manifest, request.TargetPath, context.CancellationToken);
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
                while (await timer.WaitForNextTickAsync(context.CancellationToken))
                {
                    await responseStream.WriteAsync(_updateService.GetUpdateProgress());
                    if (_updateService.State is UpdateState.Stop or UpdateState.NotSupport or UpdateState.Finish or UpdateState.Error)
                    {
                        break;
                    }
                }
            }
            else
            {
                throw new PlatformNotSupportedException($"Platform ({(Architecture)request.Architecture}, {(InstallType)request.InstallType}) is not supported.");
            }
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Update operation canceled ({Version}).", request.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update controller");
            await responseStream.WriteAsync(new UpdateProgress
            {
                State = (int)UpdateState.Error,
                ErrorMessage = ex.Message,
            });
        }
    }





}
