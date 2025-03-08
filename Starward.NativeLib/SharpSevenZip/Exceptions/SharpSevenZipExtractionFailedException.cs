using System.Runtime.Serialization;

namespace SharpSevenZip.Exceptions;

/// <summary>
/// Exception class for fail to extract an archive in SharpSevenZipExtractor.
/// </summary>
public class SharpSevenZipExtractionFailedException : SharpSevenZipException
{
    /// <summary>
    /// Exception default message which is displayed if no extra information is specified
    /// </summary>
    public const string DEFAULT_MESSAGE = "The extraction has failed for an unknown reason with code ";

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipExtractionFailedException class
    /// </summary>
    public SharpSevenZipExtractionFailedException()
        : base(DEFAULT_MESSAGE) { }

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipExtractionFailedException class
    /// </summary>
    /// <param name="message">Additional detailed message</param>
    public SharpSevenZipExtractionFailedException(string message)
        : base(DEFAULT_MESSAGE, message) { }

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipExtractionFailedException class
    /// </summary>
    /// <param name="message">Additional detailed message</param>
    /// <param name="inner">Inner exception occurred</param>
    public SharpSevenZipExtractionFailedException(string message, Exception inner)
        : base(DEFAULT_MESSAGE, message, inner) { }
}
