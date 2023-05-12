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
using Starward.Service.Cache;
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


    private GameBiz selectBiz;


    private async void Page_Loading(FrameworkElement sender, object args)
    {
        try
        {
            games = await _client.GetGameInfoAsync();
            foreach (var game in games)
            {
                _ = ImageCacheService.Instance.PreCacheAsync(new Uri(game.Logo));
                _ = ImageCacheService.Instance.PreCacheAsync(new Uri(game.Poster));
            }
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
    private async Task NextAsync()
    {
        try
        {
            AppConfig.SelectGameBiz = selectBiz;
            AppConfig.SetConfigDirectory(WelcomePage.Current.ConfigDirecory);
            if (Grid_GameInfo.Opacity == 1)
            {
                logoAction.Execute(this, null!);
                TextBlock_Slogan.Opacity = 1;
                Rectangle_Mask.Opacity = 1;
                Button_Next.Opacity = 0;
                Button_Preview.Opacity = 0;
                StackPanel_SelectGame.Opacity = 0;
                TextBlock_Description.Opacity = 0;
                HyperlinkButton_HomePage.Opacity = 0;
                Border_Description_Shadow.Opacity = 0;
                Border_Logo_Shadow.Opacity = 0;
                await Task.Delay(3000);
            }
            MainWindow.Current.NavigateTo(typeof(MainPage), null!, new DrillInNavigationTransitionInfo());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, null);
        }
    }




    private async void ComboBox_GameBiz_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (ComboBox_GameBiz.SelectedItem is FrameworkElement ele)
            {
                if (Enum.TryParse(ele.Tag as string, out GameBiz biz))
                {
                    selectBiz = biz;
                }
                else
                {
                    selectBiz = GameBiz.None;
                }
            }
            if (selectBiz != GameBiz.None)
            {
                Button_Next.IsEnabled = true;
            }
            else
            {
                Button_Next.IsEnabled = false;
            }
            await ChangeGameInfoAsync();
        }
        catch (Exception ex)
        {

        }
    }



    private CancellationTokenSource? source;


    private async Task ChangeGameInfoAsync()
    {
        if (games is null)
        {
            return;
        }
        var sw = Stopwatch.StartNew();
        try
        {
            source?.Cancel();
            Grid_GameInfo.Opacity = 0;
            Rectangle_Mask.Opacity = 1;
            var game_info = games.FirstOrDefault(x => x.GameBiz == selectBiz);
            if (game_info != null)
            {
                source = new();
                var logoTask = ImageCacheService.Instance.GetFromCacheAsync(new Uri(game_info.Logo));
                var posterTask = ImageCacheService.Instance.GetFromCacheAsync(new Uri(game_info.Poster));
                await Task.WhenAll(logoTask, posterTask);
                var logo = logoTask.Result;
                var poster = posterTask.Result;
                if (logo is null || poster is null)
                {
                    Grid_GameInfo.Opacity = 0;
                    return;
                }

                if (sw.ElapsedMilliseconds < 300)
                {
                    await Task.Delay(300 - (int)sw.ElapsedMilliseconds);
                }

                if (source.IsCancellationRequested)
                {
                    return;
                }

                Image_Logo.Source = logo;
                Image_Poster.Source = poster;
                Image_Logo_Action.Source = logo;

                TextBlock_Description.Text = game_info.Description;
                HyperlinkButton_HomePage.NavigateUri = new Uri(game_info.HomePage);
                TextBlock_HomePage.Text = game_info.HomePage;
                TextBlock_Slogan.Text = game_info.Slogan;
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
