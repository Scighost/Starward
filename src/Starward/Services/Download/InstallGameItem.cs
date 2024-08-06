using System.Collections.Generic;

namespace Starward.Services.Download;

internal class InstallGameItem
{


    public InstallGameItemType Type { get; set; }


    public string FileName { get; set; }


    public string Url { get; set; }


    public string Path { get; set; }


    public List<string> DecompressPackageFiles { get; set; }


    public string DecompressPath { get; set; }


    public long Size { get; set; }


    public long DecompressedSize { get; set; }


    public string MD5 { get; set; }


    public bool WriteAsTempFile { get; set; }


    public string HardLinkSource { get; set; }


    public bool HardLinkSkipVerify { get; set; }


    public void EnsureValid()
    {
        // todo
    }


}
