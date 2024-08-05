using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Starward.Helpers;
using Starward.Services.Download;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.Setting;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class DownloadSettingPage : PageBase
{


    private readonly ILogger<DownloadSettingPage> _logger = AppConfig.GetLogger<DownloadSettingPage>();


    public DownloadSettingPage()
    {
        this.InitializeComponent();
        InitializeDefaultInstallPath();
    }




    private void InitializeDefaultInstallPath()
    {
        try
        {
            string? path = AppConfig.DefaultGameInstallationPath;
            if (Directory.Exists(path))
            {
                DefaultInstallPath = path;
            }
            else
            {
                AppConfig.DefaultGameInstallationPath = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get default intall path");
        }
    }



    [ObservableProperty]
    private string? defaultInstallPath;
    partial void OnDefaultInstallPathChanged(string? value)
    {
        AppConfig.DefaultGameInstallationPath = value;
    }



    [ObservableProperty]
    private int speedLimit = AppConfig.SpeedLimitKBPerSecond;
    partial void OnSpeedLimitChanged(int value)
    {
        InstallGameManager.SetRateLimit(value * 1024);
        AppConfig.SpeedLimitKBPerSecond = value;
    }



    [RelayCommand]
    private async Task ChangeDefaultInstallPathAsync()
    {
        try
        {
            var path = await FileDialogHelper.PickFolderAsync(MainWindow.Current.WindowHandle);
            if (Directory.Exists(path))
            {
                DefaultInstallPath = path;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change default install path");
        }
    }




    [RelayCommand]
    private async Task OpenDefaultInstallPathAsync()
    {

        try
        {
            if (Directory.Exists(DefaultInstallPath))
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(DefaultInstallPath);
                await Launcher.LaunchFolderAsync(folder);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open default install path");
        }
    }




    [RelayCommand]
    private void DeleteDefaultInstallPath()
    {
        DefaultInstallPath = null;
    }



}
