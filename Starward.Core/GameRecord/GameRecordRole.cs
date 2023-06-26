using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord;


public class GameRecordRole
{

    [JsonPropertyName("game_uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }


    [JsonPropertyName("game_biz")]
    public string GameBiz { get; set; }


    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }


    [JsonPropertyName("level")]
    public int Level { get; set; }


    [JsonPropertyName("region")]
    public string Region { get; set; }


    [JsonPropertyName("region_name")]
    public string RegionName { get; set; }


    [JsonIgnore]
    public string? Cookie { get; set; }

}


internal class GameRecordRoleWrapper
{
    [JsonPropertyName("list")]
    public List<GameRecordRole> List { get; set; }
}