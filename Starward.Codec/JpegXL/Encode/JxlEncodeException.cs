namespace Starward.Codec.JpegXL.Encode;

/// <summary>
/// The exception that is thrown when a JPEG XL encoder error occurs.
/// </summary>
public class JxlEncodeException : JxlException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JxlEncodeException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public JxlEncodeException(string message) : base(message)
    {

    }

}
