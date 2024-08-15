/*namespace ZipStreamDownload.Http;

/// <summary>
/// HTTP分段下载流
/// </summary>
public class HttpPartialDownloadBufferStream : Stream
{
    /// <summary>
    /// <see cref="HttpPartialDownloadStream"/>的实例。
    /// </summary>
    private readonly HttpPartialDownloadStream _httpPartialDownloadStream;

    /// <summary>
    /// 当前流的读取或写入位置
    /// </summary>
    private long _position;

    /// <summary>
    /// 上次下载的数据量
    /// </summary>
    private long _lastDownloadCount;

    /// <summary>
    /// 缓存流
    /// </summary>
    private readonly Stream _bufferStream;

    /// <summary>
    /// 上一次每秒下载字节数
    /// </summary>
    private long _lastDownloadByteFiveSecond;

    /// <summary>
    /// 获取一个值，指示当前流是否支持读取。
    /// </summary>
    /// <returns>如果流支持读取，则为true；否则为false。</returns>
    public override bool CanRead => true;

    /// <summary>
    /// 获取一个值，指示当前流是否支持查找。
    /// </summary>
    /// <returns>如果流支持查找，则为true；否则为否则为false。</returns>
    public override bool CanSeek => false;

    /// <summary>
    /// 获取一个值，指示当前流是否支持写入。
    /// </summary>
    /// <returns>如果流支持写入，则为true；否则为否则为否则为false。</returns>
    public override bool CanWrite => false;

    /// <summary>
    /// 获取流的长度（以字节为单位）。
    /// </summary>
    /// <returns>一个长整型的值，表示流的长度（以字节为单位）。</returns>
    /// <exception cref="ObjectDisposedException">在对象释放后调用了方法。</exception>
    /// <exception cref="InvalidOperationException">当此对象未调用InitBefferStream或InitStream方法初始化引发的此异常。</exception>
    public override long Length => _httpPartialDownloadStream.Length;

    /// <summary>
    /// 获取当前流中的位置。
    /// </summary>
    /// <exception cref="NotSupportedException">当试图设置此属性的值时引发此异常。</exception>
    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// 创建一个HTTP分段下载流的实例。
    /// </summary>
    /// <param name="httpPartialDownloadStream"><see cref="HttpPartialDownloadStream"/>的实例。</param>
    /// <param name="bufferSize"></param>
    public HttpPartialDownloadBufferStream(HttpPartialDownloadStream httpPartialDownloadStream, int bufferSize = 1024)
    {
        _httpPartialDownloadStream = httpPartialDownloadStream;
        _bufferStream = new MemoryStream(bufferSize);
    }

    /// <summary>
    /// 清除此流的所有缓冲区，并将任何缓冲数据写入底层设备。
    /// </summary>
    /// <remarks>此参数在该类型的流中调用不会执行任何动作。</remarks>
    public override void Flush() => _httpPartialDownloadStream.Flush();

    /// <summary>
    /// 清除此流的所有缓冲区，并将任何缓冲数据写入底层设备。
    /// </summary>
    /// <remarks>此参数在该类型的流中调用不会执行任何动作。</remarks>
    public override Task FlushAsync(CancellationToken cancellationToken)
        => _httpPartialDownloadStream.FlushAsync(cancellationToken);

    /// <summary>
    /// 从当前流中异步读取字节序列，并根据读取的字节数在流中前进位置。
    /// </summary>
    /// <param name="count">从当前流中读取的最大字节数。</param>
    /// <param name="bufferStreamReadFunc">读取数据缓存流的回调方法。</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌。默认值为“<see cref="CancellationToken.None"/>”。</param>
    /// <returns>
    /// 表示异步读取操作的任务。TResult参数的值包含读入缓冲区的字节总数。
    /// 如果当前可用的字节数小于请求的字节数，则结果值可以小于请求的比特位数，
    /// 或者如果计数为0或已到达流的末尾，结果值可以为0（零）。
    /// </returns>
    public async ValueTask<int> _ReadAsync(int count, Func<int, int, ValueTask<int>> bufferStreamReadFunc,
        CancellationToken cancellationToken = default)
    {
        if (_position == _httpPartialDownloadStream.Length) return 0;
        if (_position + count > _httpPartialDownloadStream.Length)
            count = (int)(_httpPartialDownloadStream.Length - _position);

        var totalCount = 0;
        while (true)
        {
            if (_lastDownloadCount > 0 &&
                _httpPartialDownloadStream.Position >= _lastDownloadCount &&
                _bufferStream.Position < _lastDownloadCount)
            {
                var resultCount = await bufferStreamReadFunc(totalCount,
                    Math.Min(count, (int)(_lastDownloadCount - _bufferStream.Position)));

                count -= resultCount;
                totalCount += resultCount;

                if (count <= 0)
                {
                    _position += totalCount;
                    return totalCount;
                }
            }

            _bufferStream.SetLength(0);
            _bufferStream.Position = 0;
            if (_lastDownloadByteFiveSecond < 1048576) _lastDownloadByteFiveSecond = 1048576;
            _lastDownloadCount = await _httpPartialDownloadStream.Read(_lastDownloadByteFiveSecond, false
                , cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 从当前流中读取一个字节序列，并将流中的位置向前移动读取的字节数。
    /// </summary>
    /// <param name="buffer">字节数组。当此方法返回时，缓冲区包含指定的字节数组，其中offset和（offset + count - 1）之间的值被从当前源读取的字节替换。</param>
    /// <param name="offset">缓冲区中从零开始存储从当前流读取的数据的字节偏移量。</param>
    /// <param name="count">从当前流中读取的最大字节数。</param>
    /// <returns>读入缓冲区的字节总数。如果当前没有那么多字节可用，则这可能小于请求的字节数，或者如果计数为0或已到达流的末尾，则为零（0）。</returns>
    public override int Read(byte[] buffer, int offset, int count)
        => _ReadAsync(buffer.Length, (start, length) =>
        {
            var sliceBuffer = buffer.AsSpan().Slice(start, length);
            return ValueTask.FromResult(_bufferStream.Read(sliceBuffer));
        }).GetAwaiter().GetResult();

    /// <summary>
    /// 从当前流中异步读取字节序列，并根据读取的字节数在流中前进位置。
    /// </summary>
    /// <param name="buffer">用于写入数据的缓冲区。</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌。默认值为“<see cref="CancellationToken.None"/>”。</param>
    /// <returns>
    /// 表示异步读取操作的任务。TResult参数的值包含读入缓冲区的字节总数。
    /// 如果当前可用的字节数小于请求的字节数，则结果值可以小于请求的比特位数，
    /// 或者如果计数为0或已到达流的末尾，结果值可以为0（零）。
    /// </returns>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer,
        CancellationToken cancellationToken = default)
        => _ReadAsync(buffer.Length, (start, length) =>
        {
            var sliceBuffer = buffer.Slice(start, length);
            return _bufferStream.ReadAsync(sliceBuffer, cancellationToken);
        }, cancellationToken);


    /// <summary>
    /// 设置当前流中的位置。（当前流不支持）
    /// </summary>
    /// <param name="offset">相对于原始参数的字节偏移量。</param>
    /// <param name="origin"><see cref="SeekOrigin"/>类型的值，表示用于获取新位置的参考点。</param>
    /// <returns>当前流中的新位置。</returns>
    /// <exception cref="NotSupportedException">当调用此方法时引发此异常。</exception>
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// 设置当前流的长度。（当前流不支持）
    /// </summary>
    /// <param name="value">当前流的所需长度（以字节为单位）。</param>
    /// <exception cref="NotSupportedException">当调用此方法时引发此异常。</exception>
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// 将一系列字节写入当前流，并将此流中的当前位置向前移动写入的字节数。（当前流不支持）
    /// </summary>
    /// <param name="buffer">字节数组。此方法将计数字节从缓冲区复制到当前流。</param>
    /// <param name="offset">缓冲区中从零开始将字节复制到当前流的字节偏移量。</param>
    /// <param name="count">要写入当前流的字节数。</param>
    /// <exception cref="NotSupportedException">当调用此方法时引发此异常。</exception>
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}*/