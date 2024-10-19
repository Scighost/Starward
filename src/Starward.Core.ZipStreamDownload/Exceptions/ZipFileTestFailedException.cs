using System.Diagnostics.CodeAnalysis;
using Starward.Core.ZipStreamDownload.Resources;

namespace Starward.Core.ZipStreamDownload.Exceptions;

/// <summary>
/// 当ZIP文件测试失败时引发的异常。
/// </summary>
public class ZipFileTestFailedException : ZipStreamDownloadException
{
    /// <summary>
    /// 创建一个当ZIP文件测试失败时引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    public ZipFileTestFailedException(string? message) : base(message)
    {
    }

    /// <summary>
    /// 创建一个当ZIP文件测试失败时引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">引发该异常的异常</param>
    public ZipFileTestFailedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 根据ZIP实体名称和异常原因引发ZIP文件测试失败的异常。
    /// </summary>
    /// <param name="zipEntryName">ZIP实体名称</param>
    /// <param name="reason">异常原因</param>
    /// <exception cref="InvalidZipEntryNameException">当ZIP文件测试失败时引发此异常。</exception>
    [DoesNotReturn]
    internal static void ThrowByZipEntryNameAndReason(string zipEntryName, string reason)
    {
        throw new InvalidZipEntryNameException(
            string.Format(ExceptionMessages.ZipFileTestFailedExceptionMessage, zipEntryName, reason));
    }

    /// <summary>
    /// 根据中心文件下载时的异常原因引发ZIP文件测试失败的异常。
    /// </summary>
    /// <param name="reason">异常原因</param>
    /// <exception cref="InvalidZipEntryNameException">当ZIP文件测试失败时引发此异常。</exception>
    [DoesNotReturn]
    internal static void ThrowByReasonCentralDirectory(string reason)
    {
        throw new InvalidZipEntryNameException(
            string.Format(ExceptionMessages.ZipFileTestFailedExceptionCentralDirectoryMessage, reason));
    }
}