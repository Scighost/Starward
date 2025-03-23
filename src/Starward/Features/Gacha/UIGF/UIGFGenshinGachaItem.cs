using Starward.Core.Gacha.Genshin;
using System.Text.Json.Serialization;

namespace Starward.Features.Gacha.UIGF;

public class UIGFGenshinGachaItem : GenshinGachaItem
{

    [JsonPropertyName("uigf_gacha_type")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int UIGFGachaType { get; set; }


    public override GenshinGachaItem Clone() => (UIGFGenshinGachaItem)MemberwiseClone();

}
