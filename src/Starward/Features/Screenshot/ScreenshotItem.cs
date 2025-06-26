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

    public string FullName { get; set; }

    public DateTime CreationTime { get; set; }

    public string CreationTimeText { get; set; }

    public string TimeMonth { get; set; }


    public ScreenshotItem(string file) : this(new FileInfo(file))
    {

    }


    public ScreenshotItem(FileInfo info)
    {
        FullName = info.FullName;
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
        TimeMonth = CreationTime.ToString("yyyy-MM");
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
        if (DateTime.TryParseExact(name, "yyyy_M_d HH_mm_ss", null, DateTimeStyles.None, out time))
        {
            return true;
        }
        return false;
    }


    [GeneratedRegex(@"(\d{4})\.(\d{2})\.(\d{2}) - (\d{2})\.(\d{2})\.(\d{2})\.(\d+)")]
    private static partial Regex NvidiaNameRegex();



}


public class ScreenshotItemGroup : ObservableCollection<ScreenshotItem>
{

    public string Header { get; set; }


    public ScreenshotItemGroup(string header, IEnumerable<ScreenshotItem> list) : base(list)
    {
        Header = header;
    }

}