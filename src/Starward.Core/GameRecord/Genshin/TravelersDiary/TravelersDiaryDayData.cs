using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.TravelersDiary;

/// <summary>
/// 旅行札记每日统计
/// </summary>
public class TravelersDiaryDayData
{

    [JsonPropertyName("current_primogems")]
    public int CurrentPrimogems { get; set; }


    [JsonPropertyName("current_mora")]
    public int CurrentMora { get; set; }


    [JsonPropertyName("last_primogems")]
    public int LastPrimogems { get; set; }


    [JsonPropertyName("last_mora")]
    public int LastMora { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }


}
