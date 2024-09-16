using System.ComponentModel;

namespace Starward.Core.Gacha.Genshin;

public readonly record struct GenshinGachaType(int Value) : IGachaType
{


    /// <summary>
    /// 新手祈愿
    /// </summary>
    [Description("新手祈愿")]
    public const int NoviceWish = 100;

    /// <summary>
    /// 常驻祈愿
    /// </summary>
    [Description("常驻祈愿")]
    public const int PermanentWish = 200;

    /// <summary>
    /// 角色活动祈愿
    /// </summary>
    [Description("角色活动祈愿")]
    public const int CharacterEventWish = 301;

    /// <summary>
    /// 武器活动祈愿
    /// </summary>
    [Description("武器活动祈愿")]
    public const int WeaponEventWish = 302;

    /// <summary>
    /// 角色活动祈愿-2
    /// </summary>
    [Description("角色活动祈愿-2")]
    public const int CharacterEventWish_2 = 400;

    /// <summary>
    /// 集录祈愿
    /// </summary>
    [Description("集录祈愿")]
    public const int ChronicledWish = 500;



    public string ToLocalization() => Value switch
    {
        NoviceWish => CoreLang.GachaType_NoviceWish,
        PermanentWish => CoreLang.GachaType_PermanentWish,
        CharacterEventWish => CoreLang.GachaType_CharacterEventWish,
        CharacterEventWish_2 => CoreLang.GachaType_CharacterEventWish_2,
        WeaponEventWish => CoreLang.GachaType_WeaponEventWish,
        ChronicledWish => CoreLang.GachaType_ChronicledWish,
        _ => "",
    };



    public override string ToString() => Value.ToString();
    public static implicit operator GenshinGachaType(int value) => new(value);
    public static implicit operator int(GenshinGachaType value) => value.Value;


}
