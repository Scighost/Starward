using System.Collections.Generic;
using System.Security.Principal;

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


    public string SymbolSource { get; set; }


    public bool SkipVerifyWhenSymbol { get; set; }


    public void EnsureValid()
    {
        // todo
    }


}
