using System.Net;
using Starward.Core.ZipStreamDownload.Resources;

namespace Starward.Core.ZipStreamDownload.Http.Exceptions;

/// <summary>
/// 当HTTP服务器返回的状态码不受支持时引发的异常。
/// </summary>
public class HttpStatusCodeInvalidException : HttpPartialDownloadException
{
    /// <summary>
    /// 创建一个当HTTP服务器返回的状态码不受支持时引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    public HttpStatusCodeInvalidException(string? message) : base(message)
    {
    }

    /// <summary>
    /// 创建一个当HTTP服务器返回的状态码不受支持时引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">引发该异常的异常</param>
    public HttpStatusCodeInvalidException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 当<paramref name="statusCode"/>参数传入的状态码不为分段下载所需的状态码时引发此异常。
    /// </summary>
    /// <param name="statusCode">HTTP状态码的枚举</param>
    /// <param name="message">状态消息</param>
    /// <exception cref="HttpStatusCodeInvalidException">当HTTP服务器返回的状态码不受支持时引发此异常。</exception>
    internal static void ThrowIfCodeNotEqualPartialContent(HttpStatusCode statusCode, string? message)
    {
        if (statusCode == HttpStatusCode.PartialContent) return;
        throw new HttpStatusCodeInvalidException(
            string.Format(ExceptionMessages.HttpStatusCodeInvalidExceptionMessage, (int)statusCode,
                message ?? "null"));
    }
}