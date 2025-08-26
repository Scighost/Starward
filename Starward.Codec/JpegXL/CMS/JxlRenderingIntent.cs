namespace Starward.Codec.JpegXL.CMS;

/// <summary>
/// Rendering intent for color encoding, as specified in ISO 15076-1:2010
/// </summary>
public enum JxlRenderingIntent
{
    /// <summary>
    /// Perceptual
    /// </summary>
    Perceptual = 0,

    /// <summary>
    /// Media-relative colorimetric
    /// </summary>
    Relative = 1,

    /// <summary>
    /// Saturation
    /// </summary>
    Saturation = 2,

    /// <summary>
    /// ICC-absolute colorimetric
    /// </summary>
    Absolute = 3,
}