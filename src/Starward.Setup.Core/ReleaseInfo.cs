using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Starward.Setup.Core;

public class ReleaseInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    /// <summary>
    /// Key: "{architecture}-{install_type}"
    /// </summary>
    [JsonPropertyName("releases")]
    public Dictionary<string, ReleaseInfoDetail> Releases { get; set; }

    public bool TryGetReleaseInfoDetail(Architecture architecture, InstallType installType, [NotNullWhen(true)] out ReleaseInfoDetail? detail)
    {
        detail = null;
        if (Releases is null)
        {
            return false;
        }
        string key = $"{architecture}-{installType}".ToLower();
        return Releases.TryGetValue(key, out detail);
    }
}


public class ReleaseInfoDetail
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("architecture")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [JsonConverter(typeof(JsonStringIgnoreCaseEnumConverter<Architecture>))]
    public Architecture Architecture { get; set; }

    [JsonPropertyName("install_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [JsonConverter(typeof(JsonStringIgnoreCaseEnumConverter<InstallType>))]
    public InstallType InstallType { get; set; }

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

    [JsonPropertyName("setup")]
    public ReleaseSetup? Setup { get; set; }

    [JsonPropertyName("diffs")]
    public Dictionary<string, ReleaseInfoDiff> Diffs { get; set; }
}


public class ReleaseSetup
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("file_name")]
    public string FileName { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("hash")]
    public string Hash { get; set; }
}


public class ReleaseInfoDiff
{
    [JsonPropertyName("diff_version")]
    public string DiffVersion { get; set; }

    [JsonPropertyName("diff_size")]
    public long DiffSize { get; set; }

    [JsonPropertyName("manifest_url")]
    public string ManifestUrl { get; set; }

}

