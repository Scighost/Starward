using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Starward.Services.InstallGame;

internal class FileSliceStream : Stream
{

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    private readonly long _length;
    public override long Length => _length;

    public override long Position
    {
        get
        {
            if (disposedValue)
            {
                throw new ObjectDisposedException("Cannot access a closed file.");
            }
            long pos = 0;
            for (int i = 0; i < _streamIndex; i++)
            {
                pos += _streamLengths[i];
            }
            return pos + _currentStream.Position;
        }
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Position));
            }
            Seek(value, SeekOrigin.Begin);
        }
    }



    private readonly IList<FileStream> _fileStreams;

    private readonly long[] _streamLengths;

    private FileStream _currentStream;

    private int _streamIndex;


    public FileSliceStream(IEnumerable<string> files)
    {
        _fileStreams = files.Select(File.OpenRead).ToList();
        _streamLengths = _fileStreams.Select(x => x.Length).ToArray();
        _length = _fileStreams.Sum(x => x.Length);
        _currentStream = _fileStreams.First();
    }



    public override void Flush()
    {
        _currentStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int length = _currentStream.Read(buffer, offset, count);
        if (length == 0)
        {
            if (_streamIndex < _fileStreams.Count - 1)
            {
                _streamIndex++;
                _currentStream = _fileStreams[_streamIndex];
                _currentStream.Position = 0;
                length = _currentStream.Read(buffer, offset, count);
            }
        }
        return length;
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
        long position = offset = Math.Clamp(offset, 0, Length);
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
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }



    private bool disposedValue;

    protected override void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {

            }

            foreach (var fs in _fileStreams)
            {
                fs.Dispose();
            }

            disposedValue = true;
        }
    }

    ~FileSliceStream()
    {
        Dispose(disposing: false);
    }


    public override async ValueTask DisposeAsync()
    {
        foreach (var fs in _fileStreams)
        {
            await fs.DisposeAsync();
        }
    }



}


