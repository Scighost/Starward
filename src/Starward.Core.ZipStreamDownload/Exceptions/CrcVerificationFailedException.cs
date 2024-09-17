using System.Diagnostics.CodeAnalysis;
using Starward.Core.ZipStreamDownload.Resources;

namespace Starward.Core.ZipStreamDownload.Exceptions;

/// <summary>
/// 当CRC校验失败时引发的异常
/// </summary>
public class CrcVerificationFailedException : ZipStreamDownloadException
{
    /// <summary>
    /// 创建一个当CRC校验失败时引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    public CrcVerificationFailedException(string? message) : base(message)
    {
    }

    /// <summary>
    /// 创建一个当CRC校验失败时引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">引发该异常的异常</param>
    public CrcVerificationFailedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 使用ZipEntry的名称引发CRC校验失败时引发的异常。
    /// </summary>
    /// <param name="zipEntryName">ZipEntry的名称</param>
    /// <exception cref="InvalidZipEntryNameException">当CRC校验失败时引发此异常。</exception>
    [DoesNotReturn]
    internal static void ThrowByZipEntryName(string zipEntryName)
    {
        throw new InvalidZipEntryNameException(
            string.Format(ExceptionMessages.CrcVerificationFailedExceptionMessage, zipEntryName));
    }
}