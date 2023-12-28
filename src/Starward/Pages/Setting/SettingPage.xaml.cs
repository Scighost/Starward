using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.Setting;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingPage : Page
{

    public SettingPage()
    {
        this.InitializeComponent();
        SettingFrame.Navigate(typeof(AboutSettingPage));
    }


    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            if (args.InvokedItemContainer?.IsSelected ?? false)
            {
                return;
            }
            if (args.IsSettingsInvoked)
            {
            }
            else
            {
                var item = args.InvokedItemContainer as NavigationViewItem;
                if (item != null)
                {
                    if (item.Tag is "Completed")
                    {
                        MainWindow.Current.CloseOverlayPage();
                        return;
                    }
                    var type = item.Tag switch
                    {
                        nameof(AboutSettingPage) => typeof(AboutSettingPage),
                        nameof(AppearanceSettingPage) => typeof(AppearanceSettingPage),
                        nameof(FileSettingPage) => typeof(FileSettingPage),
                        nameof(AdvancedSettingPage) => typeof(AdvancedSettingPage),
                        _ => null,
                    };
                    if (type is not null)
                    {
                        SettingFrame.Navigate(type);
                    }
                }
            }
        }
        catch { }
    }


}
