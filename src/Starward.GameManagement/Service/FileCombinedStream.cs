namespace Starward.GameManagement.Service;

/// <summary>
/// 几个文件合并成一个流
/// </summary>
internal class FileCombinedStream : Stream
{

    public override bool CanRead => !_disposedValue;

    public override bool CanSeek => !_disposedValue;

    public override bool CanWrite => false;

    private readonly long _length;
    public override long Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            return _length;
        }
    }

    public override long Position
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            long pos = 0;
            for (int i = 0; i < _streamIndex; i++)
            {
                pos += _streamLengths[i];
            }
            return pos + _currentStream.Position;
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(Position));
            Seek(value, SeekOrigin.Begin);
        }
    }



    private readonly List<FileStream> _fileStreams;

    private readonly long[] _streamLengths;

    private FileStream _currentStream;

    private int _streamIndex;


    public FileCombinedStream(IEnumerable<string> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        var fileList = files as IReadOnlyList<string> ?? files.ToList();
        if (fileList.Count == 0)
        {
            throw new ArgumentException("At least one file is required.", nameof(files));
        }

        _fileStreams = new List<FileStream>(fileList.Count);
        try
        {
            foreach (var path in fileList)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(path);
                _fileStreams.Add(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
            }
            _streamLengths = _fileStreams.Select(x => x.Length).ToArray();
            _length = _fileStreams.Sum(x => x.Length);
            _currentStream = _fileStreams[0];
        }
        catch
        {
            foreach (var fs in _fileStreams)
            {
                fs.Dispose();
            }
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
        int totalRead = 0;
        while (!buffer.IsEmpty)
        {
            if (_streamIndex >= _fileStreams.Count)
            {
                break;
            }
            int read = _currentStream.Read(buffer);
            if (read == 0)
            {
                if (_streamIndex >= _fileStreams.Count - 1)
                {
                    break;
                }
                _streamIndex++;
                _currentStream = _fileStreams[_streamIndex];
                _currentStream.Position = 0;
                continue;
            }
            totalRead += read;
            buffer = buffer.Slice(read);
        }
        return totalRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        int totalRead = 0;
        while (!buffer.IsEmpty)
        {
            if (_streamIndex >= _fileStreams.Count)
            {
                break;
            }
            int read = await _currentStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                if (_streamIndex >= _fileStreams.Count - 1)
                {
                    break;
                }
                _streamIndex++;
                _currentStream = _fileStreams[_streamIndex];
                _currentStream.Position = 0;
                continue;
            }
            totalRead += read;
            buffer = buffer.Slice(read);
        }
        return totalRead;
    }


    public override long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        if (origin is SeekOrigin.Current)
        {
            offset += Position;
        }
        if (origin is SeekOrigin.End)
        {
            offset += Length;
        }
        offset = Math.Clamp(offset, 0, _length);
        long position = offset;
        for (int i = 0; i < _streamLengths.Length; i++)
        {
            if (position < _streamLengths[i] || i == _streamLengths.Length - 1)
            {
                _streamIndex = i;
                _currentStream = _fileStreams[i];
                _currentStream.Position = position;
                break;
            }
            else
            {
                position -= _streamLengths[i];
            }
        }
        return offset;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("FileCombinedStream does not support SetLength.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("FileCombinedStream does not support Write.");
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        throw new NotSupportedException("FileCombinedStream does not support Write.");
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("FileCombinedStream does not support Write.");
    }



    private bool _disposedValue;

    protected override void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                foreach (var fs in _fileStreams)
                {
                    fs.Dispose();
                }
            }
            _disposedValue = true;
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_disposedValue)
        {
            foreach (var fs in _fileStreams)
            {
                await fs.DisposeAsync().ConfigureAwait(false);
            }
            _disposedValue = true;
        }
        GC.SuppressFinalize(this);
    }

}
