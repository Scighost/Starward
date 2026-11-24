using Starward.GameInstall;

namespace Starward.Features.GameInstall;

class GameInstallTaskStartedMessage
{
    public GameInstallProgress InstallTask { get; init; }

    public GameInstallTaskStartedMessage(GameInstallProgress installTask)
    {
        InstallTask = installTask;
    }
}
