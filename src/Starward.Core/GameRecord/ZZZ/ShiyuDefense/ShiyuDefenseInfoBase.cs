using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

public class ShiyuDefenseInfoBase
{

    [JsonIgnore]
    public int Uid { get; set; }


    [JsonIgnore]
    public string Version { get; set; }

    /// <summary>
    /// 最高评分
    /// </summary>
    [JsonIgnore]
    public string MaxRating { get; set; }

    /// <summary>
    /// 最高评分次数
    /// </summary>
    [JsonIgnore]
    public int MaxRatingTimes { get; set; }

    /// <summary>
    /// V2分数
    /// </summary>
    [JsonIgnore]
    public int V2Score { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}