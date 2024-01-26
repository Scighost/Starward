namespace Starward.Services.InstallGame;

internal class DownloadFileTask
{

    public string FileName { get; set; }


    public string Url { get; set; }


    public long Size { get; set; }


    public long DownloadSize { get; set; }


    public required string MD5 { get; set; }


    public bool IsSegment { get; set; }

}
