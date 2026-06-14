using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.PureFiction;

public class PureFictionFloorDetail
{

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("round_num")]
    public int RoundNum { get; set; }

    [JsonPropertyName("star_num")]
    public int StarNum { get; set; }

    [JsonPropertyName("node_1")]
    public PureFictionNode Node1 { get; set; }

    [JsonPropertyName("node_2")]
    public PureFictionNode Node2 { get; set; }

    [JsonPropertyName("node_3")]
    public PureFictionNode? Node3 { get; set; }

    [JsonPropertyName("maze_id")]
    public int MazeId { get; set; }

    [JsonPropertyName("is_fast")]
    public bool IsFast { get; set; }

    [JsonIgnore]
    public int TotalScore => Node1.Score + Node2.Score + (Node3?.Score ?? 0);

    [JsonIgnore]
    public int NormalStarNum => Math.Min(StarNum, 3);

    [JsonIgnore]
    public int ExtraStarNum => Math.Max(0, StarNum - 3);


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



