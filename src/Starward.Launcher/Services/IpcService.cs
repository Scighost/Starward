using System.Threading.Tasks;
using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using dotnetCampus.Ipc.Pipes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Starward.Launcher.Services;

internal class IpcService
{
    private readonly Task<JsonIpcDirectRoutedClientProxy> _task;

    public IpcService(IpcProvider ipcProvider, ILogger<IpcService> logger, IOptions<IpcOptions> options)
    {
        ipcProvider.StartServer();
        logger.LogTrace("Waiting for IPC connection: {peerName}", options.Value.Ipc);
        _task = new JsonIpcDirectRoutedProvider(ipcProvider).GetAndConnectClientAsync(options.Value.Ipc);
    }

    public async Task<string?> GetIconPathAsync()
    {
        var proxy = await _task;
        return (await proxy.GetResponseAsync<GetIconPathResponse>(nameof(GetIconPathAsync)))?.IconPath;
    }

    public async Task OnLeftClickAsync()
    {
        var proxy = await _task;
        await proxy.NotifyAsync(nameof(OnLeftClickAsync));
    }

    public async Task OnRightClickAsync()
    {
        var proxy = await _task;
        await proxy.NotifyAsync(nameof(OnRightClickAsync));
    }

    public async Task PingAsync()
    {
        var proxy = await _task;
        await proxy.NotifyAsync(nameof(PingAsync));
    }

    public record GetIconPathResponse(string IconPath);
}