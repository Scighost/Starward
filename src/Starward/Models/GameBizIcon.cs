using CommunityToolkit.Mvvm.ComponentModel;
using Starward.Core;
using System;


namespace Starward.Models;


public partial class GameBizIcon : ObservableObject, IEquatable<GameBizIcon>
{

    public GameBiz GameBiz { get; set; }

    public string GameIcon => GameBizToIcon(GameBiz);

    public string GameName => GameBiz.ToGameName();

    public string ServerIcon => GameBizToServerIcon(GameBiz);

    public string ServerText => GameBizToServerText(GameBiz);

    public string ServerName => GameBiz.ToGameServerName();

    public bool CurrentGameBiz { get; set; }

    [ObservableProperty]
    private double maskOpacity = 1.0;



    private static string GameBizToIcon(GameBiz gameBiz)
    {
        return gameBiz.ToGame().Value switch
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
        return gameBiz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hkrpg_cn or GameBiz.bh3_cn or GameBiz.nap_cn => "ms-appx:///Assets/Image/gameicon_hyperion.png",
            GameBiz.hk4e_global or GameBiz.hkrpg_global or GameBiz.nap_global => "ms-appx:///Assets/Image/gameicon_hoyolab.png",
            GameBiz.clgm_cn => "ms-appx:///Assets/Image/gameicon_cloud.png",
            GameBiz.hk4e_bilibili or GameBiz.hkrpg_bilibili or GameBiz.nap_bilibili => "ms-appx:///Assets/Image/gameicon_bilibili.png",
            _ => "ms-appx:///Assets/Image/Transparent.png",
        };
    }


    private static string GameBizToServerText(GameBiz gameBiz)
    {
        return gameBiz.Value switch
        {
            GameBiz.bh3_global => "EA",
            GameBiz.bh3_jp => "JP",
            GameBiz.bh3_kr => "KR",
            GameBiz.bh3_os => "SA",
            GameBiz.bh3_asia => "TC",
            _ => "",
        };
    }


    public bool Equals(GameBizIcon? other)
    {
        return ReferenceEquals(this, other) || GameBiz == other?.GameBiz;
    }

}


