using Microsoft.UI.Xaml.Data;
using System;
using Starward.Core.GameRecord.ZZZ.Common;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Starward.Features.GameRecord.ZZZ;

internal class ZZZRatingImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is "1")
        {
            var img = (ZZZRating)value switch //  Larger Icon
            {
                ZZZRating.C => "ms-appx:///Assets/Image/C_Level.png",
                ZZZRating.B => "ms-appx:///Assets/Image/B_Level.png",
                ZZZRating.A => "ms-appx:///Assets/Image/A_Level.png",
                ZZZRating.S => "ms-appx:///Assets/Image/S_Level.png",
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
            return new BitmapImage(new Uri(img));
        }
        else
        {
            var img = (ZZZRating)value switch
            {
                ZZZRating.C => "ms-appx:///Assets/Image/C_Level_S.png",
                ZZZRating.B => "ms-appx:///Assets/Image/B_Level_S.png",
                ZZZRating.A => "ms-appx:///Assets/Image/A_Level_S.png",
                ZZZRating.S => "ms-appx:///Assets/Image/S_Level_S.png",
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
            return new BitmapImage(new Uri(img));
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
