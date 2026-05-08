using Microsoft.UI.Xaml;

namespace Starward.Helpers;

public static class BindConverter
{


    public static Visibility BoolToVisibility(bool value)
    {
        return value ? Visibility.Visible : Visibility.Collapsed;
    }


    public static Visibility BoolToVisibilityReversed(bool value)
    {
        return value ? Visibility.Collapsed : Visibility.Visible;
    }


    public static Visibility ObjectToVisibility(object? value)
    {
        return value is not null ? Visibility.Visible : Visibility.Collapsed;
    }


    public static Visibility ObjectToVisibilityReversed(object? value)
    {
        return value is null ? Visibility.Visible : Visibility.Collapsed;
    }



}
