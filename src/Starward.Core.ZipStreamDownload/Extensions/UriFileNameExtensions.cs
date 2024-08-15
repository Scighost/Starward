using System.Web;

namespace Starward.Core.ZipStreamDownload.Extensions;

/// <summary>
/// <see cref="Uri"/>文件名扩展
/// </summary>
internal static class UriFileNameExtensions
{
    /// <summary>
    /// 获取URI中的文件名。
    /// </summary>
    /// <param name="uri">URI对象</param>
    /// <returns>URI中的文件名</returns>
    /// <exception cref="InvalidOperationException">此实例表示相对URI，此属性仅对绝对URI有效。</exception>
    public static string? GetFileName(this Uri uri)
    {
        var absolutePath = HttpUtility.UrlDecode(uri.AbsolutePath);
        var absolutePathLastSplit = absolutePath.LastIndexOf('/');
        if (absolutePathLastSplit == -1) return absolutePath;
        return absolutePathLastSplit == absolutePath.Length - 1 ? null : absolutePath[(absolutePathLastSplit + 1)..];
    }
}