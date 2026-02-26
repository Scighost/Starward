using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.CheckIn;

public class CheckInInfo
{
    [JsonPropertyName("total_sign_day")]
    public int TotalSignDay { get; set; }

    [JsonPropertyName("today")]
    public string Today { get; set; }

    [JsonPropertyName("is_sign")]
    public bool IsSign { get; set; }

    [JsonPropertyName("first_bind")]
    public bool FirstBind { get; set; }

    [JsonPropertyName("is_sub")]
    public bool IsSub { get; set; }

    [JsonPropertyName("region")]
    public string Region { get; set; }
}
