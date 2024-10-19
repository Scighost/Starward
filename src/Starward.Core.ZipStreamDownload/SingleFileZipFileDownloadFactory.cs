using System.Diagnostics.CodeAnalysis;
using Starward.Core.ZipStreamDownload.Http;

namespace Starward.Core.ZipStreamDownload;

/// <summary>
/// 用于单文件下载的<see cref="ZipFileDownload"/>的工厂
/// </summary>
/// <param name="httpClient"><see cref="HttpClient"/>的实例，用于文件下载。</param>
public class SingleFileZipFileDownloadFactory(HttpClient httpClient) : IZipFileDownloadFactory
{
    /// <summary>
    /// ZIP文件URL。
    /// </summary>
    [StringSyntax(StringSyntaxAttribute.Uri)]
    public string? ZipFileUrl
    {
        get => _zipFileUri?.ToString();
        set => _zipFileUri = value == null ? null : new HttpPartialDownloadStreamUri(value, _httpPartialDnsResolve);
    }

    /// <summary>
    /// ZIP文件URL的URI对象。
    /// </summary>
    public Uri? ZipFileUri => _zipFileUri?.Uri;

    /// <summary>
    /// 获取或设置一个一个返回<see cref="RateLimiterOption"/>实例的委托，表示按字节下载限速的限速器的选项。
    /// </summary>
    public Func<RateLimiterOption>? DownloadBytesRateLimiterOptionBuilder { get; set; }

    /// <summary>
    /// 自动重试选项
    /// </summary>
    public AutoRetryOptions AutoRetryOptions { get; } = new();

    /// <summary>
    /// ZIP文件URL（内部，<see cref="HttpPartialDownloadStreamUri"/>类型）。
    /// </summary>
    private HttpPartialDownloadStreamUri? _zipFileUri;

    /// <summary>
    /// 一个<see cref="HttpPartialDnsResolve"/>的实例，用于DNS解析、IP地址测试和缓存。
    /// </summary>
    private readonly HttpPartialDnsResolve _httpPartialDnsResolve = new();

    /// <summary>
    /// 获取一个用于单文件下载的<see cref="ZipFileDownload"/>类的新实例。
    /// </summary>
    /// <returns><see cref="ZipFileDownload"/>的实例</returns>
    /// <exception cref="InvalidOperationException">当属性ZipFileUrl为空时引发此异常。</exception>
    public ZipFileDownload GetInstance()
        => new(async (startBytes, endBytes) =>
            await SingleFileHttpPartialDownloadStream.GetInstanceAsync(httpClient,
                _zipFileUri ?? throw new InvalidOperationException(), startBytes, endBytes, AutoRetryOptions,
                ZipFileDownload.MediaType).ConfigureAwait(false), DownloadBytesRateLimiterOptionBuilder);
}