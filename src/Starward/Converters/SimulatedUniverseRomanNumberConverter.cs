using Microsoft.UI.Xaml.Data;
using System;

namespace Starward.Converters;

internal class SimulatedUniverseRomanNumberConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (int)value switch
        {
            1 => "Ⅰ",
            2 => "Ⅱ",
            3 => "Ⅲ",
            4 => "Ⅳ",
            5 => "Ⅴ",
            6 => "Ⅵ",
            7 => "Ⅶ",
            8 => "Ⅷ",
            9 => "Ⅸ",
            10 => "Ⅹ",
            _ => "",
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

}