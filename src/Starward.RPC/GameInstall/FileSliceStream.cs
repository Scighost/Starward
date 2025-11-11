using System;
using System.IO;
using System.Threading.Tasks;

namespace Starward.RPC.GameInstall;

/// <summary>
/// 文件切片流
/// </summary>
internal class FileSliceStream : Stream
{

    private readonly FileStream _sourceStream;

    private readonly long _startPosition;


    private readonly long _length;
    public override long Length => _length;


    private long _currentPosition;
    public override long Position
    {
        get => _currentPosition;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override bool CanRead => _sourceStream.CanRead;

    public override bool CanSeek => _sourceStream.CanSeek;

    public override bool CanWrite => false;


    public FileSliceStream(string filePath, long startPosition, long length)
    {
        _sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        _startPosition = startPosition;
        _length = length;
        _currentPosition = 0;

        // 将文件流移动到起始位置
        _sourceStream.Seek(startPosition, SeekOrigin.Begin);
    }


    public override void Flush() => _sourceStream.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        // 确保不会超过剩余数据的长度
        int bytesToRead = (int)Math.Min(count, _length - _currentPosition);
        if (bytesToRead == 0)
        {
            return 0; // 结束读取
        }

        int bytesRead = _sourceStream.Read(buffer, offset, bytesToRead);
        _currentPosition += bytesRead;
        return bytesRead;
    }


    public override long Seek(long offset, SeekOrigin origin)
    {
        if (origin is SeekOrigin.Current)
        {
            offset += Position;
        }
        if (origin is SeekOrigin.End)
        {
            offset += Length;
        }
        offset = Math.Clamp(offset, 0, _length);
        long position = _startPosition + offset;
        position = _sourceStream.Seek(position, SeekOrigin.Begin);
        _currentPosition = position - _startPosition;
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




    private bool disposedValue;

    protected override void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {

            }
            _sourceStream.Dispose();
            disposedValue = true;
        }
    }

    ~FileSliceStream()
    {
        Dispose(disposing: false);
    }


    public override async ValueTask DisposeAsync()
    {
        await _sourceStream.DisposeAsync();
    }


}
