using Starward.Core.HoYoPlay;

namespace Starward.Features.Background;

internal class BackgroundChangedMessage
{

    public GameBackground? GameBackground { get; set; }

    public BackgroundChangedMessage(GameBackground? gameBackground = null)
    {
        GameBackground = gameBackground;
    }

}
