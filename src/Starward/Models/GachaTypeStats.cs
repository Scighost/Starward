using Starward.Core;
using Starward.Core.Gacha;
using System;
using System.Collections.Generic;

namespace Starward.Models;

public class GachaTypeStats
{

    public GachaType GachaType { get; set; }

    public string GachaTypeText => GachaType.ToLocalization();

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

    public List<GachaLogItemEx> List_5 { get; set; }

    public List<GachaLogItemEx> List_4 { get; set; }

}
