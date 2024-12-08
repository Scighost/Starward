using CommunityToolkit.Mvvm.ComponentModel;
using Starward.Core;
using Starward.Core.HoYoPlay;
using System;


namespace Starward.Features.GameSelector;

public partial class GameBizIcon : ObservableObject, IEquatable<GameBizIcon>
{


    public GameId GameId { get; set; }

    public GameBiz GameBiz { get; set; }


    public string GameIcon { get; set => SetProperty(ref field, value); }

    public string GameName { get; set => SetProperty(ref field, value); }

    public string ServerIcon { get; set => SetProperty(ref field, value); }

    public string ServerName { get; set => SetProperty(ref field, value); }

    public double MaskOpacity { get; set => SetProperty(ref field, value); } = 1.0;

    public bool IsSelected
    {
        get;
        set
        {
            field = value;
            MaskOpacity = value ? 0 : 1;
        }
    }



    public GameBizIcon(GameBiz gameBiz)
    {
        GameBiz = gameBiz;
        GameId = GameId.FromGameBiz(gameBiz)!;
        GameIcon = GameBizToIcon(gameBiz);
        ServerIcon = GameBizToServerIcon(gameBiz);
        GameName = gameBiz.ToGameName();
        ServerName = gameBiz.ToGameServerName();
    }



    public GameBizIcon(GameInfo gameInfo)
    {
        GameId = gameInfo;
        GameBiz = gameInfo.GameBiz;
        GameIcon = gameInfo.Display.Icon.Url;
        ServerIcon = GameBizToServerIcon(gameInfo.GameBiz);
        GameName = gameInfo.Display.Name;
        ServerName = gameInfo.GameBiz.ToGameServerName();
    }



    private static string GameBizToIcon(GameBiz gameBiz)
    {
        return gameBiz.Game switch
        {
            GameBiz.bh3 => "ms-appx:///Assets/Image/icon_bh3.jpg",
            GameBiz.hk4e => "ms-appx:///Assets/Image/icon_ys.jpg",
            GameBiz.hkrpg => "ms-appx:///Assets/Image/icon_sr.jpg",
            GameBiz.nap => "ms-appx:///Assets/Image/icon_zzz.jpg",
            _ => "ms-appx:///Assets/Image/Transparent.png",
        };
    }


    private static string GameBizToServerIcon(GameBiz gameBiz)
    {
        return gameBiz.Server switch
        {
            "cn" => "ms-appx:///Assets/Image/gameicon_hyperion.png",
            "global" => "ms-appx:///Assets/Image/gameicon_hoyolab.png",
            "bilibili" => "ms-appx:///Assets/Image/gameicon_bilibili.png",
            _ => "ms-appx:///Assets/Image/Transparent.png",
        };
    }


    public bool Equals(GameBizIcon? other)
    {
        return ReferenceEquals(this, other) || GameBiz == other?.GameBiz;
    }

}
