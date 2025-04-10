using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

/// <summary>
/// 式舆防卫战buff
/// </summary>
public class ShiyuDefenseBuff
{

    /// <summary>
    /// 名称
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; }

    /// <summary>
    /// 详细描述，格式化富文本
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



