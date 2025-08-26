using Starward.Codec.JpegXL.CodeStream;

namespace Starward.Codec.JpegXL.CMS;

/// <summary>
/// Color space of the image data.
/// </summary>
public enum JxlColorSpace
{

    /// <summary>
    /// Tristimulus RGB
    /// </summary>
    RGB = 0,

    /// <summary>
    /// Luminance based, the primaries in <see cref="JxlColorEncoding"/> must be ignored.
    /// This value implies that <see cref="JxlBasicInfo.NumColorChannels"/> is 1, any
    /// other value implies <see cref="JxlBasicInfo.NumColorChannels"/> is 3.
    /// </summary>
    Gray = 1,

    /// <summary>
    /// XYB (opsin) color space
    /// </summary>
    XYB = 2,

    /// <summary>
    /// None of the other table entries describe the color space appropriately
    /// </summary>
    Unknown = 3,
}