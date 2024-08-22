using System.Threading.RateLimiting;

namespace Starward.Core.ZipStreamDownload.Http;

/// <summary>
/// 按字节下载限速的限速器的选项
/// </summary>
public struct RateLimiterOption
{
    /// <summary>
    /// 一个<see cref="RateLimiter"/>的实例，表示按按字节下载限速的限速器。
    /// </summary>
    public required RateLimiter RateLimiter { get; init; }

    /// <summary>
    /// 限速器单次最大可获取的许可数。
    /// </summary>
    public required int TokenLimit { get; init; }
}

internal class RateLimitReadStream(Stream innerStream, Func<RateLimiterOption> rateLimiterOptionBuilder) : Stream
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

    public override void Flush() => innerStream.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken)
        => innerStream.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count)
    {
        count = WaitAcquired(count);
        return innerStream.Read(buffer, offset, count);
    }

    public override int Read(Span<byte> buffer)
    {
        var count = WaitAcquired(buffer.Length);
        return innerStream.Read(buffer[..count]);
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        count = await WaitAcquiredAsync(count, cancellationToken).ConfigureAwait(false);
        return await innerStream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var count = await WaitAcquiredAsync(buffer.Length, cancellationToken).ConfigureAwait(false);
        return await innerStream.ReadAsync(buffer[..count], cancellationToken).ConfigureAwait(false);
    }

    public override int ReadByte()
    {
        WaitAcquired();
        return innerStream.ReadByte();
    }

    public override long Seek(long offset, SeekOrigin origin) => innerStream.Seek(offset, origin);

    private int WaitAcquired(int permitCount = 1) => WaitAcquiredAsync(permitCount).GetAwaiter().GetResult();

    private async Task<int> WaitAcquiredAsync(int permitCount = 1, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            try
            {
                var rateLimiterOption = rateLimiterOptionBuilder();

                var permitCountLimited = Math.Min(permitCount, rateLimiterOption.TokenLimit);

                var lease = await rateLimiterOption.RateLimiter.AcquireAsync(permitCountLimited, cancellationToken)
                    .ConfigureAwait(false);
                if (lease.IsAcquired) return permitCountLimited;
                if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    await Task.Delay(retryAfter, cancellationToken).ConfigureAwait(false);
                else await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
            catch (ArgumentOutOfRangeException)
            {
                break;
            }
        }
        return permitCount;
    }

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