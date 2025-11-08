using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace BuildTool;

public class ReleaseInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    /// <summary>
    /// Key: "{architecture}-{release_type}"
    /// </summary>
    [JsonPropertyName("releases")]
    public Dictionary<string, ReleaseInfoDetail> Releases { get; set; }
}

public class ReleaseInfoDetail
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("architecture")]
    [JsonConverter(typeof(JsonStringIgnoreCaseEnumConverter<Architecture>))]
    public Architecture Architecture { get; set; }

    [JsonPropertyName("release_type")]
    [JsonConverter(typeof(JsonStringIgnoreCaseEnumConverter<ReleaseType>))]
    public ReleaseType ReleaseType { get; set; }

    [JsonPropertyName("build_time")]
    public DateTimeOffset BuildTime { get; set; }

    [JsonPropertyName("disable_auto_update")]
    public bool DisableAutoUpdate { get; set; }

    [JsonPropertyName("package_url")]
    public string PackageUrl { get; set; }

    [JsonPropertyName("package_size")]
    public long PackageSize { get; set; }

    [JsonPropertyName("package_hash")]
    public string PackageHash { get; set; }

    [JsonPropertyName("manifest_url")]
    public string ManifestUrl { get; set; }

    [JsonPropertyName("diffs")]
    public Dictionary<string, ReleaseInfoDiff> Diffs { get; set; }

}


public class ReleaseInfoDiff
{
    [JsonPropertyName("diff_version")]
    public string DiffVersion { get; set; }

    [JsonPropertyName("manifest_url")]
    public string ManifestUrl { get; set; }
}