using Starward.Core.HoYoPlay;


namespace Starward.Features.GameSelector;

public class PinGameIconMessage
{
    public GameId GameId { get; set; }

    public LauncherConfig? LauncherConfig { get; set; }

    public LauncherConfigModel? LauncherModel { get; set; }

    public bool IsPinned { get; set; }


    public PinGameIconMessage(GameId gameId, LauncherConfigModel? launcherModel, bool isPinned)
    {
        GameId = gameId;
        LauncherModel = launcherModel;
        IsPinned = isPinned;
    }


    public PinGameIconMessage(GameId gameId, LauncherConfig? launcherConfig, bool isPinned)
    {
        GameId = gameId;
        LauncherConfig = launcherConfig;
        IsPinned = isPinned;
    }
}

