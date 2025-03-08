using Starward.RPC.GameInstall;

namespace Starward.Features.GameInstall;

class GameInstallTaskStartedMessage
{
    public GameInstallTask InstallTask { get; init; }

    public GameInstallTaskStartedMessage(GameInstallTask installTask)
    {
        InstallTask = installTask;
    }
}
