using System.Text.Json.Serialization;

namespace Starward.Core.Gacha.Genshin;

public class WishRecordItem : GachaLogItem
{

    [JsonIgnore]
    public new WishType GachaType { get => (WishType)base.GachaType; set => base.GachaType = (int)value; }

}
