using System.Runtime.CompilerServices;
using ICSharpCode.SharpZipLib.Zip;
using Starward.Core.ZipStreamDownload.Resources;

namespace Starward.Core.ZipStreamDownload.Exceptions;

/// <summary>
/// 异常帮助类（当满足特定条件时抛出特定异常）
/// </summary>
internal static class ThrowException
{
    /// <summary>
    /// 如果给定的目录不存在则引发找不到路径的异常。
    /// </summary>
    /// <param name="directoryInfo">要进行检查的目录信息</param>
    /// <exception cref="DirectoryNotFoundException">当给定的路径不存在时引发此异常</exception>
    public static void ThrowDirectoryNotFoundExceptionIfDirectoryNotExists(DirectoryInfo directoryInfo)
    {
        if (!directoryInfo.Exists)
            throw new DirectoryNotFoundException(
                string.Format(ExceptionMessages.DirectoryNotFoundExceptionMessage, directoryInfo.FullName));
    }

    /// <summary>
    /// 如ZipEntry不为文件时引发异常。
    /// </summary>
    /// <param name="zipEntry"><see cref="ZipEntry"/>的实例</param>
    /// <param name="paramName">参数名称</param>
    /// <exception cref="ArgumentException">当参数错误时引发的异常</exception>
    public static void ThrowArgumentExceptionIfZipEntryNotIsFile(ZipEntry zipEntry,
        [CallerArgumentExpression(nameof(zipEntry))] string? paramName = null)
    {
        if (!zipEntry.IsFile)
            throw new ArgumentException(ExceptionMessages.ZipEntryNotIsFileArgumentExceptionMessage, paramName);
    }
}