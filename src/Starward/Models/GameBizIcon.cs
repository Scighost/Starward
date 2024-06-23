using CommunityToolkit.Mvvm.ComponentModel;
using Starward.Core;


namespace Starward.Models;


public partial class GameBizIcon : ObservableObject
{

    public GameBiz GameBiz { get; set; }

    public string GameIcon => GameBizToIcon(GameBiz);

    public string GameName => GameBiz.ToGameName();

    public string ServerIcon => GameBizToServerIcon(GameBiz);

    public string ServerText => GameBizToServerText(GameBiz);

    public string ServerName => GameBiz.ToGameServer();

    public bool CurrentGameBiz { get; set; }

    [ObservableProperty]
    private double maskOpacity = 1.0;



    private static string GameBizToIcon(GameBiz gameBiz)
    {
        return gameBiz.ToGame() switch
        {
            GameBiz.Honkai3rd => "ms-appx:///Assets/Image/icon_bh3.jpg",
            GameBiz.GenshinImpact => "ms-appx:///Assets/Image/icon_ys.jpg",
            GameBiz.StarRail => "ms-appx:///Assets/Image/icon_sr.jpg",
            GameBiz.ZZZ => "ms-appx:///Assets/Image/icon_zzz.jpg",
            _ => "ms-appx:///Assets/Image/Transparent.png",
        };
    }


    private static string GameBizToServerIcon(GameBiz gameBiz)
    {
        return gameBiz switch
        {
            GameBiz.hk4e_cn or GameBiz.hkrpg_cn or GameBiz.bh3_cn or GameBiz.nap_cn => "ms-appx:///Assets/Image/gameicon_hyperion.png",
            GameBiz.hk4e_global or GameBiz.hkrpg_global => "ms-appx:///Assets/Image/gameicon_hoyolab.png",
            GameBiz.hk4e_cloud => "ms-appx:///Assets/Image/gameicon_cloud.png",
            GameBiz.hk4e_bilibili or GameBiz.hkrpg_bilibili => "ms-appx:///Assets/Image/gameicon_bilibili.png",
            _ => "ms-appx:///Assets/Image/Transparent.png",
        };
    }


    private static string GameBizToServerText(GameBiz gameBiz)
    {
        return gameBiz switch
        {
            GameBiz.bh3_global => "EA",
            GameBiz.bh3_jp => "JP",
            GameBiz.bh3_kr => "KR",
            GameBiz.bh3_overseas => "SA",
            GameBiz.bh3_tw => "TC",
            _ => "",
        };
    }

}


