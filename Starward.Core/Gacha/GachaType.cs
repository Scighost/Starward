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
    [Description("角色跃迁")]
    CharacterEventWarp = 11,


    /// <summary>
    /// 光锥活动跃迁
    /// </summary>
    [Description("光锥跃迁")]
    LightConeEventWarp = 12,

}
