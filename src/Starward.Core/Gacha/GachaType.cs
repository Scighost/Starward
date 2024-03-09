using System.ComponentModel;

namespace Starward.Core.Gacha;

public enum GachaType
{

    /// <summary>
    /// 群星跃迁
    /// </summary>
    [Description("群星跃迁")]
    StellarWarp = 1,


    /// <summary>
    /// 始发跃迁
    /// </summary>
    [Description("始发跃迁")]
    DepartureWarp = 2,


    /// <summary>
    /// 角色活动跃迁
    /// </summary>
    [Description("角色活动跃迁")]
    CharacterEventWarp = 11,


    /// <summary>
    /// 光锥活动跃迁
    /// </summary>
    [Description("光锥活动跃迁")]
    LightConeEventWarp = 12,


    /// <summary>
    /// 新手祈愿
    /// </summary>
    [Description("新手祈愿")]
    NoviceWish = 100,

    /// <summary>
    /// 常驻祈愿
    /// </summary>
    [Description("常驻祈愿")]
    PermanentWish = 200,

    /// <summary>
    /// 角色活动祈愿
    /// </summary>
    [Description("角色活动祈愿")]
    CharacterEventWish = 301,

    /// <summary>
    /// 武器活动祈愿
    /// </summary>
    [Description("武器活动祈愿")]
    WeaponEventWish = 302,

    /// <summary>
    /// 角色活动祈愿-2
    /// </summary>
    [Description("角色活动祈愿-2")]
    CharacterEventWish_2 = 400,

    /// <summary>
    /// 集录祈愿
    /// </summary>
    [Description("集录祈愿")]
    ChronicledWish = 500,
}
