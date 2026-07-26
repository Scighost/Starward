using CommunityToolkit.Mvvm.ComponentModel;
using Starward.Core;
using Starward.Core.Gacha.Genshin;
using Starward.Core.Gacha.StarRail;
using Starward.Core.Gacha.ZZZ;
using System;
using System.Collections.Generic;


namespace Starward.Features.Gacha.UIGF;

public class GachaUidArchiveDisplay : ObservableObject
{

    public GameBiz Game { get; set; }

    public string GameIcon { get; set; }

    /// <summary>
    /// 千星奇域与原神本体共用 Uid，需要单独成行，故用此标记区分
    /// </summary>
    public bool IsGenshinBeyond { get; set; }

    /// <summary>
    /// 列表中显示的档案名称，如「原神」「千星奇域」
    /// </summary>
    public string ArchiveName => IsGenshinBeyond ? Lang.GenshinBeyondGachaPage_MiliastraWonderlandOde : Game.ToGameName();

    public long Uid { get; set; }

    public int Count { get; set; }

    public string LastItemGachaType { get; set; }

    public string LastItemName { get; set; }

    public DateTime LastItemTime { get; set; }


    public List<UIGFGenshinGachaItem>? hke4List { get; set; }

    public List<StarRailGachaItem>? hkrpgList { get; set; }

    public List<ZZZGachaItem>? napList { get; set; }

    public List<GenshinBeyondGachaItem>? hk4eUgcList { get; set; }


    public int Timezone
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                LastItemTimeOffest = LastItemTime.AddHours(value);
            }
        }
    }


    public DateTime LastItemTimeOffest { get; set => SetProperty(ref field, value); }



    public string? Result { get; set => SetProperty(ref field, value); }


    public string? Error { get; set => SetProperty(ref field, value); }


}