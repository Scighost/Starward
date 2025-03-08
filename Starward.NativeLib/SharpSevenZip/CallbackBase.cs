using SharpSevenZip.Exceptions;
using System.Collections.ObjectModel;

namespace SharpSevenZip;

internal class CallbackBase : MarshalByRefObject
{
    /// <summary>
    /// User exceptions thrown during the requested operations, for example, in events.
    /// </summary>
    private readonly List<Exception> _exceptions = new();

    /// <summary>
    /// Initializes a new instance of the CallbackBase class.
    /// </summary>
    protected CallbackBase()
    {
        Password = "";
        ReportErrors = true;
    }

    /// <summary>
    /// Initializes a new instance of the CallbackBase class.
    /// </summary>
    /// <param name="password">The archive password.</param>
    protected CallbackBase(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new SharpSevenZipException("Empty password was specified.");
        }

        Password = password;
        ReportErrors = true;
    }

    /// <summary>
    /// Gets or sets the archive password
    /// </summary>
    public string Password { get; }

    /// <summary>
    /// Gets or sets the value indicating whether the current procedure was cancelled.
    /// </summary>
    public bool Canceled { get; set; }

    /// <summary>
    /// Gets or sets throw exceptions on archive errors flag
    /// </summary>
    public bool ReportErrors { get; }

    /// <summary>
    /// Gets the user exceptions thrown during the requested operations, for example, in events.
    /// </summary>
    public ReadOnlyCollection<Exception> Exceptions => new(_exceptions);

    public void AddException(Exception e)
    {
        _exceptions.Add(e);
    }

    public void ClearExceptions()
    {
        _exceptions.Clear();
    }

    public bool HasExceptions => _exceptions.Count > 0;

    /// <summary>
    /// Throws the specified exception when is able to.
    /// </summary>
    /// <param name="e">The exception to throw.</param>
    /// <param name="handler">The handler responsible for the exception.</param>
    public bool ThrowException(CallbackBase? handler, params Exception[] e)
    {
        if (ReportErrors && (handler == null || !handler.Canceled))
        {
            throw e[0];
        }

        return false;
    }

    /// <summary>
    /// Throws the first exception in the list if any exists.
    /// </summary>
    /// <returns>True means no exceptions.</returns>
    public bool ThrowException()
    {
        if (HasExceptions && ReportErrors)
        {
            throw _exceptions[0];
        }

        return true;
    }

    public void ThrowUserException()
    {
        if (HasExceptions)
        {
            throw new SharpSevenZipException(SharpSevenZipException.USER_EXCEPTION_MESSAGE);
        }
    }
}
