using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.StygianOnslaught;

public class StygianOnslaughtBattle
{

    /// <summary>
    /// 多人模式是 null
    /// </summary>
    [JsonPropertyName("best")]
    public StygianOnslaughtBest? Best { get; set; }


    [JsonPropertyName("challenge")]
    public List<StygianOnslaughtChallenge> Challenge { get; set; }


    [JsonPropertyName("has_data")]
    public bool HasData { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}
