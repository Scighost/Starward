using System.Collections.Generic;

namespace Starward.Models;




public class DownloadGameResource
{
    public DownloadPackageState Game { get; set; }

    public List<DownloadPackageState> Voices { get; set; } = new(4);

    public long FreeSpace { get; set; }
}



public class DownloadPackageState
{

    public string Name { get; set; }

    public string Url { get; set; }

    public long PackageSize { get; set; }

    public long DecompressedSize { get; set; }

    public long DownloadedSize { get; set; }

}


