using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ApocalypticShadow;

public class ApocalypticShadowFloorDetail
{

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("star_num")]
    public string StarNumStr { get; set; }

    [JsonIgnore]
    public int StarNum => int.Parse(StarNumStr);

    [JsonPropertyName("node_1")]
    public ApocalypticShadowNode Node1 { get; set; }

    [JsonPropertyName("node_2")]
    public ApocalypticShadowNode Node2 { get; set; }

    [JsonPropertyName("maze_id")]
    public int MazeId { get; set; }

    [JsonPropertyName("is_fast")]
    public bool IsFast { get; set; }

    [JsonPropertyName("last_update_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime LastUpdateTime { get; set; }

    [JsonIgnore]
    public int TotalScore => Node1.Score + Node2.Score;

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



