namespace Starward.Core.Gacha.ZZZ;

public class ZZZGachaItem : GachaLogItem
{

    public override IGachaType GetGachaType() => new ZZZGachaType(GachaType);

}
