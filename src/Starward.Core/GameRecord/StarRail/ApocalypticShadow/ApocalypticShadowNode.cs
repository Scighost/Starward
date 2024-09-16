using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ApocalypticShadow;

public class ApocalypticShadowNode
{

    [JsonPropertyName("challenge_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime ChallengeTime { get; set; }

    [JsonPropertyName("avatars")]
    public List<ApocalypticShadowAvatar> Avatars { get; set; }

    [JsonPropertyName("buff")]
    public ApocalypticShadowBuff Buff { get; set; }

    [JsonPropertyName("score")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Score { get; set; }

    [JsonPropertyName("boss_defeated")]
    public bool BossDefeated { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



