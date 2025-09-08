using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;


/// <summary>
/// 游戏扫描信息
/// </summary>
public class GameScanInfo
{

    [JsonPropertyName("game_id")]
    public string GameId { get; set; }

    /// <summary>
    /// 不同版本的游戏 exe md5
    /// </summary>
    [JsonPropertyName("game_exe_list")]
    public List<GameScanInfoExe> GameExeList { get; set; }

}


/// <summary>
/// 不同版本的游戏 exe md5
/// </summary>
public class GameScanInfoExe
{

    [JsonPropertyName("version")]
    public string Version { get; set; }


    [JsonPropertyName("md5")]
    public string MD5 { get; set; }

}
