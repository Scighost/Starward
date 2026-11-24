using CommunityToolkit.Mvvm.ComponentModel;
using Starward.Core;
using Starward.Core.HoYoPlay;
using System.Collections.Generic;
using System.Text.Json.Serialization;


namespace Starward.Features.GameSelector;

public class PinnedGameIcon : ObservableObject
{

    public GameId GameId { get; set; }

    public string LauncherId { get; set; }

    public string GameIcon { get; set => SetProperty(ref field, value); }

    public string GameName { get; set => SetProperty(ref field, value); }

    public string? LauncherIcon { get; set => SetProperty(ref field, value); }

    public string? LauncherName { get; set => SetProperty(ref field, value); }

    [JsonIgnore]
    public double MaskOpacity { get; set => SetProperty(ref field, value); } = 1.0;

    [JsonIgnore]
    public bool IsSelected
    {
        get;
        set
        {
            field = value;
            MaskOpacity = value ? 0 : 1;
        }
    }


    public PinnedGameIcon(LauncherConfigModel launcher, GameInfo gameInfo)
    {
        GameId = new GameId(gameInfo.Id, gameInfo.GameBiz);
        LauncherId = launcher.Id;
        LauncherName = launcher.Name;
        LauncherIcon = launcher.SmallIcon;
        UpdateInfo(gameInfo);
    }


    public void UpdateInfo(GameInfo gameInfo)
    {
        GameIcon = _gameIcons.GetValueOrDefault(gameInfo.GameBiz.Game) ?? gameInfo.Display.Icon.Url;
        GameName = gameInfo.Display.Name;
    }


    private static Dictionary<string, string> _gameIcons = new()
    {
        [GameBiz.bh3] = "ms-appx:///Assets/Image/icon_bh3.jpg",
        [GameBiz.hk4e] = "ms-appx:///Assets/Image/icon_ys.jpg",
        [GameBiz.hkrpg] = "ms-appx:///Assets/Image/icon_sr.jpg",
        [GameBiz.nap] = "ms-appx:///Assets/Image/icon_zzz.jpg",
        [GameBiz.hyg] = "ms-appx:///Assets/Image/icon_hyg.jpg",
    };


    public static string? GetGameIcon(string game)
    {
        return _gameIcons.GetValueOrDefault(game);
    }

}