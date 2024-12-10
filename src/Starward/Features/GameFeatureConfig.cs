using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.GameLauncher;
using System.Collections.Generic;

namespace Starward.Features;

internal partial class GameFeatureConfig
{


    private GameFeatureConfig()
    {

    }


    public List<string> SupportedPages { get; init; } = [];



    public static GameFeatureConfig FromGameId(GameId? gameId)
    {
        if (gameId is null)
        {
            return None;
        }
        return gameId.GameBiz.Value switch
        {
            GameBiz.bh3_cn => bh3_cn,
            _ => Default,
        };
    }





    private static readonly GameFeatureConfig None = new();


    private static readonly GameFeatureConfig Default = new()
    {
        SupportedPages = [nameof(GameLauncherPage)]
    };


    private static readonly GameFeatureConfig bh3_cn = new()
    {
        SupportedPages = [nameof(GameLauncherPage), "GameSettingPage"],
    };




}
