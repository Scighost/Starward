namespace SharpSevenZip;

/// <summary>
/// Check the format of archives.
/// </summary>
public static class SharpSevenZipArchiveFormat
{
    /// <summary>
    /// Gets the InArchiveFormat for a specific extension.
    /// </summary>
    /// <param name="stream">The stream to identify.</param>
    /// <returns>Corresponding InArchiveFormat.</returns>
    public static InArchiveFormat CheckFormat(Stream stream)
    {
        return FileChecker.CheckSignature(stream, out _, out _);
    }

    /// <summary>
    /// Gets the InArchiveFormat for a specific extension.
    /// </summary>
    /// <param name="stream">The stream to identify.</param>
    /// <param name="offset">The archive beginning offset.</param>
    /// <param name="isExecutable">True if the original format of the stream is PE; otherwise, false.</param>
    /// <returns>Corresponding InArchiveFormat.</returns>
    public static InArchiveFormat CheckFormat(Stream stream, out int offset, out bool isExecutable)
    {
        return FileChecker.CheckSignature(stream, out offset, out isExecutable);
    }

    /// <summary>
    /// Gets the InArchiveFormat for a specific file name.
    /// </summary>
    /// <param name="fileName">The archive file name.</param>
    /// <returns>Corresponding InArchiveFormat.</returns>
    /// <exception cref="System.ArgumentException"/>
    public static InArchiveFormat CheckFormat(string fileName)
    {
        return FileChecker.CheckSignature(fileName, out _, out _);
    }

    /// <summary>
    /// Gets the InArchiveFormat for a specific file name.
    /// </summary>
    /// <param name="fileName">The archive file name.</param>
    /// <param name="offset">The archive beginning offset.</param>
    /// <param name="isExecutable">True if the original format of the file is PE; otherwise, false.</param>
    /// <returns>Corresponding InArchiveFormat.</returns>
    /// <exception cref="System.ArgumentException"/>
    public static InArchiveFormat CheckFormat(string fileName, out int offset, out bool isExecutable)
    {
        return FileChecker.CheckSignature(fileName, out offset, out isExecutable);
    }
}
