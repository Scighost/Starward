using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.DailyNote;

/// <summary>
/// 活跃度
/// </summary>
public class Vitality
{
    [JsonPropertyName("max")]
    public int Max { get; set; }

    [JsonPropertyName("current")]
    public int Current { get; set; }
}