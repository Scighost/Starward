namespace Starward.Codec.AVIF;

public enum avifTransferCharacteristics : ushort
{
    /// <summary>
    /// This is actually reserved, but libavif uses it as a sentinel value.
    /// </summary>
    Unknown = 0,

    BT709 = 1,

    Unspecified = 2,

    /// <summary>
    /// 2.2 Gamma
    /// </summary>
    BT470M = 4,

    /// <summary>
    /// 2.8 Gamma
    /// </summary>
    BT470BG = 5,

    BT601 = 6,

    SMPTE240 = 7,

    Linear = 8,

    LOG100 = 9,

    LOG100_SQRT10 = 10,

    IEC61966 = 11,

    BT1361 = 12,

    SRGB = 13,

    BT2020_10BIT = 14,

    BT2020_12BIT = 15,

    /// <summary>
    /// Perceptual Quantizer (HDR); BT.2100 PQ
    /// </summary>
    PQ = 16,

    SMPTE2084 = 16,

    SMPTE428 = 17,

    /// <summary>
    /// Hybrid Log-Gamma (HDR); ARIB STD-B67; BT.2100 HLG
    /// </summary>
    HLG = 18,
};
