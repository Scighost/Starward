using Microsoft.UI.Xaml.Data;
using System;

namespace Starward.Converters;

internal class TimeSpanToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TimeSpan time)
        {
            return $"{Math.Floor(time.TotalHours)}h {time.Minutes}m";
        }
        else
        {
            return null!;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
