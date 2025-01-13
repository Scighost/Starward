using Grpc.Core;

namespace Starward.RPC.Lifecycle;

internal class LifecycleController : Lifecycle.LifecycleBase
{



    public override Task<RpcServerInfo> GetRpcServerInfo(EmptyMessage request, ServerCallContext context)
    {
        return Task.FromResult(new RpcServerInfo
        {
            ProcessId = Environment.ProcessId,
            ProcessPath = Environment.ProcessPath,
        });
    }



    public override Task<AssociateProcessesMessage> AssociateProcesses(AssociateProcessesMessage request, ServerCallContext context)
    {
        LifecycleManager.AssociateProcesses(request.ProcessId, request.KeepRunningOnExited, request.NoLongerChange);
        var (process, keepRunning, noLongerChange) = LifecycleManager.GetAssociatedProcess();
        return Task.FromResult(new AssociateProcessesMessage
        {
            ProcessId = process?.Id ?? 0,
            KeepRunningOnExited = keepRunning,
            NoLongerChange = noLongerChange,
        });
    }



}
