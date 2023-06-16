// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Starward.Core.Metadata;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.Welcome;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class SelectLanguagePage : Page
{

    private readonly ILogger<SelectLanguagePage> _logger = AppConfig.GetLogger<SelectLanguagePage>();

    private readonly HttpClient _httpClient = AppConfig.GetService<HttpClient>();

    private readonly MetadataClient _metadataClient = AppConfig.GetService<MetadataClient>();


    public SelectLanguagePage()
    {
        this.InitializeComponent();
        AppConfig.Language = null;
        CultureInfo.CurrentUICulture = CultureInfo.InstalledUICulture;
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



    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        TestCDNCommand.Execute(null);
    }



    [RelayCommand]
    private void Next()
    {
        WelcomePage.Current.NavigateTo(typeof(SelectDirectoryPage), null!, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
    }



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
                    TextBlock_TestCND_CF.Text = "Network Error";
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
                    TextBlock_TestCDN_GH.Text = "Network Error";
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
                    TextBlock_TestCDN_JD.Text = "Network Error";
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



    private void ComboBox_Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (ComboBox_Language.SelectedItem is ComboBoxItem item)
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
                MainPage.Current.NavigateTo(typeof(SettingPage), infoOverride: new Microsoft.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
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


}
