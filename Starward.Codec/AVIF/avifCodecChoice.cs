namespace Starward.Codec.AVIF;

/// <summary>
/// Codec choice
/// </summary>
public enum avifCodecChoice
{

    /// <summary>
    /// Auto
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Encode and decode
    /// </summary>
    AOM,

    /// <summary>
    /// Decode only
    /// </summary>
    DAV1D,

    /// <summary>
    /// Decode only
    /// </summary>
    LIBGAV1,

    /// <summary>
    /// Encode only
    /// </summary>
    RAV1E,

    /// <summary>
    /// Encode only
    /// </summary>
    SVT,

    /// <summary>
    /// Experimental (AV2)
    /// </summary>
    AVM,

}



public enum avifCodecFlag : uint
{
    CanDecode = (1 << 0),
    CanEncode = (1 << 1),
}
