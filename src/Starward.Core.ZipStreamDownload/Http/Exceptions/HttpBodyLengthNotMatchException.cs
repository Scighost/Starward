using System.Diagnostics.CodeAnalysis;
using Starward.Core.ZipStreamDownload.Resources;

namespace Starward.Core.ZipStreamDownload.Http.Exceptions;

/// <summary>
/// 当HTTP请求体的长度与请求头相关参数不匹配引发的异常。
/// </summary>
public class HttpBodyLengthNotMatchException : HttpPartialDownloadException
{
    /// <summary>
    /// 创建一个当HTTP请求体的长度与请求头相关参数不匹配引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    public HttpBodyLengthNotMatchException(string? message) : base(message)
    {
    }

    /// <summary>
    /// 创建一个当HTTP请求体的长度与请求头相关参数不匹配引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">引发该异常的异常</param>
    public HttpBodyLengthNotMatchException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 引发TTP请求体的长度与请求头相关参数不匹配的异常。
    /// </summary>
    /// <exception cref="HttpBodyLengthNotMatchException">当HTTP请求体的长度与请求头相关参数不匹配时引发此异常。</exception>
    [DoesNotReturn]
    internal static void Throw()
    {
        throw new HttpBodyLengthNotMatchException(
            string.Format(ExceptionMessages.HttpFileModifiedDuringPartialDownloadMessage));
    }
}