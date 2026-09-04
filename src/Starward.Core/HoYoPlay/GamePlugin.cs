using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;


/// <summary>
/// 游戏插件发布信息
/// </summary>
public class GamePluginRelease
{

    [JsonPropertyName("game")]
    public GameId GameId { get; set; }


    [JsonPropertyName("plugins")]
    public List<GamePlugin> Plugins { get; set; }

}


/// <summary>
/// 游戏插件
/// </summary>
public class GamePlugin
{

    [JsonPropertyName("plugin_id")]
    public string PluginId { get; set; }


    [JsonPropertyName("release_id")]
    public string ReleaseId { get; set; }


    [JsonPropertyName("version")]
    public string Version { get; set; }


    [JsonPropertyName("plugin_pkg")]
    public GamePluginPackage PluginPackage { get; set; }

}


/// <summary>
/// 游戏插件安装包
/// </summary>
public class GamePluginPackage
{

    [JsonPropertyName("url")]
    public string Url { get; set; }


    [JsonPropertyName("md5")]
    public string MD5 { get; set; }


    [JsonPropertyName("size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Size { get; set; }


    [JsonPropertyName("decompressed_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long DecompressedSize { get; set; }


    [JsonPropertyName("command")]
    public string Command { get; set; }


    [JsonPropertyName("validation")]
    public string Validation { get; set; }

}
