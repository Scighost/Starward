using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.ImaginariumTheater;

internal class ImaginariumTheaterWarpper
{

    [JsonPropertyName("data")]
    public List<ImaginariumTheaterInfo> Data { get; set; }


    [JsonPropertyName("is_unlock")]
    public bool IsUnlock { get; set; }


    [JsonPropertyName("links")]
    public ImaginariumTheaterLinks Links { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}

