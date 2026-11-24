namespace Starward.Helpers;

internal static class SizeTextFormat
{

    private const double KB = 1 << 10;

    private const double MB = 1 << 20;

    private const double GB = 1 << 30;



    public static string ToMB(long value)
    {
        return $"{value / MB:F2} MB";
    }


    public static string ToGB(long value)
    {
        return $"{value / GB:F2} GB";
    }



}