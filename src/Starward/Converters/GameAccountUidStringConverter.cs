using Microsoft.UI.Xaml.Data;
using System;

namespace Starward.Converters;

public class GameAccountUidStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            int uid when uid > 0 => uid.ToString(),
            _ => string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (int.TryParse(value as string, out int uid))
        {
            return uid;
        }
        else
        {
            return 0;
        }
    }
}

