using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

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
            Architecture arch = request.Architecture switch
            {
                "x64" => Architecture.X64,
                "arm64" => Architecture.Arm64,
                _ => RuntimeInformation.OSArchitecture,
            };
            var release = await _metadataClient.GetReleaseAsync(request.Version, arch, context.CancellationToken);
            _ = _updateService.PrepareForUpdateAsync(release, request.TargetPath, context.CancellationToken);
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
