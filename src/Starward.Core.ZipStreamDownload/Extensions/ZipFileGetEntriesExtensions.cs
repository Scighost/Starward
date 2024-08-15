using ICSharpCode.SharpZipLib.Zip;

namespace Starward.Core.ZipStreamDownload.Extensions;

/// <summary>
/// <see cref="ZipFile"/>获取实体列表扩展
/// </summary>
internal static class ZipFileGetEntriesExtensions
{
    /// <summary>
    /// 获取目录类型的<see cref="ZipEntry"/>的列表。
    /// </summary>
    /// <param name="zipFile"><see cref="ZipFile"/>的实例。</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务，可以获取<see cref="ZipEntry"/>的列表。</returns>
    /// <exception cref="OperationCanceledException">令牌已被请求取消。</exception>
    public static Task<List<ZipEntry>> GetDirectoryEntriesAsync(this ZipFile zipFile,
        CancellationToken cancellationToken = default)
    {
        var entries = new List<ZipEntry>();
        foreach (ZipEntry entry in zipFile)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry.IsDirectory) entries.Add(entry);
        }
        return Task.FromResult(entries);
    }

    /// <summary>
    /// 获取目录类型的<see cref="ZipEntry"/>的列表。
    /// </summary>
    /// <param name="zipFile"><see cref="ZipFile"/>的实例。</param>
    /// <returns><see cref="ZipEntry"/>的列表。</returns>
    public static List<ZipEntry> GetDirectoryEntries(this ZipFile zipFile)
        => GetDirectoryEntriesAsync(zipFile).GetAwaiter().GetResult();

    /// <summary>
    /// 获取文件类型的<see cref="ZipEntry"/>的列表。
    /// </summary>
    /// <param name="zipFile"><see cref="ZipFile"/>的实例。</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务，可以获取<see cref="ZipEntry"/>的列表。</returns>
    /// <exception cref="OperationCanceledException">令牌已被请求取消。</exception>
    public static Task<List<ZipEntry>> GetFileEntriesAsync(this ZipFile zipFile,
        CancellationToken cancellationToken = default)
    {
        var entries = new List<ZipEntry>();
        foreach (ZipEntry entry in zipFile)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry.IsFile) entries.Add(entry);
        }
        return Task.FromResult(entries);
    }

    /// <summary>
    /// 获取文件类型的<see cref="ZipEntry"/>的列表。
    /// </summary>
    /// <param name="zipFile"><see cref="ZipFile"/>的实例。</param>
    /// <returns><see cref="ZipEntry"/>的列表。</returns>
    public static List<ZipEntry> GetFileEntries(this ZipFile zipFile)
        => GetFileEntriesAsync(zipFile).GetAwaiter().GetResult();
}