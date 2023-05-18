using System.Text.Json.Serialization;

namespace Starward.Core.Metadata;

public class ReleaseVersion
{

    public string Version { get; set; }


    public string Architecture { get; set; }


    public DateTimeOffset BuildTime { get; set; }


    public bool DisableAutoUpdate { get; set; }


    public string Install { get; set; }


    public long InstallSize { get; set; }


    public string InstallHash { get; set; }


    public string Portable { get; set; }


    public long PortableSize { get; set; }


    public string PortableHash { get; set; }


    public List<ReleaseFile> SeparateFiles { get; set; }


    [JsonIgnore]
    public string ReleasePage => $"https://github.com/Scighost/Starward/releases/tag/{Version}";


}



public class ReleaseFile
{

    public string Path { get; set; }


    public long Size { get; set; }


    public string Hash { get; set; }

    [JsonIgnore]
#if DEBUG
    public string Url => $"https://starward.scighost.com/release/separate_files/dev/{Hash}";
#else
    public string Url => $"https://starward.scighost.com/release/separate_files/{Hash}";
#endif

}