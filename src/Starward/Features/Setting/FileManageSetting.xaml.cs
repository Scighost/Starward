using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using SharpSevenZip;
using Starward.Controls;
using Starward.Features.Database;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;


namespace Starward.Features.Setting;

[INotifyPropertyChanged]
public sealed partial class FileManageSetting : UserControl
{

    private readonly ILogger<FileManageSetting> _logger = AppConfig.GetLogger<FileManageSetting>();


    public FileManageSetting()
    {
        this.InitializeComponent();
        Loaded += FileManageSetting_Loaded;
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, _) => this.Bindings.Update());
        this.Unloaded += (_, _) => WeakReferenceMessenger.Default.UnregisterAll(this);
    }



    private async void FileManageSetting_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(300);
        GetLastBackupTime();
        _ = UpdateCacheSizeAsync();
    }




    #region 数据文件夹



    /// <summary>
    /// 修改数据文件夹
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task ChangeUserDataFolderAsync()
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
                AppConfig.SaveConfiguration();
                AppInstance.GetCurrent().UnregisterKey();
                Process.Start(AppConfig.StarwardExecutePath);
                App.Current.Exit();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change data folder");
        }
    }



    /// <summary>
    /// 打开数据文件夹
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OpenUserDataFolderAsync()
    {
        try
        {
            if (Directory.Exists(AppConfig.UserDataFolder))
            {
                await Launcher.LaunchUriAsync(new Uri(AppConfig.UserDataFolder));
            }
        }
        catch (Exception ex)
        {

        }
    }


    /// <summary>
    /// 删除所有设置
    /// </summary>
    /// <returns></returns>
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
                AppInstance.GetCurrent().UnregisterKey();
                Process.Start(AppConfig.StarwardExecutePath);
                App.Current.Exit();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete all setting");
        }

    }



    private void TextBlock_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        if (sender.FontSize > 12)
        {
            sender.FontSize -= 1;
        }
    }



    #endregion




    #region 备份数据库



    public string LastDatabaseBackupTime { get; set => SetProperty(ref field, value); }


    private void GetLastBackupTime()
    {
        try
        {
            if (DatabaseService.TryGetValue("LastBackupDatabase", out string? file, out DateTime time))
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
            if (Directory.Exists(AppConfig.UserDataFolder))
            {
                var folder = Path.Combine(AppConfig.UserDataFolder, "DatabaseBackup");
                Directory.CreateDirectory(folder);
                DateTime time = DateTime.Now;
                await Task.Run(() =>
                {
                    string file = Path.Combine(folder, $"StarwardDatabase_{time:yyyyMMdd_HHmmss}.db");
                    string archive = Path.ChangeExtension(file, ".7z");
                    DatabaseService.BackupDatabase(file);
                    new SharpSevenZipCompressor().CompressFiles(archive, file);
                    DatabaseService.SetValue("LastBackupDatabase", Path.GetFileName(archive), time);
                    File.Delete(file);
                });
                LastDatabaseBackupTime = $"{Lang.SettingPage_LastBackup}  {time:yyyy-MM-dd HH:mm:ss}";
            }
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
            if (DatabaseService.TryGetValue("LastBackupDatabase", out string? file, out DateTime time))
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



    /// <summary>
    /// 打开日志文件夹
    /// </summary>
    /// <returns></returns>
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
                await Launcher.LaunchFolderPathAsync(Path.Combine(AppConfig.CacheFolder, "log"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open log folder");
        }
    }



    #endregion




    #region Cache



    public string LogCacheSize { get; set => SetProperty(ref field, value); } = "0.00 KB";

    public string ImageCacheSize { get; set => SetProperty(ref field, value); } = "0.00 KB";

    public string WebCacheSize { get; set => SetProperty(ref field, value); } = "0.00 KB";

    public string GameCacheSize { get; set => SetProperty(ref field, value); } = "0.00 KB";


    /// <summary>
    /// 更新缓存大小
    /// </summary>
    /// <returns></returns>
    private async Task UpdateCacheSizeAsync()
    {
        try
        {
            var local = AppConfig.CacheFolder;
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



    /// <summary>
    /// 清除缓存
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        try
        {
            var local = AppConfig.CacheFolder;
            await DeleteFolderAsync(Path.Combine(local, "log"));
            await DeleteFolderAsync(Path.Combine(local, "crash"));
            await DeleteFolderAsync(Path.Combine(local, "cache"));
            await DeleteFolderAsync(Path.Combine(local, "webview"));
            await DeleteFolderAsync(Path.Combine(local, "update"));
            await DeleteFolderAsync(Path.Combine(local, "game"));
            await ClearDuplicateBgAsync();
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



    private async Task ClearDuplicateBgAsync()
    {
        try
        {
            string folder = Path.Join(AppConfig.UserDataFolder, "bg");
            if (Directory.Exists(folder))
            {
                string[] files = Directory.GetFiles(folder, "*");
                ConcurrentDictionary<string, bool> dict = new();
                await Parallel.ForEachAsync(files, async (file, _) =>
                {
                    using FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    string hash = Convert.ToHexString(await SHA256.HashDataAsync(fs));
                    if (dict.TryAdd(hash, true))
                    {
                        return;
                    }
                    fs.Dispose();
                    File.Delete(file);
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clear duplicate bg");
        }
    }



    #endregion



}
