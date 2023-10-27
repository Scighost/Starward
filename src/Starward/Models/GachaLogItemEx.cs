
using CommunityToolkit.Mvvm.ComponentModel;
using Starward.Core.Gacha;

namespace Starward.Models;

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

    public double Progress => (double)Pity / (((int)GachaType is 12 or 302) ? 80 : 90) * 100;


    private bool _IsPointerIn;
    public bool IsPointerIn
    {
        get => _IsPointerIn;
        set => SetProperty(ref _IsPointerIn, value);
    }


    public int ItemCount { get; set; }


}
