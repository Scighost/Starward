using System.Diagnostics.CodeAnalysis;
using Starward.Core.ZipStreamDownload.Resources;

namespace Starward.Core.ZipStreamDownload.Exceptions;

/// <summary>
/// 当解压ZIP文件所需的功能不受支持时引发的异常。
/// </summary>
public class FeatureNotSupportedException : ZipStreamDownloadException
{
    /// <summary>
    /// 创建一个当解压ZIP文件所需的功能不受支持时引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    public FeatureNotSupportedException(string? message) : base(message)
    {
    }

    /// <summary>
    /// 创建一个当解压ZIP文件所需的功能不受支持时引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">引发该异常的异常</param>
    public FeatureNotSupportedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 使用原因引发解压ZIP文件所需的功能不受支持的异常。
    /// </summary>
    /// <param name="reason">引发该异常的原因</param>
    /// <exception cref="InvalidZipEntryNameException">当解压ZIP文件所需的功能不受支持时引发此异常。</exception>
    [DoesNotReturn]
    internal static void ThrowByReason(string reason)
    {
        throw new InvalidZipEntryNameException(
            string.Format(ExceptionMessages.FeatureNotSupportedExceptionMessage, reason));
    }
}