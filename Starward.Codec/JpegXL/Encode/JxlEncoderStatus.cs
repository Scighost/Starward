namespace Starward.Codec.JpegXL.Encode;

/// <summary>
/// Return value for multiple encoder functions.
/// </summary>
public enum JxlEncoderStatus
{

    /// <summary>
    /// Function call finished successfully, or encoding is finished and there is nothing more to be done.
    /// </summary>
    Success = 0,

    /// <summary>
    /// An error occurred, for example out of memory.
    /// </summary>
    Error = 1,

    /// <summary>
    /// The encoder needs more output buffer to continue encoding.
    /// </summary>
    NeedMoreOutput = 2,

}