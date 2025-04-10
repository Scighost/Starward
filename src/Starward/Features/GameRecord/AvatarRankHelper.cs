using Microsoft.UI.Xaml;

namespace Starward.Features.GameRecord;

public static class AvatarRankHelper
{
    public static Visibility RankToVisibility(int rank)
    {
        return rank > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

}
