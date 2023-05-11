
using Starward.Core.Gacha.StarRail;

namespace Starward.Model;

public class WarpRecordItemEx : WarpRecordItem
{

    public int Index { get; set; }


    public int Pity { get; set; }


    public double Progress => (double)Pity / (GachaType == WarpType.LightConeEvent ? 80 : 90) * 100;

}
