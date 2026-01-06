using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ChallengePeak;

public class ChallengePeakGroup
{
    [JsonPropertyName("group_id")]
    public int GroupId { get; set; }

    [JsonPropertyName("begin_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime BeginTime { get; set; }

    [JsonPropertyName("end_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("name_mi18n")]
    public string NameMi18n { get; set; }

    [JsonPropertyName("game_version")]
    public string GameVersion { get; set; }

    [JsonPropertyName("theme_pic_path")]
    public string ThemePicPath { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
