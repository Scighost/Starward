using System;

namespace Starward.Services.Download;

internal class InstallGameItem
{

    public string FileName { get; set; }


    public string Url { get; set; }


    public string Path { get; set; }


    public bool IsFolder { get; set; }


    public long Size { get; set; }


    public long DecompressedSize { get; set; }


    public string MD5 { get; set; }


    public Range Range { get; set; }


    public bool WriteAsTempFile { get; set; }


    public InstallGameItemType Type { get; set; }


}
