namespace Starward.Core.ZipStreamDownload.Exceptions;

/// <summary>
/// 当ZIP流式下载时出现错误引发的异常。
/// </summary>
public class ZipStreamDownloadException : Exception
{
    /// <summary>
    /// 创建一个当ZIP流式下载时出现错误引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    public ZipStreamDownloadException(string? message) : base(message)
    {
    }

    /// <summary>
    /// 创建一个当ZIP流式下载时出现错误引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">引发该异常的异常</param>
    public ZipStreamDownloadException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}