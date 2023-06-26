using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ForgottenHall;

public class ForgottenHallFloorDetail
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("round_num")]
    public int RoundNum { get; set; }

    [JsonPropertyName("star_num")]
    public int StarNum { get; set; }

    [JsonPropertyName("node_1")]
    public ForgottenHallNode Node1 { get; set; }

    [JsonPropertyName("node_2")]
    public ForgottenHallNode Node2 { get; set; }

    [JsonPropertyName("is_chaos")]
    public bool IsChaos { get; set; }
}


