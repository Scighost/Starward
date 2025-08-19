using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Starward.Features.Screenshot;

public partial class ScreenshotItem
{

    public string Name { get; set; }

    public string FilePath { get; set; }

    public string FileName { get; set; }

    public string FileInfo { get; set; }

    public DateTime CreationTime { get; set; }

    public string CreationTimeText { get; set; }

    public string TimeMonthDay { get; set; }


    public ScreenshotItem(string file) : this(new FileInfo(file))
    {

    }


    public ScreenshotItem(FileInfo info)
    {
        FilePath = info.FullName;
        FileName = info.Name;
        FileInfo = GetFileInfo(info);
        Name = Path.GetFileNameWithoutExtension(info.Name);
        if (TryParseCreationTime(Name, out var time))
        {
            CreationTime = time;
        }
        else
        {
            CreationTime = info.CreationTime;
        }
        CreationTimeText = CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
        TimeMonthDay = CreationTime.ToString("yyyy-MM-dd");
    }



    private static bool TryParseCreationTime(string name, out DateTime time)
    {
        // star rail
        if (name.StartsWith("StarRail_Image_"))
        {
            name = name["StarRail_Image_".Length..];
            if (int.TryParse(name, out int ts))
            {
                time = DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime;
                return true;
            }
        }
        // cloud genshin
        if (name.StartsWith("GenshinlmpactPhoto "))
        {
            name = name["GenshinlmpactPhoto ".Length..];
            return DateTime.TryParseExact(name, "yyyy_MM_dd HH_mm_ss", null, DateTimeStyles.None, out time);
        }
        // genshin zzz
        if (DateTime.TryParseExact(name, "yyyyMMddHHmmss", null, DateTimeStyles.None, out time))
        {
            return true;
        }
        // honkai 3rd
        if (DateTime.TryParseExact(name[..Math.Min(19, name.Length)], "yyyy-MM-dd-HH-mm-ss", null, DateTimeStyles.None, out time))
        {
            return true;
        }
        // nvidia
        if (DateTime.TryParseExact(NvidiaNameRegex().Match(name).Groups[1].Value, "yyyy.MM.dd - HH.mm.ss.ff", null, DateTimeStyles.None, out time))
        {
            return true;
        }
        // xbox
        if (DateTime.TryParseExact(XBoxNameRegex().Match(name).Groups[1].Value, "yyyy_M_d H_mm_ss", null, DateTimeStyles.None, out time))
        {
            return true;
        }
        return false;
    }


    [GeneratedRegex(@"(\d{4})\.(\d{2})\.(\d{2}) - (\d{2})\.(\d{2})\.(\d{2})\.(\d+)")]
    private static partial Regex NvidiaNameRegex();


    [GeneratedRegex(@"(\d{4})_(\d{1,2})_(\d{1,2}) (\d{1,2})_(\d{2})_(\d{2})")]
    private static partial Regex XBoxNameRegex();


    private static string GetFileInfo(FileInfo info)
    {
        const double KB = 1 << 10, MB = 1 << 20;
        string ext = info.Extension.Replace(".", "").ToUpper() + "  ";
        string size = info.Length >= MB ? $"{info.Length / MB:F2} MB" : $"{info.Length / KB:F2} KB";
        return $"{ext}{size}".Trim();
    }


}


public class ScreenshotItemGroup : ObservableCollection<ScreenshotItem>
{

    public string Header { get; set; }


    public ScreenshotItemGroup(string header, IEnumerable<ScreenshotItem> list) : base(list)
    {
        Header = header;
    }

    public ScreenshotItemGroup(string header) : base()
    {
        Header = header;
    }

}