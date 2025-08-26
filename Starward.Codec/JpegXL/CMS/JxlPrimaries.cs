namespace Starward.Codec.JpegXL.CMS;

/// <summary>
/// Built-in primaries for color encoding. When decoding, the primaries can be
/// read from the <see cref="JxlColorEncoding.PrimariesRedXY"/>, <see cref="JxlColorEncoding.PrimariesGreenXY"/> and
/// <see cref="JxlColorEncoding.PrimariesBlueXY"/> fields regardless of the enum value. When encoding, the
/// enum values except <see cref="Custom"/> override the numerical fields.
/// Some enum values match a subset of CICP (Rec. ITU-T H.273 | ISO/IEC
/// 23091-2:2019(E)), however the white point and RGB primaries are separate
/// enums here.
/// </summary>
public enum JxlPrimaries
{
    /// <summary>
    /// The CIE xy values of the red, green and blue primaries are: (0.639998686, 0.330010138);
    /// (0.300003784, 0.600003357); (0.150002046, 0.059997204)
    /// </summary>
    sRGB = 1,

    /// <summary>
    /// Primaries must be read from the JxlColorEncoding primaries_red_xy,
    /// primaries_green_xy and primaries_blue_xy fields, or as ICC profile. This
    /// enum value is not an exact match of the corresponding CICP value.
    /// </summary>
    Custom = 2,

    /// <summary>
    /// As specified in Rec. ITU-R BT.2100-1
    /// </summary>
    BT2100 = 9,

    /// <summary>
    /// As specified in SMPTE RP 431-2
    /// </summary>
    P3 = 11,
}