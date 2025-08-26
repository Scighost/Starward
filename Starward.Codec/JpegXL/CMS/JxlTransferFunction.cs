namespace Starward.Codec.JpegXL.CMS;

/// <summary>
/// Built-in transfer functions for color encoding. Enum values match a subset
/// of CICP (Rec. ITU-T H.273 | ISO/IEC 23091-2:2019(E)) unless specified
/// otherwise.
/// </summary>
public enum JxlTransferFunction
{
    /// <summary>
    /// As specified in ITU-R BT.709-6
    /// </summary>
    Bt709 = 1,

    /// <summary>
    /// None of the other table entries describe the transfer function.
    /// </summary>
    Unknown = 2,

    /// <summary>
    /// The gamma exponent is 1
    /// </summary>
    Linear = 8,

    /// <summary>
    /// As specified in IEC 61966-2-1 sRGB
    /// </summary>
    sRGB = 13,

    /// <summary>
    /// As specified in SMPTE ST 2084
    /// </summary>
    PQ = 16,

    /// <summary>
    /// As specified in SMPTE ST 428-1
    /// </summary>
    DCI = 17,

    /// <summary>
    /// As specified in Rec. ITU-R BT.2100-1 (HLG)
    /// </summary>
    HLG = 18,

    /// <summary>
    /// Transfer function follows power law given by the gamma value in
    /// <see cref="JxlColorEncoding.Gamma"/>. Not a CICP value.
    /// </summary>
    Gamma = 65535,
}