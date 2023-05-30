// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using Starward.Core;
using Starward.Core.Metadata;
using Starward.Helpers;
using Starward.Pages.Welcome;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
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
                    TextBlock_TestCND_CF.Text = "网络异常";
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
                    TextBlock_TestCDN_GH.Text = "网络异常";
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
                    TextBlock_TestCDN_JD.Text = "网络异常";
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
        _ = MainPage.Current.UpdateBackgroundImageAsync();
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


    [RelayCommand]
    private async Task ChangeCustomBgAsync()
    {
        try
        {
            var filter = new List<(string, string)>
            {
                ("bmp", ".bmp"),
                ("jpeg", ".jpeg"),
                ("jpg", ".jpg"),
                ("png", ".png"),
                ("tif", ".tif"),
                ("tiff", ".tiff"),
                ("avif", ".avif"),
                ("heic", ".heic"),
                ("webp", ".webp"),
            };
            var file = await FileDialogHelper.PickSingleFileAsync(MainWindow.Current.HWND, filter.ToArray());
            if (File.Exists(file))
            {
                _logger.LogInformation("Background file is '{file}'", file);
                using var fs = File.OpenRead(file);
                var decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream());
                var name = Path.GetFileName(file);
                var dest = Path.Combine(AppConfig.ConfigDirectory, "bg", name);
                if (file != dest)
                {
                    File.Copy(file, dest, true);
                    _logger.LogInformation("File copied to '{dest}'", dest);
                }
                CustomBg = name;
                AppConfig.SetCustomBg(gameBiz, name);
                _ = MainPage.Current.UpdateBackgroundImageAsync();
            }
        }
        catch (COMException ex)
        {
            // 0x88982F50
            _logger.LogError(ex, "Decode error or others");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change custom background");
        }
    }


    [RelayCommand]
    private async Task OpenCustomBgAsync()
    {
        try
        {
            var file = Path.Join(AppConfig.ConfigDirectory, "bg", CustomBg);
            if (File.Exists(file))
            {
                _logger.LogError("Open image file '{file}'", file);
                await Launcher.LaunchUriAsync(new Uri(file));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open custom background");
        }
    }


    [RelayCommand]
    private void DeleteCustomBg()
    {
        AppConfig.SetCustomBg(gameBiz, null);
        CustomBg = null;
        _ = MainPage.Current.UpdateBackgroundImageAsync();
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



    [RelayCommand]
    private async Task OpenDataFolderAsync()
    {
        try
        {
            _logger.LogInformation("Open folder '{folder}'", AppConfig.ConfigDirectory);
            await Launcher.LaunchFolderPathAsync(AppConfig.ConfigDirectory);
        }
        catch { }
    }


    [RelayCommand]
    private void ChangeDataFolder()
    {
        try
        {
            AppConfig.ResetServiceProvider();
            MainWindow.Current.NavigateTo(typeof(WelcomePage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
            Registry.CurrentUser.OpenSubKey(@"Software\Starward", true)?.DeleteValue("ConfigDirectory", false);
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
                Title = "删除所有设置",
                Content = "删除完成后，会自动重启应用程序。",
                PrimaryButtonText = "删除",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                Registry.CurrentUser.OpenSubKey(@"Software", true)?.DeleteSubKeyTree("Starward");
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






    #endregion


}
