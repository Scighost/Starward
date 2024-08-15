using System.Buffers;

namespace Starward.Core.ZipStreamDownload.Extensions;

/// <summary>
/// <see cref="Stream"/>反向复制和进度报告的扩展
/// </summary>
internal static class StreamCopyToExtensions
{
    /// <summary>Validates arguments provided to the <see cref="CopyToReverse(Stream, Stream, int)"/> or <see cref="CopyToReverseAsync(Stream, Stream, int, CancellationToken)"/> methods.</summary>
    /// <param name="destination">The <see cref="Stream"/> "destination" argument passed to the copy method.</param>
    /// <param name="bufferSize">The integer "bufferSize" argument passed to the copy method.</param>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> was null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferSize"/> was not a positive value.</exception>
    /// <exception cref="NotSupportedException"><paramref name="destination"/> does not support writing.</exception>
    /// <exception cref="ObjectDisposedException"><paramref name="destination"/> does not support writing or reading.</exception>
    private static void ValidateCopyToReverseArguments(Stream destination, int bufferSize)
    {
        ArgumentNullException.ThrowIfNull(destination);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

        if (destination is { CanWrite: true, CanSeek: true }) return;
        if (destination.CanRead) throw new NotSupportedException();

        throw new ObjectDisposedException(destination.GetType().Name);
    }

    /// <summary>Validates arguments provided to the <see cref="CopyToReverse(Stream, Stream, int)"/> or <see cref="CopyToReverseAsync(Stream, Stream, int, CancellationToken)"/> methods.</summary>
    /// <param name="destination">The <see cref="Stream"/> "destination" argument passed to the copy method.</param>
    /// <param name="bufferSize">The integer "bufferSize" argument passed to the copy method.</param>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> was null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferSize"/> was not a positive value.</exception>
    /// <exception cref="NotSupportedException"><paramref name="destination"/> does not support writing.</exception>
    /// <exception cref="ObjectDisposedException"><paramref name="destination"/> does not support writing or reading.</exception>
    private static void ValidateCopyToArguments(Stream destination, int bufferSize)
    {
        ArgumentNullException.ThrowIfNull(destination);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

        if (destination.CanWrite) return;
        if (destination.CanRead) throw new NotSupportedException();

        throw new ObjectDisposedException(destination.GetType().Name);
    }

    private static int GetCopyBufferSize(Stream stream)
    {
        // This value was originally picked to be the largest multiple of 4096 that is still smaller than the large object heap threshold (85K).
        // The CopyTo{Async} buffer is short-lived and is likely to be collected at Gen0, and it offers a significant improvement in Copy
        // performance.  Since then, the base implementations of CopyTo{Async} have been updated to use ArrayPool, which will end up rounding
        // this size up to the next power of two (131,072), which will by default be on the large object heap.  However, most of the time
        // the buffer should be pooled, the LOH threshold is now configurable and thus may be different than 85K, and there are measurable
        // benefits to using the larger buffer size.  So, for now, this value remains.
        const int DefaultCopyBufferSize = 81920;

        var bufferSize = DefaultCopyBufferSize;

        if (!stream.CanSeek) return bufferSize;
        var length = stream.Length;
        var position = stream.Position;
        if (length <= position) // Handles negative overflows
        {
            // There are no bytes left in the stream to copy.
            // However, because CopyTo{Async} is virtual, we need to
            // ensure that any override is still invoked to provide its
            // own validation, so we use the smallest legal buffer size here.
            bufferSize = 1;
        }
        else
        {
            var remaining = length - position;
            if (remaining > 0)
            {
                // In the case of a positive overflow, stick to the default size
                bufferSize = (int)Math.Min(bufferSize, remaining);
            }
        }

        return bufferSize;
    }

    /// <summary>
    /// Asynchronously reads the bytes from the current stream and writes them reverse to another stream. Both streams positions are advanced by the number of bytes copied.
    /// </summary>
    /// <param name="stream">Instance of <see cref="Stream"/>.</param>
    /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
    /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero. The default size is 81920.</param>
    /// <param name="progress">progress updates report.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentNullException">destination is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">buffersize is negative or zero.</exception>
    /// <exception cref="ObjectDisposedException">Either the current stream or the destination stream is disposed.</exception>
    /// <exception cref="NotSupportedException">The current stream does not support reading, or the destination stream does not support writing.</exception>
    public static Task CopyToReverseAsync(this Stream stream, Stream destination, int bufferSize,
        IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        ValidateCopyToReverseArguments(destination, bufferSize);
        if (!stream.CanRead)
        {
            if (stream.CanWrite) throw new NotSupportedException();

            throw new ObjectDisposedException(stream.GetType().Name);
        }

        return Core(stream, destination, bufferSize, progress, cancellationToken);

        static async Task Core(Stream source, Stream destination, int bufferSize, IProgress<long>? progress,
            CancellationToken cancellationToken)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                var count = 0L;
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)
                           .ConfigureAwait(false)) != 0)
                {
                    destination.Seek(-bytesRead, SeekOrigin.Current);
                    for (var index = bytesRead - 1; index >= 0; index--)
                        await destination.WriteAsync(buffer.AsMemory().Slice(index, 1),
                            cancellationToken).ConfigureAwait(false);
                    destination.Seek(-bytesRead, SeekOrigin.Current);
                    count += bytesRead;
                    progress?.Report(count);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    /// <summary>
    /// Asynchronously reads the bytes from the current stream and writes them reverse to another stream. Both streams positions are advanced by the number of bytes copied.
    /// </summary>
    /// <param name="stream">Instance of <see cref="Stream"/>.</param>
    /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
    /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero. The default size is 81920.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentNullException">destination is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">buffersize is negative or zero.</exception>
    /// <exception cref="ObjectDisposedException">Either the current stream or the destination stream is disposed.</exception>
    /// <exception cref="NotSupportedException">The current stream does not support reading, or the destination stream does not support writing.</exception>
    public static Task CopyToReverseAsync(this Stream stream, Stream destination,
        int bufferSize, CancellationToken cancellationToken) =>
        CopyToReverseAsync(stream, destination, bufferSize, null, cancellationToken);

    /// <summary>
    /// Asynchronously reads the bytes from the current stream and writes them reverse to another stream. Both streams positions are advanced by the number of bytes copied.
    /// </summary>
    /// <param name="stream">Instance of <see cref="Stream"/>.</param>
    /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
    /// <param name="progress">progress updates report.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentNullException">destination is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">buffersize is negative or zero.</exception>
    /// <exception cref="ObjectDisposedException">Either the current stream or the destination stream is disposed.</exception>
    /// <exception cref="NotSupportedException">The current stream does not support reading, or the destination stream does not support writing.</exception>
    public static Task CopyToReverseAsync(this Stream stream, Stream destination,
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default) =>
        CopyToReverseAsync(stream, destination, GetCopyBufferSize(stream), progress, cancellationToken);

    /// <summary>
    /// Asynchronously reads the bytes from the current stream and writes them reverse to another stream. Both streams positions are advanced by the number of bytes copied.
    /// </summary>
    /// <param name="stream">Instance of <see cref="Stream"/>.</param>
    /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentNullException">destination is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">buffersize is negative or zero.</exception>
    /// <exception cref="ObjectDisposedException">Either the current stream or the destination stream is disposed.</exception>
    /// <exception cref="NotSupportedException">The current stream does not support reading, or the destination stream does not support writing.</exception>
    public static Task CopyToReverseAsync(this Stream stream, Stream destination,
        CancellationToken cancellationToken) =>
        CopyToReverseAsync(stream, destination, null, cancellationToken);

    /// <summary>
    /// Asynchronously reads the bytes from the current stream and writes them reverse to another stream. Both streams positions are advanced by the number of bytes copied.
    /// </summary>
    /// <param name="stream">Instance of <see cref="Stream"/>.</param>
    /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
    /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero. The default size is 81920.</param>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentNullException">destination is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">buffersize is negative or zero.</exception>
    /// <exception cref="ObjectDisposedException">Either the current stream or the destination stream is disposed.</exception>
    /// <exception cref="NotSupportedException">The current stream does not support reading, or the destination stream does not support writing.</exception>
    public static void CopyToReverse(this Stream stream, Stream destination, int bufferSize)
    {
        ValidateCopyToReverseArguments(destination, bufferSize);
        if (!stream.CanRead)
        {
            if (stream.CanWrite) throw new NotSupportedException();

            throw new ObjectDisposedException(stream.GetType().Name);
        }

        Core(stream, destination, bufferSize);
        return;

        static void Core(Stream source, Stream destination, int bufferSize)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                int bytesRead;
                while ((bytesRead = source.Read(buffer)) != 0)
                {
                    destination.Seek(-bytesRead, SeekOrigin.Current);
                    for (var index = bytesRead - 1; index >= 0; index--)
                        destination.Write(buffer.AsSpan().Slice(index, 1));
                    destination.Seek(-bytesRead, SeekOrigin.Current);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    /// <summary>
    /// Asynchronously reads the bytes from the current stream and writes them reverse to another stream. Both streams positions are advanced by the number of bytes copied.
    /// </summary>
    /// <param name="stream">Instance of <see cref="Stream"/>.</param>
    /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentNullException">destination is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">buffersize is negative or zero.</exception>
    /// <exception cref="ObjectDisposedException">Either the current stream or the destination stream is disposed.</exception>
    /// <exception cref="NotSupportedException">The current stream does not support reading, or the destination stream does not support writing.</exception>
    public static void CopyToReverse(this Stream stream, Stream destination) =>
        CopyToReverse(stream, destination, GetCopyBufferSize(stream));

    /// <summary>
    /// Asynchronously reads the bytes from the current stream and writes them to another stream. Both streams positions are advanced by the number of bytes copied.
    /// </summary>
    /// <param name="stream">Instance of <see cref="Stream"/>.</param>
    /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
    /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero. The default size is 81920.</param>
    /// <param name="progress">progress updates report.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentNullException">destination is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">buffersize is negative or zero.</exception>
    /// <exception cref="ObjectDisposedException">Either the current stream or the destination stream is disposed.</exception>
    /// <exception cref="NotSupportedException">The current stream does not support reading, or the destination stream does not support writing.</exception>
    public static Task CopyToAsync(this Stream stream, Stream destination, int bufferSize,
        IProgress<long>? progress,
        CancellationToken cancellationToken = default)
    {
        ValidateCopyToArguments(destination, bufferSize);
        if (!stream.CanRead)
        {
            if (stream.CanWrite) throw new NotSupportedException();

            throw new ObjectDisposedException(stream.GetType().Name);
        }

        return Core(stream, destination, bufferSize, progress, cancellationToken);

        static async Task Core(Stream source, Stream destination, int bufferSize,
            IProgress<long>? progress,
            CancellationToken cancellationToken)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                var count = 0L;
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)
                           .ConfigureAwait(false)) != 0)
                {
                    await destination.WriteAsync(buffer.AsMemory()[..bytesRead], cancellationToken)
                        .ConfigureAwait(false);
                    count += bytesRead;
                    progress?.Report(count);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    /// <summary>
    /// Asynchronously reads the bytes from the current stream and writes them to another stream. Both streams positions are advanced by the number of bytes copied.
    /// </summary>
    /// <param name="stream">Instance of <see cref="Stream"/>.</param>
    /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
    /// <param name="progress">progress updates report.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="ArgumentNullException">destination is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">buffersize is negative or zero.</exception>
    /// <exception cref="ObjectDisposedException">Either the current stream or the destination stream is disposed.</exception>
    /// <exception cref="NotSupportedException">The current stream does not support reading, or the destination stream does not support writing.</exception>
    public static Task CopyToAsync(this Stream stream, Stream destination,
        IProgress<long>? progress,
        CancellationToken cancellationToken = default) =>
        CopyToAsync(stream, destination, GetCopyBufferSize(stream), progress, cancellationToken);
}