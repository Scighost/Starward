using System.Text.Json.Serialization;

namespace Starward.Core.Gacha.StarRail;

public class WarpRecordItem : GachaLogItem
{


    [JsonPropertyName("gacha_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int GachaId { get; set; }

    [JsonIgnore]
    public new WarpType GachaType { get => (WarpType)base.GachaType; set => base.GachaType = (int)value; }


}

