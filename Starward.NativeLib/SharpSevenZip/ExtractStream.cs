using System.Buffers;

namespace SharpSevenZip;

internal class ExtractStream : Stream
{
    private int _position = 0;
    private long _length = 0;
    private IMemoryOwner<byte>? _currentData = null;
    private int _currentDataLength = 0;
    private readonly Queue<IMemoryOwner<byte>> _data = new();
    private readonly Queue<int> _dataLength = new();
    private long _totalRequestedSize = 0;
    private long _totalReadSize = 0;
    private long _totalWriteSize = 0;
    private long _unpackedSize = -1;
    private bool _isOpen = true;
    private readonly object _lock = new();

    public ExtractStream()
    {
    }

    public override bool CanRead => _isOpen;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => _currentData is null ? _length : (_length + _currentDataLength - _position);

    public override long Position { get => 0; set { } }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        lock (_lock)
        {
            if (!_isOpen)
            {
                return 0;
            }

            _totalRequestedSize += count;
        }

        while (true)
        {
            lock (_lock)
            {
                if (!_isOpen)
                {
                    return 0;
                }

                long maxTotalRequestedSize = Math.Min(_totalRequestedSize, _unpackedSize);

                if (_unpackedSize != -1 && maxTotalRequestedSize <= _totalWriteSize)
                {
                    break;
                }
            }

            //Thread.Sleep(1);
        }

        count = (int)Math.Min(count, _unpackedSize - _totalReadSize);

        int readCount = 0;

        while (readCount < count)
        {
            if (_currentData is null)
            {
                if (_data.Count == 0)
                {
                    break;
                }

                lock (_lock)
                {
                    _currentData = _data.Dequeue();
                    _currentDataLength = _dataLength.Dequeue();
                    _length -= _currentDataLength;
                    _position = 0;
                }
            }

            int currentCount = Math.Min(_currentDataLength - _position, count - readCount);
            _currentData.Memory.Span.Slice(_position, currentCount).CopyTo(buffer.AsSpan()[(offset + readCount)..]);
            readCount += currentCount;
            _position += currentCount;

            if (_position >= _currentDataLength)
            {
                lock(_lock)
                {
                    _currentData.Dispose();
                    _currentData = null;
                    _currentDataLength = 0;
                    _position = 0;
                }
            }
        }

        _totalReadSize += readCount;

        return readCount;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        lock (_lock)
        {
            _unpackedSize = value;
        }
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!_isOpen)
        {
            return;
        }

        while (true)
        {
            if (!_isOpen)
            {
                return;
            }

            long maxTotalRequestedSize = Math.Min(_totalRequestedSize, _unpackedSize);

            if (_unpackedSize != -1 && maxTotalRequestedSize > _totalWriteSize)
            {
                break;
            }

            //Thread.Sleep(1);
        }

        IMemoryOwner<byte> data = MemoryPool<byte>.Shared.Rent(count);
        buffer.AsSpan().Slice(offset, count).CopyTo(data.Memory.Span);

        lock (_lock)
        {
            _data.Enqueue(data);
            _dataLength.Enqueue(count);
            _length += count;
            _totalWriteSize += count;
        }
    }

    protected override void Dispose(bool disposing)
    {
        lock (_lock)
        {
            _isOpen = false;
            _position = 0;
            _length = 0;
            _currentData?.Dispose();
            _currentData = null;
            _currentDataLength = 0;

            while (_data.Count != 0)
            {
                IMemoryOwner<byte> data = _data.Dequeue();
                data.Dispose();
            }

            _dataLength.Clear();
            _totalRequestedSize = 0;
            _totalReadSize = 0;
            _totalWriteSize = 0;
            _unpackedSize = -1;
        }

        Thread.Sleep(1);

        base.Dispose(disposing);
    }
}
