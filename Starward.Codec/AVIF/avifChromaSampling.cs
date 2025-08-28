namespace Starward.Codec.AVIF;

public enum avifChromaUpsampling
{

    /// <summary>
    /// Chooses best trade off of speed/quality (uses BILINEAR libyuv if available,
    /// or falls back to NEAREST libyuv if available, or falls back to BILINEAR built-in)
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Chooses speed over quality (same as NEAREST)
    /// </summary>
    Fastest = 1,

    /// <summary>
    ///  Chooses the best quality upsampling, given settings (same as BILINEAR)
    /// </summary>
    BestQuality = 2,

    /// <summary>
    ///   Uses nearest-neighbor filter
    /// </summary>
    Nearest = 3,

    /// <summary>
    /// Uses bilinear filter
    /// </summary>
    Bilinear = 4
}


public enum avifChromaDownsampling
{

    /// <summary>
    /// Chooses best trade off of speed/quality (same as AVERAGE)
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Chooses speed over quality (same as AVERAGE)
    /// </summary>
    Fastest = 1,

    /// <summary>
    /// Chooses the best quality upsampling (same as AVERAGE)
    /// </summary>
    BestQuality = 2,

    /// <summary>
    /// Uses averaging filter
    /// </summary>
    Average = 3,

    /// <summary>
    /// Uses sharp yuv filter (libsharpyuv), available for 4:2:0 only, ignored for 4:2:2
    /// </summary>
    SharpYUV = 4
}
