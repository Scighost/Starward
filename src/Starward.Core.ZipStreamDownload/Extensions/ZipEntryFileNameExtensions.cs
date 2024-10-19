using ICSharpCode.SharpZipLib.Zip;

namespace Starward.Core.ZipStreamDownload.Extensions;

/// <summary>
/// <see cref="ZipEntry"/>文件名称扩展
/// </summary>
internal static class ZipEntryFileNameExtensions
{
    /// <summary>
    /// 获取文件名称。
    /// </summary>
    /// <param name="entry"><see cref="ZipEntry"/>的实例</param>
    /// <returns>文件名称的字符串</returns>
    /// <exception cref="InvalidOperationException">形参<paramref name="entry"/>不是文件类型的实体。</exception>
    public static string? GetFileName(this ZipEntry entry)
    {
        if (!entry.IsFile) throw new InvalidOperationException();
        var cleanName = ZipEntry.CleanName(entry.Name);
        var absolutePathLastSplit = cleanName.LastIndexOf('/');
        if (absolutePathLastSplit == -1) return cleanName;
        return absolutePathLastSplit == cleanName.Length - 1 ? null : cleanName[(absolutePathLastSplit + 1)..];
    }

    /// <summary>
    /// 获取文件目录。
    /// </summary>
    /// <param name="entry"><see cref="ZipEntry"/>的实例</param>
    /// <returns>文件名称的字符串</returns>
    /// <exception cref="InvalidOperationException">形参<paramref name="entry"/>不是文件类型的实体。</exception>
    public static string? GetFileDirectory(this ZipEntry entry)
    {
        if (!entry.IsFile) throw new InvalidOperationException();
        var cleanName = ZipEntry.CleanName(entry.Name);
        var absolutePathLastSplit = cleanName.LastIndexOf('/');
        return absolutePathLastSplit == -1 ? null : cleanName[..absolutePathLastSplit];
    }
}