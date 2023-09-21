using System.Text.Json.Serialization;

namespace Starward.Core.SelfQuery;

public class SelfQueryUserInfo
{

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }


    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }


    [JsonPropertyName("region")]
    public string Region { get; set; }


    [JsonPropertyName("level")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Level { get; set; }


    [JsonIgnore]
    public GameBiz GameBiz { get; set; }

}
