using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.TravelersDiary;

/// <summary>
/// 旅行记录原石获取的分组统计
/// </summary>
public class TravelersDiaryPrimogemsMonthGroupStats
{

    [JsonPropertyName("action_id")]
    public int ActionId { get; set; }


    [JsonPropertyName("action")]
    public string? ActionName { get; set; }


    [JsonPropertyName("num")]
    public int Number { get; set; }


    [JsonPropertyName("percent")]
    public int Percent { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}
