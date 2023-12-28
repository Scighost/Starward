using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Starward.Services;
using System;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.Setting;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class AboutSettingPage : Page
{


    private readonly ILogger<AboutSettingPage> _logger = AppConfig.GetLogger<AboutSettingPage>();


    private readonly UpdateService _updateService = AppConfig.GetService<UpdateService>();



    public AboutSettingPage()
    {
        this.InitializeComponent();
    }




    [ObservableProperty]
    private bool enablePreviewRelease = AppConfig.EnablePreviewRelease;
    partial void OnEnablePreviewReleaseChanged(bool value)
    {
        AppConfig.EnablePreviewRelease = value;
    }


    [ObservableProperty]
    private bool isUpdated;


    [ObservableProperty]
    private string? updateErrorText;


    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        try
        {
            IsUpdated = false;
            UpdateErrorText = null;
            var release = await _updateService.CheckUpdateAsync(true);
            if (release != null)
            {
                MainWindow.Current.OverlayFrameNavigateTo(typeof(UpdatePage), release, new SlideNavigationTransitionInfo());
            }
            else
            {
                IsUpdated = true;
            }
        }
        catch (Exception ex)
        {
            UpdateErrorText = ex.Message;
            _logger.LogError(ex, "Check update");
        }
    }




}
