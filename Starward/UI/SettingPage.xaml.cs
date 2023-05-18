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
using Starward.Service;
using Starward.UI.Welcome;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.UI;

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
        switch (AppConfig.ApiCDNIndex)
        {
            case 1: RadioButton_GH.IsChecked = true; break;
            case 2: RadioButton_JD.IsChecked = true; break;
            default: RadioButton_CF.IsChecked = true; break;
        }
    }


    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {

        }
        catch (Exception ex)
        {

        }
    }















    #region Console


    [ObservableProperty]
    private bool enableConsole = AppConfig.EnableConsole;



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
                catch (Exception ex)
                {
                    TextBlock_TestCND_CF.Text = ex.Message;
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
                catch (Exception ex)
                {
                    TextBlock_TestCDN_GH.Text = ex.Message;
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
                catch (Exception ex)
                {
                    TextBlock_TestCDN_JD.Text = ex.Message;
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


    [RelayCommand]
    private async Task ChangeCustomBgAsync()
    {
        try
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
            };
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".tif");
            picker.FileTypeFilter.Add(".avif");
            picker.FileTypeFilter.Add(".heic");
            picker.FileTypeFilter.Add(".webp");
            InitializeWithWindow.Initialize(picker, MainWindow.Current.HWND);
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                using var fs = await file.OpenReadAsync();
                var decoder = await BitmapDecoder.CreateAsync(fs);
                var name = Path.GetFileName(file.Path);
                var dest = Path.Combine(AppConfig.ConfigDirectory, "bg", name);
                if (file.Path != dest)
                {
                    File.Copy(file.Path, dest, true);
                }
                CustomBg = name;
                AppConfig.SetCustomBg(gameBiz, name);
                _ = MainPage.Current.UpdateBackgroundImageAsync();
            }
        }
        catch (COMException ex)
        {
            // 0x88982F50

        }
        catch (Exception ex)
        {

        }
    }


    [RelayCommand]
    private async Task OpenCustomBgAsync()
    {
        try
        {
            var file = Path.Join(AppConfig.ConfigDirectory, "bg", customBg);
            if (File.Exists(file))
            {
                await Launcher.LaunchUriAsync(new Uri(file));
            }
        }
        catch (Exception ex)
        {

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


    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        try
        {
            IsUpdated = false;
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

        }

    }


    #endregion




    #region File Manager



    [RelayCommand]
    private async Task OpenDataFolderAsync()
    {
        await Launcher.LaunchFolderPathAsync(AppConfig.ConfigDirectory);
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

        }
    }


    #endregion





}
