using System.Runtime.Serialization;

namespace SharpSevenZip.Exceptions;

/// <summary>
/// Exception class for 7-zip archive open or read operations.
/// </summary>
public class SharpSevenZipArchiveException : SharpSevenZipException
{
    /// <summary>
    /// Exception default message which is displayed if no extra information is specified
    /// </summary>
    public static string DefaultMessage =
        $"Invalid archive: open/read error! Is it encrypted and a wrong password was provided?{Environment.NewLine}" +
        "If your archive is an exotic one, it is possible that SharpSevenZipSharp has no signature for " +
        "its format and thus decided it is TAR by mistake.";

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipArchiveException class
    /// </summary>
    public SharpSevenZipArchiveException()
        : base(DefaultMessage) { }

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipArchiveException class
    /// </summary>
    /// <param name="message">Additional detailed message</param>
    public SharpSevenZipArchiveException(string message)
        : base(DefaultMessage, message) { }

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipArchiveException class
    /// </summary>
    /// <param name="message">Additional detailed message</param>
    /// <param name="inner">Inner exception occurred</param>
    public SharpSevenZipArchiveException(string message, Exception inner)
        : base(DefaultMessage, message, inner) { }
}
