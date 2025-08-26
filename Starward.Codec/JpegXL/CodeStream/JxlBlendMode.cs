namespace Starward.Codec.JpegXL.CodeStream;

/// <summary>
/// Frame blend modes.
/// When decoding, if coalescing is enabled (default), this can be ignored.
/// </summary>
public enum JxlBlendMode
{
    /// <summary>
    /// Replace blend mode
    /// </summary>
    Replace = 0,

    /// <summary>
    /// Add blend mode
    /// </summary>
    Add = 1,

    /// <summary>
    /// Blend mode
    /// </summary>
    Blend = 2,

    /// <summary>
    /// Multiply-add blend mode
    /// </summary>
    MulAdd = 3,

    /// <summary>
    /// Multiply blend mode
    /// </summary>
    Mul = 4,
}
