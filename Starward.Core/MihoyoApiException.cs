namespace Starward.Core;

public class MihoyoApiException : Exception
{

    public int ReturnCode { get; init; }


    public MihoyoApiException(int returnCode, string? message) : base($"{message} ({returnCode})")
    {
        ReturnCode = returnCode;
    }

}
