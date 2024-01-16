using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.SimulatedUniverse;

public class SimulatedUniverseRecordItem
{
    /// <summary>
    /// 第几世界
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("finish_time")]
    [JsonConverter(typeof(SimulatedUniverseTimeJsonConverter))]
    public DateTime FinishTime { get; set; }

    /// <summary>
    /// 得分
    /// </summary>
    [JsonPropertyName("score")]
    public int Score { get; set; }

    /// <summary>
    /// 通关的角色
    /// </summary>
    [JsonPropertyName("final_lineup")]
    public List<SimulatedUniverseAvatar> FinalLineup { get; set; }

    /// <summary>
    /// 祝福类型
    /// </summary>
    [JsonPropertyName("base_type_list")]
    public List<SimulatedUniverseBuffType> BuffTypeList { get; set; }

    [JsonPropertyName("cached_avatars")]
    public List<SimulatedUniverseAvatar> CachedAvatars { get; set; }


    [JsonPropertyName("buffs")]
    public List<SimulatedUniverseBuff> Buffs { get; set; }

    /// <summary>
    /// 奇物
    /// </summary>
    [JsonPropertyName("miracles")]
    public List<SimulatedUniverseMiracleItem> Miracles { get; set; }

    /// <summary>
    /// 难度
    /// </summary>
    [JsonPropertyName("difficulty")]
    public int Difficulty { get; set; }

    /// <summary>
    /// 第几世界
    /// </summary>
    [JsonPropertyName("progress")]
    public int Progress { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}


