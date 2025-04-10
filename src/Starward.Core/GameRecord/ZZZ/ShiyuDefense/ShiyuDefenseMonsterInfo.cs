using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

/// <summary>
/// 式舆防卫战敌人
/// </summary>
public class ShiyuDefenseMonsterInfo
{

    /// <summary>
    /// 敌方等级
    /// </summary>
    [JsonPropertyName("level")]
    public int Level { get; set; }

    /// <summary>
    /// 怪物列表
    /// </summary>
    [JsonPropertyName("list")]
    public List<ShiyuDefenseMonster> List { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



