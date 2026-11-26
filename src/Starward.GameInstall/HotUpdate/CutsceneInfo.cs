using Starward.Core;
using System.Text.Json.Serialization;

namespace Starward.GameInstall.HotUpdate;


public class CutsceneCollection
{
    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("dispatch")]
    public Dictionary<string, CutsceneDispatchInfo> Dispatch { get; set; }
}


public class CutsceneDispatchInfo
{

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("region")]
    [JsonConverter(typeof(JsonStringEnumConverter<ServerRegion>))]
    public ServerRegion Region { get; set; }

    [JsonPropertyName("dispatchSeed")]
    public string DispatchSeed { get; set; }

    [JsonPropertyName("dispatcherKey")]
    public string DispatcherKey { get; set; }

    [JsonPropertyName("cutscene")]
    public List<CutsceneInfo> Cutscene { get; set; }

}


public class CutsceneInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("character")]
    public string? Character { get; set; }

    [JsonPropertyName("chapter")]
    public string Chapter { get; set; }

    [JsonPropertyName("act")]
    public string Act { get; set; }

    [JsonPropertyName("quest")]
    public string Quest { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("md5")]
    public string MD5 { get; set; }
}