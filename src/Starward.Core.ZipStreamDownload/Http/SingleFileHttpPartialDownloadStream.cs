﻿using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
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
        HttpResponseMessage responseMessage, Stream responseReadStream, Uri fileUri, Uri ipAddressUri,
        AutoRetryOptions autoRetryOptions, string? mediaType, long? fileLength, DateTimeOffset? fileLastModifiedTime)
    {
        ValidatePartialHttpResponseMessage(responseMessage, mediaType, fileLength, fileLastModifiedTime);
        _httpClient = httpClient;
        FileUri = fileUri;
        _ipAddressUri = ipAddressUri;
        AutoRetryOptions = autoRetryOptions;
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

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? endBytes, AutoRetryOptions autoRetryOptions,
        string? mediaType, long? fileLength, DateTimeOffset? fileLastModifiedTime)
    {
        ValidateStartBytesAndEndBytes(startBytes, endBytes, fileLength);
        var ipAddressUri = fileUri.GetIpAddressUri();

        var retryTimes = 0;
        var autoRetryTimesOnNetworkError = autoRetryOptions.RetryTimesOnNetworkError;
        var delayMillisecond = autoRetryOptions.DelayMillisecond;
        while (true)
        {
            try
            {
                return Core(httpClient, fileUri, ipAddressUri, startBytes, endBytes + (startBytes.HasValue ? -1 : 0),
                    autoRetryOptions, mediaType, fileLength, fileLastModifiedTime);
            }
            catch (Exception e) when(e is HttpRequestException or SocketException or HttpIOException or IOException)
            {
                retryTimes++;
                if (retryTimes > autoRetryTimesOnNetworkError) throw;
                Task.Delay(delayMillisecond).GetAwaiter().GetResult();
            }
        }

        static SingleFileHttpPartialDownloadStream Core(HttpClient httpClient, Uri fileUri, Uri ipAddressUri,
            long? startBytes, long? endBytes, AutoRetryOptions autoRetryOptions, string? mediaType, long? fileLength,
            DateTimeOffset? fileLastModifiedTime)
        {
            HttpResponseMessage responseMessage;
            if (httpClient.DefaultRequestVersion >= System.Net.HttpVersion.Version20 ||
                httpClient.DefaultVersionPolicy == HttpVersionPolicy.RequestVersionOrHigher)
                responseMessage = httpClient
                    .GetPartialAsync(ipAddressUri, fileUri.Host, startBytes, endBytes, mediaType).GetAwaiter()
                    .GetResult();
            else responseMessage = httpClient.GetPartial(ipAddressUri, fileUri.Host, startBytes, endBytes, mediaType);
            try
            {
                var responseReadStream = responseMessage.Content.ReadAsStream();
                var instance = new SingleFileHttpPartialDownloadStream(httpClient, responseMessage, responseReadStream,
                    fileUri, ipAddressUri, autoRetryOptions, mediaType, fileLength, fileLastModifiedTime);
                return instance;
            }
            catch
            {
                responseMessage.Dispose();
                throw;
            }
        }
    }

    public static async Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? endBytes, AutoRetryOptions autoRetryOptions,
        string? mediaType, long? fileLength, DateTimeOffset? fileLastModifiedTime, CancellationToken cancellationToken = default)
    {
        ValidateStartBytesAndEndBytes(startBytes, endBytes, fileLength);
        var ipAddressUri = await fileUri.GetIpAddressUriAsync().ConfigureAwait(false);

        var retryTimes = 0;
        var autoRetryTimesOnNetworkError = autoRetryOptions.RetryTimesOnNetworkError;
        var delayMillisecond = autoRetryOptions.DelayMillisecond;
        while (true)
        {
            try
            {
                return await CoreAsync(httpClient, fileUri, ipAddressUri, startBytes,
                        endBytes + (startBytes.HasValue ? -1 : 0), autoRetryOptions, mediaType, fileLength,
                        fileLastModifiedTime, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when(e is HttpRequestException or SocketException or HttpIOException or IOException)
            {
                retryTimes++;
                if (retryTimes > autoRetryTimesOnNetworkError) throw;
                await Task.Delay(delayMillisecond, cancellationToken).ConfigureAwait(false);
            }
        }

        static async Task<SingleFileHttpPartialDownloadStream> CoreAsync(HttpClient httpClient, Uri fileUri,
            Uri ipAddressUri, long? startBytes, long? endBytes, AutoRetryOptions autoRetryOptions, string? mediaType,
            long? fileLength, DateTimeOffset? fileLastModifiedTime, CancellationToken cancellationToken = default)
        {
            var responseMessage = await httpClient.GetPartialAsync(ipAddressUri, fileUri.Host, startBytes, endBytes,
                mediaType, cancellationToken).ConfigureAwait(false);
            try
            {
                var responseReadStream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken)
                    .ConfigureAwait(false);
                var instance = new SingleFileHttpPartialDownloadStream(httpClient, responseMessage, responseReadStream,
                    fileUri, ipAddressUri, autoRetryOptions, mediaType, fileLength, fileLastModifiedTime);
                return instance;
            }
            catch
            {
                responseMessage.Dispose();
                throw;
            }
        }
    }

    protected override bool ResetRangeCore(long? startBytes = null, long? endBytes = null)
    {
        var (newStartBytes, newEndBytes) = ValidateAndGetStartBytesAndEndBytes(startBytes, endBytes);
        if (StartBytes == newStartBytes && EndBytes == newEndBytes) return false;

        var retryTimes = 0;
        var autoRetryTimesOnNetworkError = AutoRetryOptions.RetryTimesOnNetworkError;
        var delayMillisecond = AutoRetryOptions.DelayMillisecond;
        while (true)
        {
            try
            {
                return Core();
            }
            catch (Exception e) when(e is HttpRequestException or SocketException or HttpIOException or IOException)
            {
                retryTimes++;
                if (retryTimes > autoRetryTimesOnNetworkError) throw;
                Task.Delay(delayMillisecond).GetAwaiter().GetResult();
            }
        }

        bool Core()
        {
            HttpResponseMessage responseMessage;
            if (_httpClient.DefaultRequestVersion >= System.Net.HttpVersion.Version20 ||
                _httpClient.DefaultVersionPolicy == HttpVersionPolicy.RequestVersionOrHigher)
                responseMessage = _httpClient.GetPartialAsync(_ipAddressUri, FileUri.Host, newStartBytes,
                    newEndBytes - 1, _mediaType).GetAwaiter().GetResult();
            else responseMessage = _httpClient.GetPartial(_ipAddressUri, FileUri.Host, newStartBytes,
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
    }

    protected override async Task<bool> ResetRangeAsyncCore(long? startBytes = null, long? endBytes = null,
        CancellationToken cancellationToken = default)
    {
        var (newStartBytes, newEndBytes) = ValidateAndGetStartBytesAndEndBytes(startBytes, endBytes);
        if (StartBytes == newStartBytes && EndBytes == newEndBytes) return false;

        var retryTimes = 0;
        var autoRetryTimesOnNetworkError = AutoRetryOptions.RetryTimesOnNetworkError;
        var delayMillisecond = AutoRetryOptions.DelayMillisecond;
        while (true)
        {
            try
            {
                return await CoreAsync().ConfigureAwait(false);
            }
            catch (Exception e) when(e is HttpRequestException or SocketException or HttpIOException or IOException)
            {
                retryTimes++;
                if (retryTimes > autoRetryTimesOnNetworkError) throw;
                await Task.Delay(delayMillisecond, cancellationToken).ConfigureAwait(false);
            }
        }

        async Task<bool> CoreAsync()
        {
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

        var retryTimes = 0;
        var autoRetryTimesOnNetworkError = AutoRetryOptions.RetryTimesOnNetworkError;
        var delayMillisecond = AutoRetryOptions.DelayMillisecond;
        while (true)
        {
            try
            {
                if (retryTimes > 0) SeekOnRetry();
                count = _responseReadStream.Read(buffer, offset, count);
                break;
            }
            catch (Exception e) when(e is HttpRequestException or SocketException or HttpIOException or IOException)
            {
                retryTimes++;
                if (retryTimes > autoRetryTimesOnNetworkError) throw;
                Task.Delay(delayMillisecond).GetAwaiter().GetResult();
            }
        }

        AddPositionActually(count);
        return count;
    }

    public override int Read(Span<byte> buffer)
    {
        ThrowIfThisIsDisposed();
        SeekActually();
        int count;
        if ((count = GetReadCount(buffer.Length)) == 0) return 0;

        var retryTimes = 0;
        var autoRetryTimesOnNetworkError = AutoRetryOptions.RetryTimesOnNetworkError;
        var delayMillisecond = AutoRetryOptions.DelayMillisecond;
        while (true)
        {
            try
            {
                if (retryTimes > 0) SeekOnRetry();
                count = _responseReadStream.Read(buffer[..count]);
                break;
            }
            catch (Exception e) when(e is HttpRequestException or SocketException or HttpIOException or IOException)
            {
                retryTimes++;
                if (retryTimes > autoRetryTimesOnNetworkError) throw;
                Task.Delay(delayMillisecond).GetAwaiter().GetResult();
            }
        }

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

        var retryTimes = 0;
        var autoRetryTimesOnNetworkError = AutoRetryOptions.RetryTimesOnNetworkError;
        var delayMillisecond = AutoRetryOptions.DelayMillisecond;
        while (true)
        {
            try
            {
                if (retryTimes > 0) await SeekOnRetryAsync(cancellationToken).ConfigureAwait(false);
                count = await _responseReadStream.ReadAsync(buffer, offset, count, cancellationToken)
                    .ConfigureAwait(false);
                break;
            }
            catch (Exception e) when(e is HttpRequestException or SocketException or HttpIOException or IOException)
            {
                retryTimes++;
                if (retryTimes > autoRetryTimesOnNetworkError) throw;
                await Task.Delay(delayMillisecond, cancellationToken).ConfigureAwait(false);
            }
        }

        AddPositionActually(count);
        return count;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfThisIsDisposed();
        await SeekActuallyAsync(cancellationToken).ConfigureAwait(false);
        int count;
        if ((count = GetReadCount(buffer.Length)) == 0) return 0;

        var retryTimes = 0;
        var autoRetryTimesOnNetworkError = AutoRetryOptions.RetryTimesOnNetworkError;
        var delayMillisecond = AutoRetryOptions.DelayMillisecond;
        while (true)
        {
            try
            {
                if (retryTimes > 0) await SeekOnRetryAsync(cancellationToken).ConfigureAwait(false);
                count = await _responseReadStream.ReadAsync(buffer[..count], cancellationToken).ConfigureAwait(false);
                break;
            }
            catch (Exception e) when(e is HttpRequestException or SocketException or HttpIOException or IOException)
            {
                retryTimes++;
                if (retryTimes > autoRetryTimesOnNetworkError) throw;
                await Task.Delay(delayMillisecond, cancellationToken).ConfigureAwait(false);
            }
        }

        AddPositionActually(count);
        return count;
    }

    public override int ReadByte()
    {
        ThrowIfThisIsDisposed();
        SeekActually();
        if (Position == Length) return -1;
        int byteRent;

        var retryTimes = 0;
        var autoRetryTimesOnNetworkError = AutoRetryOptions.RetryTimesOnNetworkError;
        var delayMillisecond = AutoRetryOptions.DelayMillisecond;
        while (true)
        {
            try
            {
                if (retryTimes > 0) SeekOnRetry();
                byteRent = _responseReadStream.ReadByte();
                break;
            }
            catch (Exception e) when(e is HttpRequestException or SocketException or HttpIOException or IOException)
            {
                retryTimes++;
                if (retryTimes > autoRetryTimesOnNetworkError) throw;
                Task.Delay(delayMillisecond).GetAwaiter().GetResult();
            }
        }

        if (byteRent >= 0) AddPositionActually();
        return byteRent;
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        ThrowIfThisIsDisposed();
        ValidateCopyToArguments(destination, bufferSize);
        SeekActually();

        var retryTimes = 0;
        var autoRetryTimesOnNetworkError = AutoRetryOptions.RetryTimesOnNetworkError;
        var delayMillisecond = AutoRetryOptions.DelayMillisecond;
        while (true)
        {
            try
            {
                if (retryTimes > 0) SeekOnRetry();
                _responseReadStream.CopyTo(destination, bufferSize);
                break;
            }
            catch (Exception e) when(e is HttpRequestException or SocketException or HttpIOException or IOException)
            {
                retryTimes++;
                if (retryTimes > autoRetryTimesOnNetworkError) throw;
                Task.Delay(delayMillisecond).GetAwaiter().GetResult();
            }
        }

        SetPositionToEndActually();
    }

    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        ThrowIfThisIsDisposed();
        ValidateCopyToArguments(destination, bufferSize);
        await SeekActuallyAsync(cancellationToken).ConfigureAwait(false);

        var retryTimes = 0;
        var autoRetryTimesOnNetworkError = AutoRetryOptions.RetryTimesOnNetworkError;
        var delayMillisecond = AutoRetryOptions.DelayMillisecond;
        while (true)
        {
            try
            {
                if (retryTimes > 0) await SeekOnRetryAsync(cancellationToken).ConfigureAwait(false);
                await _responseReadStream.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(false);
                break;
            }
            catch (Exception e) when(e is HttpRequestException or SocketException or HttpIOException or IOException)
            {
                retryTimes++;
                if (retryTimes > autoRetryTimesOnNetworkError) throw;
                await Task.Delay(delayMillisecond, cancellationToken).ConfigureAwait(false);
            }
        }

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

    protected override void SeekActuallyCore(long fakePosition) => SeekActuallyCore(fakePosition, true);

    private void SeekActuallyCore(long fakePosition, bool retry)
    {
        if (!retry)
        {
            Core();
            return;
        }

        var retryTimes = 0;
        var autoRetryTimesOnNetworkError = AutoRetryOptions.RetryTimesOnNetworkError;
        var delayMillisecond = AutoRetryOptions.DelayMillisecond;
        while (true)
        {
            try
            {
                if (retryTimes > 0) SeekOnRetry();
                Core();
                return;
            }
            catch (Exception e) when(e is HttpRequestException or SocketException or HttpIOException or IOException)
            {
                retryTimes++;
                if (retryTimes > autoRetryTimesOnNetworkError) throw;
                Task.Delay(delayMillisecond).GetAwaiter().GetResult();
            }
        }

        void Core()
        {
            HttpResponseMessage responseMessage;
            if (_httpClient.DefaultRequestVersion >= System.Net.HttpVersion.Version20 ||
                _httpClient.DefaultVersionPolicy == HttpVersionPolicy.RequestVersionOrHigher)
                responseMessage = _httpClient.GetPartialAsync(FileUri, StartBytes + fakePosition, EndBytes - 1,
                        _mediaType).GetAwaiter().GetResult();
            else responseMessage = _httpClient.GetPartial(FileUri, StartBytes + fakePosition, EndBytes - 1, _mediaType);
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
    }

    protected override Task SeekActuallyAsyncCore(long fakePosition,
        CancellationToken cancellationToken = default) => SeekActuallyAsyncCore(fakePosition, true, cancellationToken);

    private async Task SeekActuallyAsyncCore(long fakePosition, bool retry,
        CancellationToken cancellationToken = default)
    {
        if (!retry)
        {
            await CoreAsync().ConfigureAwait(false);
            return;
        }

        var retryTimes = 0;
        var autoRetryTimesOnNetworkError = AutoRetryOptions.RetryTimesOnNetworkError;
        var delayMillisecond = AutoRetryOptions.DelayMillisecond;
        while (true)
        {
            try
            {
                if (retryTimes > 0) await SeekOnRetryAsync(cancellationToken).ConfigureAwait(false);
                await CoreAsync().ConfigureAwait(false);
                return;
            }
            catch (Exception e) when(e is HttpRequestException or SocketException or HttpIOException or IOException)
            {
                retryTimes++;
                if (retryTimes > autoRetryTimesOnNetworkError) throw;
                await Task.Delay(delayMillisecond, cancellationToken).ConfigureAwait(false);
            }
        }

        async Task CoreAsync()
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
    }

    private void SeekOnRetry() => SeekActuallyCore(Position, false);

    private Task SeekOnRetryAsync(CancellationToken cancellationToken = default)
        => SeekActuallyAsyncCore(Position, false, cancellationToken);

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

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, AutoRetryOptions autoRetryOptions)
        => GetInstance(httpClient, fileUri, null, null, autoRetryOptions, null, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri)
        => GetInstance(httpClient, fileUri, null, null, new AutoRetryOptions(), null, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? endBytes, AutoRetryOptions autoRetryOptions)
        => GetInstance(httpClient, fileUri, startBytes, endBytes, autoRetryOptions, null, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? endBytes)
        => GetInstance(httpClient, fileUri, startBytes, endBytes, new AutoRetryOptions(), null, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, AutoRetryOptions autoRetryOptions)
        => GetInstance(httpClient, fileUri, startBytes, null, autoRetryOptions, null, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes)
        => GetInstance(httpClient, fileUri, startBytes, null, new AutoRetryOptions(), null, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? endBytes, AutoRetryOptions autoRetryOptions,
        string? mediaType)
        => GetInstance(httpClient, fileUri, startBytes, endBytes, autoRetryOptions, mediaType, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? endBytes, string? mediaType)
        => GetInstance(httpClient, fileUri, startBytes, endBytes, new AutoRetryOptions(), mediaType, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, AutoRetryOptions autoRetryOptions, string? mediaType)
        => GetInstance(httpClient, fileUri, startBytes, null, autoRetryOptions, mediaType, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, string? mediaType)
        => GetInstance(httpClient, fileUri, startBytes, null, new AutoRetryOptions(), mediaType, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, AutoRetryOptions autoRetryOptions, string? mediaType)
        => GetInstance(httpClient, fileUri, null, null, autoRetryOptions, mediaType, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, string? mediaType)
        => GetInstance(httpClient, fileUri, null, null, new AutoRetryOptions(), mediaType, null, null);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, string? mediaType, long? fileLength, AutoRetryOptions autoRetryOptions,
        DateTimeOffset? fileLastModifiedTime)
        => GetInstance(httpClient, fileUri, null, null, autoRetryOptions, mediaType, fileLength, fileLastModifiedTime);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, string? mediaType, long? fileLength, DateTimeOffset? fileLastModifiedTime)
        => GetInstance(httpClient, fileUri, null, null, new AutoRetryOptions(), mediaType, fileLength,
            fileLastModifiedTime);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? endBytes, AutoRetryOptions autoRetryOptions,
        long? fileLength, DateTimeOffset? fileLastModifiedTime)
        => GetInstance(httpClient, fileUri, startBytes, endBytes, autoRetryOptions, null, fileLength,
            fileLastModifiedTime);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? endBytes, long? fileLength,
        DateTimeOffset? fileLastModifiedTime)
        => GetInstance(httpClient, fileUri, startBytes, endBytes, new AutoRetryOptions(), null, fileLength,
            fileLastModifiedTime);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, AutoRetryOptions autoRetryOptions, long? fileLength,
        DateTimeOffset? fileLastModifiedTime)
        => GetInstance(httpClient, fileUri, startBytes, null, autoRetryOptions, null, fileLength, fileLastModifiedTime);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? fileLength, DateTimeOffset? fileLastModifiedTime)
        => GetInstance(httpClient, fileUri, null, null, new AutoRetryOptions(), null, fileLength, fileLastModifiedTime);

    public static SingleFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? endBytes, string? mediaType, long? fileLength,
        DateTimeOffset? fileLastModifiedTime)
        => GetInstance(httpClient, fileUri, startBytes, endBytes, new AutoRetryOptions(), mediaType, fileLength,
            fileLastModifiedTime);



    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, AutoRetryOptions autoRetryOptions,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, null, null, autoRetryOptions, null, null, null, cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, null, null, new AutoRetryOptions(), null, null, null,
            cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? endBytes, AutoRetryOptions autoRetryOptions,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, endBytes, autoRetryOptions,
            null, null, null, cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? endBytes,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, endBytes, new AutoRetryOptions(), null, null, null,
            cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, AutoRetryOptions autoRetryOptions,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, null, autoRetryOptions, null, null, null,
            cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, null, new AutoRetryOptions(), null, null, null,
            cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? endBytes, AutoRetryOptions autoRetryOptions,
        string? mediaType, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, endBytes, autoRetryOptions, mediaType, null, null,
            cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? endBytes, string? mediaType,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, endBytes, new AutoRetryOptions(), mediaType, null, null,
            cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, AutoRetryOptions autoRetryOptions, string? mediaType,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, null, autoRetryOptions, mediaType, null, null,
            cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, string? mediaType,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, null, new AutoRetryOptions(), mediaType, null, null,
            cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, AutoRetryOptions autoRetryOptions, string? mediaType,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, null, null, autoRetryOptions, mediaType, null, null,
            cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, string? mediaType, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, null, null, new AutoRetryOptions(), mediaType, null, null,
            cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, AutoRetryOptions autoRetryOptions, string? mediaType, long? fileLength,
        DateTimeOffset? fileLastModifiedTime, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, null, null, autoRetryOptions, mediaType, fileLength,
            fileLastModifiedTime, cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, string? mediaType, long? fileLength, DateTimeOffset? fileLastModifiedTime,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, null, null, new AutoRetryOptions(), mediaType, fileLength,
            fileLastModifiedTime, cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? endBytes, AutoRetryOptions autoRetryOptions,
        long? fileLength, DateTimeOffset? fileLastModifiedTime, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, endBytes, autoRetryOptions, null, fileLength,
            fileLastModifiedTime, cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? endBytes, long? fileLength,
        DateTimeOffset? fileLastModifiedTime, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, endBytes, new AutoRetryOptions(), null, fileLength,
            fileLastModifiedTime, cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, AutoRetryOptions autoRetryOptions, long? fileLength,
        DateTimeOffset? fileLastModifiedTime, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, null, autoRetryOptions, null, fileLength,
            fileLastModifiedTime, cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? fileLength, DateTimeOffset? fileLastModifiedTime,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, null, new AutoRetryOptions(), null, fileLength,
            fileLastModifiedTime, cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, AutoRetryOptions autoRetryOptions, long? fileLength,
        DateTimeOffset? fileLastModifiedTime, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, null, null, autoRetryOptions, null, fileLength, fileLastModifiedTime,
            cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? fileLength, DateTimeOffset? fileLastModifiedTime,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, null, null, new AutoRetryOptions(), null, fileLength,
            fileLastModifiedTime, cancellationToken);

    public static Task<SingleFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        HttpPartialDownloadStreamUri fileUri, long? startBytes, long? endBytes, string? mediaType, long? fileLength,
        DateTimeOffset? fileLastModifiedTime, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUri, startBytes, endBytes, new AutoRetryOptions(), mediaType, fileLength,
            fileLastModifiedTime, cancellationToken);

    #endregion
}