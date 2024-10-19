using System.Buffers;
using System.Runtime.CompilerServices;

namespace Starward.Core.ZipStreamDownload.Extensions;

/// <summary>
/// <see cref="Stream"/>按字节读取扩展
/// </summary>
internal static class StreamByteOrderExtensions
{
    /// <summary>
    /// 反转字节数组的字节顺序。
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="OperationCanceledException">令牌已被请求取消。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReverseBytes(Span<byte> bytes, CancellationToken cancellationToken = default)
    {
        for (var index = 0; index < bytes.Length / 2; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bytes[bytes.Length - 1 - index] = (byte)(bytes[bytes.Length - 1 - index] ^ bytes[index]);
            bytes[index] = (byte)(bytes[index] ^ bytes[bytes.Length - 1 - index]);
            bytes[bytes.Length - 1 - index] = (byte)(bytes[bytes.Length - 1 - index] ^ bytes[index]);
        }
    }

    /// <summary>
    /// 按小端序读取字节数据（异步）。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="count">要读取的字节的数量</param>
    /// <param name="reverse">是否进行反向读取</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务，可以获取读取的字节数据的数组。</returns>
    /// <exception cref="ArgumentOutOfRangeException">形参的值超出范围。</exception>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流不支持查找或读取。</exception>
    /// <exception cref="EndOfStreamException">在读取计数字节数之前到达流的末尾。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    /// <exception cref="OperationCanceledException">令牌已被请求取消。</exception>
    private static async ValueTask<byte[]> ReadLittleEndianBytesAsync(this Stream stream, int count,
        bool reverse = false, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        if ((reverse && !stream.CanSeek) || !stream.CanRead) throw new NotSupportedException();

        var buffer = ArrayPool<byte>.Shared.Rent(count);
        try
        {
            if (reverse)
            {
                stream.Seek(-1, SeekOrigin.Current);
                for (var index = count - 1; index >= 0; index--)
                {
                    await stream.ReadExactlyAsync(buffer, index, 1, cancellationToken).ConfigureAwait(false);
                    if (index > 0) stream.Seek(-2, SeekOrigin.Current);
                    else stream.Seek(-1, SeekOrigin.Current);
                }
            }
            else await stream.ReadExactlyAsync(buffer, 0, count, cancellationToken).ConfigureAwait(false);
            if (!BitConverter.IsLittleEndian && !reverse || BitConverter.IsLittleEndian && reverse)
                ReverseBytes(buffer, cancellationToken);
            return buffer;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// 按小端序读取字节数据。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="count">要读取的字节的数量</param>
    /// <param name="reverse">是否进行反向读取</param>
    /// <returns>读取的字节数据的数组。</returns>
    /// <exception cref="ArgumentOutOfRangeException">形参的值超出范围。</exception>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流不支持查找或读取。</exception>
    /// <exception cref="EndOfStreamException">在读取计数字节数之前到达流的末尾。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    public static byte[] ReadLittleEndianBytes(this Stream stream, int count, bool reverse = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        if ((reverse && !stream.CanSeek) || !stream.CanRead) throw new NotSupportedException();

        var buffer = new byte[count];
        if (reverse)
        {
            stream.Seek(-1, SeekOrigin.Current);
            for (var index = count - 1; index >= 0; index--)
            {
                stream.ReadExactly(buffer, index, 1);
                if (index > 0) stream.Seek(-2, SeekOrigin.Current);
                else stream.Seek(-1, SeekOrigin.Current);
            }
        }
        else stream.ReadExactly(buffer, 0, count);

        if (!BitConverter.IsLittleEndian && !reverse || BitConverter.IsLittleEndian && reverse)
            ReverseBytes(buffer);
        return buffer;
    }

    /// <summary>
    /// 按小端序写入字节数据（异步）。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="bytes">要写入的字节数据的数组</param>
    /// <param name="reverse">是否进行反向写入</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务。</returns>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流不支持查找或写入。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    /// <exception cref="OperationCanceledException">令牌已被请求取消。</exception>
    public static async Task WriteLittleEndianBytesAsync(this Stream stream,
        ReadOnlyMemory<byte> bytes, bool reverse = false, CancellationToken cancellationToken = default)
    {
        if ((reverse && !stream.CanSeek) || !stream.CanWrite) throw new NotSupportedException();

        ReadOnlyMemory<byte> bytesMemory;
        if (!BitConverter.IsLittleEndian && !reverse || BitConverter.IsLittleEndian && reverse)
        {
            var bytesArray = bytes.ToArray();
            ReverseBytes(bytesArray, cancellationToken);
            bytesMemory = bytesArray.AsMemory();
        }
        else bytesMemory = bytes;

        if (reverse)
        {
            stream.Seek(-1, SeekOrigin.Current);
            for (var index = bytesMemory.Length - 1; index >= 0; index--)
            {
                await stream.WriteAsync(bytesMemory.Slice(index, 1), cancellationToken).ConfigureAwait(false);
                stream.Seek(-2, SeekOrigin.Current);
            }
            stream.Seek(1, SeekOrigin.Current);
        }
        else await stream.WriteAsync(bytesMemory, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 按小端序写入字节数据。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="bytes">要写入的字节数据的数组</param>
    /// <param name="reverse">是否进行反向写入</param>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流不支持查找或写入。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    public static void WriteLittleEndianBytes(this Stream stream,
        ReadOnlySpan<byte> bytes, bool reverse = false)
    {
        if ((reverse && !stream.CanSeek) || !stream.CanWrite) throw new NotSupportedException();

        ReadOnlySpan<byte> bytesSpan;
        if (!BitConverter.IsLittleEndian && !reverse || BitConverter.IsLittleEndian && reverse)
        {
            var bytesArray = bytes.ToArray();
            ReverseBytes(bytesArray);
            bytesSpan = bytesArray;
        }
        else bytesSpan = bytes;

        if (reverse)
        {
            stream.Seek(-1, SeekOrigin.Current);
            for (var index = bytesSpan.Length - 1; index >= 0; index--)
            {
                stream.Write(bytesSpan.Slice(index, 1));
                stream.Seek(-2, SeekOrigin.Current);
            }
            stream.Seek(1, SeekOrigin.Current);
        }
        else stream.Write(bytesSpan);
    }

    /// <summary>
    /// 跳过指定数量的字节。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="count">要调过的字节数</param>
    /// <param name="reverse">是否反向跳过</param>
    /// <exception cref="ArgumentOutOfRangeException">形参的值超出范围。</exception>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流既不支持查找也不支持读取。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    /// <exception cref="EndOfStreamException">在读取计数字节数之前到达流的末尾。</exception>
    public static void SkipBytes(this Stream stream, int count, bool reverse = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        if (stream.CanSeek) stream.Seek(count * (reverse ? -1 : 1), SeekOrigin.Current);
        else if (stream.CanRead)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(count);
            try
            {
                stream.ReadExactly(buffer, 0, count);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    /// <summary>
    /// 读取一个短整型数据。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="reverse">是否反向读取</param>
    /// <returns>读取的短整型数据</returns>
    /// <exception cref="ArgumentOutOfRangeException">形参的值超出范围。</exception>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流不支持查找或读取。</exception>
    /// <exception cref="EndOfStreamException">在读取计数字节数之前到达流的末尾。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ReadShort(this Stream stream, bool reverse = false) =>
        BitConverter.ToInt16(ReadLittleEndianBytes(stream, sizeof(short), reverse));

    /// <summary>
    /// 读取一个无符号短整型数据。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="reverse">是否反向读取</param>
    /// <returns>读取的无符号短整型数据</returns>
    /// <exception cref="ArgumentOutOfRangeException">形参的值超出范围。</exception>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流不支持查找或读取。</exception>
    /// <exception cref="EndOfStreamException">在读取计数字节数之前到达流的末尾。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUshort(this Stream stream, bool reverse = false) =>
        BitConverter.ToUInt16(ReadLittleEndianBytes(stream, sizeof(ushort), reverse));

    /// <summary>
    /// 读取一个整型数据。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="reverse">是否反向读取</param>
    /// <returns>读取的无符号短整型数据</returns>
    /// <exception cref="ArgumentOutOfRangeException">形参的值超出范围。</exception>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流不支持查找或读取。</exception>
    /// <exception cref="EndOfStreamException">在读取计数字节数之前到达流的末尾。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadInt(this Stream stream, bool reverse = false) =>
        BitConverter.ToInt32(ReadLittleEndianBytes(stream, sizeof(int), reverse));

    /// <summary>
    /// 读取一个无符号整型数据。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="reverse">是否反向读取</param>
    /// <returns>读取的无符号短整型数据</returns>
    /// <exception cref="ArgumentOutOfRangeException">形参的值超出范围。</exception>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流不支持查找或读取。</exception>
    /// <exception cref="EndOfStreamException">在读取计数字节数之前到达流的末尾。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUint(this Stream stream, bool reverse = false) =>
        BitConverter.ToUInt32(ReadLittleEndianBytes(stream, sizeof(uint), reverse));

    /// <summary>
    /// 读取一个长整型数据。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="reverse">是否反向读取</param>
    /// <returns>读取的长整型数据</returns>
    /// <exception cref="ArgumentOutOfRangeException">形参的值超出范围。</exception>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流不支持查找或读取。</exception>
    /// <exception cref="EndOfStreamException">在读取计数字节数之前到达流的末尾。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ReadLong(this Stream stream, bool reverse = false) =>
        BitConverter.ToInt64(ReadLittleEndianBytes(stream, sizeof(long), reverse));

    /// <summary>
    /// 读取一个无符号长整型数据。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="reverse">是否反向读取</param>
    /// <returns>读取的无符号长整型数据</returns>
    /// <exception cref="ArgumentOutOfRangeException">形参的值超出范围。</exception>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流不支持查找或读取。</exception>
    /// <exception cref="EndOfStreamException">在读取计数字节数之前到达流的末尾。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReadUlong(this Stream stream, bool reverse = false) =>
        BitConverter.ToUInt64(ReadLittleEndianBytes(stream, sizeof(ulong), reverse));

    /// <summary>
    /// 写入一个短整型数据。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="value">要写入的短整型数据</param>
    /// <param name="reverse">是否反向写入</param>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流不支持查找或写入。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteNumber(this Stream stream, short value, bool reverse = false) =>
        stream.WriteLittleEndianBytes(BitConverter.GetBytes(value), reverse);

    /// <summary>
    /// 写入一个无符号短整型数据。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="value">要写入的无符号短整型数据</param>
    /// <param name="reverse">是否反向写入</param>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流不支持查找或写入。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteNumber(this Stream stream, ushort value, bool reverse = false) =>
        stream.WriteLittleEndianBytes(BitConverter.GetBytes(value), reverse);

    /// <summary>
    /// 写入一个整型数据。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="value">要写入的整型数据</param>
    /// <param name="reverse">是否反向写入</param>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流不支持查找或写入。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteNumber(this Stream stream, int value, bool reverse = false) =>
        stream.WriteLittleEndianBytes(BitConverter.GetBytes(value), reverse);

    /// <summary>
    /// 写入一个无符号整型数据。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="value">要写入的无符号整型数据</param>
    /// <param name="reverse">是否反向写入</param>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流不支持查找或写入。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteNumber(this Stream stream, uint value, bool reverse = false) =>
        stream.WriteLittleEndianBytes(BitConverter.GetBytes(value), reverse);

    /// <summary>
    /// 写入一个长整型数据。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="value">要写入的长整型数据</param>
    /// <param name="reverse">是否反向写入</param>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流不支持查找或写入。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteNumber(this Stream stream, long value, bool reverse = false) =>
        stream.WriteLittleEndianBytes(BitConverter.GetBytes(value), reverse);

    /// <summary>
    /// 写入一个无符号长整型数据。
    /// </summary>
    /// <param name="stream"><see cref="Stream"/>的实例</param>
    /// <param name="value">要写入的无符号长整型数据</param>
    /// <param name="reverse">是否反向写入</param>
    /// <exception cref="IOException">发生I/O错误。</exception>
    /// <exception cref="NotSupportedException">流不支持查找或写入。</exception>
    /// <exception cref="ObjectDisposedException">在流关闭后调用了方法。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteNumber(this Stream stream, ulong value, bool reverse = false) =>
        stream.WriteLittleEndianBytes(BitConverter.GetBytes(value), reverse);
}