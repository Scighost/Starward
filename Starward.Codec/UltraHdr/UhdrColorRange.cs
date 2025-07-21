namespace Starward.Codec.UltraHdr;

/// <summary>
/// List of supported color ranges
/// </summary>
public enum UhdrColorRange
{
    /// <summary>
    /// Unspecified
    /// </summary>
    Unspecified = -1,
    /// <summary>
    /// Y {[16..235], UV [16..240]} * pow(2, (bpc - 8))
    /// </summary>
    LimitedRange = 0,
    /// <summary>
    /// YUV/RGB {[0..255]} * pow(2, (bpc - 8))
    /// </summary>
    FullRange = 1,
}
