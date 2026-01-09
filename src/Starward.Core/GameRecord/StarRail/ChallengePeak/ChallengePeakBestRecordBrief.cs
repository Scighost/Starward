using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ChallengePeak;

public class ChallengePeakBestRecordBrief
{
    [JsonPropertyName("total_battle_num")]
    public int TotalBattleNum { get; set; }

    [JsonPropertyName("mob_stars")]
    public int MobStars { get; set; }

    [JsonPropertyName("boss_stars")]
    public int BossStars { get; set; }

    [JsonPropertyName("challenge_peak_rank_icon_type")]
    public string ChallengePeakRankIconType { get; set; }

    [JsonPropertyName("challenge_peak_rank_icon")]
    public string ChallengePeakRankIcon { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
