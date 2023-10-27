// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Starward.Core.Metadata;
using Starward.Services;
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

    private readonly WelcomeService _welcomeService = AppConfig.GetService<WelcomeService>();


    public SelectLanguagePage()
    {
        this.InitializeComponent();
        CultureInfo.CurrentUICulture = CultureInfo.InstalledUICulture;
    }


    [ObservableProperty]
    private bool settingGridLoad;


    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(480);
        SettingGridLoad = true;
        InitializeLanguageComboBox();
        _welcomeService.ApiCDNIndex = AppConfig.ApiCDNIndex;
        switch (AppConfig.ApiCDNIndex)
        {
            case 1: RadioButton_GH.IsChecked = true; break;
            case 2: RadioButton_JD.IsChecked = true; break;
            default: RadioButton_CF.IsChecked = true; break;
        }
        _welcomeService.WindowSizeMode = AppConfig.WindowSizeMode;
        switch (AppConfig.WindowSizeMode)
        {
            case 1: RadioButton_WindowSize_Small.IsChecked = true; break;
            default: RadioButton_WindowSize_Normal.IsChecked = true; break;
        }
        TestCDNCommand.Execute(null);
    }



    private void Grid_OverMask_Loaded(object sender, RoutedEventArgs e)
    {
        Grid_OverMask.Visibility = Visibility.Collapsed;
    }


    private bool enableSelectionChanged = false;


    private void InitializeLanguageComboBox()
    {
        try
        {
            ComboBox_Language.Items.Clear();
            ComboBox_Language.Items.Add(new ComboBoxItem
            {
                Content = Lang.SettingPage_FollowSystem,
                Tag = "",
            });
            foreach (var (Title, LangCode) in Localization.LanguageList)
            {
                ComboBox_Language.Items.Add(new ComboBoxItem
                {
                    Content = Title,
                    Tag = LangCode,
                });
            }
            ComboBox_Language.SelectedIndex = 0;
        }
        catch { }
        finally
        {
            enableSelectionChanged = true;
        }
    }



    [RelayCommand]
    private void Next()
    {
        _welcomeService.NavigateTo(typeof(SelectDirectoryPage), null!, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
    }



    private void RadioButton_WindowSize_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement fe)
            {
                switch (fe.Tag)
                {
                    case "small":
                        _welcomeService.WindowSizeMode = 1;
                        MainWindow.Current.ResizeToCertainSize(1064, 648);
                        break;
                    default:
                        _welcomeService.WindowSizeMode = 0;
                        MainWindow.Current.ResizeToCertainSize(1280, 768);
                        break;
                }
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
                    TextBlock_TestCND_CF.Text = Lang.Common_NetworkError;
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
                    TextBlock_TestCDN_GH.Text = Lang.Common_NetworkError;
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
                    TextBlock_TestCDN_JD.Text = Lang.Common_NetworkError;
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
            _welcomeService.ApiCDNIndex = index;
        }
    }



    private void ComboBox_Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (ComboBox_Language.SelectedItem is ComboBoxItem item)
            {
                if (enableSelectionChanged)
                {
                    var lang = item.Tag as string;
                    _logger.LogInformation("Language change to {lang}", lang);
                    _welcomeService.TextLanguage = lang!;
                    if (string.IsNullOrWhiteSpace(lang))
                    {
                        CultureInfo.CurrentUICulture = CultureInfo.InstalledUICulture;
                    }
                    else
                    {
                        CultureInfo.CurrentUICulture = new CultureInfo(lang);
                    }
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
        var len = (int)(48 * MainWindow.Current.UIScale);
        MainWindow.Current.SetDragRectangles(new Windows.Graphics.RectInt32(0, 0, 100000, len));
    }

}
