namespace Starward.Codec.JpegXL.Decode;

/// <summary>
/// The result of <see cref="JxlDecoderNativeMethod.JxlSignatureCheck"/>.
/// </summary>
public enum JxlSignature
{

    /// <summary>
    /// Not enough bytes were passed to determine if a valid signature was found.
    /// </summary>
    NotEnoughBytes = 0,

    /// <summary>
    /// No valid JPEG XL header was found.
    /// </summary>
    Invalid = 1,

    /// <summary>
    /// A valid JPEG XL codestream signature was found, that is a JPEG XL image without container.
    /// </summary>
    CodeStream = 2,

    /// <summary>
    /// A valid container signature was found, that is a JPEG XL image embedded in a box format container.
    /// </summary>
    Container = 3,

}
