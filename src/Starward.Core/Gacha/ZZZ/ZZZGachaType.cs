using System.ComponentModel;

namespace Starward.Core.Gacha.ZZZ;

public readonly record struct ZZZGachaType(int Value) : IGachaType
{


    /// <summary>
    /// 常驻频段
    /// </summary>
    [Description("常驻频段")]
    public const int StandardChannel = 1;

    /// <summary>
    /// 独家频段
    /// </summary>
    [Description("独家频段")]
    public const int ExclusiveChannel = 2;

    /// <summary>
    /// 音擎频段
    /// </summary>
    [Description("音擎频段")]
    public const int WEngineChannel = 3;

    /// <summary>
    /// 邦布频段
    /// </summary>
    [Description("邦布频段")]
    public const int BangbooChannel = 5;


    /// <summary>
    /// 独家重映
    /// </summary>
    [Description("独家重映")]
    public const int ExclusiveRescreening = 102;


    /// <summary>
    /// 音擎回响
    /// </summary>
    [Description("音擎回响")]
    public const int WEngineReverberation = 103;


    public string ToLocalization() => Value switch
    {
        StandardChannel => CoreLang.GachaType_StandardChannel,
        ExclusiveChannel => CoreLang.GachaType_ExclusiveChannel,
        WEngineChannel => CoreLang.GachaType_WEngineChannel,
        BangbooChannel => CoreLang.GachaType_BangbooChannel,
        ExclusiveRescreening => CoreLang.GachaType_ExclusiveRescreening,
        WEngineReverberation => CoreLang.GachaType_WEngineReverberation,
        _ => "",
    };



    public override string ToString() => Value.ToString();
    public static implicit operator ZZZGachaType(int value) => new(value);
    public static implicit operator int(ZZZGachaType gachaType) => gachaType.Value;


}