using Starward.Features.GameSetting;
using Starward.Core.HoYoPlay;
using Starward.Core.Hypergryph;
using Starward.Features.Gacha;
using Starward.Features.Gacha.Endfield;
using Starward.Features.GameLauncher;
using Starward.Features.GameRecord;
using Starward.Features.Screenshot;
using Starward.Features.SelfQuery;
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

    /// <summary>
    /// 支持云游戏
    /// </summary>
    public bool SupportCloudGame { get; init; }

    /// <summary>
    /// 支持实时便笺
    /// </summary>
    public bool SupportDailyNote { get; init; }


    public static GameFeatureConfig FromGameId(GameId? gameId)
    {
        if (gameId is null)
        {
            return None;
        }
        GameFeatureConfig config = gameId.Id switch
        {
            GameId.bh3_cn => bh3_cn,
            GameId.bh3_glb or GameId.bh3_jp or GameId.bh3_kr or GameId.bh3_os or GameId.bh3_asia => bh3_global,
            GameId.hk4e_cn => hk4e_cn,
            GameId.hk4e_global => hk4e_global,
            GameId.hk4e_bili => hk4e_bilibili,
            GameId.hkrpg_cn => hkrpg_cn,
            GameId.hkrpg_global => hkrpg_global,
            GameId.hkrpg_bili => hkrpg_bilibili,
            GameId.nap_cn => nap_cn,
            GameId.nap_global => nap_global,
            GameId.nap_bili => nap_bilibili,
            HypergryphGameConstants.ArknightsAppCode => arknights_cn,
            HypergryphGameConstants.EndfieldAppCode => endfield_cn,
            HypergryphGameConstants.GlobalEndfieldAppCode => endfield_global,
            _ => Default,
        };
        return config;
    }


    private static readonly GameFeatureConfig None = new();


    private static readonly GameFeatureConfig Default = new()
    {
        SupportedPages = [nameof(GameLauncherPage)]
    };


    private static readonly GameFeatureConfig arknights_cn = new()
    {
        SupportedPages = [nameof(GameLauncherPage)],
    };


    private static readonly GameFeatureConfig endfield_cn = new()
    {
        SupportedPages =
        [
            nameof(GameLauncherPage),
            nameof(GameSettingPage),
            nameof(ScreenshotPage),
            nameof(EndfieldGachaPage),
        ],
    };


    private static readonly GameFeatureConfig endfield_global = new()
    {
        SupportedPages =
        [
            nameof(GameLauncherPage),
            nameof(GameSettingPage),
            nameof(ScreenshotPage),
        ],
    };


    private static readonly GameFeatureConfig bh3_cn = new()
    {
        SupportedPages =
        [
            nameof(GameLauncherPage),
            nameof(ScreenshotPage),
            nameof(GameRecordPage),
        ],
        InGameNoticesWindow = true,
        SupportDailyNote = true,
    };


    private static readonly GameFeatureConfig bh3_global = new()
    {
        SupportedPages =
        [
            nameof(GameLauncherPage),
            nameof(ScreenshotPage),
            nameof(GameRecordPage),
        ],
        InGameNoticesWindow = true,
        SupportDailyNote = true,
    };


    private static readonly GameFeatureConfig hk4e_cn = new()
    {
        SupportedPages =
        [
            nameof(GameLauncherPage),
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
            nameof(GameRecordPage),
            nameof(SelfQueryPage),
            nameof(GenshinBeyondGachaPage),
        ],
        InGameNoticesWindow = true,
        SupportCloudGame = true,
        SupportDailyNote = true,
    };


    private static readonly GameFeatureConfig hk4e_global = new()
    {
        SupportedPages =
        [
            nameof(GameLauncherPage),
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
            nameof(GameRecordPage),
            nameof(SelfQueryPage),
            nameof(GenshinBeyondGachaPage),
        ],
        InGameNoticesWindow = true,
        SupportCloudGame = true,
        SupportDailyNote = true,
    };


    private static readonly GameFeatureConfig hk4e_bilibili = new()
    {
        SupportedPages =
        [
            nameof(GameLauncherPage),
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
            nameof(GameRecordPage),
            nameof(SelfQueryPage),
            nameof(GenshinBeyondGachaPage),
        ],
        InGameNoticesWindow = true,
        SupportDailyNote = true,
    };


    private static readonly GameFeatureConfig hkrpg_cn = new()
    {
        SupportedPages =
        [
            nameof(GameLauncherPage),
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
            nameof(GameRecordPage),
            nameof(SelfQueryPage),
        ],
        InGameNoticesWindow = true,
        SupportDailyNote = true,
    };


    private static readonly GameFeatureConfig hkrpg_global = new()
    {
        SupportedPages =
        [
            nameof(GameLauncherPage),
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
            nameof(GameRecordPage),
            nameof(SelfQueryPage),
        ],
        InGameNoticesWindow = true,
        SupportDailyNote = true,
    };


    private static readonly GameFeatureConfig hkrpg_bilibili = new()
    {
        SupportedPages =
        [
            nameof(GameLauncherPage),
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
            nameof(GameRecordPage),
            nameof(SelfQueryPage),
        ],
        InGameNoticesWindow = true,
        SupportDailyNote = true,
    };



    private static readonly GameFeatureConfig nap_cn = new()
    {
        SupportedPages =
        [
            nameof(GameLauncherPage),
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
            nameof(GameRecordPage),
            nameof(SelfQueryPage),
        ],
        InGameNoticesWindow = true,
        SupportDailyNote = true,
        SupportCloudGame = true,
    };


    private static readonly GameFeatureConfig nap_global = new()
    {
        SupportedPages =
        [
            nameof(GameLauncherPage),
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
            nameof(GameRecordPage),
            nameof(SelfQueryPage),
        ],
        InGameNoticesWindow = true,
        SupportDailyNote = true,
    };


    private static readonly GameFeatureConfig nap_bilibili = new()
    {
        SupportedPages =
        [
            nameof(GameLauncherPage),
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
            nameof(GameRecordPage),
            nameof(SelfQueryPage),
        ],
        InGameNoticesWindow = true,
        SupportDailyNote = true,
    };



}
