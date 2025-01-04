using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.GameLauncher;
using Starward.Features.GameSetting;
using System.Collections.Generic;

namespace Starward.Features;

internal partial class GameFeatureConfig
{


    private GameFeatureConfig()
    {

    }


    /// <summary>
    /// 支持的页面
    /// </summary>
    public List<string> SupportedPages { get; init; } = [];


    /// <summary>
    /// 游戏内通知内容
    /// </summary>
    public bool InGameNoticesWindow { get; init; }



    public static GameFeatureConfig FromGameId(GameId? gameId)
    {
        if (gameId is null)
        {
            return None;
        }
        return gameId.GameBiz.Game switch
        {
            GameBiz.bh3 => bh3,
            GameBiz.hk4e => hk4e,
            GameBiz.hkrpg => hkrpg,
            GameBiz.nap => nap,
            _ => Default,
        };
    }





    private static readonly GameFeatureConfig None = new();


    private static readonly GameFeatureConfig Default = new()
    {
        SupportedPages = [nameof(GameLauncherPage)]
    };


    private static readonly GameFeatureConfig bh3 = new()
    {
        SupportedPages = [nameof(GameLauncherPage), nameof(GameSettingPage)],
        InGameNoticesWindow = true,
    };


    private static readonly GameFeatureConfig hk4e = new()
    {
        SupportedPages = [nameof(GameLauncherPage), nameof(GameSettingPage)],
        InGameNoticesWindow = true,
    };


    private static readonly GameFeatureConfig hkrpg = new()
    {
        SupportedPages = [nameof(GameLauncherPage), nameof(GameSettingPage)],
        InGameNoticesWindow = true,
    };

    private static readonly GameFeatureConfig nap = new()
    {
        SupportedPages = [nameof(GameLauncherPage), nameof(GameSettingPage)],
        InGameNoticesWindow = true,
    };



}
