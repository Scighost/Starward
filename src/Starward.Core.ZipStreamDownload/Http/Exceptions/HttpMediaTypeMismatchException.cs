using System.Net.Http.Headers;
using Starward.Core.ZipStreamDownload.Resources;

namespace Starward.Core.ZipStreamDownload.Http.Exceptions;

/// <summary>
/// 当HTTP服务器返回的文件类型和所需类型不匹配时引发的异常。
/// </summary>
public class HttpMediaTypeMismatchException : HttpPartialDownloadException
{
    /// <summary>
    /// 创建一个当当HTTP服务器返回的文件类型和所需类型不匹配时引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    public HttpMediaTypeMismatchException(string? message) : base(message)
    {
    }

    /// <summary>
    /// 创建一个当HTTP服务器返回的文件类型和所需类型不匹配时引发的异常的实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">引发该异常的异常</param>
    public HttpMediaTypeMismatchException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 当<paramref name="contentHeaders"/>中的MediaType与<paramref name="mediaType"/>传入的字符串不匹配时引发异常。
    /// </summary>
    /// <param name="contentHeaders"><see cref="HttpContentHeaders"/>的实例</param>
    /// <param name="mediaType">要进行验证的媒体类型</param>
    /// <exception cref="HttpMediaTypeMismatchException">当HTTP服务器返回的文件类型和所需类型不匹配时引发此异常。</exception>
    internal static void ThrowIfMediaTypeMismatch(HttpContentHeaders contentHeaders, string? mediaType)
    {
        if (string.IsNullOrEmpty(mediaType) &&
            contentHeaders.ContentType != null &&
            contentHeaders.ContentType.MediaType != mediaType)
            throw new HttpMediaTypeMismatchException(ExceptionMessages.HttpMediaTypeMismatchExceptionMessage);
    }

    /// <summary>
    /// 当<paramref name="version"/>的版本小于1.1时引发的异常。
    /// </summary>
    /// <param name="version"><see cref="Version"/>的实例，表示HTTP协议版本。</param>
    /// <exception cref="HttpMediaTypeMismatchException">当HTTP服务器返回的版本小于所需版本时引发此异常。</exception>
    internal static void ThrowIfVersionLessThenHttp11(Version version)
    {
        if (version.Major < 1 || version is { Major: 1, Minor: < 1 })
            throw new HttpMediaTypeMismatchException(ExceptionMessages.HttpMediaTypeMismatchExceptionMessage);
    }
}