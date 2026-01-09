using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ChallengePeak;

public class ChallengePeakRecord
{
    [JsonPropertyName("group")]
    public ChallengePeakGroup Group { get; set; }

    [JsonPropertyName("boss_info")]
    public ChallengePeakBossInfo BossInfo { get; set; }

    [JsonPropertyName("mob_infos")]
    public List<ChallengePeakMobInfo> MobInfos { get; set; }

    [JsonPropertyName("has_challenge_record")]
    public bool HasChallengeRecord { get; set; }

    [JsonPropertyName("battle_num")]
    public int BattleNum { get; set; }

    [JsonPropertyName("boss_record")]
    public ChallengePeakBossRecord BossRecord { get; set; }

    [JsonPropertyName("mob_records")]
    public List<ChallengePeakMobRecord> MobRecords { get; set; }

    [JsonPropertyName("boss_stars")]
    public int BossStars { get; set; }

    [JsonPropertyName("mob_stars")]
    public int MobStars { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }


    [JsonIgnore]
    public List<ChallengePeakRecordMob> Mobs => MobInfos.LeftJoin(MobRecords, x => x.MazeId, y => y.MazeId, (x, y) => new ChallengePeakRecordMob(x, y)).ToList();
}
