using System.Diagnostics.CodeAnalysis;
using Starward.Core.ZipStreamDownload.Resources;

namespace Starward.Core.ZipStreamDownload.Http.Exceptions;

/// <summary>
/// 当HTTP服务器可能不支持分段下载时引发的异常。
/// </summary>
public class HttpServerNotSupportedPartialDownloadException : HttpPartialDownloadException
{
    /// <summary>
    /// 创建一个当HTTP服务器可能不支持分段下载时引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    public HttpServerNotSupportedPartialDownloadException(string? message) : base(message)
    {
    }

    /// <summary>
    /// 创建一个当HTTP服务器可能不支持分段下载时引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">引发该异常的异常</param>
    public HttpServerNotSupportedPartialDownloadException(string? message, Exception? innerException) :
        base(message, innerException)
    {
    }

    /// <summary>
    /// 引发一个HTTP服务器可能不支持分段下载的异常。
    /// </summary>
    /// <exception cref="HttpServerNotSupportedPartialDownloadException">HTTP服务器可能不支持分段下载时引发此异常。</exception>
    [DoesNotReturn]
    internal static void Throw()
    {
        throw new HttpServerNotSupportedPartialDownloadException(
            ExceptionMessages.HttpServerNotSupportedPartialDownloadExceptionMessage);
    }
}