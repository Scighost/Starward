using Grpc.Core;
using Microsoft.Extensions.Logging;
using Starward.RPC.Update.Metadata;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.RPC.Update;

internal class UpdateController : Updater.UpdaterBase
{


    private readonly ILogger<UpdateController> _logger;


    private readonly MetadataClient _metadataClient;


    private readonly UpdateService _updateService;


    public UpdateController(ILogger<UpdateController> logger, MetadataClient metadataClient, UpdateService updateService)
    {
        _logger = logger;
        _metadataClient = metadataClient;
        _updateService = updateService;
    }




    public override async Task Update(UpdateRequest request, IServerStreamWriter<UpdateProgress> responseStream, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Start to update ({Version}), target path: {path}.", request.Version, request.TargetPath);
            var release = await _metadataClient.GetReleaseInfoAsync(request.Version, (Architecture)request.Architecture, (InstallType)request.InstallType, context.CancellationToken);
            string manifestUrl = release.ManifestUrl;
            if (release.Diffs?.TryGetValue(request.CurrentVersion, out var diff) ?? false)
            {
                manifestUrl = diff.ManifestUrl;
            }
            var manifest = await _metadataClient.GetReleaseManifestAsync(manifestUrl, context.CancellationToken);
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
