using Starward.Core.Warp;

namespace Starward.Models;

public class WarpRecordItemEx : WarpRecordItem
{

    public int Index { get; set; }


    public int Pity { get; set; }


    public double Progress => (double)Pity / (WarpType == WarpType.LightConeEventWarp ? 80 : 90) * 100;

}
