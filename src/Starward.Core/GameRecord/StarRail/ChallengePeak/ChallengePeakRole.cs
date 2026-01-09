using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ChallengePeak;

public class ChallengePeakRole
{
    [JsonPropertyName("server")]
    public string Server { get; set; }

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("role_id")]
    public string RoleId { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}