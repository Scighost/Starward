namespace Starward.Codec.UltraHdr;

/// <summary>
/// List of supported color transfers
/// </summary>
public enum UhdrColorTransfer
{
    /// <summary>
    /// Unspecified
    /// </summary>
    Unspecified = -1,
    /// <summary>
    /// Linear
    /// </summary>
    Linear = 0,
    /// <summary>
    /// Hybrid log gamma
    /// </summary>
    HLG = 1,
    /// <summary>
    /// Perceptual Quantizer
    /// </summary>
    PQ = 2,
    /// <summary>
    /// sRGB Gamma
    /// </summary>
    SRGB = 3,
}
