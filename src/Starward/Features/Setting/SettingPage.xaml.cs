using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Starward.Frameworks;


namespace Starward.Features.Setting;

public sealed partial class SettingPage : PageBase
{


    public SettingPage()
    {
        this.InitializeComponent();
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, _) => this.Bindings.Update());
    }



    private void FlipView_Settings_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var grid = VisualTreeHelper.GetChild(FlipView_Settings, 0);
            if (grid != null)
            {
                var count = VisualTreeHelper.GetChildrenCount(grid);
                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var child = VisualTreeHelper.GetChild(grid, i);
                        if (child is Button button)
                        {
                            button.IsHitTestVisible = false;
                            button.Opacity = 0;
                        }
                        else if (child is ScrollViewer scrollViewer)
                        {
                            scrollViewer.PointerWheelChanged += (_, e) => e.Handled = true;
                        }
                    }
                }
            }
        }
        catch { }
    }




    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            if (args.InvokedItemContainer?.Tag is string index && int.TryParse(index, out int target))
            {
                int steps = target - FlipView_Settings.SelectedIndex;
                if (steps > 0)
                {
                    for (int i = 0; i < steps; i++)
                    {
                        FlipView_Settings.SelectedIndex++;
                    }
                }
                else
                {
                    for (int i = 0; i < -steps; i++)
                    {
                        FlipView_Settings.SelectedIndex--;
                    }
                }
            }
        }
        catch { }
    }




    protected override void OnUnloaded()
    {
        FlipView_Settings.Items.Clear();
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }



}
