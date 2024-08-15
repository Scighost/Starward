using Starward.Core.ZipStreamDownload.Http;

namespace Starward.Core.ZipStreamDownload;

/// <summary>
/// 用于分卷件下载的<see cref="ZipFileDownload"/>的工厂
/// </summary>
/// <param name="httpClient"><see cref="HttpClient"/>的实例，用于文件下载。</param>
public class VolumesFileZipFileDownloadFactory(HttpClient httpClient) : IZipFileDownloadFactory
{
    /// <summary>
    /// 分卷ZIP文件的URL列表。
    /// </summary>
    /// <remarks>必须按照分卷顺序传入。</remarks>
    public List<string>? ZipFileUrlList
    {
        get => ZipFileUriList?.Select(u => u.ToString()).ToList();
        set => ZipFileUriList = value?.Select(u => new Uri(u)).ToList();
    }

    /// <summary>
    /// 分卷ZIP文件的URL的URI对象的列表。
    /// </summary>
    /// <remarks>必须按照分卷顺序传入。</remarks>
    public List<Uri>? ZipFileUriList { get; set; }

    /// <summary>
    /// ZIP文件URL的URI对象。
    /// </summary>
    /// <remarks>用于FastZipStreamDownload获取ZIP文件名</remarks>
    public Uri? ZipFileUri
    {
        get
        {
            var url = ZipFileUriList?.FirstOrDefault()?.ToString();
            if (url == null) return null;
            var querySplit = url.IndexOf('?'); //去除Query。
            var query = "";
            if (querySplit >= 0)
            {
                query = url[querySplit..];
                url = url[..querySplit];
            }
            if (url.EndsWith(".001")) url = url[..^4]; //删除.001后缀
            return new Uri(url + query);
        }
    }

    /// <summary>
    /// 获取或设置一个一个返回<see cref="RateLimiterOption"/>实例的委托，表示按字节下载限速的限速器的选项。
    /// </summary>
    public Func<RateLimiterOption>? DownloadBytesRateLimiterOptionBuilder { get; set; }

    /// <summary>
    /// 获取一个用于分卷文件下载的<see cref="ZipFileDownload"/>类的新实例。
    /// </summary>
    /// <returns><see cref="ZipFileDownload"/>的实例</returns>
    /// <exception cref="InvalidOperationException">当属性ZipFileUriList为空时引发此异常。</exception>
    public ZipFileDownload GetInstance()
        => new(async (startBytes, endBytes) =>
            await VolumesFileHttpPartialDownloadStream.GetInstanceAsync(httpClient,
                ZipFileUriList ?? throw new InvalidOperationException(), startBytes, endBytes,
                ZipFileDownload.MediaType).ConfigureAwait(false), DownloadBytesRateLimiterOptionBuilder);
}