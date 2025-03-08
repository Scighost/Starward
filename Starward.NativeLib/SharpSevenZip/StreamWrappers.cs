using SharpSevenZip.EventArguments;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SharpSevenZip;

/// <summary>
/// A class that has DisposeStream property.
/// </summary>
internal class DisposeVariableWrapper
{
    public bool DisposeStream { protected get; set; }

    protected DisposeVariableWrapper(bool disposeStream)
    {
        DisposeStream = disposeStream;
    }
}

/// <summary>
/// Stream wrapper used in InStreamWrapper
/// </summary>
internal class StreamWrapper : DisposeVariableWrapper, IDisposable
{
    /// <summary>
    /// File name associated with the stream (for date fix)
    /// </summary>
    private readonly string? _fileName;

    private readonly DateTime _fileTime;

    /// <summary>
    /// Worker stream for reading, writing and seeking.
    /// </summary>
    private Stream? _baseStream;

    /// <summary>
    /// Initializes a new instance of the StreamWrapper class
    /// </summary>
    /// <param name="baseStream">Worker stream for reading, writing and seeking</param>
    /// <param name="fileName">File name associated with the stream (for attributes fix)</param>
    /// <param name="time">File last write time (for attributes fix)</param>
    /// <param name="disposeStream">Indicates whether to dispose the baseStream</param>
    protected StreamWrapper(Stream baseStream, string fileName, DateTime time, bool disposeStream)
        : base(disposeStream)
    {
        _baseStream = baseStream;
        _fileName = fileName;
        _fileTime = time;
    }

    /// <summary>
    /// Initializes a new instance of the StreamWrapper class
    /// </summary>
    /// <param name="baseStream">Worker stream for reading, writing and seeking</param>
    /// <param name="disposeStream">Indicates whether to dispose the baseStream</param>
    protected StreamWrapper(Stream baseStream, bool disposeStream)
        : base(disposeStream)
    {
        _baseStream = baseStream;
    }

    /// <summary>
    /// Gets the worker stream for reading, writing and seeking.
    /// </summary>
    protected Stream? BaseStream => _baseStream;

    #region IDisposable Members

    /// <summary>
    /// Cleans up any resources used and fixes file attributes.
    /// </summary>
    public void Dispose()
    {
        if (_baseStream != null && DisposeStream)
        {
            try
            {
                _baseStream.Dispose();
            }
            catch (ObjectDisposedException) { }
            _baseStream = null;
        }

        if (!string.IsNullOrEmpty(_fileName) && File.Exists(_fileName))
        {
            try
            {
                File.SetLastWriteTime(_fileName, _fileTime);
                File.SetLastAccessTime(_fileName, _fileTime);
                File.SetCreationTime(_fileName, _fileTime);
            }
            catch (ArgumentOutOfRangeException) { }
        }

        //GC.SuppressFinalize(this);
    }

    #endregion

    public virtual void Seek(long offset, SeekOrigin seekOrigin, IntPtr newPosition)
    {
        if (BaseStream != null)
        {
            long position = BaseStream.Seek(offset, seekOrigin);
            if (newPosition != IntPtr.Zero)
            {
                Marshal.WriteInt64(newPosition, position);
            }
        }
    }
}

/// <summary>
/// IInStream wrapper used in stream read operations.
/// </summary>
internal sealed class InStreamWrapper : StreamWrapper, ISequentialInStream, IInStream
{
    /// <summary>
    /// Initializes a new instance of the InStreamWrapper class.
    /// </summary>
    /// <param name="baseStream">Stream for writing data</param>
    /// <param name="disposeStream">Indicates whether to dispose the baseStream</param>
    public InStreamWrapper(Stream baseStream, bool disposeStream)
        : base(baseStream, disposeStream) { }

    #region ISequentialInStream Members

    /// <summary>
    /// Reads data from the stream.
    /// </summary>
    /// <param name="data">A data array.</param>
    /// <param name="size">The array size.</param>
    /// <returns>The read bytes count.</returns>
    public unsafe int Read(IntPtr data, uint size)
    {
        if (size == 0 || BaseStream is null)
        {
            return 0;
        }

        int readCount = 0;

#if NET6_0_OR_GREATER
        Span<byte> buffer = new(data.ToPointer(), (int)size);

        readCount = BaseStream.Read(buffer);
#else
        byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent((int)size);

        try
        {
            readCount = BaseStream.Read(buffer, 0, (int)size);
            Marshal.Copy(buffer, 0, data, (int)size);
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }
#endif

        if (readCount > 0)
        {
            OnBytesRead(readCount);
        }

        return readCount;
    }

    #endregion

    /// <summary>
    /// Occurs when IntEventArgs.Value bytes were read from the source.
    /// </summary>
    public event EventHandler<IntEventArgs>? BytesRead;

    private void OnBytesRead(int e)
    {
        BytesRead?.Invoke(this, new IntEventArgs(e));
    }
}

/// <summary>
/// IOutStream wrapper used in stream write operations.
/// </summary>
internal sealed class OutStreamWrapper : StreamWrapper, ISequentialOutStream, IOutStream
{
    /// <summary>
    /// Initializes a new instance of the OutStreamWrapper class
    /// </summary>
    /// <param name="baseStream">Stream for writing data</param>
    /// <param name="fileName">File name (for attributes fix)</param>
    /// <param name="time">Time of the file creation (for attributes fix)</param>
    /// <param name="disposeStream">Indicates whether to dispose the baseStream</param>
    public OutStreamWrapper(Stream baseStream, string fileName, DateTime time, bool disposeStream)
        : base(baseStream, fileName, time, disposeStream)
    { }

    /// <summary>
    /// Initializes a new instance of the OutStreamWrapper class
    /// </summary>
    /// <param name="baseStream">Stream for writing data</param>
    /// <param name="disposeStream">Indicates whether to dispose the baseStream</param>
    public OutStreamWrapper(Stream baseStream, bool disposeStream)
        : base(baseStream, disposeStream)
    { }

    #region IOutStream Members

    public int SetSize(long newSize)
    {
        BaseStream!.SetLength(newSize);
        return 0;
    }

    #endregion

    #region ISequentialOutStream Members

    /// <summary>
    /// Writes data to the stream
    /// </summary>
    /// <param name="data">Data array</param>
    /// <param name="size">Array size</param>
    /// <param name="processedSize">Count of written bytes</param>
    /// <returns>Zero if Ok</returns>
    public unsafe int Write(IntPtr data, uint size, IntPtr processedSize)
    {
#if NET6_0_OR_GREATER
        Span<byte> buffer = new(data.ToPointer(), (int)size);
        BaseStream!.Write(buffer);
#else
        using var stream = new UnmanagedMemoryStream((byte*)data.ToPointer(), size);
        stream.CopyTo(BaseStream!);
#endif

        if (processedSize != IntPtr.Zero)
        {
            Marshal.WriteInt32(processedSize, (int)size);
        }

        OnBytesWritten((int)size);
        return 0;
    }

    #endregion

    /// <summary>
    /// Occurs when IntEventArgs.Value bytes were written.
    /// </summary>
    public event EventHandler<IntEventArgs>? BytesWritten;

    private void OnBytesWritten(int e)
    {
        BytesWritten?.Invoke(this, new IntEventArgs(e));
    }
}

/// <summary>
/// Base multi volume stream wrapper class.
/// </summary>
internal class MultiStreamWrapper : DisposeVariableWrapper, IDisposable
{
    protected readonly Dictionary<int, KeyValuePair<long, long>> StreamOffsets = new();

    protected readonly List<Stream> Streams = new();
    protected int CurrentStream;
    protected long Position;
    protected long StreamLength;

    /// <summary>
    /// Initializes a new instance of the MultiStreamWrapper class.
    /// </summary>
    /// <param name="dispose">Perform Dispose() if requested to.</param>
    protected MultiStreamWrapper(bool dispose)
        : base(dispose) { }

    /// <summary>
    /// Gets the total length of input data.
    /// </summary>
    public long Length => StreamLength;

    #region IDisposable Members

    /// <summary>
    /// Cleans up any resources used and fixes file attributes.
    /// </summary>
    public virtual void Dispose()
    {
        if (DisposeStream)
        {
            foreach (Stream stream in Streams)
            {
                try
                {
                    stream.Dispose();
                }
                catch (ObjectDisposedException) { }
            }
            Streams.Clear();
        }
        //GC.SuppressFinalize(this);
    }

    #endregion

    protected static string VolumeNumber(int num)
    {
        string prefix;
        if (num < 10)
        {
            prefix = ".00";
        }
        else if (num < 100)
        {
            prefix = ".0";
        }
        else
        {
            prefix = ".";
        }
        return prefix + num.ToString(CultureInfo.InvariantCulture);
    }

    private int StreamNumberByOffset(long offset)
    {
        foreach (int number in StreamOffsets.Keys)
        {
            if (StreamOffsets[number].Key <= offset &&
                StreamOffsets[number].Value >= offset)
            {
                return number;
            }
        }
        return -1;
    }

    public void Seek(long offset, SeekOrigin seekOrigin, IntPtr newPosition)
    {
        var absolutePosition = seekOrigin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(seekOrigin)),
        };
        CurrentStream = StreamNumberByOffset(absolutePosition);
        long delta = Streams[CurrentStream].Seek(
            absolutePosition - StreamOffsets[CurrentStream].Key, SeekOrigin.Begin);
        Position = StreamOffsets[CurrentStream].Key + delta;
        if (newPosition != IntPtr.Zero)
        {
            Marshal.WriteInt64(newPosition, Position);
        }
    }
}

/// <summary>
/// IInStream wrapper used in stream multi volume read operations.
/// </summary>
internal sealed class InMultiStreamWrapper : MultiStreamWrapper, ISequentialInStream, IInStream
{
    /// <summary>
    /// Initializes a new instance of the InMultiStreamWrapper class.
    /// </summary>
    /// <param name="fileName">The archive file name.</param>
    /// <param name="dispose">Perform Dispose() if requested to.</param>
    public InMultiStreamWrapper(string fileName, bool dispose)
        : base(dispose)
    {
        string baseName = fileName[..^4];
        int i = 0;
        while (File.Exists(fileName))
        {
            Streams.Add(new FileStream(fileName, FileMode.Open, FileAccess.Read));
            long length = Streams[i].Length;
            StreamOffsets.Add(i++, new KeyValuePair<long, long>(StreamLength, StreamLength + length));
            StreamLength += length;
            fileName = baseName + VolumeNumber(i + 1);
        }
    }

    #region ISequentialInStream Members

    /// <summary>
    /// Reads data from the stream.
    /// </summary>
    /// <param name="data">A data array.</param>
    /// <param name="size">The array size.</param>
    /// <returns>The read bytes count.</returns>
    public unsafe int Read(IntPtr data, uint size)
    {
        if (size == 0)
        {
            return 0;
        }

#if !NET6_0_OR_GREATER
        byte[] buffer;
#endif

        var readSize = (int)size;
        int readCount = 0;

#if NET6_0_OR_GREATER
        Span<byte> buffer0 = new((data + readCount).ToPointer(), readSize);
        int count0 = Streams[CurrentStream].Read(buffer0);
        readCount += count0;
        readSize -= count0;
        Position += count0;
#else
        buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(readSize);

        try
        {
            int count = Streams[CurrentStream].Read(buffer, 0, readSize);
            Marshal.Copy(buffer, 0, data + readCount, readSize);
            readCount += count;
            readSize -= count;
            Position += count;
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }
#endif

        while (readCount < (int)size)
        {
            if (CurrentStream == Streams.Count - 1)
            {
                return readCount;
            }

            CurrentStream++;
            Streams[CurrentStream].Seek(0, SeekOrigin.Begin);

#if NET6_0_OR_GREATER
            Span<byte> buffer1 = new((data + readCount).ToPointer(), readSize);
            int count1 = Streams[CurrentStream].Read(buffer1);
            readCount += count1;
            readSize -= count1;
            Position += count1;
#else
            buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(readSize);

            try
            {
                int count = Streams[CurrentStream].Read(buffer, 0, readSize);
                Marshal.Copy(buffer, 0, data + readCount, readSize);
                readCount += count;
                readSize -= count;
                Position += count;
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
#endif
        }

        return readCount;
    }

    #endregion
}

/// <summary>
/// IOutStream wrapper used in multi volume stream write operations.
/// </summary>
internal sealed class OutMultiStreamWrapper : MultiStreamWrapper, ISequentialOutStream, IOutStream
{
    private readonly string _archiveName;
    private readonly long _volumeSize;
    private long _overallLength;

    /// <summary>
    /// Initializes a new instance of the OutMultiStreamWrapper class.
    /// </summary>
    /// <param name="archiveName">The archive name.</param>
    /// <param name="volumeSize">The volume size.</param>
    public OutMultiStreamWrapper(string archiveName, long volumeSize)
        : base(true)
    {
        _archiveName = archiveName;
        _volumeSize = volumeSize;
        CurrentStream = -1;
        NewVolumeStream();
    }

    #region IOutStream Members

    public int SetSize(long newSize)
    {
        return 0;
    }

    #endregion

    #region ISequentialOutStream Members

    public unsafe int Write(IntPtr data, uint size, IntPtr processedSize)
    {
        int offset = 0;
        var originalSize = (int)size;
        Position += size;
        _overallLength = Math.Max(Position + 1, _overallLength);
        while (size > _volumeSize - Streams[CurrentStream].Position)
        {
            var count = (int)(_volumeSize - Streams[CurrentStream].Position);
#if NET6_0_OR_GREATER
            Span<byte> buffer0 = new((data + offset).ToPointer(), count);
            Streams[CurrentStream].Write(buffer0);
#else
            using var stream0 = new UnmanagedMemoryStream((byte*)(data + offset).ToPointer(), count);
            stream0.CopyTo(Streams[CurrentStream]);
#endif


            size -= (uint)count;
            offset += count;
            NewVolumeStream();
        }

#if NET6_0_OR_GREATER
        Span<byte> buffer1 = new((data + offset).ToPointer(), (int)size);
        Streams[CurrentStream].Write(buffer1);
#else
        using var stream1 = new UnmanagedMemoryStream((byte*)(data + offset).ToPointer(), (int)size);
        stream1.CopyTo(Streams[CurrentStream]);
#endif

        if (processedSize != IntPtr.Zero)
        {
            Marshal.WriteInt32(processedSize, originalSize);
        }

        return 0;
    }

    #endregion

    public override void Dispose()
    {
        int lastIndex = Streams.Count - 1;
        Streams[lastIndex].SetLength(lastIndex > 0 ? Streams[lastIndex].Position : _overallLength);
        base.Dispose();
    }

    private void NewVolumeStream()
    {
        CurrentStream++;
        Streams.Add(File.Create(_archiveName + VolumeNumber(CurrentStream + 1)));
        Streams[CurrentStream].SetLength(_volumeSize);
        StreamOffsets.Add(CurrentStream, new KeyValuePair<long, long>(0, _volumeSize - 1));
    }
}

internal sealed class FakeOutStreamWrapper : ISequentialOutStream, IDisposable
{
    #region IDisposable Members

    public void Dispose()
    {
        //GC.SuppressFinalize(this);
    }

    #endregion

    #region ISequentialOutStream Members

    /// <summary>
    /// Does nothing except calling the BytesWritten event
    /// </summary>
    /// <param name="data">Data array</param>
    /// <param name="size">Array size</param>
    /// <param name="processedSize">Count of written bytes</param>
    /// <returns>Zero if Ok</returns>
    public unsafe int Write(IntPtr data, uint size, IntPtr processedSize)
    {
        OnBytesWritten((int)size);

        if (processedSize != IntPtr.Zero)
        {
            Marshal.WriteInt32(processedSize, (int)size);
        }

        return 0;
    }

    #endregion

    /// <summary>
    /// Occurs when IntEventArgs.Value bytes were written
    /// </summary>
    public event EventHandler<IntEventArgs>? BytesWritten;

    private void OnBytesWritten(int e)
    {
        BytesWritten?.Invoke(this, new IntEventArgs(e));
    }
}
