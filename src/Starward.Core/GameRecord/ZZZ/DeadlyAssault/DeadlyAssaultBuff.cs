using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.DeadlyAssault;

/// <summary>
/// 危局强袭战Buff
/// </summary>
public class DeadlyAssaultBuff
{

    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    /// <summary>
    /// 描述，格式化富文本
    /// </summary>
    [JsonPropertyName("desc")]
    public string Desc { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



