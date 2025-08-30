using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.StygianOnslaught;

public class StygianOnslaughtChallenge
{

    /// <summary>
    /// 怪物名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonPropertyName("second")]
    public int Second { get; set; }


    [JsonPropertyName("teams")]
    public List<StygianOnslaughtAvatar> Teams { get; set; }


    [JsonPropertyName("best_avatar")]
    public List<StygianOnslaughtBestAvatar> BestAvatar { get; set; }


    [JsonPropertyName("monster")]
    public StygianOnslaughtMonster Monster { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}
