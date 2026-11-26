using Microsoft.UI.Xaml.Data;
using System;

namespace Starward.Converters;

internal partial class EnumToIntConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Enum)
        {
            return System.Convert.ToInt32(value);
        }
        else
        {
            return 0;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        Type enumType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return Enum.ToObject(enumType, System.Convert.ToInt32(value));
    }
}