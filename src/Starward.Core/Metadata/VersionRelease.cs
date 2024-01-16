using System.Text.Json.Serialization;

namespace Starward.Core.Metadata;

public class ReleaseVersion : IJsonOnDeserialized
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


    public string SeparatePrefix { get; set; }


    public List<ReleaseFile> SeparateFiles { get; set; }


    [JsonIgnore]
    public string ReleasePage => $"https://github.com/Scighost/Starward/releases/tag/{Version}";


    public void OnDeserialized()
    {
        if (SeparateFiles is not null)
        {
            string prefix;
            if (string.IsNullOrWhiteSpace(SeparatePrefix))
            {
#if DEV
                prefix = $"https://starward.scighost.com/release/separate_files/dev/";
#else
                prefix = $"https://starward.scighost.com/release/separate_files/";
#endif
            }
            else
            {
                prefix = SeparatePrefix;
            }
            foreach (var file in SeparateFiles)
            {
                if (string.IsNullOrWhiteSpace(file.Url))
                {
                    file.Url = Path.Combine(prefix, file.Hash);
                }
            }

        }
    }

}



public class ReleaseFile
{

    public string Path { get; set; }


    public long Size { get; set; }


    public string Hash { get; set; }


    public string Url { get; set; }

}