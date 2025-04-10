using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.DeadlyAssault;

/// <summary>
/// 危局强袭战Boss
/// </summary>
public class DeadlyAssaultBoss
{

    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("race_icon")]
    public string RaceIcon { get; set; }

    [JsonPropertyName("bg_icon")]
    public string BgIcon { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



