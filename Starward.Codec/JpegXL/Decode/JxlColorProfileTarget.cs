namespace Starward.Codec.JpegXL.Decode;

/// <summary>
/// Defines which color profile to get: the profile from the codestream
/// metadata header, which represents the color profile of the original image,
/// or the color profile from the pixel data produced by the decoder. Both are
/// the same if the <see cref="CodeStream.JxlBasicInfo.UsesOriginalProfile"/> is <see langword="true"/>.
/// </summary>
public enum JxlColorProfileTarget
{
    /// <summary>
    /// Get the color profile of the original image from the metadata.
    /// </summary>
    Original = 0,

    /// <summary>
    /// Get the color profile of the pixel data the decoder outputs.
    /// </summary>
    Data = 1,
}