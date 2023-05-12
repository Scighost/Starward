using System.Text.Json.Serialization;

namespace Starward.Core.Hyperion.Genshin.SpiralAbyss
{
    /// <summary>
    /// 深境螺旋间
    /// </summary>
    public class SpiralAbyssLevel
    {

        [JsonIgnore]
        public int Id { get; set; }


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
