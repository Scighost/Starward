using System.Runtime.Serialization;

namespace SharpSevenZip.Exceptions;

/// <summary>
/// Exception class for 7-zip library operations.
/// </summary>
public class SharpSevenZipLibraryException : SharpSevenZipException
{
    /// <summary>
    /// Exception default message which is displayed if no extra information is specified
    /// </summary>
    public const string DEFAULT_MESSAGE = "Can not load 7-zip library or internal COM error!";

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipLibraryException class
    /// </summary>
    public SharpSevenZipLibraryException()
        : base(DEFAULT_MESSAGE) { }

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipLibraryException class
    /// </summary>
    /// <param name="message">Additional detailed message</param>
    public SharpSevenZipLibraryException(string message)
        : base(DEFAULT_MESSAGE, message) { }

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipLibraryException class
    /// </summary>
    /// <param name="message">Additional detailed message</param>
    /// <param name="inner">Inner exception occurred</param>
    public SharpSevenZipLibraryException(string message, Exception inner)
        : base(DEFAULT_MESSAGE, message, inner) { }
}
