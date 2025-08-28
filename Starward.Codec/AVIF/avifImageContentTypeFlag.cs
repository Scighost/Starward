namespace Starward.Codec.AVIF;

/// <summary>
/// Types of image content that can be decoded.
/// </summary>
[Flags]
public enum avifImageContentTypeFlag : uint
{
    None = 0,

    /// <summary>
    /// Color only or alpha only is not currently supported.
    /// </summary>
    ColorAndAlpha = (1 << 0) | (1 << 1),

    GainMap = (1 << 2),

    ALL = ColorAndAlpha | GainMap,

    DecodeDefault = ColorAndAlpha,
}
