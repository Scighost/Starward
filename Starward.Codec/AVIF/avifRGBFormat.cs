namespace Starward.Codec.AVIF;

public enum avifRGBFormat
{
    RGB = 0,
    /// <summary>
    /// This is the default format set in avifRGBImageSetDefaults().
    /// </summary>
    RGBA,
    ARGB,
    BGR,
    BGRA,
    ABGR,
    /// <summary>
    /// uint16_t: [r4 r3 r2 r1 r0 g5 g4 g3 g2 g1 g0 b4 b3 b2 b1 b0]
    /// <para/>
    /// This format is only supported for YUV -> RGB conversion and when avifRGBImage.depth is set to 8.
    /// </summary>
    RGB565,
    GRAY,
    GRAYA,
    AGRAY,
    COUNT
}
