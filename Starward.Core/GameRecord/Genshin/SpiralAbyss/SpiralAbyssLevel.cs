using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.SpiralAbyss
{
    /// <summary>
    /// 深境螺旋间
    /// </summary>
    public class SpiralAbyssLevel
    {

        [JsonPropertyName("index")]
        public int Index { get; set; }


        [JsonPropertyName("star")]
        public int Star { get; set; }


        [JsonPropertyName("max_star")]
        public int MaxStar { get; set; }


        [JsonPropertyName("battles")]
        public List<SpiralAbyssBattle> Battles { get; set; }


        [JsonIgnore]
        public DateTimeOffset FirstBattleTime => Battles.FirstOrDefault()?.Time ?? new();

    }
}
