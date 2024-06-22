using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;


/// <summary>
/// 需要删除的文件
/// </summary>
public class GameDeprecatedFileConfig
{

    [JsonPropertyName("game")]
    public GameId GameId { get; set; }


    [JsonPropertyName("deprecated_files")]
    public List<GameDeprecatedFile> DeprecatedFiles { get; set; }

}

public class GameDeprecatedFile
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}