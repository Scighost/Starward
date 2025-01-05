
using CommunityToolkit.Mvvm.ComponentModel;
using Starward.Core.Gacha;
using Starward.Core.Gacha.Genshin;
using Starward.Core.Gacha.StarRail;
using Starward.Core.Gacha.ZZZ;

namespace Starward.Features.Gacha;

[INotifyPropertyChanged]
public partial class GachaLogItemEx : GachaLogItem
{

    /// <summary>
    /// 不要删除，导出 Excel 时有用
    /// </summary>
    public string IdText => Id.ToString();

    public int Index { get; set; }

    public int Pity { get; set; }

    public string Icon { get; set; }

    public double Progress => (double)Pity / ((GachaType is GenshinGachaType.WeaponEventWish or StarRailGachaType.LightConeEventWarp or ZZZGachaType.WEngineChannel) ? 80 : 90) * 100;


    private bool _IsPointerIn;
    public bool IsPointerIn
    {
        get => _IsPointerIn;
        set => SetProperty(ref _IsPointerIn, value);
    }


    public int ItemCount { get; set; }


}
