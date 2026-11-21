using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Starward.Core.Gacha.Genshin;
using Starward.Core.Gacha.StarRail;
using Starward.Core.Gacha.ZZZ;
using Starward.Features.Gacha.Endfield;
using System;
using Windows.Foundation;
using Windows.UI;

namespace Starward.Features.Gacha;

internal partial class GachaPityProgressBackgroundBrushConverter : IValueConverter
{

    private static Color Red = Color.FromArgb(0xFF, 0xC8, 0x3C, 0x23);
    private static Color Green = Color.FromArgb(0xFF, 0x00, 0xE0, 0x79);

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is EndfieldGachaItem endfield)
        {
            if (endfield.IsFree || endfield.Pity <= 0) return null!;
            bool shortPool = endfield.RecordType == "weapon"
                || endfield.PoolType == "E_CharacterGachaPoolType_Beginner";
            return CreateBrush(endfield.Pity, shortPool ? 40 : 80, shortPool ? 40 : 66);
        }
        if (value is GachaLogItemEx item)
        {
            int pity = item.Pity;

            int point = 74;
            double guarantee = 90;
            if (item.GachaType is GenshinGachaType.WeaponEventWish or StarRailGachaType.LightConeEventWarp or StarRailGachaType.LightConeCollaborationWarp)
            {
                point = 63;
                guarantee = 80;
            }
            else if (item.GachaType is ZZZGachaType.WEngineChannel or ZZZGachaType.WEngineReverberation or ZZZGachaType.BangbooChannel)
            {
                point = 65;
                guarantee = 80;
            }
            return CreateBrush(pity, guarantee, point);
        }
        return null!;
    }


    private static LinearGradientBrush CreateBrush(int pity, double guarantee, int point)
    {
        double offset = Math.Clamp(pity / guarantee, 0, 1);
        Color color = pity < point ? Green : Red;
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0), Opacity = 0.4 };
        brush.GradientStops.Add(new GradientStop { Color = color, Offset = 0 });
        brush.GradientStops.Add(new GradientStop { Color = color, Offset = offset });
        brush.GradientStops.Add(new GradientStop { Color = Colors.Transparent, Offset = offset });
        brush.GradientStops.Add(new GradientStop { Color = Colors.Transparent, Offset = 1 });
        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

}
