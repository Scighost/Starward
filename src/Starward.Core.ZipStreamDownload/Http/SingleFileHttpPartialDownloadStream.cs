using System.Net;
using System.Net.Http.Headers;
using Starward.Core.ZipStreamDownload.Http.Exceptions;
using Starward.Core.ZipStreamDownload.Http.Extensions;

namespace Starward.Core.ZipStreamDownload.Http;

internal sealed class SingleFileHttpPartialDownloadStream : HttpPartialDownloadStream
{
    public Uri FileUri { get; }

    public HttpContentHeaders HttpContentHeaders
    {
        get
        {
            ThrowIfThisIsDisposed();
            return _responseMessage.Content.Headers;
        }
    }

    public HttpResponseHeaders HttpResponseHeaders
    {
        get
        {
            ThrowIfThisIsDisposed();
            return _responseMessage.Headers;
        }
    }

    public HttpResponseHeaders HttpTrailingHeaders
    {
        get
        {
            ThrowIfThisIsDisposed();
            return _responseMessage.TrailingHeaders;
        }
    }

    public Version HttpVersion
    {
        get
        {
            ThrowIfThisIsDisposed();
            return _responseMessage.Version;
        }
    }

    public HttpRequestMessage? HttpRequestMessage
    {
        get
        {
            ThrowIfThisIsDisposed();
            return _responseMessage.RequestMessage;
        }
    }

    private readonly HttpClient _httpClient;

    private readonly string? _mediaType;

    private HttpResponseMessage _responseMessage;

    private Stream _responseReadStream;

    private readonly Uri _ipAddressUri;

    private SingleFileHttpPartialDownloadStream(HttpClient httpClient,
        HttpResponseMessage responseMessage, Stream responseReadStream,
        Uri fileUri, Uri ipAddressUri, string? mediaType, long? fileLength, DateTimeOffset? fileLastModifiedTime)
    {
        ValidatePartialHttpResponseMessage(responseMessage, mediaType, fileLength, fileLastModifiedTime);
        _httpClient = httpClient;
        FileUri = fileUri;
        _ipAddressUri = ipAddressUri;
        _mediaType = mediaType;
        _responseMessage = responseMessage;
        _responseReadStream = responseReadStream;

        var contentHeaders = responseMessage.Content.Headers;
        var contentRange = contentHeaders.ContentRange!;
        StartBytes = contentRange.From!.Value;
        EndBytes = contentRange.To!.Value + 1;
        FileLength = contentRange.Length!.Value;
        if (contentHeaders.LastModified.HasValue) FileLastModifiedTime = contentHeaders.LastModified.Value;
    }

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient, Uri fileUri,
        long? startBytes, long? endBytes, string? mediaType, long? fileLength, DateTimeOffset? fileLastModifiedTime)
    {
        ValidateStartBytesAndEndBytes(startBytes, endBytes, fileLength);
        var ipAddressUri = fileUri.GetIpAddressUri();
        var responseMessage = httpClient.GetPartial(ipAddressUri, fileUri.Host, startBytes,
            endBytes + (startBytes.HasValue ? -1 : 0), mediaType);
        try
        {
            var responseReadStream = responseMessage.Content.ReadAsStream();
            var instance = new SingleFileHttpPartialDownloadStream(httpClient, responseMessage, responseReadStream,
                fileUri, ipAddressUri, mediaType, fileLength, fileLastModifiedTime);
            return instance;
        }
        catch
        {
            responseMessage.Dispose();
            throw;
        }
    }

    public static async Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient, Uri fileUri,
        long? startBytes, long? endBytes, string? mediaType, long? fileLength, DateTimeOffset? fileLastModifiedTime,
        CancellationToken cancellationToken = default)
    {
        ValidateStartBytesAndEndBytes(startBytes, endBytes, fileLength);
        var ipAddressUri = await fileUri.GetIpAddressUriAsync(cancellationToken);
        var responseMessage = await httpClient.GetPartialAsync(ipAddressUri, fileUri.Host, startBytes,
            endBytes + (startBytes.HasValue ? -1 : 0), mediaType, cancellationToken).ConfigureAwait(false);
        try
        {
            var responseReadStream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            var instance = new SingleFileHttpPartialDownloadStream(httpClient, responseMessage, responseReadStream,
                fileUri, ipAddressUri, mediaType, fileLength, fileLastModifiedTime);
            return instance;
        }
        catch
        {
            responseMessage.Dispose();
            throw;
        }
    }

    protected override bool ResetRangeCore(long? startBytes = null, long? endBytes = null)
    {
        var (newStartBytes, newEndBytes) = ValidateAndGetStartBytesAndEndBytes(startBytes, endBytes);
        if (StartBytes == newStartBytes && EndBytes == newEndBytes) return false;
        var responseMessage = _httpClient.GetPartial(_ipAddressUri, FileUri.Host, newStartBytes,
            newEndBytes - 1, _mediaType);
        try
        {
            ValidatePartialHttpResponseMessage(responseMessage, _mediaType, FileLength, FileLastModifiedTime);
            var responseReadStream = responseMessage.Content.ReadAsStream();
            try
            {
                _responseReadStream.Dispose();
                _responseMessage.Dispose();
            }
            catch
            {
                responseReadStream.Dispose();
                throw;
            }
            _responseReadStream = responseReadStream;
        }
        catch
        {
            responseMessage.Dispose();
            throw;
        }
        _responseMessage = responseMessage;
        var contentHeaders = responseMessage.Content.Headers;
        var contentRange = contentHeaders.ContentRange!;
        StartBytes = contentRange.From!.Value;
        EndBytes = contentRange.To!.Value + 1;
        if (FileLastModifiedTime == null && contentHeaders.LastModified != null)
            FileLastModifiedTime = contentHeaders.LastModified;
        return true;
    }

    protected override async Task<bool> ResetRangeAsyncCore(long? startBytes = null, long? endBytes = null,
        CancellationToken cancellationToken = default)
    {
        var (newStartBytes, newEndBytes) = ValidateAndGetStartBytesAndEndBytes(startBytes, endBytes);
        if (StartBytes == newStartBytes && EndBytes == newEndBytes) return false;
        var responseMessage = await _httpClient.GetPartialAsync(_ipAddressUri, FileUri.Host, newStartBytes,
            newEndBytes - 1, _mediaType, cancellationToken).ConfigureAwait(false);
        try
        {
            ValidatePartialHttpResponseMessage(responseMessage, _mediaType, FileLength, FileLastModifiedTime);
            var responseReadStream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            try
            {
                await _responseReadStream.DisposeAsync().ConfigureAwait(false);
                _responseMessage.Dispose();
            }
            catch
            {
                await responseReadStream.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            _responseReadStream = responseReadStream;
        }
        catch
        {
            responseMessage.Dispose();
            throw;
        }
        _responseMessage = responseMessage;
        var contentRange = responseMessage.Content.Headers.ContentRange!;
        StartBytes = contentRange.From!.Value;
        EndBytes = contentRange.To!.Value + 1;
        return true;
    }

    public override void Flush()
    {
        ThrowIfThisIsDisposed();
        _responseReadStream.Flush();
        base.Flush();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        ThrowIfThisIsDisposed();
        await _responseReadStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        await base.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfThisIsDisposed();
        ValidateBufferArguments(buffer, offset, count);
        SeekActually();
        if ((count = GetReadCount(count)) == 0) return 0;
        count = _responseReadStream.Read(buffer, offset, count);
        AddPositionActually(count);
        return count;
    }

    public override int Read(Span<byte> buffer)
    {
        ThrowIfThisIsDisposed();
        SeekActually();
        int count;
        if ((count = GetReadCount(buffer.Length)) == 0) return 0;
        count = _responseReadStream.Read(buffer[..count]);
        AddPositionActually(count);
        return count;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
        CancellationToken cancellationToken)
    {
        ThrowIfThisIsDisposed();
        ValidateBufferArguments(buffer, offset, count);
        await SeekActuallyAsync(cancellationToken).ConfigureAwait(false);
        if ((count = GetReadCount(count)) == 0) return 0;
        count = await _responseReadStream.ReadAsync(buffer, offset, count, cancellationToken)
            .ConfigureAwait(false);
        AddPositionActually(count);
        return count;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfThisIsDisposed();
        await SeekActuallyAsync(cancellationToken).ConfigureAwait(false);
        int count;
        if ((count = GetReadCount(buffer.Length)) == 0) return 0;
        count = await _responseReadStream.ReadAsync(buffer[..count], cancellationToken).ConfigureAwait(false);
        AddPositionActually(count);
        return count;
    }

    public override int ReadByte()
    {
        ThrowIfThisIsDisposed();
        SeekActually();
        if (Position == Length) return -1;
        var byteRent = _responseReadStream.ReadByte();
        if (byteRent >= 0) AddPositionActually();
        return byteRent;
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        ThrowIfThisIsDisposed();
        ValidateCopyToArguments(destination, bufferSize);
        _responseReadStream.CopyTo(destination, bufferSize);
        SetPositionToEndActually();
    }

    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        ThrowIfThisIsDisposed();
        ValidateCopyToArguments(destination, bufferSize);
        await SeekActuallyAsync(cancellationToken).ConfigureAwait(false);
        await _responseReadStream.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(false);
        SetPositionToEndActually();
    }

    protected override void Dispose(bool disposing)
    {
        SetDisposed();
        if (!disposing) return;
        _responseReadStream.Dispose();
        _responseMessage.Dispose();
    }

    public override async ValueTask DisposeAsync()
    {
        SetDisposed();
        await _responseReadStream.DisposeAsync().ConfigureAwait(false);
        _responseMessage.Dispose();
    }

    protected override void SeekActuallyCore(long fakePosition)
    {
        var responseMessage = _httpClient.GetPartial(FileUri, StartBytes + fakePosition, EndBytes - 1, _mediaType);
        try
        {
            ValidatePartialHttpResponseMessage(responseMessage, _mediaType, FileLength, FileLastModifiedTime);
            var responseReadStream = responseMessage.Content.ReadAsStream();
            try
            {
                _responseReadStream.Dispose();
                _responseMessage.Dispose();
            }
            catch
            {
                responseReadStream.Dispose();
                throw;
            }
            _responseReadStream = responseReadStream;
        }
        catch
        {
            responseMessage.Dispose();
            throw;
        }
        _responseMessage = responseMessage;
    }

    protected override async Task SeekActuallyAsyncCore(long fakePosition,
        CancellationToken cancellationToken = default)
    {
       var responseMessage = await _httpClient.GetPartialAsync(FileUri, StartBytes + fakePosition, EndBytes - 1,
           _mediaType, cancellationToken).ConfigureAwait(false);
       try
       {
           ValidatePartialHttpResponseMessage(responseMessage, _mediaType, FileLength, FileLastModifiedTime);
           var responseReadStream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken)
               .ConfigureAwait(false);
           try
           {
               await _responseReadStream.DisposeAsync().ConfigureAwait(false);
               _responseMessage.Dispose();
           }
           catch
           {
               await responseReadStream.DisposeAsync().ConfigureAwait(false);
               throw;
           }
           _responseReadStream = responseReadStream;
       }
       catch
       {
           responseMessage.Dispose();
           throw;
       }
       _responseMessage = responseMessage;
    }

    private static void ValidatePartialHttpResponseMessage(HttpResponseMessage responseMessage,
        string? mediaType = null, long? contentRangeLength = null,  DateTimeOffset? lastModified = null)
    {
        responseMessage.EnsureSuccessStatusCode();

        if (responseMessage.StatusCode == HttpStatusCode.OK)
            HttpServerNotSupportedPartialDownloadException.Throw();

        HttpStatusCodeInvalidException.ThrowIfCodeNotEqualPartialContent(responseMessage.StatusCode,
            responseMessage.ReasonPhrase);

        HttpMediaTypeMismatchException.ThrowIfVersionLessThenHttp11(responseMessage.Version);

        var responseHeaders = responseMessage.Content.Headers;

        HttpMediaTypeMismatchException.ThrowIfMediaTypeMismatch(responseHeaders, mediaType);

        if (responseHeaders.ContentRange == null)
            HttpServerNotSupportedPartialDownloadException.Throw();

        var contentRange = responseHeaders.ContentRange!;
        if (contentRange.Unit != "bytes" ||
            !contentRange.HasRange || !contentRange.HasLength ||
            contentRange.To is null or < 0 || contentRange.To < contentRange.From ||
            contentRange.To > contentRange.Length || contentRange.From > contentRange.Length)
            HttpServerNotSupportedPartialDownloadException.Throw();

        if (responseHeaders.ContentLength != null &&
            responseHeaders.ContentLength != contentRange.To - contentRange.From + 1)
            HttpServerNotSupportedPartialDownloadException.Throw();

        if (contentRangeLength != null && contentRangeLength != (long)contentRange.Length!)
            HttpFileModifiedDuringPartialDownload.Throw();

        if (lastModified != null && responseHeaders.LastModified != null &&
            lastModified != responseHeaders.LastModified)
            HttpFileModifiedDuringPartialDownload.Throw();
    }

    #region 重载方法

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient, Uri fileUri)
        => GetInstance(httpClient, fileUri, null, null, null, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient, Uri fileUri,
        long? startBytes, long? endBytes)
        => GetInstance(httpClient, fileUri, startBytes, endBytes, null, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient, Uri fileUri,
        long? startBytes)
        => GetInstance(httpClient, fileUri, startBytes, null, null, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient, Uri fileUri,
        long? startBytes, long? endBytes, string? mediaType)
        => GetInstance(httpClient, fileUri, startBytes, endBytes, mediaType, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient, Uri fileUri,
        long? startBytes, string? mediaType)
        => GetInstance(httpClient, fileUri, startBytes, null, mediaType, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient, Uri fileUri, string? mediaType)
        => GetInstance(httpClient, fileUri, null, null, mediaType, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient, Uri fileUri,
        string? mediaType, long? fileLength, DateTimeOffset? fileLastModifiedTime)
        => GetInstance(httpClient, fileUri, null, null, mediaType, fileLength, fileLastModifiedTime);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient, Uri fileUri,
        long? startBytes, long? endBytes, long? fileLength, DateTimeOffset? fileLastModifiedTime)
        => GetInstance(httpClient, fileUri, startBytes, endBytes, null, fileLength, fileLastModifiedTime);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient, Uri fileUri,
        long? startBytes, long? fileLength, DateTimeOffset? fileLastModifiedTime)
        => GetInstance(httpClient, fileUri, startBytes, null, null, fileLength, fileLastModifiedTime);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient, Uri fileUri,
        long? fileLength, DateTimeOffset? fileLastModifiedTime)
        => GetInstance(httpClient, fileUri, null, null, null, fileLength, fileLastModifiedTime);



    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient, Uri fileUri,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, null, null, null, null, null, cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient, Uri fileUri,
        long? startBytes, long? endBytes, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, endBytes, null, null, null, cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient, Uri fileUri,
        long? startBytes, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, null, null, null, null, cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient, Uri fileUri,
        long? startBytes, long? endBytes, string? mediaType, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, endBytes, mediaType, null, null, cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient, Uri fileUri,
        long? startBytes, string? mediaType, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, null, mediaType, null, null, cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient, Uri fileUri,
        string? mediaType, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, null, null, mediaType, null, null, cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient, Uri fileUri,
        string? mediaType, long? fileLength, DateTimeOffset? fileLastModifiedTime,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, null, null, mediaType, fileLength, fileLastModifiedTime,
            cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient, Uri fileUri,
        long? startBytes, long? endBytes, long? fileLength, DateTimeOffset? fileLastModifiedTime,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, endBytes, null, fileLength, fileLastModifiedTime,
            cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient, Uri fileUri,
        long? startBytes, long? fileLength, DateTimeOffset? fileLastModifiedTime,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, null, null, fileLength, fileLastModifiedTime,
            cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient, Uri fileUri,
        long? fileLength, DateTimeOffset? fileLastModifiedTime, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, null, null, null, fileLength, fileLastModifiedTime,
            cancellationToken);

    #endregion
}