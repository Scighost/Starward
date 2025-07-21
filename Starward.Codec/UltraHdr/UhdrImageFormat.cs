namespace Starward.Codec.UltraHdr;

/// <summary>
/// List of supported codecs
/// </summary>
public enum UhdrImageFormat
{
    /// <summary>
    /// Compress {Hdr, Sdr rendition} to an {Sdr rendition + Gain Map} using jpeg
    /// </summary>
    JPEG,
    /// <summary>
    /// Compress {Hdr, Sdr rendition} to an {Sdr rendition + Gain Map} using heif
    /// </summary>
    HEIF,
    /// <summary>
    /// Compress {Hdr, Sdr rendition} to an {Sdr rendition + Gain Map} using avif
    /// </summary>
    AVIF,
}
