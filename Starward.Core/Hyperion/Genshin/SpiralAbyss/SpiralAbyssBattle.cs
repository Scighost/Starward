using System.Text.Json.Serialization;

namespace Starward.Core.Hyperion.Genshin.SpiralAbyss
{

    /// <summary>
    /// 深境螺旋一场战斗
    /// </summary>
    public class SpiralAbyssBattle
    {
        [JsonIgnore]
        public int Id { get; set; }


        [JsonPropertyName("index")]
        public int Index { get; set; }


        [JsonPropertyName("timestamp")]
        [JsonConverter(typeof(SpiralAbyssTimeJsonConverter))]
        public DateTimeOffset Time { get; set; }


        [JsonPropertyName("avatars")]
        public List<SpiralAbyssAvatar> Avatars { get; set; }


    }
}
