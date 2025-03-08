using System.Runtime.Serialization;

namespace SharpSevenZip.Exceptions;

/// <summary>
/// Base SharpSevenZip exception class.
/// </summary>
public class SharpSevenZipException : Exception
{
    /// <summary>
    /// The message for thrown user exceptions.
    /// </summary>
    internal const string USER_EXCEPTION_MESSAGE = "The extraction was successful but " +
        "some exceptions were thrown in your events. Check UserExceptions for details.";

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipException class
    /// </summary>
    public SharpSevenZipException() : base("SharpSevenZip unknown exception.") { }

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipException class
    /// </summary>
    /// <param name="defaultMessage">Default exception message</param>
    public SharpSevenZipException(string defaultMessage)
        : base(defaultMessage) { }

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipException class
    /// </summary>
    /// <param name="defaultMessage">Default exception message</param>
    /// <param name="message">Additional detailed message</param>
    public SharpSevenZipException(string defaultMessage, string message)
        : base(defaultMessage + " Message: " + message) { }

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipException class
    /// </summary>
    /// <param name="defaultMessage">Default exception message</param>
    /// <param name="message">Additional detailed message</param>
    /// <param name="inner">Inner exception occurred</param>
    public SharpSevenZipException(string defaultMessage, string message, Exception inner)
        : base(
            defaultMessage + (defaultMessage.EndsWith(" ", StringComparison.CurrentCulture) ? "" : " Message: ") +
            message, inner)
    { }

    /// <summary>
    /// Initializes a new instance of the SharpSevenZipException class
    /// </summary>
    /// <param name="defaultMessage">Default exception message</param>
    /// <param name="inner">Inner exception occurred</param>
    public SharpSevenZipException(string defaultMessage, Exception inner)
        : base(defaultMessage, inner) { }
}
