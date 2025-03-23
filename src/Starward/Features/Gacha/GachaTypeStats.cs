using System;
using System.Collections.Generic;

namespace Starward.Features.Gacha;

public class GachaTypeStats
{

    public int GachaType { get; set; }

    public string GachaTypeText { get; set; }

    public int Count { get; set; }

    public int Pity_5 { get; set; }

    public int Pity_4 { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int Count_5 { get; set; }

    public int Count_5_Up { get; set; }

    public int Count_4 { get; set; }

    public int Count_3 { get; set; }

    public double Ratio_5 { get; set; }

    public double Ratio_4 { get; set; }

    public double Ratio_3 { get; set; }

    public double Average_5 { get; set; }

    public double Average_5_Up { get; set; }

    public List<GachaLogItemEx> List_5 { get; set; }

    public List<GachaLogItemEx> List_4 { get; set; }

    public string Avarage_5_Desc_Text => Count_5_Up == 0 ? "" : $" / UP";

    public string Avarage_5_Up_Text => Count_5_Up == 0 ? "" : $" / {Average_5_Up:F2} ({Count_5_Up})";

}
