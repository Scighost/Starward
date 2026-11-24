using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Threading;


namespace Starward.Features;


[INotifyPropertyChanged]
public abstract partial class ContentDialogBase : ContentDialog
{

    private readonly CancellationTokenSource UnloadCancellationTokenSource = new();

    protected CancellationToken UnloadCancellationToken => UnloadCancellationTokenSource.Token;


    public ContentDialogBase()
    {
        Style = App.Current.Resources["DefaultContentDialogStyle"] as Style;
        Background = App.Current.Resources["CustomAcrylicBrush"] as Brush;
        BorderThickness = new Thickness(0);
        CornerRadius = new CornerRadius(8);
        Resources["ContentDialogMaxWidth"] = 100000d;
        Resources["ContentDialogPadding"] = new Thickness(0);
        Resources["ContentDialogSmokeFill"] = App.Current.Resources["ControlFillColorTransparentBrush"] as Brush;
        Resources["ContentDialogTopOverlay"] = App.Current.Resources["ControlFillColorTransparentBrush"] as Brush;
        Loaded += ContentDialogBase_Loaded;
        Unloaded += ContentDialogBase_Unloaded;
    }



    [RelayCommand]
    protected void Close()
    {
        this.Hide();
    }


    private void ContentDialogBase_Loaded(object sender, RoutedEventArgs e)
    {
        OnLoaded();
    }


    private void ContentDialogBase_Unloaded(object sender, RoutedEventArgs e)
    {
        UnloadCancellationTokenSource.Cancel();
        OnUnloaded();
        this.Loaded -= ContentDialogBase_Loaded;
        this.Unloaded -= ContentDialogBase_Unloaded;
    }


    protected virtual void OnLoaded()
    {

    }


    protected virtual void OnUnloaded()
    {

    }


    protected void TextBlock_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        if (sender.FontSize > 12)
        {
            sender.FontSize -= 1;
        }
    }

}
