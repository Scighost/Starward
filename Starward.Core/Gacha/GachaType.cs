using System.ComponentModel;

namespace Starward.Core.Gacha;

public enum GachaType
{

    /// <summary>
    /// 群星跃迁
    /// </summary>
    [Description("Stellar Warp")]
    StellarWarp = 1,


    /// <summary>
    /// 始发跃迁
    /// </summary>
    [Description("Depature Warp")]
    DepartureWarp = 2,


    /// <summary>
    /// 角色活动跃迁
    /// </summary>
    [Description("Character Event Warp")]
    CharacterEventWarp = 11,


    /// <summary>
    /// 光锥活动跃迁
    /// </summary>
    [Description("Light Cone Event Warp")]
    LightConeEventWarp = 12,


    /// <summary>
    /// 新手祈愿
    /// </summary>
    [Description("Novice Wish")]
    NoviceWish = 100,

    /// <summary>
    /// 常驻祈愿
    /// </summary>
    [Description("Permanent Wish")]
    PermanentWish = 200,

    /// <summary>
    /// 角色活动祈愿
    /// </summary>
    [Description("Character Event Wish")]
    CharacterEventWish = 301,

    /// <summary>
    /// 武器活动祈愿
    /// </summary>
    [Description("Weapon Event Wish")]
    WeaponEventWish = 302,

    /// <summary>
    /// 角色活动祈愿-2
    /// </summary>
    [Description("Character Event Wish 2")]
    CharacterEventWish_2 = 400,

}
