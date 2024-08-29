using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace Starward.Core.ZipStreamDownload.Http.Extensions;

/// <summary>
/// <see cref="HttpClient"/>的分段下载扩展。
/// </summary>
internal static class HttpClientGetPartialExtensions
{
    /// <summary>
    /// 创建一个HTTP分段下载请求消息。
    /// </summary>
    /// <param name="httpClient"><see cref="HttpClient"/>的实例。</param>
    /// <param name="requestUri">要发送请求的Uri。</param>
    /// <param name="rangeHeaderValue"><see cref="RangeHeaderValue"/>的实例，代表分段下载请求头的值。</param>
    /// <returns><see cref="HttpRequestMessage"/>的实例，表示一个HTTP分段下载请求消息。</returns>
    /// <exception cref="ArgumentNullException">请求为空。</exception>
    /// <exception cref="NotSupportedException">
    /// HTTP版本为2.0或更高版本，或者版本策略设置为<see cref="HttpVersionPolicy.RequestVersionOrHigher"/>。
    /// -或者-
    /// 从<see cref="HttpContent"/>派生的自定义类不会重写
    /// <see cref="HttpContent.SerializeToStream(Stream,System.Net.TransportContext?,CancellationToken)"/>方法。
    /// -或者-
    /// 自定义<see cref="HttpMessageHandler"/>不会覆盖
    /// <see cref="HttpMessageHandler.Send(HttpRequestMessage,CancellationToken)"/>方法。
    /// </exception>
    /// <exception cref="InvalidOperationException">请求消息已由<see cref="HttpClient"/>实例发送。</exception>
    /// <exception cref="HttpRequestException">由于网络连接、DNS故障或服务器证书验证等潜在问题，请求失败。</exception>
    /// <exception cref="TaskCanceledException">
    /// 如果<see cref="TaskCanceledException"/>异常嵌套了<see cref="TimeoutException"/>：请求因超时而失败。
    /// </exception>
    private static HttpRequestMessage CreatePartialHttpRequestMessage
        (HttpClient httpClient, Uri requestUri, RangeHeaderValue rangeHeaderValue)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
            Version = httpClient.DefaultRequestVersion,
            VersionPolicy = httpClient.DefaultVersionPolicy
        };
        request.Headers.Connection.Add("Keep-Alive");
        request.Headers.Range = rangeHeaderValue;
        return request;
    }

    /// <summary>
    /// 发起一个HTTP分段下载请求。
    /// </summary>
    /// <param name="httpClient"><see cref="HttpClient"/>的实例。</param>
    /// <param name="requestUri">要发送请求的Uri。</param>
    /// <param name="from">
    /// 分段下载的开始字节索引（包含），如果此值为空，则形参<paramref name="to"/>代表获取最后多少个字节的数据。
    /// </param>
    /// <param name="to">
    /// 分段下载结束字节索引（包含），如果此值为空，则获取从形参<paramref name="from"/>所代表的字节索引到最后一个字节的数据。
    /// </param>
    /// <param name="acceptType">接受的文件MIME类型，如果设置了此值，将在请求时加入请求头。</param>
    /// <returns><see cref="HttpResponseMessage"/>的实例，表示HTTP分段下载的响应消息。</returns>
    /// <exception cref="ArgumentNullException">请求为空。</exception>
    /// <exception cref="NotSupportedException">
    /// HTTP版本为2.0或更高版本，或者版本策略设置为<see cref="HttpVersionPolicy.RequestVersionOrHigher"/>。
    /// -或者-
    /// 从<see cref="HttpContent"/>派生的自定义类不会重写
    /// <see cref="HttpContent.SerializeToStream(Stream,System.Net.TransportContext?,CancellationToken)"/>方法。
    /// -或者-
    /// 自定义<see cref="HttpMessageHandler"/>不会覆盖
    /// <see cref="HttpMessageHandler.Send(HttpRequestMessage,CancellationToken)"/>方法。
    /// </exception>
    /// <exception cref="InvalidOperationException">请求消息已由<see cref="HttpClient"/>实例发送。</exception>
    /// <exception cref="HttpRequestException">由于网络连接、DNS故障或服务器证书验证等潜在问题，请求失败。</exception>
    /// <exception cref="TaskCanceledException">
    /// 如果<see cref="TaskCanceledException"/>异常嵌套了<see cref="TimeoutException"/>：请求因超时而失败。
    /// </exception>
    public static HttpResponseMessage GetPartial(this HttpClient httpClient,
        Uri requestUri, long? from, long? to, string? acceptType)
    {
        return GetPartial(httpClient, requestUri, null, from, to, acceptType);
    }

    /// <summary>
    /// 发起一个HTTP分段下载请求。
    /// </summary>
    /// <param name="httpClient"><see cref="HttpClient"/>的实例。</param>
    /// <param name="requestUri">要发送请求的Uri。</param>
    /// <param name="hostName">如果传入，覆盖Uri中的主机名。</param>
    /// <param name="from">
    /// 分段下载的开始字节索引（包含），如果此值为空，则形参<paramref name="to"/>代表获取最后多少个字节的数据。
    /// </param>
    /// <param name="to">
    /// 分段下载结束字节索引（包含），如果此值为空，则获取从形参<paramref name="from"/>所代表的字节索引到最后一个字节的数据。
    /// </param>
    /// <param name="acceptType">接受的文件MIME类型，如果设置了此值，将在请求时加入请求头。</param>
    /// <returns><see cref="HttpResponseMessage"/>的实例，表示HTTP分段下载的响应消息。</returns>
    /// <exception cref="ArgumentNullException">请求为空。</exception>
    /// <exception cref="NotSupportedException">
    /// HTTP版本为2.0或更高版本，或者版本策略设置为<see cref="HttpVersionPolicy.RequestVersionOrHigher"/>。
    /// -或者-
    /// 从<see cref="HttpContent"/>派生的自定义类不会重写
    /// <see cref="HttpContent.SerializeToStream(Stream,System.Net.TransportContext?,CancellationToken)"/>方法。
    /// -或者-
    /// 自定义<see cref="HttpMessageHandler"/>不会覆盖
    /// <see cref="HttpMessageHandler.Send(HttpRequestMessage,CancellationToken)"/>方法。
    /// </exception>
    /// <exception cref="InvalidOperationException">请求消息已由<see cref="HttpClient"/>实例发送。</exception>
    /// <exception cref="HttpRequestException">由于网络连接、DNS故障或服务器证书验证等潜在问题，请求失败。</exception>
    /// <exception cref="TaskCanceledException">
    /// 如果<see cref="TaskCanceledException"/>异常嵌套了<see cref="TimeoutException"/>：请求因超时而失败。
    /// </exception>
    public static HttpResponseMessage GetPartial(this HttpClient httpClient,
        Uri requestUri, string? hostName, long? from, long? to, string? acceptType)
    {
        return GetPartial(httpClient, requestUri, hostName, new RangeHeaderValue(from, to), acceptType);
    }

    /// <summary>
    /// 发起一个HTTP分段下载请求（异步）。
    /// </summary>
    /// <param name="httpClient"><see cref="HttpClient"/>的实例。</param>
    /// <param name="requestUri">要发送请求的Uri。</param>
    /// <param name="from">
    /// 分段下载的开始字节索引（包含），如果此值为空，则形参<paramref name="to"/>代表获取最后多少个字节的数据。
    /// </param>
    /// <param name="to">
    /// 分段下载结束字节索引（包含），如果此值为空，则获取从形参<paramref name="from"/>所代表的字节索引到最后一个字节的数据。
    /// </param>
    /// <param name="acceptType">接受的文件MIME类型，如果设置了此值，将在请求时加入请求头。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>返回一个任务，可获取一个<see cref="HttpResponseMessage"/>的实例，表示HTTP分段下载的响应消息。</returns>
    /// <exception cref="ArgumentNullException">请求为空。</exception>
    /// <exception cref="NotSupportedException">
    /// HTTP版本为2.0或更高版本，或者版本策略设置为<see cref="HttpVersionPolicy.RequestVersionOrHigher"/>。
    /// -或者-
    /// 从<see cref="HttpContent"/>派生的自定义类不会重写
    /// <see cref="HttpContent.SerializeToStream(Stream,System.Net.TransportContext?,CancellationToken)"/>方法。
    /// -或者-
    /// 自定义<see cref="HttpMessageHandler"/>不会覆盖
    /// <see cref="HttpMessageHandler.Send(HttpRequestMessage,CancellationToken)"/>方法。
    /// </exception>
    /// <exception cref="InvalidOperationException">请求消息已由<see cref="HttpClient"/>实例发送。</exception>
    /// <exception cref="HttpRequestException">由于网络连接、DNS故障或服务器证书验证等潜在问题，请求失败。</exception>
    /// <exception cref="TaskCanceledException">
    /// 如果<see cref="TaskCanceledException"/>异常嵌套了<see cref="TimeoutException"/>：请求因超时而失败。
    /// </exception>
    public static Task<HttpResponseMessage> GetPartialAsync(this HttpClient httpClient,
        Uri requestUri, long? from, long? to, string? acceptType,
        CancellationToken cancellationToken = default)
    {
        return GetPartialAsync(httpClient, requestUri, null, from, to, acceptType, cancellationToken);
    }

    /// <summary>
    /// 发起一个HTTP分段下载请求（异步）。
    /// </summary>
    /// <param name="httpClient"><see cref="HttpClient"/>的实例。</param>
    /// <param name="requestUri">要发送请求的Uri。</param>
    /// <param name="hostName">如果传入，覆盖Uri中的主机名。</param>
    /// <param name="from">
    /// 分段下载的开始字节索引（包含），如果此值为空，则形参<paramref name="to"/>代表获取最后多少个字节的数据。
    /// </param>
    /// <param name="to">
    /// 分段下载结束字节索引（包含），如果此值为空，则获取从形参<paramref name="from"/>所代表的字节索引到最后一个字节的数据。
    /// </param>
    /// <param name="acceptType">接受的文件MIME类型，如果设置了此值，将在请求时加入请求头。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>返回一个任务，可获取一个<see cref="HttpResponseMessage"/>的实例，表示HTTP分段下载的响应消息。</returns>
    /// <exception cref="ArgumentNullException">请求为空。</exception>
    /// <exception cref="NotSupportedException">
    /// HTTP版本为2.0或更高版本，或者版本策略设置为<see cref="HttpVersionPolicy.RequestVersionOrHigher"/>。
    /// -或者-
    /// 从<see cref="HttpContent"/>派生的自定义类不会重写
    /// <see cref="HttpContent.SerializeToStream(Stream,System.Net.TransportContext?,CancellationToken)"/>方法。
    /// -或者-
    /// 自定义<see cref="HttpMessageHandler"/>不会覆盖
    /// <see cref="HttpMessageHandler.Send(HttpRequestMessage,CancellationToken)"/>方法。
    /// </exception>
    /// <exception cref="InvalidOperationException">请求消息已由<see cref="HttpClient"/>实例发送。</exception>
    /// <exception cref="HttpRequestException">由于网络连接、DNS故障或服务器证书验证等潜在问题，请求失败。</exception>
    /// <exception cref="TaskCanceledException">
    /// 如果<see cref="TaskCanceledException"/>异常嵌套了<see cref="TimeoutException"/>：请求因超时而失败。
    /// </exception>
    public static Task<HttpResponseMessage> GetPartialAsync(this HttpClient httpClient,
        Uri requestUri, string? hostName, long? from, long? to, string? acceptType,
        CancellationToken cancellationToken = default)
    {
        return GetPartialAsync(httpClient, requestUri, hostName, new RangeHeaderValue(from, to), acceptType,
            cancellationToken);
    }

    /// <summary>
    /// 发起一个HTTP分段下载请求。
    /// </summary>
    /// <param name="httpClient"><see cref="HttpClient"/>的实例。</param>
    /// <param name="requestUri">要发送请求的Uri。</param>
    /// <param name="hostName">如果传入，覆盖Uri中的主机名。</param>
    /// <param name="rangeHeaderValue"><see cref="RangeHeaderValue"/>的实例，代表分段下载请求头的值。</param>
    /// <param name="acceptType">接受的文件MIME类型，如果设置了此值，将在请求时加入请求头。</param>
    /// <returns><see cref="HttpResponseMessage"/>的实例，表示HTTP分段下载的响应消息。</returns>
    /// <exception cref="ArgumentNullException">请求为空。</exception>
    /// <exception cref="NotSupportedException">
    /// HTTP版本为2.0或更高版本，或者版本策略设置为<see cref="HttpVersionPolicy.RequestVersionOrHigher"/>。
    /// -或者-
    /// 从<see cref="HttpContent"/>派生的自定义类不会重写
    /// <see cref="HttpContent.SerializeToStream(Stream,System.Net.TransportContext?,CancellationToken)"/>方法。
    /// -或者-
    /// 自定义<see cref="HttpMessageHandler"/>不会覆盖
    /// <see cref="HttpMessageHandler.Send(HttpRequestMessage,CancellationToken)"/>方法。
    /// </exception>
    /// <exception cref="InvalidOperationException">请求消息已由<see cref="HttpClient"/>实例发送。</exception>
    /// <exception cref="HttpRequestException">由于网络连接、DNS故障或服务器证书验证等潜在问题，请求失败。</exception>
    /// <exception cref="TaskCanceledException">
    /// 如果<see cref="TaskCanceledException"/>异常嵌套了<see cref="TimeoutException"/>：请求因超时而失败。
    /// </exception>
    public static HttpResponseMessage GetPartial(this HttpClient httpClient,
        Uri requestUri, string? hostName, RangeHeaderValue rangeHeaderValue, string? acceptType)
    {
        var request = CreatePartialHttpRequestMessage(httpClient, requestUri, rangeHeaderValue);

        if (!string.IsNullOrEmpty(acceptType))
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptType));
        if (!string.IsNullOrEmpty(hostName)) request.Headers.Host = hostName;

        return httpClient.Send(request, HttpCompletionOption.ResponseHeadersRead);
    }

    /// <summary>
    /// 发起一个HTTP分段下载请求（异步）。
    /// </summary>
    /// <param name="httpClient"><see cref="HttpClient"/>的实例。</param>
    /// <param name="requestUri">要发送请求的Uri。</param>
    /// <param name="hostName">如果传入，覆盖Uri中的主机名。</param>
    /// <param name="rangeHeaderValue"><see cref="RangeHeaderValue"/>的实例，代表分段下载请求头的值。</param>
    /// <param name="acceptType">接受的文件MIME类型，如果设置了此值，将在请求时加入请求头。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>返回一个任务，可获取一个<see cref="HttpResponseMessage"/>的实例，表示HTTP分段下载的响应消息。</returns>
    /// <exception cref="ArgumentNullException">请求为空。</exception>
    /// <exception cref="InvalidOperationException">请求消息已由<see cref="HttpClient"/>实例发送。</exception>
    /// <exception cref="HttpRequestException">由于网络连接、DNS故障或服务器证书验证等潜在问题，请求失败。</exception>
    /// <exception cref="TaskCanceledException">
    /// 如果<see cref="TaskCanceledException"/>异常嵌套了<see cref="TimeoutException"/>：请求因超时而失败。
    /// </exception>
    public static Task<HttpResponseMessage> GetPartialAsync(this HttpClient httpClient,
        Uri requestUri, string? hostName, RangeHeaderValue rangeHeaderValue, string? acceptType,
        CancellationToken cancellationToken = default)
    {
        var request = CreatePartialHttpRequestMessage(httpClient, requestUri, rangeHeaderValue);

        if (!string.IsNullOrEmpty(acceptType))
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptType));
        if (!string.IsNullOrEmpty(hostName)) request.Headers.Host = hostName;

        return httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    /// <summary>
    /// 发起一个HTTP分段下载请求。
    /// </summary>
    /// <param name="httpClient"><see cref="HttpClient"/>的实例。</param>
    /// <param name="requestUri">要发送请求的Uri。</param>
    /// <param name="from">
    /// 分段下载的开始字节索引（包含），如果此值为空，则形参<paramref name="to"/>代表获取最后多少个字节的数据。
    /// </param>
    /// <param name="to">
    /// 分段下载结束字节索引（包含），如果此值为空，则获取从形参<paramref name="from"/>所代表的字节索引到最后一个字节的数据。
    /// </param>
    /// <param name="acceptType">接受的文件MIME类型，如果设置了此值，将在请求时加入请求头。</param>
    /// <returns><see cref="HttpResponseMessage"/>的实例，表示HTTP分段下载的响应消息。</returns>
    /// <exception cref="ArgumentNullException">请求为空。</exception>
    /// <exception cref="NotSupportedException">
    /// HTTP版本为2.0或更高版本，或者版本策略设置为<see cref="HttpVersionPolicy.RequestVersionOrHigher"/>。
    /// -或者-
    /// 从<see cref="HttpContent"/>派生的自定义类不会重写
    /// <see cref="HttpContent.SerializeToStream(Stream,System.Net.TransportContext?,CancellationToken)"/>方法。
    /// -或者-
    /// 自定义<see cref="HttpMessageHandler"/>不会覆盖
    /// <see cref="HttpMessageHandler.Send(HttpRequestMessage,CancellationToken)"/>方法。
    /// </exception>
    /// <exception cref="InvalidOperationException">请求消息已由<see cref="HttpClient"/>实例发送。</exception>
    /// <exception cref="HttpRequestException">由于网络连接、DNS故障或服务器证书验证等潜在问题，请求失败。</exception>
    /// <exception cref="TaskCanceledException">
    /// 如果<see cref="TaskCanceledException"/>异常嵌套了<see cref="TimeoutException"/>：请求因超时而失败。
    /// </exception>
    public static HttpResponseMessage GetPartial(this HttpClient httpClient,
        [StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, long? from, long? to, string? acceptType) =>
        GetPartial(httpClient, new Uri(requestUri), from, to, acceptType);

    /// <summary>
    /// 发起一个HTTP分段下载请求（异步）。
    /// </summary>
    /// <param name="httpClient"><see cref="HttpClient"/>的实例。</param>
    /// <param name="requestUri">要发送请求的Uri。</param>
    /// <param name="from">
    /// 分段下载的开始字节索引（包含），如果此值为空，则形参<paramref name="to"/>代表获取最后多少个字节的数据。
    /// </param>
    /// <param name="to">
    /// 分段下载结束字节索引（包含），如果此值为空，则获取从形参<paramref name="from"/>所代表的字节索引到最后一个字节的数据。
    /// </param>
    /// <param name="acceptType">接受的文件MIME类型，如果设置了此值，将在请求时加入请求头。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>返回一个任务，可获取一个<see cref="HttpResponseMessage"/>的实例，表示HTTP分段下载的响应消息。</returns>
    /// <exception cref="ArgumentNullException">请求为空。</exception>
    /// <exception cref="NotSupportedException">
    /// HTTP版本为2.0或更高版本，或者版本策略设置为<see cref="HttpVersionPolicy.RequestVersionOrHigher"/>。
    /// -或者-
    /// 从<see cref="HttpContent"/>派生的自定义类不会重写
    /// <see cref="HttpContent.SerializeToStream(Stream,System.Net.TransportContext?,CancellationToken)"/>方法。
    /// -或者-
    /// 自定义<see cref="HttpMessageHandler"/>不会覆盖
    /// <see cref="HttpMessageHandler.Send(HttpRequestMessage,CancellationToken)"/>方法。
    /// </exception>
    /// <exception cref="InvalidOperationException">请求消息已由<see cref="HttpClient"/>实例发送。</exception>
    /// <exception cref="HttpRequestException">由于网络连接、DNS故障或服务器证书验证等潜在问题，请求失败。</exception>
    /// <exception cref="TaskCanceledException">
    /// 如果<see cref="TaskCanceledException"/>异常嵌套了<see cref="TimeoutException"/>：请求因超时而失败。
    /// </exception>
    public static Task<HttpResponseMessage> GetPartialAsync(this HttpClient httpClient,
        [StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, long? from, long? to, string? acceptType,
        CancellationToken cancellationToken = default) =>
        GetPartialAsync(httpClient, new Uri(requestUri), from, to, acceptType, cancellationToken);

    /// <summary>
    /// 发起一个HTTP分段下载请求。
    /// </summary>
    /// <param name="httpClient"><see cref="HttpClient"/>的实例。</param>
    /// <param name="requestUri">要发送请求的Uri。</param>
    /// <param name="rangeHeaderValue"><see cref="RangeHeaderValue"/>的实例，代表分段下载请求头的值。</param>
    /// <param name="acceptType">接受的文件MIME类型，如果设置了此值，将在请求时加入请求头。</param>
    /// <returns><see cref="HttpResponseMessage"/>的实例，表示HTTP分段下载的响应消息。</returns>
    /// <exception cref="ArgumentNullException">请求为空。</exception>
    /// <exception cref="NotSupportedException">
    /// HTTP版本为2.0或更高版本，或者版本策略设置为<see cref="HttpVersionPolicy.RequestVersionOrHigher"/>。
    /// -或者-
    /// 从<see cref="HttpContent"/>派生的自定义类不会重写
    /// <see cref="HttpContent.SerializeToStream(Stream,System.Net.TransportContext?,CancellationToken)"/>方法。
    /// -或者-
    /// 自定义<see cref="HttpMessageHandler"/>不会覆盖
    /// <see cref="HttpMessageHandler.Send(HttpRequestMessage,CancellationToken)"/>方法。
    /// </exception>
    /// <exception cref="InvalidOperationException">请求消息已由<see cref="HttpClient"/>实例发送。</exception>
    /// <exception cref="HttpRequestException">由于网络连接、DNS故障或服务器证书验证等潜在问题，请求失败。</exception>
    /// <exception cref="TaskCanceledException">
    /// 如果<see cref="TaskCanceledException"/>异常嵌套了<see cref="TimeoutException"/>：请求因超时而失败。
    /// </exception>
    public static HttpResponseMessage GetPartial(this HttpClient httpClient,
        [StringSyntax(StringSyntaxAttribute.Uri)] string requestUri,
        RangeHeaderValue rangeHeaderValue, string? acceptType) =>
        GetPartial(httpClient, new Uri(requestUri), null, rangeHeaderValue, acceptType);

    /// <summary>
    /// 发起一个HTTP分段下载请求。
    /// </summary>
    /// <param name="httpClient"><see cref="HttpClient"/>的实例。</param>
    /// <param name="requestUri">要发送请求的Uri。</param>
    /// <param name="hostName">如果传入，覆盖Uri中的主机名。</param>
    /// <param name="rangeHeaderValue"><see cref="RangeHeaderValue"/>的实例，代表分段下载请求头的值。</param>
    /// <param name="acceptType">接受的文件MIME类型，如果设置了此值，将在请求时加入请求头。</param>
    /// <returns><see cref="HttpResponseMessage"/>的实例，表示HTTP分段下载的响应消息。</returns>
    /// <exception cref="ArgumentNullException">请求为空。</exception>
    /// <exception cref="NotSupportedException">
    /// HTTP版本为2.0或更高版本，或者版本策略设置为<see cref="HttpVersionPolicy.RequestVersionOrHigher"/>。
    /// -或者-
    /// 从<see cref="HttpContent"/>派生的自定义类不会重写
    /// <see cref="HttpContent.SerializeToStream(Stream,System.Net.TransportContext?,CancellationToken)"/>方法。
    /// -或者-
    /// 自定义<see cref="HttpMessageHandler"/>不会覆盖
    /// <see cref="HttpMessageHandler.Send(HttpRequestMessage,CancellationToken)"/>方法。
    /// </exception>
    /// <exception cref="InvalidOperationException">请求消息已由<see cref="HttpClient"/>实例发送。</exception>
    /// <exception cref="HttpRequestException">由于网络连接、DNS故障或服务器证书验证等潜在问题，请求失败。</exception>
    /// <exception cref="TaskCanceledException">
    /// 如果<see cref="TaskCanceledException"/>异常嵌套了<see cref="TimeoutException"/>：请求因超时而失败。
    /// </exception>
    public static HttpResponseMessage GetPartial(this HttpClient httpClient,
        [StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, string hostName,
        RangeHeaderValue rangeHeaderValue, string? acceptType) =>
        GetPartial(httpClient, new Uri(requestUri), hostName, rangeHeaderValue, acceptType);


    /// <summary>
    /// 发起一个HTTP分段下载请求（异步）。
    /// </summary>
    /// <param name="httpClient"><see cref="HttpClient"/>的实例。</param>
    /// <param name="requestUri">要发送请求的Uri。</param>
    /// <param name="rangeHeaderValue"><see cref="RangeHeaderValue"/>的实例，代表分段下载请求头的值。</param>
    /// <param name="acceptType">接受的文件MIME类型，如果设置了此值，将在请求时加入请求头。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>返回一个任务，可获取一个<see cref="HttpResponseMessage"/>的实例，表示HTTP分段下载的响应消息。</returns>
    /// <exception cref="ArgumentNullException">请求为空。</exception>
    /// <exception cref="NotSupportedException">
    /// HTTP版本为2.0或更高版本，或者版本策略设置为<see cref="HttpVersionPolicy.RequestVersionOrHigher"/>。
    /// -或者-
    /// 从<see cref="HttpContent"/>派生的自定义类不会重写
    /// <see cref="HttpContent.SerializeToStream(Stream,System.Net.TransportContext?,CancellationToken)"/>方法。
    /// -或者-
    /// 自定义<see cref="HttpMessageHandler"/>不会覆盖
    /// <see cref="HttpMessageHandler.Send(HttpRequestMessage,CancellationToken)"/>方法。
    /// </exception>
    /// <exception cref="InvalidOperationException">请求消息已由<see cref="HttpClient"/>实例发送。</exception>
    /// <exception cref="HttpRequestException">由于网络连接、DNS故障或服务器证书验证等潜在问题，请求失败。</exception>
    /// <exception cref="TaskCanceledException">
    /// 如果<see cref="TaskCanceledException"/>异常嵌套了<see cref="TimeoutException"/>：请求因超时而失败。
    /// </exception>
    public static Task<HttpResponseMessage> GetPartialAsync(this HttpClient httpClient,
        [StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, RangeHeaderValue rangeHeaderValue,
        string? acceptType, CancellationToken cancellationToken = default) =>
        GetPartialAsync(httpClient, requestUri, null, rangeHeaderValue, acceptType, cancellationToken);

    /// <summary>
    /// 发起一个HTTP分段下载请求（异步）。
    /// </summary>
    /// <param name="httpClient"><see cref="HttpClient"/>的实例。</param>
    /// <param name="requestUri">要发送请求的Uri。</param>
    /// <param name="hostName">如果传入，覆盖Uri中的主机名。</param>
    /// <param name="rangeHeaderValue"><see cref="RangeHeaderValue"/>的实例，代表分段下载请求头的值。</param>
    /// <param name="acceptType">接受的文件MIME类型，如果设置了此值，将在请求时加入请求头。</param>
    /// <param name="cancellationToken">取消操作的令牌。</param>
    /// <returns>返回一个任务，可获取一个<see cref="HttpResponseMessage"/>的实例，表示HTTP分段下载的响应消息。</returns>
    /// <exception cref="ArgumentNullException">请求为空。</exception>
    /// <exception cref="NotSupportedException">
    /// HTTP版本为2.0或更高版本，或者版本策略设置为<see cref="HttpVersionPolicy.RequestVersionOrHigher"/>。
    /// -或者-
    /// 从<see cref="HttpContent"/>派生的自定义类不会重写
    /// <see cref="HttpContent.SerializeToStream(Stream,System.Net.TransportContext?,CancellationToken)"/>方法。
    /// -或者-
    /// 自定义<see cref="HttpMessageHandler"/>不会覆盖
    /// <see cref="HttpMessageHandler.Send(HttpRequestMessage,CancellationToken)"/>方法。
    /// </exception>
    /// <exception cref="InvalidOperationException">请求消息已由<see cref="HttpClient"/>实例发送。</exception>
    /// <exception cref="HttpRequestException">由于网络连接、DNS故障或服务器证书验证等潜在问题，请求失败。</exception>
    /// <exception cref="TaskCanceledException">
    /// 如果<see cref="TaskCanceledException"/>异常嵌套了<see cref="TimeoutException"/>：请求因超时而失败。
    /// </exception>
    public static Task<HttpResponseMessage> GetPartialAsync(this HttpClient httpClient,
        [StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, string? hostName,
        RangeHeaderValue rangeHeaderValue, string? acceptType, CancellationToken cancellationToken = default) =>
        GetPartialAsync(httpClient, new Uri(requestUri), hostName, rangeHeaderValue, acceptType, cancellationToken);
}