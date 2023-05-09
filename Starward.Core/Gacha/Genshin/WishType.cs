using System.ComponentModel;

namespace Starward.Core.Gacha.Genshin;

public enum WishType
{
    /// <summary>
    /// 新手祈愿
    /// </summary>
    [Description("Novice Wish")]
    Novice = 100,

    /// <summary>
    /// 常驻祈愿
    /// </summary>
    [Description("Permanent Wish")]
    Permanent = 200,

    /// <summary>
    /// 角色活动祈愿
    /// </summary>
    [Description("Character Event Wish")]
    CharacterEvent = 301,

    /// <summary>
    /// 武器活动祈愿
    /// </summary>
    [Description("Weapon Event Wish")]
    WeaponEvent = 302,

    /// <summary>
    /// 角色活动祈愿-2
    /// </summary>
    [Description("Character Event Wish 2")]
    CharacterEvent_2 = 400,
}