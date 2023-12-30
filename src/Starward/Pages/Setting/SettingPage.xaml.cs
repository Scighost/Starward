using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Starward.Messages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.Setting;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingPage : PageBase
{

    public SettingPage()
    {
        this.InitializeComponent();
        SettingFrame.Navigate(typeof(AboutSettingPage));
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, m) =>
        {
            _languageChangedMessage = m;
            this.Bindings.Update();
        });

    }


    protected override void OnLoaded()
    {
        MainWindow.Current.KeyDown += SettingPage_KeyDown;
    }


    protected override void OnUnloaded()
    {
        if (MainWindow.Current is not null)
        {
            MainWindow.Current.KeyDown -= SettingPage_KeyDown;
        }
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }


    private void SettingPage_KeyDown(object? sender, MainWindow.KeyDownEventArgs e)
    {
        try
        {
            if (e.Handled)
            {
                return;
            }
            if (e.VirtualKey == Windows.System.VirtualKey.Escape)
            {
                ClosePage();
                e.Handled = true;
            }
        }
        catch { }
    }


    private void ClosePage()
    {
        MainWindow.Current.CloseOverlayPage();
    }


    private LanguageChangedMessage? _languageChangedMessage;



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
                        if (_languageChangedMessage is not null)
                        {
                            _languageChangedMessage.Completed = true;
                            WeakReferenceMessenger.Default.Send(_languageChangedMessage);
                        }
                        ClosePage();
                        return;
                    }
                    var type = item.Tag switch
                    {
                        nameof(AboutSettingPage) => typeof(AboutSettingPage),
                        nameof(AppearanceSettingPage) => typeof(AppearanceSettingPage),
                        nameof(ExperienceSettingPage) => typeof(ExperienceSettingPage),
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
