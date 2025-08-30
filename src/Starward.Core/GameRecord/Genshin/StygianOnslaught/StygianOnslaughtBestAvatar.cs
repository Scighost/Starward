using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.StygianOnslaught;

public class StygianOnslaughtBestAvatar
{

    [JsonPropertyName("avatar_id")]
    public int AvatarId { get; set; }


    [JsonPropertyName("side_icon")]
    public string SideIcon { get; set; }


    [JsonPropertyName("dps")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int DPS { get; set; }

    /// <summary>
    /// 1：最强一击，2：最高总伤害
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}