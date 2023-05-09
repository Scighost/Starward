using System.Text.Json.Serialization;

namespace Starward.Core.Hoyolab.Genshin.SpiralAbyss
{
    /// <summary>
    /// 深境螺旋层
    /// </summary>
    public class SpiralAbyssFloor
    {
        [JsonIgnore]
        public int Id { get; set; }


        [JsonPropertyName("index")]
        public int Index { get; set; }


        [JsonPropertyName("icon")]
        public string Icon { get; set; }


        [JsonPropertyName("is_unlock")]
        public bool IsUnlock { get; set; }


        [JsonPropertyName("settle_time")]
        public string SettleTime { get; set; }


        [JsonPropertyName("star")]
        public int Star { get; set; }


        [JsonPropertyName("max_star")]
        public int MaxStar { get; set; }


        [JsonPropertyName("levels")]
        public List<SpiralAbyssLevel> Levels { get; set; }


    }
}
