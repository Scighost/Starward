using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.StygianOnslaught;

public class StygianOnslaughtMonsterTag
{

    /// <summary>
    /// 1：优势，0：劣势
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }


    /// <summary>
    /// 存在类似 <c>{SPRITE_PRESET#11003}</c> 的标签，包含大括号
    /// </summary>
    [JsonPropertyName("desc")]
    public string Desc { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}
