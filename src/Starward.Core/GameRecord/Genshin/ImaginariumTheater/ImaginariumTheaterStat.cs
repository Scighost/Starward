using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.ImaginariumTheater;

public class ImaginariumTheaterStat
{

    /// <summary>
    /// 难度
    /// </summary>
    [JsonPropertyName("difficulty_id")]
    public int DifficultyId { get; set; }

    /// <summary>
    /// 抵达最大轮数
    /// </summary>
    [JsonPropertyName("max_round_id")]
    public int MaxRoundId { get; set; }

    /// <summary>
    /// 纹章类型
    /// </summary>
    [JsonPropertyName("heraldry")]
    public int Heraldry { get; set; }

    /// <summary>
    /// int[8]，0 未获得明星挑战星章，1 获得明星挑战星章
    /// </summary>
    [JsonPropertyName("get_medal_round_list")]
    public List<int> GetMedalRoundList { get; set; }

    /// <summary>
    /// 明星挑战星章数量
    /// </summary>
    [JsonPropertyName("medal_num")]
    public int MedalNum { get; set; }

    /// <summary>
    /// 消耗幻剧之花
    /// </summary>
    [JsonPropertyName("coin_num")]
    public int CoinNum { get; set; }

    /// <summary>
    /// 触发场外观众声援次数
    /// </summary>
    [JsonPropertyName("avatar_bonus_num")]
    public int AvatarBonusNum { get; set; }

    /// <summary>
    /// 助演角色支援其他玩家次数
    /// </summary>
    [JsonPropertyName("rent_cnt")]
    public int RentCnt { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

