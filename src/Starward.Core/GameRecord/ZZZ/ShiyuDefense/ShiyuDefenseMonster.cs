using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

/// <summary>
/// 式舆防卫战怪物，Weakness=1代表弱点，-1代表抗性
/// </summary>
public class ShiyuDefenseMonster
{

    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 怪物名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// 弱点属性，此值无用
    /// </summary>
    [JsonPropertyName("weak_element_type")]
    public int WeakElementType { get; set; }

    /// <summary>
    /// 冰弱点
    /// </summary>
    [JsonPropertyName("ice_weakness")]
    public int IceWeakness { get; set; }

    /// <summary>
    /// 火弱点
    /// </summary>
    [JsonPropertyName("fire_weakness")]
    public int FireWeakness { get; set; }

    /// <summary>
    /// 电弱点
    /// </summary>
    [JsonPropertyName("elec_weakness")]
    public int ElecWeakness { get; set; }

    /// <summary>
    /// 以太弱点
    /// </summary>
    [JsonPropertyName("ether_weakness")]
    public int EtherWeakness { get; set; }

    /// <summary>
    /// 物理弱点
    /// </summary>
    [JsonPropertyName("physics_weakness")]
    public int PhysicsWeakness { get; set; }

    /// <summary>
    /// 怪物图片
    /// </summary>
    [JsonPropertyName("icon_url")]
    public string IconUrl { get; set; }

    /// <summary>
    /// 图标图片
    /// </summary>
    [JsonPropertyName("race_icon")]
    public string RaceIcon { get; set; }

    /// <summary>
    /// 背景图片
    /// </summary>
    [JsonPropertyName("bg_icon")]
    public string BgIcon { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



