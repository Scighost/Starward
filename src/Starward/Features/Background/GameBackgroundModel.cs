using Starward.Core.HoYoPlay;

namespace Starward.Features.Background;

public class GameBackgroundModel
{
    public string Id { get; set; }

    public int Type { get; set; }

    public bool Last { get; set; }

    public GameBackground GameBackground { get; set; }

    public GameBackgroundModel(GameBackground gameBackground)
    {
        GameBackground = gameBackground;
        Id = gameBackground.Id;
        Type = gameBackground.Type is GameBackground.BACKGROUND_TYPE_VIDEO ? 1 : 0;
    }
}