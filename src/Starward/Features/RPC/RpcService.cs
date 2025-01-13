using Microsoft.Extensions.Logging;
using Starward.Frameworks;
using Starward.RPC;
using Starward.RPC.Lifecycle;
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
                KeepRunningOnExited(AppSetting.KeepRpcServerRunningInBackground);
            }
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_CANCELLED)
        {
            return false;
        }
        return true;
    }





    public async void KeepRunningOnExited(bool value, bool noLongerChange = false)
    {
        try
        {
            if (CheckRpcServerRunning() && !_noLongerChange)
            {
                var client = CreateRpcClient<Lifecycle.LifecycleClient>();
                await client.AssociateProcessesAsync(new AssociateProcessesMessage
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






    public static T CreateRpcClient<T>() where T : Grpc.Core.ClientBase<T>
    {
        return RpcClientFactory.CreateRpcClient<T>();
    }



}
