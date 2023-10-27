using Microsoft.UI.Xaml.Data;
using System;

namespace Starward.Converters;

internal class SimulatedUniverseWorldIconConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return $"ms-appx:///Assets/Image/PicRoguePlanetM{(int)value}.png";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

}
