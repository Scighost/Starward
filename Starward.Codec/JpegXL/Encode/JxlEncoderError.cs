namespace Starward.Codec.JpegXL.Encode;

/// <summary>
/// Error conditions:
/// API usage errors have the 0x80 bit set to 1.
/// Other errors have the 0x80 bit set to 0.
/// </summary>
public enum JxlEncoderError
{

    /// <summary>
    /// No error
    /// </summary>
    OK = 0,

    /// <summary>
    /// Generic encoder error due to unspecified cause
    /// </summary>
    Generic = 1,

    /// <summary>
    /// Out of memory.
    /// </summary>
    OOM = 2,

    /// <summary>
    /// JPEG bitstream reconstruction data could not be represented (e.g. too much tail data)
    /// </summary>
    JBRD = 3,

    /// <summary>
    /// Input is invalid (e.g. corrupt JPEG file or ICC profile)
    /// </summary>
    BadInput = 4,

    /// <summary>
    /// The encoder doesn't (yet) support this. Either no version of libjxl
    /// supports this, and the API is used incorrectly, or the libjxl version
    /// should have been checked before trying to do this.
    /// </summary>
    NotSupported = 0x80,

    /// <summary>
    /// The encoder API is used in an incorrect way.
    /// In this case, a debug build of libjxl should output a specific error
    /// message. (if not, please open an issue about it)
    /// </summary>
    ApiUsageError = 0x81,

}