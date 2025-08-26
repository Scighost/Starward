namespace Starward.Codec.JpegXL.Decode;

/// <summary>
/// The exception that is thrown when a JPEG XL decoder error occurs.
/// </summary>
public class JxlDecodeException : JxlException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JxlDecodeException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public JxlDecodeException(string message) : base(message)
    {

    }

}
