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


    /// <summary>
    /// 小于 1 GB 用 MB 显示，否则用 GB。
    /// </summary>
    /// <remarks>
    /// 预下载补丁常常只有几百 MB，统一按 GB 显示会变成 "0.36 GB"，不如 "365.83 MB" 直观，
    /// 也不便与官方启动器的显示值直接比对。
    /// </remarks>
    public static string ToBestFit(long value)
    {
        return value < GB ? ToMB(value) : ToGB(value);
    }



}