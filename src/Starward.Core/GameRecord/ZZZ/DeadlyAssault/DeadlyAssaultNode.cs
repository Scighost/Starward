using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;
using Starward.Core.GameRecord.ZZZ.Common;

namespace Starward.Core.GameRecord.ZZZ.DeadlyAssault;

public class DeadlyAssaultNode
{

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("star")]
    public int Star { get; set; }

    [JsonPropertyName("total_star")]
    public int TotalStar { get; set; }

    [JsonPropertyName("challenge_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime ChallengeTime { get; set; }

    [JsonPropertyName("boss")]
    public List<DeadlyAssaultBoss> Boss { get; set; }

    [JsonPropertyName("buffer")]
    public List<DeadlyAssaultBuff> Buff { get; set; }

    [JsonPropertyName("avatar_list")]
    public List<ZZZAvatar> Avatars { get; set; }

    [JsonPropertyName("buddy")]
    public ZZZBuddy Buddy { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



