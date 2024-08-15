namespace Starward.Core.ZipStreamDownload.Http;

public abstract class HttpPartialDownloadStream : Stream
{
    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => EndBytes - StartBytes;

    public override long Position
    {
        get
        {
            ThrowIfThisIsDisposed();
            return _fakePosition;
        }
        set
        {
            ThrowIfThisIsDisposed();
            SeekSimulated(value);
        }
    }

    public long StartBytes { get; protected set; }

    public long EndBytes { get; protected set; }

    public long FileLength { get; protected init; }

    public DateTimeOffset FileLastModifiedTime { get; protected init; }

    /// <summary>
    /// 标识此类释放已经释放资源
    /// </summary>
    private bool _disposed;

    private long _realPosition;

    private long _fakePosition;

    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfThisIsDisposed();
        SeekSimulated(ValidateSeekArgumentsAndGetNewPosition(offset, origin));
        return _fakePosition;
    }

    public override void Flush()
    {
        ThrowIfThisIsDisposed();
        SeekActually();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        ThrowIfThisIsDisposed();
        await SeekActuallyAsync(cancellationToken).ConfigureAwait(false);
    }

    public bool ResetRange(long? startBytes = null, long? endBytes = null)
    {
        ThrowIfThisIsDisposed();
        var result = ResetRangeCore(startBytes, endBytes);
        if (result) _fakePosition = _realPosition = 0;
        return result;
    }

    protected abstract bool ResetRangeCore(long? startBytes = null, long? endBytes = null);

    public async Task<bool> ResetRangeAsync(long? startBytes = null, long? endBytes = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfThisIsDisposed();
        var result = await ResetRangeAsyncCore(startBytes, endBytes, cancellationToken).ConfigureAwait(false);
        if (result) _fakePosition = _realPosition = 0;
        return result;
    }

    protected virtual Task<bool> ResetRangeAsyncCore(long? startBytes = null, long? endBytes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(ResetRangeCore(startBytes, endBytes));
        }
        catch (Exception e)
        {
            return Task.FromException<bool>(e);
        }
    }

    protected int GetReadCount(int count)
    {
        if (_realPosition == Length) return 0;
        if (_realPosition + count > Length) return (int)Math.Min(Length - _realPosition, int.MaxValue);
        return count;
    }

    private void SeekSimulated(long newPosition)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(newPosition);
        if (newPosition > Length) throw new EndOfStreamException();
        _fakePosition = newPosition;
    }

    protected void SetPositionToEndActually()
    {
        if (_realPosition != _fakePosition) throw new InvalidOperationException();
        _realPosition = _fakePosition = Length;
    }

    protected void AddPositionActually(int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (_realPosition != _fakePosition) throw new InvalidOperationException();
        _fakePosition = _realPosition += count;
    }

    protected void SeekActually()
    {
        if (!ValidateSeekActual()) return;
        SeekActuallyCore(_fakePosition);
        _realPosition = _fakePosition;
    }

    protected async Task SeekActuallyAsync(CancellationToken cancellationToken = default)
    {
       if (!ValidateSeekActual()) return;
       await SeekActuallyAsyncCore(_fakePosition, cancellationToken).ConfigureAwait(false);
       _realPosition = _fakePosition;
    }

    protected abstract void SeekActuallyCore(long fakePosition);

    protected virtual Task SeekActuallyAsyncCore(long fakePosition, CancellationToken cancellationToken = default)
    {
        try
        {
            SeekActuallyCore(fakePosition);
        }
        catch (Exception e)
        {
            Task.FromException(e);
        }
        return Task.CompletedTask;
    }

    private bool ValidateSeekActual()
    {
        return _fakePosition != _realPosition && _fakePosition != Length;
    }

    protected long ValidateSeekArgumentsAndGetNewPosition(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                if (offset < 0) throw new EndOfStreamException();
                return offset;
            case SeekOrigin.Current:
                if (offset < 0 && offset * -1 > Position ||
                    offset > 0 && offset > Length - Position
                   ) throw new EndOfStreamException();
                return Position + offset;
            case SeekOrigin.End:
                if (offset > 0) throw new EndOfStreamException();
                return Length + offset;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
        }
    }

    protected static void ValidateStartBytesAndEndBytes(long? startBytes, long? endBytes, long? fileLength = null)
    {
        if (startBytes != null)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(startBytes.Value);
            if (fileLength.HasValue)
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(startBytes.Value, fileLength.Value);
        }
        if (endBytes != null)
            ArgumentOutOfRangeException.ThrowIfNegative(endBytes.Value);
        if (startBytes != null && endBytes != null)
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(startBytes.Value, endBytes.Value);
    }

    protected (long, long) ValidateAndGetStartBytesAndEndBytes(long? startBytes, long? endBytes)
    {
        ValidateStartBytesAndEndBytes(startBytes, endBytes, FileLength);
        long newStartBytes, newEndBytes;

        if (startBytes == null && endBytes == null)
        {
            newStartBytes = 0;
            newEndBytes = FileLength;
        }
        else if (startBytes != null && endBytes == null)
        {
            newStartBytes = startBytes.Value;
            newEndBytes = FileLength;
        }
        else if (startBytes == null && endBytes != null)
        {
            newStartBytes = FileLength - Math.Min(endBytes.Value, FileLength);
            newEndBytes = FileLength;
        }
        else
        {
            newStartBytes = startBytes!.Value;
            newEndBytes = Math.Min(endBytes!.Value, FileLength);
        }
        return (newStartBytes, newEndBytes);
    }

    protected void ThrowIfThisIsDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    protected void SetDisposed() => _disposed = true;

    #region 不支持的方法

    public override void SetLength(long value) => throw new NotSupportedException();

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback,
        object? state) => throw new NotSupportedException();

    public override void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public override void WriteByte(byte value) => throw new NotSupportedException();

    #endregion
}