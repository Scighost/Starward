namespace Starward.Codec.UltraHdr;

public class UhdrException : Exception
{

    /// <summary>
    /// Uhdr error code
    /// </summary>
    public UhdrCodecError ErrorCode { get; private set; }


    public string? Detail { get; private set; }


    public UhdrException(UhdrErrorInfo errorInfo, string message) : base(message)
    {
        ErrorCode = errorInfo.ErrorCode;
        Detail = errorInfo.Detail;
    }


    public UhdrException(UhdrCodecError errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }


}