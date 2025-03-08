using System.Runtime.Serialization;

namespace SharpSevenZip.Exceptions;

/// <summary>
/// Exception class for ArchiveExtractCallback.
/// </summary>
public class ExtractionFailedException : SharpSevenZipException
{
    /// <summary>
    /// Exception default message which is displayed if no extra information is specified
    /// </summary>
    public const string DEFAULT_MESSAGE = "Could not extract files!";

    /// <summary>
    /// Initializes a new instance of the ExtractionFailedException class
    /// </summary>
    public ExtractionFailedException()
        : base(DEFAULT_MESSAGE) { }

    /// <summary>
    /// Initializes a new instance of the ExtractionFailedException class
    /// </summary>
    /// <param name="message">Additional detailed message</param>
    public ExtractionFailedException(string message)
        : base(DEFAULT_MESSAGE, message) { }

    /// <summary>
    /// Initializes a new instance of the ExtractionFailedException class
    /// </summary>
    /// <param name="message">Additional detailed message</param>
    /// <param name="inner">Inner exception occurred</param>
    public ExtractionFailedException(string message, Exception inner)
        : base(DEFAULT_MESSAGE, message, inner) { }
}
