namespace SharpSevenZip;

/// <summary>
/// The Stream extension class to emulate the archive part of a stream.
/// </summary>
internal class ArchiveEmulationStreamProxy : Stream, IDisposable
{
    private readonly bool _leaveOpen;

    /// <summary>
    /// Initializes a new instance of the ArchiveEmulationStream class.
    /// </summary>
    /// <param name="stream">The stream to wrap.</param>
    /// <param name="offset">The stream offset.</param>
    /// <param name="leaveOpen">Whether or not the stream should be closed after operation completes.</param>
    public ArchiveEmulationStreamProxy(Stream stream, int offset, bool leaveOpen = false)
    {
        Source = stream;
        Offset = offset;
        Source.Position = offset;

        _leaveOpen = leaveOpen;
    }

    /// <summary>
    /// Gets the file offset.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// The source wrapped stream.
    /// </summary>
    public Stream Source { get; }

    /// <inheritdoc />
    public override bool CanRead => Source.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => Source.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => Source.CanWrite;

    /// <inheritdoc />
    public override void Flush()
    {
        Source.Flush();
    }

    /// <inheritdoc />
    public override long Length => Source.Length - Offset;

    /// <inheritdoc />
    public override long Position
    {
        get => Source.Position - Offset;
        set => Source.Position = value;
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        return Source.Read(buffer, offset, count);
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        return Source.Seek(origin == SeekOrigin.Begin ? offset + Offset : offset,
            origin) - Offset;
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        Source.SetLength(value);
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        Source.Write(buffer, offset, count);
    }

    /// <inheritdoc />
    public new void Dispose()
    {
        if (!_leaveOpen)
        {
            Source.Dispose();
        }
    }

    /// <inheritdoc />
    public override void Close()
    {
        if (!_leaveOpen)
        {
            Source.Close();
        }
    }
}
