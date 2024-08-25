using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.ImaginariumTheater;

public class ImaginariumTheaterFightStatisic
{

    /// <summary>
    /// 击败最多敌人
    /// </summary>
    [JsonPropertyName("max_defeat_avatar")]
    public ImaginariumTheaterFightStatisicAvatar MaxDefeatAvatar { get; set; }

    /// <summary>
    /// 最高伤害输出
    /// </summary>
    [JsonPropertyName("max_damage_avatar")]
    public ImaginariumTheaterFightStatisicAvatar MaxDamageAvatar { get; set; }

    /// <summary>
    /// 最高承受伤害
    /// </summary>
    [JsonPropertyName("max_take_damage_avatar")]
    public ImaginariumTheaterFightStatisicAvatar MaxTakeDamageAvatar { get; set; }

    /// <summary>
    /// 本次消耗幻剧之花
    /// </summary>
    [JsonPropertyName("total_coin_consumed")]
    public ImaginariumTheaterFightStatisicAvatar TotalCoinConsumed { get; set; }

    /// <summary>
    /// 最快完成演出的队伍
    /// </summary>
    [JsonPropertyName("shortest_avatar_list")]
    public List<ImaginariumTheaterFightStatisicAvatar> ShortestAvatarList { get; set; }

    /// <summary>
    /// 演出总时长（秒）
    /// </summary>
    [JsonPropertyName("total_use_time")]
    public int TotalUseTime { get; set; }


    [JsonPropertyName("is_show_battle_stats")]
    public bool IsShowBattleStats { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}

