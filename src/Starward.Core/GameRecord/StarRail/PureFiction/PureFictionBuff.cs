using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.PureFiction;

public class PureFictionBuff
{

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name_mi18n")]
    public string Name { get; set; }

    [JsonPropertyName("desc_mi18n")]
    public string Desc { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



