using Starward.Core;
using Starward.Core.Gacha.StarRail;
using System;
using System.Collections.Generic;

namespace Starward.Model;

public class WarpTypeStats
{

    public WarpType WarpType { get; set; }

    public string WarpTypeText => WarpType.ToDescription();

    public int Count { get; set; }

    public int Pity_5 { get; set; }

    public int Pity_4 { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int Count_5 { get; set; }

    public int Count_4 { get; set; }

    public int Count_3 { get; set; }

    public double Ratio_5 { get; set; }

    public double Ratio_4 { get; set; }

    public double Ratio_3 { get; set; }

    public double Average_5 { get; set; }

    public List<WarpRecordItemEx> List_5 { get; set; }

    public List<WarpRecordItemEx> List_4 { get; set; }

}
