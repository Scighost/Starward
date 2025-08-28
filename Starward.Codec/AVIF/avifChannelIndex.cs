namespace Starward.Codec.AVIF;

public enum avifChannelIndex
{
    /// <summary>
    /// These can be used as the index for the yuvPlanes and yuvRowBytes arrays in avifImage.
    /// </summary>
    Y = 0,

    /// <summary>
    /// These can be used as the index for the yuvPlanes and yuvRowBytes arrays in avifImage.
    /// </summary>
    U = 1,

    /// <summary>
    /// These can be used as the index for the yuvPlanes and yuvRowBytes arrays in avifImage.
    /// </summary>
    V = 2,

    /// <summary>
    ///  This may not be used in yuvPlanes and yuvRowBytes, but is available for use with avifImagePlane().
    /// </summary>
    Alpha = 3
}
