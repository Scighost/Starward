using System;
using System.IO;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using dotnetCampus.Ipc.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Starward.Launcher.Services;

namespace Starward.Launcher;

internal class Worker(
    LaunchService launchService,
    IpcService ipcService,
    TrayIconService trayIconService,
    IHostApplicationLifetime lifetime,
    ILogger<Worker> logger,
    IConfiguration configuration,
    IOptions<IpcOptions> options,
    WindowsPrincipal principal) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (principal.IsInRole(WindowsBuiltInRole.Administrator)) logger.LogWarning("Running as administrator");
        logger.LogTrace("Worker setting up");
#if DEBUG
        launchService.BasePath = Environment.CurrentDirectory;
#endif
        if (!configuration.GetSection("Starward:Launcher:NoLaunch").Exists())
        {
            var process = launchService.Launch([
                ..Environment.GetCommandLineArgs()[1..],
                "--Starward:Launcher:TrayIcon:Ipc",
                options.Value.Ipc
            ]);
            if (process?.HasExited is not false)
            {
                logger.LogCritical("Failed to launch Starward, or it exited, quitting launcher");
                lifetime.StopApplication();
                return;
            }

            process.EnableRaisingEvents = true;
            process.Exited += (_, _) =>
            {
                logger.LogTrace("Starward exited, quitting");
                lifetime.StopApplication();
            };
            launchService.RemoveOldVersions();
        }

        var tcs = new TaskCompletionSource<string?>();
        _ = Task.Run(async () =>
        {
            try
            {
                var ret = await ipcService.GetIconPathAsync();
                tcs.TrySetResult(ret);
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
        }, stoppingToken);
        stoppingToken.Register(() => tcs.TrySetCanceled());
        var icon = await tcs.Task;
        if (File.Exists(icon))
        {
            logger.LogInformation("Using icon: {icon}", icon);
            trayIconService.SetIcon(icon);
        }
        else
        {
            logger.LogWarning("Icon file not found: {icon}", icon);
        }

        trayIconService.OnLeftClick += async () =>
        {
            try
            {
                await ipcService.OnLeftClickAsync();
            }
            catch
            {
                // ignored
            }
        };
        trayIconService.OnRightClick += async () =>
        {
            try
            {
                await ipcService.OnRightClickAsync();
            }
            catch
            {
                // ignored
            }
        };
        trayIconService.Create();
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ipcService.PingAsync();
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (IpcRemoteException e)
        {
            logger.LogWarning(e, "IPC connection lost, quitting");
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}