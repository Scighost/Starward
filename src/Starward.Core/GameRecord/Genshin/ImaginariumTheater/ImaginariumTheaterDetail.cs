using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.ImaginariumTheater;

public class ImaginariumTheaterDetail
{

    [JsonPropertyName("rounds_data")]
    public List<ImaginariumTheaterRoundsData> RoundsData { get; set; }

    /// <summary>
    /// 数据不全，完整数据参考 <see cref="ImaginariumTheaterInfo.Stat"/>
    /// </summary>
    [JsonPropertyName("detail_stat")]
    public ImaginariumTheaterStat DetailStat { get; set; }


    [JsonPropertyName("lineup_link")]
    public string LineupLink { get; set; }

    /// <summary>
    /// 待命角色
    /// </summary>
    [JsonPropertyName("backup_avatars")]
    public List<ImaginariumTheaterAvatar> BackupAvatars { get; set; }


    [JsonPropertyName("fight_statisic")]
    public ImaginariumTheaterFightStatisic FightStatisic { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}

