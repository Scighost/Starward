using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Starward.Converters;

internal class SelfQueryStatsNumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        long num = (long)value;
        if (num > 0)
        {
            return $"+{num}";
        }
        if (num < 0)
        {
            return num.ToString();
        }
        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}



internal class SelfQueryStatsNumberBrushConverter : IValueConverter
{

    private static Brush? GreenBrush = App.Current.Resources["SystemFillColorSuccessBrush"] as Brush;
    private static Brush? RedBrush = App.Current.Resources["SystemFillColorCriticalBrush"] as Brush;
    private static Brush? DefaultBrush = App.Current.Resources["TextFillColorSecondaryBrush"] as Brush;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        long num = (long)value;
        if (num > 0)
        {
            return GreenBrush!;
        }
        if (num < 0)
        {
            return RedBrush!;
        }
        return DefaultBrush!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
