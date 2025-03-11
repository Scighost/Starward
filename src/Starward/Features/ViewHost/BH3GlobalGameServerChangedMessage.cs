namespace Starward.Features.ViewHost;

public class BH3GlobalGameServerChangedMessage
{
    public string GameId { get; set; }

    public BH3GlobalGameServerChangedMessage(string gameId)
    {
        GameId = gameId;
    }
}