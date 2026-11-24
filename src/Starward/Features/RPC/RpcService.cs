using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Starward.Features.RPC;

internal static class RpcService
{

    public const string StartupMagic = "zb8L3ShgFjeyDxeA";

    public static string PipeName => $"Starward.RPC/{Process.GetCurrentProcess().SessionId}/{AppConfig.AppVersion}";

    public static string Authorization => field ??= RandomNumberGenerator.GetHexString(16);


    public static bool CheckRpcServerRunning()
    {
        var instance = App.FindInstanceForKey("Starward.RPC");
        return instance is not null && !instance.IsCurrent;
    }



    //public static async Task<RpcServerInfo> GetRpcServerInfoAsync(DateTime deadline)
    //{
    //    var client = CreateRpcClient<Env.EnvClient>();
    //    return await client.GetRpcServerInfoAsync(new EmptyMessage(), deadline: deadline);
    //}




    /// <summary>
    /// 仅在操作取消时返回 false
    /// </summary>
    /// <returns></returns>
    public static async Task<bool> EnsureRpcServerRunningAsync()
    {
        const int ERROR_CANCELLED = 0x000004C7;
        try
        {
            if (!CheckRpcServerRunning())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = AppConfig.StarwardExecutePath,
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    Arguments = $"rpc {StartupMagic} {Environment.ProcessId} {Authorization}",
                });
            }
            //try
            //{
            //    var client = RpcClientFactory.CreateRpcClient<Env.EnvClient>();
            //    await client.GetRpcServerInfoAsync(new EmptyMessage(), deadline: DateTime.UtcNow.AddSeconds(5));
            //}
            //catch (RpcException ex) when (ex.Status is { StatusCode: StatusCode.DeadlineExceeded })
            //{
            //    throw new TimeoutException("Checking RPC server timed out.");
            //}
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_CANCELLED)
        {
            return false;
        }
        return true;
    }



    private static GrpcChannel CreateChannel()
    {
        var connectionFactory = new NamedPipesConnectionFactory(PipeName);
        var httpClient = new HttpClient(new SocketsHttpHandler
        {
            ConnectCallback = connectionFactory.ConnectAsync,
            // 防止因为系统代理无法建立连接
            UseProxy = false,
        }, true);
        httpClient.DefaultRequestHeaders.Add("Authorization", Authorization);
        return GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpClient = httpClient,
        });
    }




    public static T CreateRpcClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : ClientBase<T>
    {
        return (T)Activator.CreateInstance(typeof(T), CreateChannel())!;
    }




}
