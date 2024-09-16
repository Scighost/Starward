using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.ImaginariumTheater;

public class ImaginariumTheaterRoundsData
{

    /// <summary>
    /// 参演名单
    /// </summary>
    [JsonPropertyName("avatars")]
    public List<ImaginariumTheaterAvatar> Avatars { get; set; }

    /// <summary>
    /// 神秘收获
    /// </summary>
    [JsonPropertyName("choice_cards")]
    public List<ImaginariumTheaterBuff> ChoiceCards { get; set; }

    /// <summary>
    /// 奇妙助益
    /// </summary>
    [JsonPropertyName("buffs")]
    public List<ImaginariumTheaterBuff> Buffs { get; set; }

    /// <summary>
    /// 获得明星挑战星章
    /// </summary>
    [JsonPropertyName("is_get_medal")]
    public bool IsGetMedal { get; set; }

    /// <summary>
    /// 第几轮
    /// </summary>
    [JsonPropertyName("round_id")]
    public int RoundId { get; set; }

    /// <summary>
    /// 完成时间，timestamp
    /// </summary>
    [JsonPropertyName("finish_time")]
    public string FinishTime { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    [JsonPropertyName("finish_date_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime FinishDateTime { get; set; }

    /// <summary>
    /// 敌人
    /// </summary>
    [JsonPropertyName("enemies")]
    public List<ImaginariumTheaterEnemy> Enemies { get; set; }

    /// <summary>
    /// 辉彩祝福
    /// </summary>
    [JsonPropertyName("splendour_buff")]
    public ImaginariumTheaterSplendourBuff SplendourBuff { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}

