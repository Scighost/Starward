namespace Starward.Core.Metadata;

public class ReleaseVersion
{

    public string Version { get; set; }


    public string Architecture { get; set; }


    public DateTimeOffset BuildTime { get; set; }


    public string Install { get; set; }


    public long InstallSize { get; set; }


    public string InstallHash { get; set; }


    public string Portable { get; set; }


    public long PortableSize { get; set; }


    public string PortableHash { get; set; }


    public List<ReleaseFile> SeparateFiles { get; set; }


}



public class ReleaseFile
{

    public string Path { get; set; }


    public long Size { get; set; }


    public string Hash { get; set; }

}