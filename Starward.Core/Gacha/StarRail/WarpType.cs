using System.ComponentModel;

namespace Starward.Core.Gacha.StarRail;

public enum WarpType
{

    /// <summary>
    /// 群星跃迁
    /// </summary>
    [Description("Stellar Warp")]
    Stellar = 1,


    /// <summary>
    /// 始发跃迁
    /// </summary>
    [Description("Depature Warp")]
    Departure = 2,


    /// <summary>
    /// 角色活动跃迁
    /// </summary>
    [Description("Character Event Warp")]
    CharacterEvent = 11,


    /// <summary>
    /// 光锥活动跃迁
    /// </summary>
    [Description("Light Cone Event Warp")]
    LightConeEvent = 12,

}
