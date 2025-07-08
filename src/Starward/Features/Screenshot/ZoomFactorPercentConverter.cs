using Microsoft.UI.Xaml.Data;
using System;


namespace Starward.Features.Screenshot;

public class ZoomFactorPercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return ((double)value).ToString("P0");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}