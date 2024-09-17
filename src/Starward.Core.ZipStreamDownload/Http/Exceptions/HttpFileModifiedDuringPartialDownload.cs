using System.Diagnostics.CodeAnalysis;
using Starward.Core.ZipStreamDownload.Resources;

namespace Starward.Core.ZipStreamDownload.Http.Exceptions;

/// <summary>
/// 当HTTP服务器上的文件在分段下载过程中被修改引发的异常。
/// </summary>
public class HttpFileModifiedDuringPartialDownload : HttpPartialDownloadException
{
    /// <summary>
    /// 创建一个当HTTP服务器上的文件在分段下载过程中被修改引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    public HttpFileModifiedDuringPartialDownload(string? message) : base(message)
    {
    }

    /// <summary>
    /// 创建一个当HTTP服务器上的文件在分段下载过程中被修改引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">引发该异常的异常</param>
    public HttpFileModifiedDuringPartialDownload(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 引发HTTP服务器上的文件在分段下载过程中被修改的异常。
    /// </summary>
    /// <exception cref="HttpFileModifiedDuringPartialDownload">当HTTP服务器上的文件在分段下载过程中被修改时引发此异常。</exception>
    [DoesNotReturn]
    internal static void Throw()
    {
        throw new HttpFileModifiedDuringPartialDownload(
            string.Format(ExceptionMessages.HttpFileModifiedDuringPartialDownloadMessage));
    }
}