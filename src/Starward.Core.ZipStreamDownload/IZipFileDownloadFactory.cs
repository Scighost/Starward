using Starward.Core.ZipStreamDownload.Http;

namespace Starward.Core.ZipStreamDownload;

/// <summary>
/// ZIP文件下载对象<see cref="ZipFileDownload"/>的工厂。
/// </summary>
public interface IZipFileDownloadFactory
{
    /// <summary>
    /// ZIP文件URL的URI对象。
    /// </summary>
    /// <remarks>用于FastZipStreamDownload获取ZIP文件名</remarks>
    public Uri? ZipFileUri { get; }

    /// <summary>
    /// 获取<see cref="ZipFileDownload"/>的新实例。
    /// </summary>
    /// <returns><see cref="ZipFileDownload"/>的实例</returns>
    ZipFileDownload GetInstance();

    /// <summary>
    /// 获取或设置一个一个返回<see cref="RateLimiterOption"/>实例的委托，表示按字节下载限速的限速器的选项。
    /// </summary>
    public Func<RateLimiterOption>? DownloadBytesRateLimiterOptionBuilder { get; set; }
}