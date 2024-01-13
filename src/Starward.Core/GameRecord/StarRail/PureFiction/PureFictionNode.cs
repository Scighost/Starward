using Starward.Core.GameRecord.StarRail.ForgottenHall;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.PureFiction;

public class PureFictionNode
{

    [JsonPropertyName("challenge_time")]
    [JsonConverter(typeof(ForgottenHallTimeJsonConverter))]
    public DateTime ChallengeTime { get; set; }

    [JsonPropertyName("avatars")]
    public List<PureFictionAvatar> Avatars { get; set; }

    [JsonPropertyName("buff")]
    public PureFictionBuff Buff { get; set; }

    [JsonPropertyName("score")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Score { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



