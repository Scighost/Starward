using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Helpers;
using Starward.Services.Download;
using Starward.Services.Launcher;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using Vanara.Extensions;
using Vanara.PInvoke;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls;

[INotifyPropertyChanged]
public sealed partial class InstallGameDialog : ContentDialog
{


    private const double GB = 1 << 30;


    private readonly ILogger<InstallGameDialog> _logger = AppConfig.GetLogger<InstallGameDialog>();

    private readonly GameLauncherService _gameLauncherService = AppConfig.GetService<GameLauncherService>();

    private InstallGameService _installGameService;


    public InstallGameDialog()
    {
        this.InitializeComponent();
    }



    public GameBiz CurrentGameBiz { get; set; }



    [ObservableProperty]
    private string installationPath;


    [ObservableProperty]
    public bool isSupportHardLink;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnzipSpaceText))]
    private long unzipSpaceBytes;

    public string UnzipSpaceText => UnzipSpaceBytes == 0 ? "..." : $"{UnzipSpaceBytes / GB:F2} GB";


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvailableSpaceText))]
    private long availableSpaceBytes;

    public string AvailableSpaceText => AvailableSpaceBytes == 0 ? "..." : $"{AvailableSpaceBytes / GB:F2} GB";



    [ObservableProperty]
    private bool desktopShortcut;


    [ObservableProperty]
    private bool hardLink;
    partial void OnHardLinkChanged(bool value)
    {
        if (value && !IsAdmin())
        {
            Button_Installation.IsEnabled = false;
            StackPanel_FreeSpace.Visibility = Visibility.Collapsed;
            TextBlock_NoPermission.Visibility = Visibility.Visible;
            TextBlock_LinkWarning.Visibility = Visibility.Collapsed;
        }
        else if (string.IsNullOrWhiteSpace(InstallationPath))
        {
            Button_Installation.IsEnabled = false;
            StackPanel_FreeSpace.Visibility = Visibility.Visible;
            TextBlock_NoPermission.Visibility = Visibility.Collapsed;
            TextBlock_LinkWarning.Visibility = Visibility.Collapsed;
        }
        else
        {
            _ = ChangeInstallationPathInternalAsync(InstallationPath);
        }
    }


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HardLinkTargetText))]
    private GameBiz hardLinkTarget;


    [ObservableProperty]
    private string hardLinkPath;


    public string HardLinkTargetText => $"{HardLinkTarget.ToGameName()} - {HardLinkTarget.ToGameServerName()}";



    private async void ContentDialog_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _installGameService = InstallGameService.FromGameBiz(CurrentGameBiz);
            await InitializeHardLinkAsync();
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
                        TextBlock_LinkWarning.Visibility = Visibility.Collapsed;
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



    private async Task InitializeHardLinkAsync()
    {
        try
        {
            if (CurrentGameBiz.ToGame().Value is GameBiz.hk4e or GameBiz.hkrpg or GameBiz.nap)
            {
                foreach (var biz in GameBiz.AllGameBizs)
                {
                    if (biz.ToGame() == CurrentGameBiz.ToGame() && biz != CurrentGameBiz && biz != GameBiz.clgm_cn)
                    {
                        var path = _gameLauncherService.GetGameInstallPath(biz);
                        var version = await _gameLauncherService.GetLocalGameVersionAsync(biz, path);
                        var exe = _gameLauncherService.IsGameExeExists(biz, path);
                        (_, var link) = await _gameLauncherService.GetHardLinkInfoAsync(biz, path);
                        if (path != null && version != null && exe && string.IsNullOrWhiteSpace(link))
                        {
                            HardLinkTarget = biz;
                            HardLinkPath = path;
                            IsSupportHardLink = true;
                            return;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize symbolic link");
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



    private bool IsAdmin()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
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
                    if (HardLink)
                    {
                        if (Path.GetPathRoot(HardLinkPath) != Path.GetPathRoot(path))
                        {
                            Button_Installation.IsEnabled = false;
                            StackPanel_FreeSpace.Visibility = Visibility.Collapsed;
                            TextBlock_NoPermission.Visibility = Visibility.Collapsed;
                            TextBlock_LinkWarning.Visibility = Visibility.Visible;
                            return;
                        }
                    }

                    StackPanel_FreeSpace.Visibility = Visibility.Visible;
                    TextBlock_NoPermission.Visibility = Visibility.Collapsed;
                    TextBlock_LinkWarning.Visibility = Visibility.Collapsed;
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
                    TextBlock_LinkWarning.Visibility = Visibility.Collapsed;
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
            if (HardLink)
            {
                await _installGameService.StartHardLinkAsync(HardLinkTarget);
            }
            else
            {
                await _installGameService.StartInstallGameAsync();
            }
            InstallGameManager.Instance.AddInstallService(_installGameService);
            AppConfig.SetGameInstallPath(CurrentGameBiz, InstallationPath);
            AppConfig.SetGameInstallPathRemovable(CurrentGameBiz, DriveHelper.IsDeviceRemovableOrOnUSB(InstallationPath));
            if (DesktopShortcut)
            {
                CreateDesktopShortcut();
            }
            this.Hide();
        }
        catch (Exception ex)
        {
            NotificationBehavior.Instance.Error(ex, "Start installation failed.", 10000);
            _logger.LogError(ex, "Start installation {biz} failed.", CurrentGameBiz);
        }
    }




    private void CreateDesktopShortcut()
    {
        try
        {
            string name = $"{CurrentGameBiz.ToGameName()} - {CurrentGameBiz.ToGameServerName()}.lnk";
            var savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), name);
            string exe;
            if (AppConfig.IsPortable)
            {
                string? baseDir = Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd('/', '\\'));
                exe = Path.Join(baseDir, "Starward.exe");
            }
            else
            {
                var temp = Process.GetCurrentProcess().MainModule?.FileName;
                if (!File.Exists(temp))
                {
                    temp = Path.Combine(AppContext.BaseDirectory, "Starward.exe");
                }
                exe = temp;
            }
            string? icon = GetIconPath(CurrentGameBiz);
            if (File.Exists(exe) && File.Exists(icon))
            {
                Ole32.CoCreateInstance(new Guid("00021401-0000-0000-C000-000000000046"), null, Ole32.CLSCTX.CLSCTX_INPROC_SERVER, new Guid("000214F9-0000-0000-C000-000000000046"), out object ppv);
                if (ppv is Shell32.IShellLinkW shellLink)
                {
                    shellLink.SetPath(exe);
                    shellLink.SetArguments($"startgame --biz {CurrentGameBiz}");
                    shellLink.SetIconLocation(icon, 0);
                    shellLink.QueryInterface(new Guid("0000010b-0000-0000-C000-000000000046"), out object? ppf);
                    if (ppf is IPersistFile file)
                    {
                        file.Save(savePath, true);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create desktop shortcut");
        }
    }




    private static string? GetIconPath(GameBiz gameBiz)
    {
        try
        {
            var source = gameBiz.ToGame().Value switch
            {
                GameBiz.bh3 => @"Assets\Image\icon_bh3.ico",
                GameBiz.hk4e => @"Assets\Image\icon_ys.ico",
                GameBiz.hkrpg => @"Assets\Image\icon_sr.ico",
                GameBiz.nap => @"Assets\Image\icon_zzz.ico",
                _ => "",
            };
            source = Path.Combine(AppContext.BaseDirectory, source);
            if (File.Exists(source))
            {
                var target = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Starward\icon");
                Directory.CreateDirectory(target);
                target = Path.Combine(target, Path.GetFileName(source));
                File.Move(source, target, true);
                return target;
            }
        }
        catch { }
        return null;
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
                AppConfig.SetGameInstallPathRemovable(CurrentGameBiz, DriveHelper.IsDeviceRemovableOrOnUSB(path));
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




    [ComImport]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPersistFile
    {
        // IPersist portion
        void GetClassID(out Guid pClassID);

        // IPersistFile portion
        [PreserveSig]
        int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string? pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }



}
