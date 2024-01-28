// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Starward.Core;
using Starward.Core.Metadata;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.Welcome;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class SelectGamePage : PageBase
{

    private readonly ILogger<SelectGamePage> _logger = AppConfig.GetLogger<SelectGamePage>();



    public SelectGamePage()
    {
        this.InitializeComponent();
        InitializeSomeGame();
    }



    private void InitializeSomeGame()
    {
        TextBlockHelper.Inlines(TextBlock_SomeGame.Inlines, Lang.SelectGamePage_SomeGame, ("{Starward}", null), ("{miHoYo/HoYoverse}", null));
    }




    private List<GameInfo> games;



    protected override void OnLoaded()
    {
        try
        {
            InitializeGameComboBox();
            _ = LoadGameInfoAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Cannot get game info: {Exception}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get game info");
        }
    }



    private async Task LoadGameInfoAsync()
    {
        try
        {
            var file = Path.Combine(AppContext.BaseDirectory, @"Assets\game_info.json");
            if (File.Exists(file))
            {
                var str = await File.ReadAllTextAsync(file);
                games = JsonSerializer.Deserialize<List<GameInfo>>(str)!;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load game info");
        }
    }



    [RelayCommand]
    private void Preview()
    {
        WelcomeWindow.Current.NavigateTo(typeof(SelectDirectoryPage), null!, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
    }




    [RelayCommand]
    private async Task NextAsync()
    {
        try
        {
            WelcomeWindow.Current.ApplySetting();
            AppConfig.SetLastRegionOfGame(GameBiz.None, SelectBiz);
            if (Grid_GameInfo.Opacity == 1)
            {
                logoAction.Execute(this, null!);
                TextBlock_Slogan.Opacity = 1;
                TextBlock_HoYoSlogan.Opacity = 1;
                Rectangle_Mask.Opacity = 1;
                Button_Next.Opacity = 0;
                Button_Preview.Opacity = 0;
                StackPanel_SelectGame.Opacity = 0;
                //TextBlock_Description.Opacity = 0;
                HyperlinkButton_HomePage.Opacity = 0;
                await Task.Delay(3000);
            }
            WelcomeWindow.Current.OpenMainWindow();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigate to MainPage");
        }
    }






    #region Select Game



    private GameBiz SelectBiz;




    private void InitializeGameComboBox()
    {
        ComboBox_Game.Items.Clear();
        ComboBox_Game.Items.Add(new ComboBoxItem { Content = GameBiz.bh3_cn.ToGameName(), Tag = GameBiz.Honkai3rd });
        ComboBox_Game.Items.Add(new ComboBoxItem { Content = GameBiz.hk4e_cn.ToGameName(), Tag = GameBiz.GenshinImpact });
        ComboBox_Game.Items.Add(new ComboBoxItem { Content = GameBiz.hkrpg_cn.ToGameName(), Tag = GameBiz.StarRail });
    }



    private void ComboBox_Game_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Button_Next.IsEnabled = false;
        ComboBox_GameServer.Items.Clear();
        if (e.AddedItems.FirstOrDefault() is ComboBoxItem item)
        {
            if (item.Tag is GameBiz.Honkai3rd)
            {
                ComboBox_GameServer.Items.Add(new ComboBoxItem { Content = GameBiz.bh3_cn.ToGameServer(), Tag = GameBiz.bh3_cn });
                ComboBox_GameServer.Items.Add(new ComboBoxItem { Content = GameBiz.bh3_global.ToGameServer(), Tag = GameBiz.bh3_global });
                ComboBox_GameServer.Items.Add(new ComboBoxItem { Content = GameBiz.bh3_jp.ToGameServer(), Tag = GameBiz.bh3_jp });
                ComboBox_GameServer.Items.Add(new ComboBoxItem { Content = GameBiz.bh3_kr.ToGameServer(), Tag = GameBiz.bh3_kr });
                ComboBox_GameServer.Items.Add(new ComboBoxItem { Content = GameBiz.bh3_overseas.ToGameServer(), Tag = GameBiz.bh3_overseas });
                ComboBox_GameServer.Items.Add(new ComboBoxItem { Content = GameBiz.bh3_tw.ToGameServer(), Tag = GameBiz.bh3_tw });
            }
            if (item.Tag is GameBiz.GenshinImpact)
            {
                ComboBox_GameServer.Items.Add(new ComboBoxItem { Content = GameBiz.hk4e_cn.ToGameServer(), Tag = GameBiz.hk4e_cn });
                ComboBox_GameServer.Items.Add(new ComboBoxItem { Content = GameBiz.hk4e_global.ToGameServer(), Tag = GameBiz.hk4e_global });
                ComboBox_GameServer.Items.Add(new ComboBoxItem { Content = GameBiz.hk4e_cloud.ToGameServer(), Tag = GameBiz.hk4e_cloud });
                ComboBox_GameServer.Items.Add(new ComboBoxItem { Content = GameBiz.hk4e_bilibili.ToGameServer(), Tag = GameBiz.hk4e_bilibili });
            }
            if (item.Tag is GameBiz.StarRail)
            {
                ComboBox_GameServer.Items.Add(new ComboBoxItem { Content = GameBiz.hkrpg_cn.ToGameServer(), Tag = GameBiz.hkrpg_cn });
                ComboBox_GameServer.Items.Add(new ComboBoxItem { Content = GameBiz.hkrpg_global.ToGameServer(), Tag = GameBiz.hkrpg_global });
                ComboBox_GameServer.Items.Add(new ComboBoxItem { Content = GameBiz.hkrpg_bilibili.ToGameServer(), Tag = GameBiz.hkrpg_bilibili });
            }
        }
    }


    private void ComboBox_GameServer_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is ComboBoxItem item)
        {
            ChangeGameBiz(item.Tag?.ToString() ?? "");
        }
    }


    private void ChangeGameBiz(string bizStr)
    {
        if (Enum.TryParse<GameBiz>(bizStr, out var biz))
        {
            _logger.LogInformation("Change game region to {gamebiz}", biz);
            SelectBiz = biz;
            if (SelectBiz != GameBiz.None)
            {
                Button_Next.IsEnabled = true;
            }
            else
            {
                Button_Next.IsEnabled = false;
            }
            ChangeGameInfo();
        }
    }






    #endregion




    private void ChangeGameInfo()
    {
        if (games is null)
        {
            return;
        }
        var sw = Stopwatch.StartNew();
        try
        {
            var game_info = games.FirstOrDefault(x => x.GameBiz == SelectBiz);
            if (game_info is null)
            {
                Grid_GameInfo.Opacity = 0;
                _logger.LogInformation("Game info of {GameBiz} is NULL.", SelectBiz);
                return;
            }

            Image_Logo.Source = game_info.Logo;
            Image_Poster.Source = game_info.Poster;
            Image_Logo_Action.Source = game_info.Logo;

            //TextBlock_Description.Text = game_info.Description;
            HyperlinkButton_HomePage.NavigateUri = new Uri(game_info.HomePage);
            TextBlock_HomePage.Text = game_info.HomePage;
            TextBlock_Slogan.Text = game_info.Slogan;
            TextBlock_HoYoSlogan.Text = game_info.HoYoSlogan;
            Grid_GameInfo.Opacity = 1;
            if (game_info.Fonts?.Count > 0)
            {
                var font = game_info.Fonts[Random.Shared.Next(game_info.Fonts.Count)];
                TextBlock_HoYoSlogan.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily(font);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change game logo and poster");
        }
    }


}
