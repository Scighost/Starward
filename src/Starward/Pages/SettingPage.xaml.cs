// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.Metadata;
using Starward.Helpers;
using Starward.Pages.Welcome;
using Starward.Services;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class SettingPage : Page
{

    private readonly ILogger<SettingPage> _logger = AppConfig.GetLogger<SettingPage>();

    private readonly MetadataClient _metadataClient = AppConfig.GetService<MetadataClient>();

    private readonly HttpClient _httpClient = AppConfig.GetService<HttpClient>();

    private readonly UpdateService _updateService = AppConfig.GetService<UpdateService>();

    private readonly LauncherService _launcherService = AppConfig.GetService<LauncherService>();

    private readonly SystemTrayService _systemTrayService = AppConfig.GetService<SystemTrayService>();

    private readonly DatabaseService _databaseService = AppConfig.GetService<DatabaseService>();

    private GameBiz gameBiz;

    public SettingPage()
    {
        this.InitializeComponent();
    }


    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is GameBiz biz)
        {
            gameBiz = biz;
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
            enableCustomBg = AppConfig.GetEnableCustomBg(biz);
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
            OnPropertyChanged(nameof(EnableCustomBg));
            CustomBg = AppConfig.GetCustomBg(biz);
        }
    }



    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeLanguage();
        GetLastBackupTime();
        switch (AppConfig.ApiCDNIndex)
        {
            case 1: RadioButton_GH.IsChecked = true; break;
            case 2: RadioButton_JD.IsChecked = true; break;
            default: RadioButton_CF.IsChecked = true; break;
        }
        switch (AppConfig.WindowSizeMode)
        {
            case 1: RadioButton_WindowSize_Small.IsChecked = true; break;
            default: RadioButton_WindowSize_Normal.IsChecked = true; break;
        }
    }





    #region Language


    private bool languageInitialized;

    private void InitializeLanguage()
    {
        try
        {
            var lang = AppConfig.Language;
            ComboBox_Language.Items.Clear();
            ComboBox_Language.Items.Add(new ComboBoxItem
            {
                Content = Lang.ResourceManager.GetString(nameof(Lang.SettingPage_FollowSystem), CultureInfo.InstalledUICulture),
                Tag = "",
            });
            ComboBox_Language.SelectedIndex = 0;
            foreach (var (Title, LangCode) in Localization.LanguageList)
            {
                var box = new ComboBoxItem
                {
                    Content = Title,
                    Tag = LangCode,
                };
                ComboBox_Language.Items.Add(box);
                if (LangCode == lang)
                {
                    ComboBox_Language.SelectedItem = box;
                }
            }
        }
        finally
        {
            languageInitialized = true;
        }
    }



    private void ComboBox_Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (ComboBox_Language.SelectedItem is ComboBoxItem item)
            {
                if (languageInitialized)
                {
                    var lang = item.Tag as string;
                    _logger.LogInformation("Language change to {lang}", lang);
                    AppConfig.Language = lang;
                    if (string.IsNullOrWhiteSpace(lang))
                    {
                        CultureInfo.CurrentUICulture = CultureInfo.InstalledUICulture;
                    }
                    else
                    {
                        CultureInfo.CurrentUICulture = new CultureInfo(lang);
                    }
                    MainPage.Current.ReloadTextForLanguage();
                    this.Bindings.Update();
                }
            }
        }
        catch (CultureNotFoundException)
        {
            CultureInfo.CurrentUICulture = CultureInfo.InstalledUICulture;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change Language");
        }
    }



    private void ComboBox_Language_DropDownOpened(object sender, object e)
    {
        MainWindow.Current.SetDragRectangles();
    }

    private void ComboBox_Language_DropDownClosed(object sender, object e)
    {
        MainPage.Current.UpdateDragRectangles();
    }



    #endregion




    #region Log


    [ObservableProperty]
    private bool enableConsole = AppConfig.EnableConsole;
    partial void OnEnableConsoleChanged(bool value)
    {
        AppConfig.EnableConsole = value;
        if (value)
        {
            ConsoleHelper.Show();
        }
        else
        {
            ConsoleHelper.Hide();
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



    #region Window Size



    private void RadioButton_WindowSize_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement fe)
            {
                var index = fe.Tag switch
                {
                    "small" => 1,
                    _ => 0,
                };
                AppConfig.WindowSizeMode = index;
                MainWindow.Current.ResizeToCertainSize();
            }
        }
        catch { }
    }




    #endregion




    #region System Tray


    [ObservableProperty]
    private bool enableSystemTray = AppConfig.EnableSystemTrayIcon;
    partial void OnEnableSystemTrayChanged(bool value)
    {
        AppConfig.EnableSystemTrayIcon = value;
        if (value)
        {
            _systemTrayService.Initialize();
        }
        else
        {
            _systemTrayService.Dispose();
        }
    }


    [ObservableProperty]
    private bool exitWhenClosing = AppConfig.ExitWhenClosing;
    partial void OnExitWhenClosingChanged(bool value)
    {
        AppConfig.ExitWhenClosing = value;
    }



    [RelayCommand]
    private async Task RepairSystemTray()
    {
        try
        {
            var dialog = new ContentDialog()
            {
                Title = Lang.SettingPage_RepairSystemTray,
                Content = Lang.SettingPage_RepairSystemTrayContent,
                PrimaryButtonText = Lang.Common_Confirm,
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };
            if (await dialog.ShowAsync() is ContentDialogResult.Primary)
            {
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "PowerShell",
                    Arguments = """
                    $null = New-PSDrive -Name HKCR -PSProvider Registry -Root HKEY_CLASSES_ROOT -ErrorAction Continue;
                    $key = 'HKCR:\Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify';
                    Remove-ItemProperty -Path $key -Name IconStreams -ErrorAction Continue;
                    Remove-ItemProperty -Path $key -Name PastIconsStream -ErrorAction Continue;
                    Stop-Process -Name explorer -ErrorAction Continue;
                    """,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                });
                if (p != null)
                {
                    await p.WaitForExitAsync();
                    _logger.LogInformation("Repair system tray log:\n{log}", await p.StandardOutput.ReadToEndAsync());
                }
                if (EnableSystemTray)
                {
                    await Task.Delay(1000);
                    _systemTrayService.Initialize();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Repair system tray");
        }
    }


    #endregion



    #region CDN



    [RelayCommand]
    private async Task TestCDNAsync()
    {
        try
        {
            const string url_cf = "https://starward.scighost.com/metadata/test/test_10kb";
            const string url_gh = "https://raw.githubusercontent.com/Scighost/Starward/metadata/test/test_10kb";
            const string url_jd = "https://cdn.jsdelivr.net/gh/Scighost/Starward@metadata/test/test_10kb";

            TextBlock_TestCND_CF.Text = "";
            TextBlock_TestCDN_GH.Text = "";
            TextBlock_TestCDN_JD.Text = "";

            ProgressRing_TestCND_CF.Visibility = Visibility.Visible;
            ProgressRing_TestCND_GH.Visibility = Visibility.Visible;
            ProgressRing_TestCND_JD.Visibility = Visibility.Visible;

            var sw = Stopwatch.StartNew();

            var cfTask = async () =>
            {
                try
                {
                    await _httpClient.GetByteArrayAsync(url_cf);
                    TextBlock_TestCND_CF.Text = $"{sw.ElapsedMilliseconds} ms";
                }
                catch (HttpRequestException)
                {
                    TextBlock_TestCND_CF.Text = Lang.SettingPage_TestCDNAsync_NetworkError;
                }
                finally
                {
                    ProgressRing_TestCND_CF.Visibility = Visibility.Collapsed;
                }
            };

            var ghTask = async () =>
            {
                try
                {
                    await _httpClient.GetByteArrayAsync(url_gh);
                    TextBlock_TestCDN_GH.Text = $"{sw.ElapsedMilliseconds} ms";
                }
                catch (HttpRequestException)
                {
                    TextBlock_TestCDN_GH.Text = Lang.SettingPage_TestCDNAsync_NetworkError;
                }
                finally
                {
                    ProgressRing_TestCND_GH.Visibility = Visibility.Collapsed;
                }
            };

            var jdTask = async () =>
            {
                try
                {
                    await _httpClient.GetByteArrayAsync(url_jd);
                    TextBlock_TestCDN_JD.Text = $"{sw.ElapsedMilliseconds} ms";
                }
                catch (HttpRequestException)
                {
                    TextBlock_TestCDN_JD.Text = Lang.SettingPage_TestCDNAsync_NetworkError;
                }
                finally
                {
                    ProgressRing_TestCND_JD.Visibility = Visibility.Collapsed;
                }
            };

            await Task.WhenAll(cfTask(), ghTask(), jdTask());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test CDN");
        }
    }



    private void RadioButton_CDN_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            var index = fe.Tag switch
            {
                "gh" => 1,
                "jd" => 2,
                _ => 0,
            };
            _metadataClient.SetApiPrefix(index);
            AppConfig.ApiCDNIndex = index;
        }
    }




    #endregion



    #region Background


    [ObservableProperty]
    private bool enableCustomBg;
    partial void OnEnableCustomBgChanged(bool value)
    {
        AppConfig.SetEnableCustomBg(gameBiz, value);
        _ = MainPage.Current.UpdateBackgroundImageAsync(true);
    }


    [ObservableProperty]
    private string? customBg;


    [ObservableProperty]
    private bool enableDynamicAccentColor = AppConfig.EnableDynamicAccentColor;
    partial void OnEnableDynamicAccentColorChanged(bool value)
    {
        AppConfig.EnableDynamicAccentColor = value;
        _ = MainPage.Current.UpdateBackgroundImageAsync();
    }


    [ObservableProperty]
    private bool useOneBg = AppConfig.UseOneBg;
    partial void OnUseOneBgChanged(bool value)
    {
        AppConfig.UseOneBg = value;
        AppConfig.SetCustomBg(gameBiz, CustomBg);
        AppConfig.SetEnableCustomBg(gameBiz, EnableCustomBg);
    }


    [RelayCommand]
    private async Task ChangeCustomBgAsync()
    {
        var file = await _launcherService.ChangeCustomBgAsync();
        if (file is not null)
        {
            CustomBg = file;
            AppConfig.SetCustomBg(gameBiz, file);
            _ = MainPage.Current.UpdateBackgroundImageAsync(true);
        }
    }


    [RelayCommand]
    private async Task OpenCustomBgAsync()
    {
        await _launcherService.OpenCustomBgAsync(CustomBg);
    }


    [RelayCommand]
    private void DeleteCustomBg()
    {
        AppConfig.SetCustomBg(gameBiz, null);
        CustomBg = null;
        _ = MainPage.Current.UpdateBackgroundImageAsync(true);
    }


    #endregion




    #region Update

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
                MainWindow.Current.OverlayFrameNavigateTo(typeof(UpdatePage), release, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
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


    #endregion




    #region File Manager




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
                MainWindow.Current.NavigateTo(typeof(WelcomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
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





    #endregion


}
