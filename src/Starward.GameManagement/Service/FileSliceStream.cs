namespace Starward.GameManagement.Service;

/// <summary>
/// 文件切片流
/// </summary>
internal class FileSliceStream : Stream
{
    private readonly FileStream _sourceStream;

    private readonly long _startPosition;

    private readonly long _length;
    public override long Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            return _length;
        }
    }


    private long _currentPosition;
    public override long Position
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            return _currentPosition;
        }
        set => Seek(value, SeekOrigin.Begin);
    }

    public override bool CanRead => !_disposedValue && _sourceStream.CanRead;

    public override bool CanSeek => !_disposedValue && _sourceStream.CanSeek;

    public override bool CanWrite => false;


    public FileSliceStream(string filePath, long startPosition, long length)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentOutOfRangeException.ThrowIfNegative(startPosition);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        _sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        try
        {
            if (startPosition + length > _sourceStream.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "The slice extends beyond the end of the file.");
            }
            _startPosition = startPosition;
            _length = length;
            _currentPosition = 0;
            _sourceStream.Seek(startPosition, SeekOrigin.Begin);
        }
        catch
        {
            _sourceStream.Dispose();
            throw;
        }
    }


    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        return Task.CompletedTask;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (buffer.Length - offset < count)
        {
            throw new ArgumentException("Offset and count exceed the buffer bounds.");
        }
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        long remaining = _length - _currentPosition;
        if (remaining <= 0 || buffer.IsEmpty)
        {
            return 0;
        }
        int bytesToRead = (int)Math.Min(buffer.Length, remaining);
        int bytesRead = _sourceStream.Read(buffer.Slice(0, bytesToRead));
        _currentPosition += bytesRead;
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        long remaining = _length - _currentPosition;
        if (remaining <= 0 || buffer.IsEmpty)
        {
            return 0;
        }
        int bytesToRead = (int)Math.Min(buffer.Length, remaining);
        int bytesRead = await _sourceStream.ReadAsync(buffer.Slice(0, bytesToRead), cancellationToken).ConfigureAwait(false);
        _currentPosition += bytesRead;
        return bytesRead;
    }


    public override long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        if (origin is SeekOrigin.Current)
        {
            offset += _currentPosition;
        }
        if (origin is SeekOrigin.End)
        {
            offset += _length;
        }
        offset = Math.Clamp(offset, 0, _length);
        long position = _startPosition + offset;
        position = _sourceStream.Seek(position, SeekOrigin.Begin);
        _currentPosition = position - _startPosition;
        if (_currentPosition < 0)
        {
            _currentPosition = 0;
        }
        else if (_currentPosition > _length)
        {
            _currentPosition = _length;
        }
        return _currentPosition;
    }


    public override void SetLength(long value)
    {
        throw new NotSupportedException("FileSliceStream does not support SetLength.");
    }


    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("FileSliceStream does not support Write.");
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        throw new NotSupportedException("FileSliceStream does not support Write.");
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("FileSliceStream does not support Write.");
    }


    private bool _disposedValue;

    protected override void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _sourceStream.Dispose();
            }
            _disposedValue = true;
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_disposedValue)
        {
            await _sourceStream.DisposeAsync().ConfigureAwait(false);
            _disposedValue = true;
        }
        GC.SuppressFinalize(this);
    }

}
