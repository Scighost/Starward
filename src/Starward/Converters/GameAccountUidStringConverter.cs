using Microsoft.UI.Xaml.Data;
using System;

namespace Starward.Converters;

public class GameAccountUidStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            long uid when uid > 0 => uid.ToString(),
            _ => string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (long.TryParse(value as string, out long uid))
        {
            return uid;
        }
        else
        {
            return 0L;
        }
    }
}

