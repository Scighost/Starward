// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
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
public sealed partial class SelectLanguagePage : PageBase
{

    private readonly ILogger<SelectLanguagePage> _logger = AppConfig.GetLogger<SelectLanguagePage>();

    private readonly HttpClient _httpClient = AppConfig.GetService<HttpClient>();


    public SelectLanguagePage()
    {
        this.InitializeComponent();
    }


    [ObservableProperty]
    private bool settingGridLoad;


    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(600);
        SettingGridLoad = true;
        InitializeLanguageComboBox();
        IntializeWindowSize();
        TestSpeedCommand.Execute(null);
    }



    [RelayCommand]
    private void Next()
    {
        WelcomeWindow.Current.NavigateTo(typeof(SelectDirectoryPage), null!, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
    }




    #region Language



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
                    WelcomeWindow.Current.TextLanguage = lang!;
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



    #endregion



    #region Window Size



    private void IntializeWindowSize()
    {
        try
        {
            WelcomeWindow.Current.WindowSizeMode = AppConfig.WindowSizeMode;
            switch (AppConfig.WindowSizeMode)
            {
                case 1: RadioButton_WindowSize_Small.IsChecked = true; break;
                default: RadioButton_WindowSize_Normal.IsChecked = true; break;
            }
        }
        catch { }
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
                        WelcomeWindow.Current.ChangeWindowSize(1);
                        break;
                    default:
                        WelcomeWindow.Current.ChangeWindowSize(0);
                        break;
                }
            }
        }
        catch { }
    }



    #endregion



    #region Speed Test




    [RelayCommand]
    private async Task TestSpeedAsync()
    {
        try
        {
            const string url = "https://starward.scighost.com/metadata/test/test_100kb";

            TextBlock_Delay.Text = "";
            TextBlock_Speed.Text = "";


            var sw = Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            sw.Stop();
            TextBlock_Delay.Text = $"{sw.ElapsedMilliseconds}ms";
            sw.Start();
            var bytes = await response.Content.ReadAsByteArrayAsync();
            sw.Stop();
            double speed = bytes.Length / 1024.0 / sw.Elapsed.TotalSeconds;
            if (speed < 1024)
            {
                TextBlock_Speed.Text = $"{speed:0.00}KB/s";
            }
            else
            {
                TextBlock_Speed.Text = $"{speed / 1024:0.00}MB/s";
            }
        }
        catch (Exception ex)
        {
            TextBlock_Delay.Text = Lang.Common_NetworkError;
            TextBlock_Speed.Text = "";
            _logger.LogError(ex, "Test Speed");
        }
    }




    #endregion






}
