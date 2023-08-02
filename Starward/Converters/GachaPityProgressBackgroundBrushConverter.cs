using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Starward.Core.Gacha;
using Starward.Models;
using System;
using Windows.Foundation;
using Windows.UI;

namespace Starward.Converters;

internal class GachaPityProgressBackgroundBrushConverter : IValueConverter
{

    private static Color Red = Color.FromArgb(0xFF, 0xC8, 0x3C, 0x23);
    private static Color Green = Color.FromArgb(0xFF, 0x00, 0xE0, 0x79);

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is GachaLogItemEx item)
        {
            int pity = item.Pity;
            GachaType type = item.GachaType;
            var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0), Opacity = 0.4 };
            int point = 74;
            double guarantee = 90;
            if (type is GachaType.WeaponEventWish or GachaType.LightConeEventWarp)
            {
                point = 63;
                guarantee = 80;
            }
            double offset = pity / guarantee;
            if (pity < point)
            {
                brush.GradientStops.Add(new GradientStop { Color = Green, Offset = 0 });
                brush.GradientStops.Add(new GradientStop { Color = Green, Offset = offset });
                brush.GradientStops.Add(new GradientStop { Color = Colors.Transparent, Offset = offset });
            }
            else
            {
                brush.GradientStops.Add(new GradientStop { Color = Red, Offset = 0 });
                brush.GradientStops.Add(new GradientStop { Color = Red, Offset = offset });
                brush.GradientStops.Add(new GradientStop { Color = Colors.Transparent, Offset = offset });
            }
            return brush;
        }
        return null!;
    }


    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

}
