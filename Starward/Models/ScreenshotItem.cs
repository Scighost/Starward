using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        }
        if (int.TryParse(name, out int ts))
        {
            var time = DateTimeOffset.FromUnixTimeSeconds(ts);
            CreationTime = time.LocalDateTime;
            Title = CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
        else
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