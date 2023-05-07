using System.ComponentModel;

namespace Starward.Core.Warp;

public enum WarpType
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

}
