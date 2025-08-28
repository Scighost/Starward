namespace Starward.Codec.AVIF;

public class avifException : Exception
{

    public avifResult avifResult { get; private set; }


    public avifException(string message) : base(message)
    {

    }


    public avifException(avifResult result, string message) : base(message)
    {
        avifResult = result;
    }


    public static void ThrowIfFailed(avifResult result, string? message = null)
    {
        if (result is not avifResult.OK)
        {
            string error = avifNativeMethod.avifResultToString(result).ToString();
            if (!string.IsNullOrWhiteSpace(message))
            {
                error = $"{message} {error}";
            }
            throw new avifException(result, error);
        }
    }

}


internal static class avifExceptionExtension
{

    public static void ThrowIfFailed(this avifResult result, string? message = null)
    {
        avifException.ThrowIfFailed(result, message);
    }

}
