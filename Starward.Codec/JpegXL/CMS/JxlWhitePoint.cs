namespace Starward.Codec.JpegXL.CMS;

/// <summary>
/// Built-in white points for color encoding. When decoding, the numerical xy
/// white point value can be read from the <see cref="JxlColorEncoding.WhitePoint"/>
/// field regardless of the enum value. When encoding, enum values except
/// <see cref="Custom"/> override the numerical fields. Some enum values
/// match a subset of CICP (Rec. ITU-T H.273 | ISO/IEC 23091-2:2019(E)), however
/// the white point and RGB primaries are separate enums here.
/// </summary>
public enum JxlWhitePoint
{
    /// <summary>
    /// CIE Standard Illuminant D65: 0.3127, 0.3290
    /// </summary>
    D65 = 1,

    /// <summary>
    /// White point must be read from the <see cref="JxlColorEncoding.WhitePoint"/> field,
    /// or as ICC profile. This enum value is not an exact match of the
    /// corresponding CICP value.
    /// </summary>
    Custom = 2,

    /// <summary>
    /// CIE Standard Illuminant E (equal-energy): 1/3, 1/3
    /// </summary>
    E = 10,

    /// <summary>
    /// DCI-P3 from SMPTE RP 431-2: 0.314, 0.351
    /// </summary>
    DCI = 11,
}