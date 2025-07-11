using System.ComponentModel;

namespace Starward.Core.Gacha.StarRail;

public readonly record struct StarRailGachaType(int Value) : IGachaType
{


    /// <summary>
    /// 群星跃迁
    /// </summary>
    [Description("群星跃迁")]
    public const int StellarWarp = 1;


    /// <summary>
    /// 始发跃迁
    /// </summary>
    [Description("始发跃迁")]
    public const int DepartureWarp = 2;


    /// <summary>
    /// 角色活动跃迁
    /// </summary>
    [Description("角色活动跃迁")]
    public const int CharacterEventWarp = 11;


    /// <summary>
    /// 光锥活动跃迁
    /// </summary>
    [Description("光锥活动跃迁")]
    public const int LightConeEventWarp = 12;


    /// <summary>
    /// 角色联动跃迁
    /// </summary>
    [Description("角色联动跃迁")]
    public const int CharacterCollaborationWarp = 21;


    /// <summary>
    /// 光锥联动跃迁
    /// </summary>
    [Description("光锥联动跃迁")]
    public const int LightConeCollaborationWarp = 22;



    public string ToLocalization() => Value switch
    {
        StellarWarp => CoreLang.GachaType_StellarWarp,
        DepartureWarp => CoreLang.GachaType_DepartureWarp,
        CharacterEventWarp => CoreLang.GachaType_CharacterEventWarp,
        LightConeEventWarp => CoreLang.GachaType_LightConeEventWarp,
        CharacterCollaborationWarp => CoreLang.GachaType_CharacterCollaborationWarp,
        LightConeCollaborationWarp => CoreLang.GachaType_LightConeCollaborationWarp,
        _ => "",
    };



    public override string ToString() => Value.ToString();
    public static implicit operator StarRailGachaType(int value) => new(value);
    public static implicit operator int(StarRailGachaType gachaType) => gachaType.Value;


}
