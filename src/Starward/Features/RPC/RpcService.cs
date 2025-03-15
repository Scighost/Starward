using Microsoft.Extensions.Logging;
using Starward.Frameworks;
using Starward.RPC;
using Starward.RPC.Env;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Starward.Features.RPC;

internal class RpcService
{



    private Process _rpcServerProcess;


    private ILogger<RpcService> _logger;


    private bool _noLongerChange;


    public RpcService(ILogger<RpcService> logger)
    {
        _logger = logger;
    }





    public static bool CheckRpcServerRunning()
    {
        return RpcClientFactory.CheckRpcServerRunning();
    }



    public async Task<RpcServerInfo> GetRpcServerInfoAsync(DateTime deadline)
    {
        var client = CreateRpcClient<Env.EnvClient>();
        return await client.GetRpcServerInfoAsync(new EmptyMessage(), deadline: deadline);
    }




    /// <summary>
    /// 仅在操作取消时返回 false
    /// </summary>
    /// <returns></returns>
    public async Task<bool> EnsureRpcServerRunningAsync()
    {
        const int ERROR_CANCELLED = 0x000004C7;
        try
        {
            if (!CheckRpcServerRunning() || _rpcServerProcess is null)
            {
                _rpcServerProcess?.Dispose();
                _rpcServerProcess = await RpcClientFactory.EnsureRpcServerRunningAsync();
            }
            await SetEnviromentAsync();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_CANCELLED)
        {
            return false;
        }
        return true;
    }



    private async Task SetEnviromentAsync()
    {
        var client = CreateRpcClient<Env.EnvClient>();
        await client.SetEnviromentAsync(new EnviromentMessage
        {
            ParentProcessId = Environment.ProcessId,
            KeepRunningOnExited = AppSetting.KeepRpcServerRunningInBackground,
            DownloadRateLimit = Math.Clamp(AppSetting.SpeedLimitKBPerSecond * 1024, 0, int.MaxValue),
        });
    }



    public async void TrySetEnviromentAsync()
    {
        try
        {
            if (CheckRpcServerRunning())
            {
                var client = CreateRpcClient<Env.EnvClient>();
                await client.SetEnviromentAsync(new EnviromentMessage
                {
                    ParentProcessId = Environment.ProcessId,
                    KeepRunningOnExited = AppSetting.KeepRpcServerRunningInBackground,
                    DownloadRateLimit = Math.Clamp(AppSetting.SpeedLimitKBPerSecond * 1024, 0, int.MaxValue),
                }, deadline: DateTime.UtcNow.AddSeconds(3));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set enviroment when startup");
        }
    }



    public async void KeepRunningOnExited(bool value, bool noLongerChange = false)
    {
        try
        {
            if (CheckRpcServerRunning() && !_noLongerChange)
            {
                var client = CreateRpcClient<Env.EnvClient>();
                await client.SetParentProcessAsync(new ParentProcessMessage
                {
                    ProcessId = Environment.ProcessId,
                    KeepRunningOnExited = value,
                    NoLongerChange = noLongerChange,
                });
            }
            _noLongerChange = _noLongerChange || noLongerChange;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keep rpc server running on exited");
        }
    }




    public async Task StopRpcServerAsync(DateTime deadline)
    {
        if (CheckRpcServerRunning())
        {
            var client = CreateRpcClient<Env.EnvClient>();
            await client.StopRpcServerAsync(new EmptyMessage(), deadline: deadline);
        }
    }





    public static T CreateRpcClient<T>() where T : Grpc.Core.ClientBase<T>
    {
        return RpcClientFactory.CreateRpcClient<T>();
    }



}
