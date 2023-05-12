
using Starward.Core.Gacha;

namespace Starward.Model;

public class GachaLogItemEx : GachaLogItem
{

    public int Index { get; set; }


    public int Pity { get; set; }


    public double Progress => (double)Pity / (((int)GachaType is 12 or 302) ? 80 : 90) * 100;

}
