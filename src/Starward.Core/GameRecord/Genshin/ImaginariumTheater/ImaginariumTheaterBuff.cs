using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.ImaginariumTheater;

public class ImaginariumTheaterBuff
{

    [JsonPropertyName("icon")]
    public string Icon { get; set; }


    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonPropertyName("desc")]
    public string Desc { get; set; }

    /// <summary>
    /// 已强化
    /// </summary>
    [JsonPropertyName("is_enhanced")]
    public bool IsEnhanced { get; set; }


    [JsonPropertyName("id")]
    public int Id { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}

