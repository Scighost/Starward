using System;
using System.IO;

namespace Starward.Features.Background;

public class BackgroundFileItem
{

    public string FilePath { get; set; }

    public string FileInfo { get; set; }

    public long FileSize { get; set; }

    public DateTime CreationTime { get; set; }

    public bool IsVideo { get; set; }


    public BackgroundFileItem(string file)
    {
        FilePath = file;
        var info = new FileInfo(file);
        FileSize = info.Length;
        CreationTime = info.LastWriteTime;
        FileInfo = GetFileInfo(info);
        IsVideo = BackgroundService.FileIsSupportedVideo(file);
    }


    private static string GetFileInfo(FileInfo info)
    {
        const double KB = 1 << 10, MB = 1 << 20;
        string ext = info.Extension.Replace(".", "").ToUpper();
        string size = info.Length >= MB ? $"{info.Length / MB:F2} MB" : $"{info.Length / KB:F2} KB";
        return $"{ext}  {size}".Trim();
    }


}

