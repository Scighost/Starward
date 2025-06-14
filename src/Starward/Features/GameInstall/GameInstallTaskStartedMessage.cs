using Starward.RPC.GameInstall;

namespace Starward.Features.GameInstall;

class GameInstallTaskStartedMessage
{
    public GameInstallContext InstallTask { get; init; }

    public GameInstallTaskStartedMessage(GameInstallContext installTask)
    {
        InstallTask = installTask;
    }
}
