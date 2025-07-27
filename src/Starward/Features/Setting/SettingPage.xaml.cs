using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Starward.Frameworks;
using System;


namespace Starward.Features.Setting;

public sealed partial class SettingPage : PageBase
{


    private readonly ILogger<SettingPage> _logger = AppConfig.GetLogger<SettingPage>();


    public SettingPage()
    {
        this.InitializeComponent();
        Frame_Setting.Navigate(typeof(AboutSetting));
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, _) => this.Bindings.Update());
    }



    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            Type? type = args.InvokedItemContainer?.Tag switch
            {
                nameof(AboutSetting) => typeof(AboutSetting),
                nameof(GeneralSetting) => typeof(GeneralSetting),
                nameof(DownloadSetting) => typeof(DownloadSetting),
                nameof(FileManageSetting) => typeof(FileManageSetting),
                nameof(ScreenshotSetting) => typeof(ScreenshotSetting),
                nameof(AdvancedSetting) => typeof(AdvancedSetting),
                nameof(ToolboxSetting) => typeof(ToolboxSetting),
                nameof(HotkeySetting) => typeof(HotkeySetting),
                nameof(GamepadControlSetting) => typeof(GamepadControlSetting),
                _ => null,
            };
            if (type is not null)
            {
                Frame_Setting.Navigate(type);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Setting page navigate.");
        }
    }



    protected override void OnUnloaded()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }



}
