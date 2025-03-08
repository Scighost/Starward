namespace SharpSevenZip.EventArguments;

/// <summary>
/// EventArgs used to report the file information which is going to be packed.
/// </summary>
public sealed class FileInfoEventArgs : PercentDoneEventArgs, ICancellable
{
    /// <summary>
    /// Initializes a new instance of the FileInfoEventArgs class.
    /// </summary>
    /// <param name="fileInfo">The current ArchiveFileInfo.</param>
    /// <param name="percentDone">The percent of finished work.</param>
    public FileInfoEventArgs(ArchiveFileInfo fileInfo, byte percentDone)
        : base(percentDone)
    {
        FileInfo = fileInfo;
    }

    /// <summary>
    /// Gets or sets whether to stop the current archive operation.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>
    /// Gets or sets whether to skip the current file.
    /// </summary>
    public bool Skip { get; set; }

    /// <summary>
    /// Gets the corresponding FileInfo to the event.
    /// </summary>
    public ArchiveFileInfo FileInfo { get; }
}
