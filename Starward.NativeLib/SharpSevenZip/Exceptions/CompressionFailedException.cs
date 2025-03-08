using System.Runtime.Serialization;

namespace SharpSevenZip.Exceptions;

/// <summary>
/// Exception class for ArchiveUpdateCallback.
/// </summary>
[Serializable]public class CompressionFailedException : SharpSevenZipException
{
    /// <summary>
    /// Exception default message which is displayed if no extra information is specified
    /// </summary>
    public const string DEFAULT_MESSAGE = "Could not pack files!";

    /// <summary>
    /// Initializes a new instance of the CompressionFailedException class
    /// </summary>
    public CompressionFailedException()
        : base(DEFAULT_MESSAGE) { }

    /// <summary>
    /// Initializes a new instance of the CompressionFailedException class
    /// </summary>
    /// <param name="message">Additional detailed message</param>
    public CompressionFailedException(string message)
        : base(DEFAULT_MESSAGE, message) { }

    /// <summary>
    /// Initializes a new instance of the CompressionFailedException class
    /// </summary>
    /// <param name="message">Additional detailed message</param>
    /// <param name="inner">Inner exception occurred</param>
    public CompressionFailedException(string message, Exception inner)
        : base(DEFAULT_MESSAGE, message, inner) { }
}
