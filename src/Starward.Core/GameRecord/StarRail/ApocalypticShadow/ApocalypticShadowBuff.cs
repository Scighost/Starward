using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ApocalypticShadow;

public class ApocalypticShadowBuff
{

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name_mi18n")]
    public string Name { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



