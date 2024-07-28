using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Helpers;
using Starward.Services.Download;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls;

[INotifyPropertyChanged]
public sealed partial class InstallGameDialog : ContentDialog
{


    private const double GB = 1 << 30;


    private readonly ILogger<InstallGameDialog> _logger = AppConfig.GetLogger<InstallGameDialog>();


    private InstallGameService _installGameService;


    public InstallGameDialog()
    {
        this.InitializeComponent();
    }



    public GameBiz CurrentGameBiz { get; set; }



    [ObservableProperty]
    private string installationPath;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnzipSpaceText))]
    private long unzipSpaceBytes;

    public string UnzipSpaceText => UnzipSpaceBytes == 0 ? "..." : $"{UnzipSpaceBytes / GB:F2} GB";


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvailableSpaceText))]
    private long availableSpaceBytes;

    public string AvailableSpaceText => AvailableSpaceBytes == 0 ? "..." : $"{AvailableSpaceBytes / GB:F2} GB";






    private async void ContentDialog_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _installGameService = InstallGameService.FromGameBiz(CurrentGameBiz);
            if (Directory.Exists(InstallationPath))
            {
                await ChangeInstallationPathInternalAsync(InstallationPath);
            }
            else
            {
                string? path = AppConfig.DefaultGameInstallationPath;
                if (Directory.Exists(path))
                {
                    string folder = Path.Combine(path, CurrentGameBiz.ToString());
                    InstallationPath = folder;
                    if (CanCreateDirectory(folder))
                    {
                        await ChangeInstallationPathInternalAsync(folder);
                    }
                    else
                    {
                        Button_Installation.IsEnabled = false;
                        StackPanel_FreeSpace.Visibility = Visibility.Collapsed;
                        TextBlock_NoPermission.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    UnzipSpaceBytes = await _installGameService.GetGamePackageDecompressedSizeAsync(CurrentGameBiz);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize install game dialog {biz}.", CurrentGameBiz);
        }
    }




    private void ContentDialog_Unloaded(object sender, RoutedEventArgs e)
    {

    }




    private bool CanCreateDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }



    [RelayCommand]
    private async Task ChangeInstallationPathAsync()
    {
        await ChangeInstallationPathInternalAsync();
    }




    private async Task ChangeInstallationPathInternalAsync(string? path = null)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                path = await FileDialogHelper.PickFolderAsync(MainWindow.Current.WindowHandle);
            }
            if (Directory.Exists(path))
            {
                TextBlock_InstallationPath.FontSize = 14;
                InstallationPath = path;
                if (_installGameService.CheckAccessPermission(path))
                {
                    StackPanel_FreeSpace.Visibility = Visibility.Visible;
                    TextBlock_NoPermission.Visibility = Visibility.Collapsed;

                    AvailableSpaceBytes = new DriveInfo(path).AvailableFreeSpace;
                    UnzipSpaceBytes = await _installGameService.GetGamePackageDecompressedSizeAsync(CurrentGameBiz);
                    if (UnzipSpaceBytes > 0)
                    {
                        Button_Installation.IsEnabled = true;
                    }
                    if (UnzipSpaceBytes > AvailableSpaceBytes)
                    {
                        TextBlock_AvailableSpace.Foreground = App.Current.Resources["SystemFillColorCautionBrush"] as Brush;
                    }
                    else
                    {
                        TextBlock_AvailableSpace.Foreground = App.Current.Resources["TextFillColorSecondaryBrush"] as Brush;
                    }
                }
                else
                {
                    Button_Installation.IsEnabled = false;
                    StackPanel_FreeSpace.Visibility = Visibility.Collapsed;
                    TextBlock_NoPermission.Visibility = Visibility.Visible;
                }
            }
        }
        catch (Exception ex)
        {

        }
    }





    [RelayCommand]
    private async Task StartInstallationAsync()
    {
        try
        {
            await _installGameService.InitializeAsync(CurrentGameBiz, InstallationPath);
            await _installGameService.StartInstallGameAsync();
            InstallGameManager.Instance.AddInstallService(_installGameService);
            AppConfig.SetGameInstallPath(CurrentGameBiz, InstallationPath);
            this.Hide();
        }
        catch (Exception ex)
        {
            NotificationBehavior.Instance.Error(ex, "Start installation failed.", 10000);
            _logger.LogError(ex, "Start installation {biz} failed.", CurrentGameBiz);
        }
    }




    [RelayCommand]
    private void Close()
    {
        this.Hide();
    }



    /// <summary>
    /// Restart as administrator
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void Hyperlink_Restart_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
    {
        try
        {
            var file = Process.GetCurrentProcess().MainModule?.FileName;
            if (!File.Exists(file))
            {
                file = Path.Combine(AppContext.BaseDirectory, "Starward.exe");
            }
            if (File.Exists(file))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = file,
                    UseShellExecute = true,
                    Verb = "runas",
                });
                Environment.Exit(0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "restart");
        }
    }



    /// <summary>
    /// Locate game
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private async void Hyperlink_LocateGame_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
    {
        try
        {
            var path = await FileDialogHelper.PickFolderAsync(MainWindow.Current.WindowHandle);
            if (Directory.Exists(path))
            {
                AppConfig.SetGameInstallPath(CurrentGameBiz, path);
                this.Hide();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Locate game");
        }
    }




    private void TextBlock_InstallationPath_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        sender.FontSize = 12;
    }


}
