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

    [JsonPropertyName("maze_id")]
    public int MazeId { get; set; }

    /// <summary>
    /// 快速通关
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("is_fast")]
    internal bool _isFast { get; set; }

    /// <summary>
    /// 快速通关
    /// </summary>
    [JsonIgnore]
    public bool IsFast
    {
        get
        {
            if (_isFast)
            {
                return true;
            }
            else if (Node1?.Avatars?.Count == 0 && Node2?.Avatars?.Count == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}


