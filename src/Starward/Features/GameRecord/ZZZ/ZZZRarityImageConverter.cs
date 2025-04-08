using Microsoft.UI.Xaml.Data;
using System;
using Starward.Core.GameRecord.ZZZ.Common;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Starward.Features.GameRecord.ZZZ;

internal class ZZZRarityImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var img = (ZZZRarity)value switch
        {
            ZZZRarity.A => "ms-appx:///Assets/Image/A_Rank.png",
            ZZZRarity.S => "ms-appx:///Assets/Image/S_Rank.png",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
        return new BitmapImage(new Uri(img));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
