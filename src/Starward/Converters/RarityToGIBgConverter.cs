using Microsoft.UI.Xaml.Data;
using System;

namespace Starward.Converters;

internal class RarityToGIBgConverter : IValueConverter
{

    private const string Rarity1Background = "ms-appx:///Assets/Image/Rarity_1_Background.png";
    private const string Rarity2Background = "ms-appx:///Assets/Image/Rarity_2_Background.png";
    private const string Rarity3Background = "ms-appx:///Assets/Image/Rarity_3_Background.png";
    private const string Rarity4Background = "ms-appx:///Assets/Image/Rarity_4_Background.png";
    private const string Rarity5Background = "ms-appx:///Assets/Image/Rarity_5_Background.png";
    private const string TransparentBackground = "ms-appx:///Assets/Image/Transparent.png";

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (int)value switch
        {
            1 => Rarity1Background,
            2 => Rarity2Background,
            3 => Rarity3Background,
            4 => Rarity4Background,
            5 => Rarity5Background,
            _ => TransparentBackground,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

}
