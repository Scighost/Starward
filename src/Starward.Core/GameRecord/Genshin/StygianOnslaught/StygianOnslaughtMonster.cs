using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.StygianOnslaught;

public class StygianOnslaughtMonster
{

    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonPropertyName("level")]
    public int Level { get; set; }


    [JsonPropertyName("icon")]
    public string Icon { get; set; }


    [JsonPropertyName("desc")]
    public List<string> Desc { get; set; }


    [JsonPropertyName("tags")]
    public List<StygianOnslaughtBestAvatar> Tags { get; set; }


    [JsonPropertyName("monster_id")]
    public int MonsterId { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }


    [JsonIgnore]
    public List<string> TrimmedDesc => Desc.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

}
