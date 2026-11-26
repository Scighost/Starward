using CommunityToolkit.Mvvm.ComponentModel;
using Starward.Core.HoYoPlay;


namespace Starward.Features.GameSelector;

public class GameInfoModel : ObservableObject
{
    public GameInfo GameInfo { get; set; }

    public bool IsPinned { get; set => SetProperty(ref field, value); }

    public bool EnableReservation => GameInfo.DisplayStatus is "LAUNCHER_GAME_DISPLAY_STATUS_RESERVATION_ENABLED";

    public GameInfoModel(GameInfo gameInfo)
    {
        GameInfo = gameInfo;
    }
}