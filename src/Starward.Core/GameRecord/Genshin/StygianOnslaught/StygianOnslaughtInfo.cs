using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.StygianOnslaught;

/// <summary>
/// 幽境危战
/// </summary>
public class StygianOnslaughtInfo
{

    [JsonPropertyName("schedule")]
    public StygianOnslaughtSchedule Schedule { get; set; }

    /// <summary>
    /// 单人挑战
    /// </summary>
    [JsonPropertyName("single")]
    public StygianOnslaughtBattle SinglePlayer { get; set; }

    /// <summary>
    /// 多人挑战
    /// </summary>
    [JsonPropertyName("mp")]
    public StygianOnslaughtBattle MultiPlayer { get; set; }

    /// <summary>
    /// 赋光之人
    /// </summary>
    [JsonPropertyName("blings")]
    public List<StygianOnslaughtBlingAvatar> Blings { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }


    [JsonIgnore]
    public long Uid { get; set; }

    [JsonIgnore]
    public int ScheduleId { get; set; }

    [JsonIgnore]
    public DateTime StartDateTime { get; set; }

    [JsonIgnore]
    public DateTime EndDateTime { get; set; }

    [JsonIgnore]
    public int Difficulty { get; set; }

    [JsonIgnore]
    public int Second { get; set; }

}


internal class StygianOnslaughtWrapper
{

    [JsonPropertyName("data")]
    public List<StygianOnslaughtInfo> Data { get; set; }


    [JsonPropertyName("is_unlock")]
    public bool IsUnlock { get; set; }

}
