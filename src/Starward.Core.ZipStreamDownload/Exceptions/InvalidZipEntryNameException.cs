using System.Diagnostics.CodeAnalysis;
using Starward.Core.ZipStreamDownload.Resources;

namespace Starward.Core.ZipStreamDownload.Exceptions;

/// <summary>
/// 当ZIP实体的名称无效时引发的异常。
/// </summary>
public class InvalidZipEntryNameException : ZipStreamDownloadException
{
    /// <summary>
    /// 创建一个当ZIP实体的名称无效时引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    public InvalidZipEntryNameException(string? message) : base(message)
    {
    }

    /// <summary>
    /// 创建一个当ZIP实体的名称无效时引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">引发该异常的异常</param>
    public InvalidZipEntryNameException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 使用ZIP实体名称引发ZIP实体的名称无效的异常。
    /// </summary>
    /// <param name="zipEntryName">ZIP实体名称</param>
    /// <exception cref="InvalidZipEntryNameException">当ZIP实体的名称无效时引发此异常。</exception>
    [DoesNotReturn]
    internal static void ThrowByZipEntryName(string zipEntryName)
    {
        throw new InvalidZipEntryNameException(
            string.Format(ExceptionMessages.ZipEntryFileNameNotFoundExceptionMessage, zipEntryName));
    }
}