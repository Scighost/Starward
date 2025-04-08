using Starward.Core.GameRecord.ZZZ.Common;
using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

public class ShiyuDefenseFloorDetail
{

    [JsonPropertyName("layer_index")]
    public int LayerIndex { get; set; }

    [JsonPropertyName("rating")]
    [JsonConverter(typeof(JsonStringEnumConverter<ZZZRating>))]
    public ZZZRating Rating { get; set; }

    [JsonPropertyName("layer_id")]
    public int LayerId { get; set; }

    [JsonPropertyName("buffs")]
    public List<ShiyuDefenseBuff> Buffs { get; set; }

    [JsonPropertyName("node_1")]
    public ShiyuDefenseNode Node1 { get; set; }

    [JsonPropertyName("node_2")]
    public ShiyuDefenseNode Node2 { get; set; }

    [JsonIgnore]
    public int BattleTimeSum => Node1.BattleTime + Node2.BattleTime;

    [JsonPropertyName("challenge_time")]
    [JsonConverter(typeof(TimestampStringJsonConverter))]
    public DateTimeOffset ChallengeTime { get; set; }

    [JsonPropertyName("zone_name")]
    public string ZoneName { get; set; }

    [JsonPropertyName("floor_challenge_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime FloorChallengeTime { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



