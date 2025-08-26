namespace Starward.Codec.JpegXL;

/// <summary>
/// The exception that is thrown when a JPEG XL codec error occurs.
/// </summary>
public class JxlException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JxlException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public JxlException(string message) : base(message)
    {

    }

}
