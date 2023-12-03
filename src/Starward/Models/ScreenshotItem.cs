using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace Starward.Models;

public class ScreenshotItem
{

    public string Title { get; set; }

    public string FullName { get; set; }

    public DateTime CreationTime { get; set; }


    public ScreenshotItem(FileInfo info)
    {
        FullName = info.FullName;
        var name = Path.GetFileNameWithoutExtension(info.Name);
        if (name.StartsWith("StarRail_Image_"))
        {
            name = name["StarRail_Image_".Length..];
            if (int.TryParse(name, out int ts))
            {
                var time = DateTimeOffset.FromUnixTimeSeconds(ts);
                CreationTime = time.LocalDateTime;
                Title = CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
                return;
            }
        }
        if (name.StartsWith("GenshinlmpactPhoto "))
        {
            name = name["GenshinlmpactPhoto ".Length..];
            if (DateTime.TryParseExact(name, "yyyy_MM_dd HH_mm_ss", null, DateTimeStyles.None, out var time1))
            {
                CreationTime = time1;
                Title = CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
                return;
            }
        }
        if (DateTime.TryParseExact(name, "yyyyMMddHHmmss", null, DateTimeStyles.None, out var time2))
        {
            CreationTime = time2;
            Title = CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
            return;
        }
        if (DateTime.TryParseExact(name[..Math.Min(19, name.Length)], "yyyy-MM-dd-HH-mm-ss", null, DateTimeStyles.None, out var time3))
        {
            CreationTime = time3;
            Title = CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
            return;
        }
        if (string.IsNullOrWhiteSpace(Title))
        {
            Title = name;
            CreationTime = info.CreationTime;
        }
    }


}




public class ScreenshotItemGroup : ObservableCollection<ScreenshotItem>
{

    public string Header { get; set; }


    public ScreenshotItemGroup(string header, IEnumerable<ScreenshotItem> list) : base(list)
    {
        Header = header;
    }

}