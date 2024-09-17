namespace Starward.Core.ZipStreamDownload.Http;

internal class ProgressReportReadStream(Stream innerStream, IProgress<long> progress) : Stream
{
    public override bool CanRead => innerStream.CanRead;

    public override bool CanSeek => innerStream.CanSeek;

    public override bool CanWrite => false;

    public override long Length => innerStream.Length;

    public override long Position
    {
        get => innerStream.Position;
        set
        {
            if (value > Length) throw new EndOfStreamException();
            innerStream.Position = value;
        }
    }

    private long _position;

    public override void Flush() => innerStream.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken)
        => innerStream.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count)
    {
        count = innerStream.Read(buffer, offset, count);
        var position = Interlocked.Add(ref _position, count);
        progress.Report(position);
        return count;
    }

    public override int Read(Span<byte> buffer)
    {
        var count = innerStream.Read(buffer);
        var position = Interlocked.Add(ref _position, count);
        progress.Report(position);
        return count;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        count = await innerStream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        var position = Interlocked.Add(ref _position, count);
        progress.Report(position);
        return count;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var count = await innerStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        var position = Interlocked.Add(ref _position, count);
        progress.Report(position);
        return count;
    }

    public override int ReadByte()
    {
        var result = innerStream.ReadByte();
        if (result >= 0)
        {
            var position = Interlocked.Add(ref _position, 1);
            progress.Report(position);
        }
        return result;
    }

    public override long Seek(long offset, SeekOrigin origin) => innerStream.Seek(offset, origin);

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