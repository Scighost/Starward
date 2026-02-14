using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.CheckIn;

public class CheckInResult
{
    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("risk_code")]
    public int RiskCode { get; set; }

    [JsonPropertyName("gt")]
    public string Gt { get; set; }

    [JsonPropertyName("challenge")]
    public string Challenge { get; set; }

    [JsonPropertyName("success")]
    public int Success { get; set; }

    [JsonPropertyName("is_risk")]
    public bool IsRisk { get; set; }
}
