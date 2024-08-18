using System.Net;
using System.Net.Sockets;

namespace Starward.Core.ZipStreamDownload.Http.Extensions;

public static class UriIpAddressExtensions
{
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
    /// 获取使用IP地址作为主机名的<see cref="Uri"/>的实例。（异步）
    /// </summary>
    /// <param name="uri"><see cref="Uri"/>的实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务，可以获取<see cref="Uri"/>的实例。</returns>
    public static async Task<Uri> GetIpAddressUriAsync(this Uri uri, CancellationToken cancellationToken = default)
    {
        var ipAddress = await GetIpAddressAsync(uri, cancellationToken).ConfigureAwait(false);
        var uriBuilder = new UriBuilder(uri)
        {
            Host = ipAddress.ToString()
        };
        return uriBuilder.Uri;
    }

    /// <summary>
    /// 取使用IP地址作为主机名的<see cref="Uri"/>的实例。
    /// </summary>
    /// <param name="uri"><see cref="Uri"/>的实例</param>
    /// <returns><see cref="Uri"/>的实例。</returns>
    public static Uri GetIpAddressUri(this Uri uri)
    {
        var ipAddress = GetIpAddress(uri);
        var uriBuilder = new UriBuilder(uri)
        {
            Host = ipAddress.ToString()
        };
        return uriBuilder.Uri;
    }

    /// <summary>
    /// 返回Uri主机名解析后的IP地址。（异步）
    /// </summary>
    /// <param name="uri"><see cref="Uri"/>的实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务，可获取一个<see cref="IPAddress"/>的实例。</returns>
    /// <remarks>如果为域名，解析后测试连接，并随机返回一个可用的IP地址，如果为IP地址则直接返回。</remarks>
    public static async Task<IPAddress> GetIpAddressAsync(this Uri uri, CancellationToken cancellationToken = default)
    {
        switch (uri.HostNameType)
        {
            case UriHostNameType.IPv4 or UriHostNameType.IPv6:
                return IPAddress.Parse(uri.Host);
            case UriHostNameType.Dns:
            {
                var addresses = new List<IPAddress>();
                try
                {
                    var addressesV6 = ConfuseEnumerable(
                        await Dns.GetHostAddressesAsync(uri.Host, AddressFamily.InterNetworkV6, cancellationToken)
                            .ConfigureAwait(false));
                    addresses.AddRange(addressesV6);
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.NoData)
                {
                }
                try
                {
                    var addressesV4 = ConfuseEnumerable(
                        await Dns.GetHostAddressesAsync(uri.Host, AddressFamily.InterNetwork, cancellationToken)
                            .ConfigureAwait(false));
                    addresses.AddRange(addressesV4);
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.NoData)
                {
                }
                IPAddress? result = null;
                Exception? lastException = null;
                foreach (var address in addresses)
                {
                    try
                    {
                        using var client = new TcpClient();
                        var endPoint = new IPEndPoint(address, uri.Port);
                        await client.ConnectAsync(endPoint, cancellationToken).ConfigureAwait(false);
                        result = address;
                        break;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        lastException = ex;
                    }
                }
                if (result == null) throw lastException ?? new SocketException((int)SocketError.HostNotFound);
                return result;
            }
            default:
                throw new ArgumentException();
        }
    }

    /// <summary>
    /// 返回Uri主机名解析后的IP地址。
    /// </summary>
    /// <param name="uri"><see cref="Uri"/>的实例</param>
    /// <returns>一个<see cref="IPAddress"/>的实例。</returns>
    /// <remarks>如果为域名，解析后测试连接，并随机返回一个可用的IP地址，如果为IP地址则直接返回。</remarks>
    public static IPAddress GetIpAddress(this Uri uri)
    {
        switch (uri.HostNameType)
        {
            case UriHostNameType.IPv4 or UriHostNameType.IPv6:
                return IPAddress.Parse(uri.Host);
            case UriHostNameType.Dns:
            {
                var addresses = new List<IPAddress>();
                try
                {
                    var addressesV6 = ConfuseEnumerable(Dns.GetHostAddresses(uri.Host, AddressFamily.InterNetworkV6));
                    addresses.AddRange(addressesV6);
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.NoData)
                {
                }
                try
                {
                    var addressesV4 = ConfuseEnumerable(Dns.GetHostAddresses(uri.Host, AddressFamily.InterNetwork));
                    addresses.AddRange(addressesV4);
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.NoData)
                {
                }
                IPAddress? result = null;
                Exception? lastException = null;
                foreach (var address in addresses)
                {
                    try
                    {
                        using var client = new TcpClient();
                        var endPoint = new IPEndPoint(address, uri.Port);
                        client.Connect(endPoint);
                        result = address;
                        break;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        lastException = ex;
                    }
                }
                if (result == null) throw lastException ?? new SocketException((int)SocketError.HostNotFound);
                return result;
            }
            default:
                throw new ArgumentException();
        }
    }
}