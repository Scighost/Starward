using System.Runtime.Serialization;

namespace SharpSevenZip.Exceptions;

/// <summary>
/// Exception class for empty common root if file name array in SharpSevenZipCompressor.
/// </summary>
public class SharpSevenZipInvalidFileNamesException : SharpSevenZipException
{
    /// <summary>
    /// Exception default message which is displayed if no extra information is specified
    /// </summary>
    public const string DEFAULT_MESSAGE = "Invalid file names have been specified: ";

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipInvalidFileNamesException class
    /// </summary>
    public SharpSevenZipInvalidFileNamesException()
        : base(DEFAULT_MESSAGE) { }

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipInvalidFileNamesException class
    /// </summary>
    /// <param name="message">Additional detailed message</param>
    public SharpSevenZipInvalidFileNamesException(string message)
        : base(DEFAULT_MESSAGE, message) { }

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipInvalidFileNamesException class
    /// </summary>
    /// <param name="message">Additional detailed message</param>
    /// <param name="inner">Inner exception occurred</param>
    public SharpSevenZipInvalidFileNamesException(string message, Exception inner)
        : base(DEFAULT_MESSAGE, message, inner) { }
}
