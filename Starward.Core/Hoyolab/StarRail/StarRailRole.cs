using System.Text.Json.Serialization;

namespace Starward.Core.Hoyolab.StarRail;

public class StarRailRole
{

    [JsonPropertyName("game_biz")]
    public string GameBiz { get; set; }


    [JsonPropertyName("region")]
    public string Region { get; set; }


    [JsonPropertyName("game_uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Uid { get; set; }


    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }


    [JsonPropertyName("level")]
    public int Level { get; set; }


    [JsonPropertyName("is_chosen")]
    public bool IsChosen { get; set; }


    [JsonPropertyName("region_name")]
    public string RegionName { get; set; }


    [JsonPropertyName("is_official")]
    public bool IsOfficial { get; set; }


    [JsonIgnore]
    public string? Cookie { get; set; }

}



internal class StarRailRoleWrapper
{
    [JsonPropertyName("list")]
    public List<StarRailRole> List { get; set; }
}