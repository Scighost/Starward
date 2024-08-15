namespace Starward.Core.ZipStreamDownload.Http.Exceptions;

/// <summary>
/// 当进行HTTP分段下载时出错引发的异常。
/// </summary>
public class HttpPartialDownloadException : Exception
{
    /// <summary>
    /// 创建一个当进行HTTP分段下载时出错引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    public HttpPartialDownloadException(string? message) : base(message)
    {
    }

    /// <summary>
    /// 创建一个当进行HTTP分段下载时出错引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">引发该异常的异常</param>
    public HttpPartialDownloadException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}