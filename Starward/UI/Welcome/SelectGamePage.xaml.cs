// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.Metadata;
using Starward.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.UI.Welcome;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class SelectGamePage : Page
{

    private readonly ILogger<SelectGamePage> _logger;

    private readonly MetadataClient _client;


    public SelectGamePage()
    {
        this.InitializeComponent();
        _logger = ServiceProvider.GetLogger<SelectGamePage>();
        _client = ServiceProvider.GetService<MetadataClient>();
    }


    private List<GameInfo> games;


    private GameType selectGame;

    private RegionType selectRegion;


    private async void Page_Loading(FrameworkElement sender, object args)
    {
        try
        {
            games = await _client.GetGameInfoAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, null);
        }
    }




    [RelayCommand]
    private void Preview()
    {
        WelcomePage.Current?.NavigateTo(typeof(SelectDirectoryPage), null!, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
    }




    [RelayCommand]
    private void Next()
    {
        // todo save game and region
        MainWindow.Current.NavigateTo(typeof(MainPage), null!, new DrillInNavigationTransitionInfo());
    }



    private void ComboBox_Game_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            source?.Cancel();
            ComboBox_Region.Items.Clear();
            if (ComboBox_Game.SelectedItem is ComboBoxItem item)
            {
                if (item.Tag is "StarRail")
                {
                    selectGame = GameType.StarRail;
                    _logger.LogInformation("Select StarRail");
                    ComboBox_Region.Items.Add(new ComboBoxItem { Content = "国服", Tag = "CN" });
                    ComboBox_Region.Items.Add(new ComboBoxItem { Content = "国际服", Tag = "OS" });
                }
                if (item.Tag is "Genshin")
                {
                    selectGame = GameType.Genshin;
                    _logger.LogInformation("Select Genshin");
                    ComboBox_Region.Items.Add(new ComboBoxItem { Content = "国服", Tag = "CN" });
                    ComboBox_Region.Items.Add(new ComboBoxItem { Content = "国际服", Tag = "OS" });
                }
            }
        }
        catch (Exception ex)
        {

        }
    }




    private async void ComboBox_Region_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            source?.Cancel();
            selectRegion = RegionType.None;
            if (ComboBox_Region.SelectedItem is ComboBoxItem item)
            {
                if (item.Tag is "CN")
                {
                    _logger.LogInformation("Select China Server");
                    selectRegion = RegionType.China;
                }
                if (item.Tag is "OS")
                {
                    _logger.LogInformation("Select Global Server");
                    selectRegion = RegionType.Global;
                }
            }
            if (selectRegion != RegionType.None)
            {
                Button_Next.IsEnabled = true;
                await ChangeGameInfoAsync();
            }
            else
            {
                Button_Next.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {

        }
    }



    private CancellationTokenSource? source;


    private async Task ChangeGameInfoAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            source?.Cancel();
            Grid_GameInfo.Opacity = 0;
            Rectangle_Mask.Opacity = 1;
            var game_info = games.FirstOrDefault(x => x.Game == selectGame && x.Region == selectRegion);
            if (game_info != null)
            {
                source = new();
                var logo = await CacheService.Instance.GetFileFromCacheAsync(new Uri(game_info.Logo));
                var poster = await CacheService.Instance.GetFileFromCacheAsync(new Uri(game_info.Poster));
                if (logo is null || poster is null)
                {
                    logo = await CacheService.Instance.GetFromCacheAsync(new Uri(game_info.Logo));
                    poster = await CacheService.Instance.GetFromCacheAsync(new Uri(game_info.Poster));
                }
                if (logo is null || poster is null)
                {
                    Grid_GameInfo.Opacity = 0;
                    return;
                }
                using var s_logo = await logo.OpenReadAsync();
                var bitmap_logo = new BitmapImage();
                await bitmap_logo.SetSourceAsync(s_logo);

                using var s_poster = await poster.OpenReadAsync();
                var bitmap_poster = new BitmapImage();
                await bitmap_poster.SetSourceAsync(s_poster);

                if (sw.ElapsedMilliseconds < 300)
                {
                    await Task.Delay(300 - (int)sw.ElapsedMilliseconds);
                }

                if (source.IsCancellationRequested)
                {
                    return;
                }

                Image_Logo.Source = bitmap_logo;
                Image_Poster.Source = bitmap_poster;

                TextBlock_Description.Text = game_info.Description;
                HyperlinkButton_HomePage.NavigateUri = new Uri(game_info.HomePage);
                TextBlock_HomePage.Text = game_info.HomePage;
                Grid_GameInfo.Opacity = 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, null);
            if (sw.ElapsedMilliseconds < 300)
            {
                await Task.Delay(300 - (int)sw.ElapsedMilliseconds);
            }
        }
        finally
        {
            Rectangle_Mask.Opacity = 0;
        }
    }


}
