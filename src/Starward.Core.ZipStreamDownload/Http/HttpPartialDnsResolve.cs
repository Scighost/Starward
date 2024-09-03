using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Starward.Core.ZipStreamDownload.Http;

/// <summary>
/// DNS解析、IP地址测试和缓存
/// <remarks>
/// 此类的作用是保证多个<see cref="HttpPartialDownloadStreamUri"/>实例共享一个缓存池。
/// </remarks>
/// </summary>
public class HttpPartialDnsResolve
{
    /// <summary>
    /// DNS缓存（主机名与IP地址列表的线程安全字典）
    /// </summary>
    private readonly ConcurrentDictionary<string, Lazy<Task<IPAddress[]>>> _hostIpAddresses = new();

    /// <summary>
    /// 获取可用的IP地址列表（TCP连接测试，异步）
    /// </summary>
    /// <param name="host">主机名</param>
    /// <param name="port">用于测试的TCP端口号</param>
    /// <returns>一个任务，可用获取可用的IP地址列表</returns>
    public Task<IPAddress[]> GetIpAddressesAsync(string host, int port)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 0xffff);
        var iPAddress =
            _hostIpAddresses.GetOrAdd(host, h => new Lazy<Task<IPAddress[]>>(() => GetIpAddressAsyncCore(h, port)));
        return iPAddress.Value;
    }

    /// <summary>
    /// 获取可用的IP地址列表（TCP连接测试）
    /// </summary>
    /// <param name="host">主机名</param>
    /// <param name="port">用于测试的TCP端口号</param>
    /// <returns>可用的IP地址列表</returns>
    public IPAddress[] GetIpAddresses(string host, int port)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 0xffff);
        var iPAddress =
            _hostIpAddresses.GetOrAdd(host, h => new Lazy<Task<IPAddress[]>>(() => GetIpAddressAsyncCore(h, port)));
        return iPAddress.Value.GetAwaiter().GetResult();
    }

    /// <summary>
    /// 获取可用的IP地址列表（内部，无缓存）
    /// </summary>
    /// <param name="host">主机名</param>
    /// <param name="port">>用于测试的TCP端口号</param>
    /// <returns>可用的IP地址列表</returns>
    private static async Task<IPAddress[]> GetIpAddressAsyncCore(string host, int port)
    {
        Exception? lastException = null;
        List<IPAddress>? addresses = null;
        try
        {
            addresses = ConfuseEnumerable(
                await Dns.GetHostAddressesAsync(host, AddressFamily.InterNetworkV6)
                    .ConfigureAwait(false));
        }
        catch (SocketException e) when (e.SocketErrorCode == SocketError.NoData)
        {
        }
        if (addresses != null) return (await TestAddressesAsync(addresses).ConfigureAwait(false)).ToArray();
        try
        {
            addresses = ConfuseEnumerable(
                await Dns.GetHostAddressesAsync(host, AddressFamily.InterNetwork)
                    .ConfigureAwait(false));
        }
        catch (SocketException e) when (e.SocketErrorCode == SocketError.NoData)
        {
        }
        if (addresses != null) return (await TestAddressesAsync(addresses).ConfigureAwait(false)).ToArray();
        throw lastException ?? new SocketException((int)SocketError.HostNotFound);

        async Task<List<IPAddress>> TestAddressesAsync(List<IPAddress> testAddresses)
        {
            var result = new List<IPAddress>();
            foreach (var address in testAddresses)
            {
                try
                {
                    using var client = new TcpClient();
                    var endPoint = new IPEndPoint(address, port);
                    await client.ConnectAsync(endPoint).ConfigureAwait(false);
                    result.Add(address);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    lastException = ex;
                }
            }
            if (result.Count == 0) throw lastException ?? new SocketException((int)SocketError.HostNotFound);
            return result;
        }
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
}