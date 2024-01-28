using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Starward.Controls;
using Starward.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
public sealed partial class FileSettingPage : PageBase
{

    private readonly ILogger<FileSettingPage> _logger = AppConfig.GetLogger<FileSettingPage>();

    private readonly DatabaseService _databaseService = AppConfig.GetService<DatabaseService>();


    public FileSettingPage()
    {
        this.InitializeComponent();
    }



    protected override async void OnLoaded()
    {
        GetLastBackupTime();
        await UpdateCacheSizeAsync();
    }




    #region Database



    [ObservableProperty]
    private string lastDatabaseBackupTime;


    [RelayCommand]
    private async Task OpenDataFolderAsync()
    {
        try
        {
            _logger.LogInformation("Open folder '{folder}'", AppConfig.UserDataFolder);
            await Launcher.LaunchFolderPathAsync(AppConfig.UserDataFolder);
        }
        catch { }
    }


    [RelayCommand]
    private async Task ChangeDataFolderAsync()
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = Lang.SettingPage_ReselectDataFolder,
                // 当前数据文件夹的位置是：
                // 想要重新选择吗？（你需要在选择前手动迁移数据文件）
                Content = $"""
                {Lang.SettingPage_TheCurrentLocationOfTheDataFolderIs}

                {AppConfig.UserDataFolder}

                {Lang.SettingPage_WouldLikeToReselectDataFolder}
                """,
                PrimaryButtonText = Lang.Common_Yes,
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (result is ContentDialogResult.Primary)
            {
                AppConfig.UserDataFolder = null!;
                AppConfig.ResetServiceProvider();
                AppConfig.SaveConfiguration();
                App.Current.CloseSystemTray();
                App.Current.SwitchMainWindow(new WelcomeWindow());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change data folder");
        }
    }


    [RelayCommand]
    private async Task DeleteAllSettingAsync()
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = Lang.SettingPage_DeleteAllSettings,
                // 删除完成后，将自动重启软件。
                Content = Lang.SettingPage_AfterDeletingTheSoftwareWillBeRestartedAutomatically,
                PrimaryButtonText = Lang.Common_Delete,
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                AppConfig.DeleteAllSettings();
                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (File.Exists(exe))
                {
                    Process.Start(exe);
                    Environment.Exit(0);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete all setting");
        }

    }




    private void GetLastBackupTime()
    {
        try
        {
            if (_databaseService.TryGetValue("LastBackupDatabase", out string? file, out DateTime time))
            {
                file = Path.Join(AppConfig.UserDataFolder, "DatabaseBackup", file);
                if (File.Exists(file))
                {
                    LastDatabaseBackupTime = $"{Lang.SettingPage_LastBackup}  {time:yyyy-MM-dd HH:mm:ss}";
                }
                else
                {
                    _logger.LogWarning("Last backup database file not found: {file}", file);
                }
            }
        }
        catch { }
    }



    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        try
        {
            var folder = Path.Combine(AppConfig.UserDataFolder, "DatabaseBackup");
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, $"StarwardDatabase_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            var time = await Task.Run(() => _databaseService.BackupDatabase(file));
            LastDatabaseBackupTime = $"{Lang.SettingPage_LastBackup}  {time:yyyy-MM-dd HH:mm:ss}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup database");
            LastDatabaseBackupTime = ex.Message;
        }
    }



    [RelayCommand]
    private async Task OpenLastBackupDatabaseAsync()
    {
        try
        {
            if (_databaseService.TryGetValue("LastBackupDatabase", out string? file, out DateTime time))
            {
                file = Path.Join(AppConfig.UserDataFolder, "DatabaseBackup", file);
                if (File.Exists(file))
                {
                    var item = await StorageFile.GetFileFromPathAsync(file);
                    var folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(file));
                    var options = new FolderLauncherOptions
                    {
                        ItemsToSelect = { item }
                    };
                    await Launcher.LaunchFolderAsync(folder, options);
                }
                else
                {
                    _logger.LogWarning("Last backup database file not found: {file}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open last backup database");
        }
    }





    #endregion




    #region Log



    [RelayCommand]
    private async Task OpenLogFileAsync()
    {
        try
        {
            if (File.Exists(AppConfig.LogFile))
            {
                var item = await StorageFile.GetFileFromPathAsync(AppConfig.LogFile);
                await Launcher.LaunchFileAsync(item);
            }
            else
            {
                await Launcher.LaunchFolderPathAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Starward\log"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open log file");
        }
    }


    [RelayCommand]
    private async Task OpenLogFolderAsync()
    {
        try
        {
            if (File.Exists(AppConfig.LogFile))
            {
                var item = await StorageFile.GetFileFromPathAsync(AppConfig.LogFile);
                var options = new FolderLauncherOptions();
                options.ItemsToSelect.Add(item);
                await Launcher.LaunchFolderPathAsync(Path.GetDirectoryName(AppConfig.LogFile), options);
            }
            else
            {
                await Launcher.LaunchFolderPathAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Starward\log"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open log folder");
        }
    }



    #endregion





    #region Cache



    [ObservableProperty]
    private string logCacheSize = "0.00 KB";

    [ObservableProperty]
    private string imageCacheSize = "0.00 KB";

    [ObservableProperty]
    private string webCacheSize = "0.00 KB";

    [ObservableProperty]
    private string gameCacheSize = "0.00 KB";


    private async Task UpdateCacheSizeAsync()
    {
        try
        {
            var local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Starward");
            LogCacheSize = await GetFolderSizeStringAsync(Path.Combine(local, "log"));
            ImageCacheSize = await GetFolderSizeStringAsync(Path.Combine(local, "cache"));
            WebCacheSize = await GetFolderSizeStringAsync(Path.Combine(local, "webview"));
            GameCacheSize = await GetFolderSizeStringAsync(Path.Combine(local, "game"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update cache size");
        }
    }



    private static async Task<string> GetFolderSizeStringAsync(string folder) => await Task.Run(() =>
    {
        if (Directory.Exists(folder))
        {
            double size = Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Sum(file => new FileInfo(file).Length);
            if (size < (1 << 20))
            {
                return $"{size / (1 << 10):F2} KB";
            }
            else
            {
                return $"{size / (1 << 20):F2} MB";
            }
        }
        else
        {
            return "0.00 KB";
        }
    });



    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        try
        {
            var local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Starward");
            await DeleteFolderAsync(Path.Combine(local, "log"));
            await DeleteFolderAsync(Path.Combine(local, "crash"));
            await DeleteFolderAsync(Path.Combine(local, "cache"));
            await DeleteFolderAsync(Path.Combine(local, "webview"));
            await DeleteFolderAsync(Path.Combine(local, "update"));
            await DeleteFolderAsync(Path.Combine(local, "game"));
            CachedImage.ClearCache();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clear cache");
        }
        await UpdateCacheSizeAsync();
    }



    private async Task DeleteFolderAsync(string folder) => await Task.Run(() =>
    {
        if (Directory.Exists(folder))
        {
            try
            {
                Directory.Delete(folder, true);
                Directory.CreateDirectory(folder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete folder '{folder}'", folder);
            }
        }
    });




    #endregion




}
