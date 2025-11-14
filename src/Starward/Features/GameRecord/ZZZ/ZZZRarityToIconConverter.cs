using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace Starward.Features.GameRecord.ZZZ;

internal partial class ZZZRarityToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var img = value switch
        {
            "S" => "ms-appx:///Assets/Image/S_Level_S.png",
            "A" => "ms-appx:///Assets/Image/A_Level_S.png",
            "B" => "ms-appx:///Assets/Image/B_Level_S.png",
            _ => "ms-appx:///Assets/Image/Transparent.png",
        };
        return new BitmapImage(new Uri(img));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
