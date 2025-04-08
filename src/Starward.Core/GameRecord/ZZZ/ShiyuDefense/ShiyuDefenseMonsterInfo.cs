using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

public class ShiyuDefenseMonsterInfo
{

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("list")]
    public List<ShiyuDefenseMonsterMeta> Bosses { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



