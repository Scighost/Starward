using System.Runtime.Serialization;

namespace SharpSevenZip.Exceptions;

/// <summary>
/// Exception class for LZMA operations.
/// </summary>
public class LzmaException : SharpSevenZipException
{
    /// <summary>
    /// Exception default message which is displayed if no extra information is specified
    /// </summary>
    public const string DEFAULT_MESSAGE = "Specified stream is not a valid LZMA compressed stream!";

    /// <summary>
    /// Initializes a new instance of the LzmaException class
    /// </summary>
    public LzmaException()
        : base(DEFAULT_MESSAGE) { }

    /// <summary>
    /// Initializes a new instance of the LzmaException class
    /// </summary>
    /// <param name="message">Additional detailed message</param>
    public LzmaException(string message)
        : base(DEFAULT_MESSAGE, message) { }

    /// <summary>
    /// Initializes a new instance of the LzmaException class
    /// </summary>
    /// <param name="message">Additional detailed message</param>
    /// <param name="inner">Inner exception occurred</param>
    public LzmaException(string message, Exception inner)
        : base(DEFAULT_MESSAGE, message, inner) { }
}
