using System.Collections.Generic;

namespace Starward.Services.Download;

internal class InstallGameItem
{


    public InstallGameItemType Type { get; set; }


    public string FileName { get; set; }


    public string Url { get; set; }


    public string Path { get; set; }


    public List<string> PackageFiles { get; set; }


    public string TargetPath { get; set; }


    public long Size { get; set; }


    public long DecompressedSize { get; set; }


    public string MD5 { get; set; }


    public bool WriteAsTempFile { get; set; }


    public void EnsureValid()
    {
        // todo
    }


}
