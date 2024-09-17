using System.Net;

namespace Starward.Core.ZipStreamDownload.Http;

/// <summary>
/// HTTP部分下载流使用的自定义URI对象。
/// </summary>
/// <remarks>
/// 此类的作用是保证在第一次获取IP地址前将URL中的域名进行解析，并测试可用地址。
/// 确保在DNS返回多个地址的整个部分下载过程中，只使用相同IP地址进行下载。
/// 多进程下载时将请求轮询调度负载分担到DNS解析到的多个IP上。
/// </remarks>
/// <param name="uri">一个<see cref="Uri"/>的实例，表示一个URI地址。</param>
/// <param name="httpPartialDnsResolve">一个<see cref="HttpPartialDnsResolve"/>的实例，用于DNS解析、IP地址测试和缓存。</param>
public class HttpPartialDownloadStreamUri(Uri uri, HttpPartialDnsResolve httpPartialDnsResolve)
{
    /// <summary>
    /// 获取对象初始化时传入的<see cref="Uri"/>
    /// </summary>
    public Uri Uri { get; } = uri;

    /// <summary>
    /// 获取对象初始化时传入的<see cref="Uri"/>中的主机名
    /// </summary>
    public string Host { get; } = uri.Host;

    /// <summary>
    /// 获取<see cref="HttpPartialDnsResolve"/>的实例，用于DNS解析、IP地址测试和缓存
    /// </summary>
    public HttpPartialDnsResolve HttpPartialDnsResolve { get; } = httpPartialDnsResolve;

    /// <summary>
    /// 已经获取IP地址的总次数
    /// </summary>
    private ulong _ipAddressesRequestCount;

    /// <summary>
    /// 可用的IP地址（异步懒初始化）
    /// </summary>
    private readonly Lazy<Task<IPAddress[]>> _ipAddresses = new (async () =>
    {
        return uri.HostNameType switch
        {
            UriHostNameType.IPv4 or UriHostNameType.IPv6 => [IPAddress.Parse(uri.Host)],
            UriHostNameType.Dns => await httpPartialDnsResolve.GetIpAddressesAsync(uri.Host, uri.Port)
                .ConfigureAwait(false),
            _ => throw new ArgumentException()
        };
    });

    /// <summary>
    /// 获取一个IP地址作为Host的<see cref="Uri"/>（异步）
    /// </summary>
    /// <returns>一个任务，可获取一个IP地址作为Host的<see cref="Uri"/></returns>
    public async Task<Uri> GetIpAddressUriAsync()
    {
        var ipAddresses = await _ipAddresses.Value;
        var ipAddressesRequestCount = Interlocked.Increment(ref _ipAddressesRequestCount);
        var ipAddress = ipAddresses[(int)(ipAddressesRequestCount % (ulong)ipAddresses.Length)];
        return GetIpAddressUri(Uri, ipAddress);
    }

    /// <summary>
    /// 获取一个IP地址作为Host的<see cref="Uri"/>
    /// </summary>
    /// <returns>一个IP地址作为Host的<see cref="Uri"/></returns>
    public Uri GetIpAddressUri()
    {
        var ipAddresses = _ipAddresses.Value.GetAwaiter().GetResult();
        var ipAddressesRequestCount = Interlocked.Increment(ref _ipAddressesRequestCount);
        var ipAddress = ipAddresses[(int)(ipAddressesRequestCount % (ulong)ipAddresses.Length)];
        return GetIpAddressUri(Uri, ipAddress);
    }

    /// <summary>
    /// 获取指定<see cref="HttpPartialDownloadStreamUri"/>实例的规范字符串表示形式。
    /// </summary>
    /// <returns>Uri实例的未转义规范表示。除了#、?和%。</returns>
    public override string ToString() => Uri.ToString();

    public static implicit operator Uri(HttpPartialDownloadStreamUri uri) => uri.Uri;

    /// <summary>
    /// HTTP部分下载流使用的自定义URI对象。
    /// </summary>
    /// <remarks>
    /// 此类的作用是保证在第一次获取IP地址前将URL中的域名进行解析，并测试可用地址。
    /// 确保在DNS返回多个地址的整个部分下载过程中，只使用相同IP地址进行下载。
    /// 多进程下载时将请求轮询调度负载分担到DNS解析到的多个IP上。
    /// </remarks>
    /// <param name="uri">一个字符串，表示一个URI地址。</param>
    /// <param name="httpPartialDnsResolve">一个<see cref="HttpPartialDnsResolve"/>的实例，用于DNS解析、IP地址测试和缓存。</param>
    public HttpPartialDownloadStreamUri(string uri, HttpPartialDnsResolve httpPartialDnsResolve) :
        this(new Uri(uri), httpPartialDnsResolve)
    {

    }

    /// <summary>
    /// 对一个迭代器对象进行随机排序
    /// </summary>
    /// <param name="enumerable">一个迭代器的实例</param>
    /// <typeparam name="T">迭代器的类型</typeparam>
    /// <returns>一个已经打乱顺序的列表。</returns>
    private static List<T> ConfuseEnumerable<T>(IEnumerable<T> enumerable)
    {
        var random = new Random();
        return enumerable.OrderBy(_ => random.Next()).ToList();
    }

    /// <summary>
    /// 获取使用IP地址作为主机名的<see cref="Uri"/>的实例。
    /// </summary>
    /// <param name="uri"><see cref="Uri"/>的实例</param>
    /// <param name="ipAddress"><see cref="IPAddress"/>的实例</param>
    /// <returns><see cref="Uri"/>的实例。</returns>
    private static Uri GetIpAddressUri(Uri uri, IPAddress ipAddress)
    {
        var uriBuilder = new UriBuilder(uri)
        {
            Host = ipAddress.ToString()
        };
        return uriBuilder.Uri;
    }
}