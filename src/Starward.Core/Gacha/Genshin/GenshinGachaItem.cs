namespace Starward.Core.Gacha.Genshin;

public class GenshinGachaItem : GachaLogItem
{


    public override IGachaType GetGachaType() => new GenshinGachaType(GachaType);


    public override GenshinGachaItem Clone() => (GenshinGachaItem)MemberwiseClone();


}
