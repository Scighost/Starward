// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;
using Starward.Core;
using Starward.Core.Metadata;
using Starward.Helpers;
using Starward.Services.Cache;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.Welcome;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class SelectGamePage : Page
{

    private readonly ILogger<SelectGamePage> _logger = AppConfig.GetLogger<SelectGamePage>();

    private readonly MetadataClient _client = AppConfig.GetService<MetadataClient>();

    private readonly Compositor compositor;


    public SelectGamePage()
    {
        this.InitializeComponent();
        InitializeSomeGame();
        compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
    }




    private void InitializeSomeGame()
    {
        TextBlockHelper.Inlines(TextBlock_SomeGame.Inlines, Lang.SelectGamePage_SomeGame, ("{Starward}", null), ("{miHoYo/HoYoverse}", null));
    }




    private List<GameInfo> games;




    private async void Page_Loading(FrameworkElement sender, object args)
    {
        try
        {
            await Task.Delay(16);
            UpdateButtonEffect();
            games = await _client.GetGameInfoAsync();
            foreach (var game in games)
            {
                _ = ImageCacheService.Instance.PreCacheAsync(new Uri(game.Logo));
                _ = ImageCacheService.Instance.PreCacheAsync(new Uri(game.Poster));
            }
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
            AppConfig.SelectGameBiz = SelectBiz;
            AppConfig.SetConfigDirectory(WelcomePage.Current.ConfigDirectory);
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
            _logger.LogError(ex, "Navigate to MainPage");
        }
    }






    #region Select Game



    [ObservableProperty]
    private GameBiz selectBiz;
    partial void OnSelectBizChanged(GameBiz value)
    {
        CurrentGameBizText = value switch
        {
            GameBiz.hk4e_cn or GameBiz.hkrpg_cn or GameBiz.bh3_cn => "China",
            GameBiz.hk4e_global or GameBiz.hkrpg_global or GameBiz.bh3_global => "Global",
            GameBiz.hk4e_cloud => "Cloud",
            GameBiz.bh3_tw => "TW/HK/MO",
            GameBiz.bh3_jp => "Japan",
            GameBiz.bh3_kr => "Korea",
            GameBiz.bh3_overseas => "Southeast Asia",
            _ => ""
        };
    }




    [ObservableProperty]
    private string currentGameBizText;


    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ChangeGameBizAsync(string bizStr)
    {
        if (Enum.TryParse<GameBiz>(bizStr, out var biz))
        {
            _logger.LogInformation("Change game region to {gamebiz}", biz);
            SelectBiz = biz;
            AppConfig.SetLastRegionOfGame(biz.ToGame(), biz);
            UpdateButtonEffect();
            if (SelectBiz != GameBiz.None)
            {
                Button_Next.IsEnabled = true;
            }
            else
            {
                Button_Next.IsEnabled = false;
            }
            await ChangeGameInfoAsync();
        }
    }


    private void UpdateButtonEffect()
    {
        const double OPACITY = 1;
        isSelectBH3 = false;
        isSelectYS = false;
        isSelectSR = false;
        Border_Mask_BH3.Opacity = OPACITY;
        Border_Mask_YS.Opacity = OPACITY;
        Border_Mask_SR.Opacity = OPACITY;
        if (SelectBiz.ToGame() is GameBiz.Honkai3rd)
        {
            UpdateButtonCornerRadius(Button_BH3, true);
            UpdateButtonCornerRadius(Button_YS, false);
            UpdateButtonCornerRadius(Button_SR, false);
            Border_Mask_BH3.Opacity = 0;
            isSelectBH3 = true;
            return;
        }
        if (SelectBiz.ToGame() is GameBiz.GenshinImpact)
        {
            UpdateButtonCornerRadius(Button_BH3, false);
            UpdateButtonCornerRadius(Button_YS, true);
            UpdateButtonCornerRadius(Button_SR, false);
            Border_Mask_YS.Opacity = 0;
            isSelectYS = true;
            return;
        }
        if (SelectBiz.ToGame() is GameBiz.StarRail)
        {
            UpdateButtonCornerRadius(Button_BH3, false);
            UpdateButtonCornerRadius(Button_YS, false);
            UpdateButtonCornerRadius(Button_SR, true);
            Border_Mask_SR.Opacity = 0;
            isSelectSR = true;
            return;
        }
        UpdateButtonCornerRadius(Button_BH3, false);
        UpdateButtonCornerRadius(Button_YS, false);
        UpdateButtonCornerRadius(Button_SR, false);

    }


    private void Button_Game_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Button button)
        {
            UpdateButtonCornerRadius(button, true);
        }
    }


    private void Button_Game_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Button button)
        {
            UpdateButtonCornerRadius(button, false);
        }
    }

    private bool isSelectBH3;
    private bool isSelectYS;
    private bool isSelectSR;

    private void Button_BH3_Click(object sender, RoutedEventArgs e)
    {
        isSelectBH3 = true;
    }


    private void Button_YS_Click(object sender, RoutedEventArgs e)
    {
        isSelectYS = true;
    }


    private void Button_SR_Click(object sender, RoutedEventArgs e)
    {
        isSelectSR = true;
    }


    private void MenuFlyout_Game_Closed(object sender, object e)
    {
        isSelectBH3 = false;
        isSelectYS = false;
        isSelectSR = false;
        UpdateButtonEffect();
    }

    private void UpdateButtonCornerRadius(Button button, bool isSelect)
    {
        var visual = ElementCompositionPreview.GetElementVisual(button);
        CompositionRoundedRectangleGeometry geometry;
        if (visual.Clip is CompositionGeometricClip clip && clip.Geometry is CompositionRoundedRectangleGeometry geo)
        {
            geometry = geo;
        }
        else
        {
            geometry = compositor.CreateRoundedRectangleGeometry();
            geometry.Size = new Vector2((float)button.ActualWidth, (float)button.ActualHeight);
            geometry.CornerRadius = Vector2.Zero;
            clip = compositor.CreateGeometricClip(geometry);
            visual.Clip = clip;
        }

        if (button.Tag is "bh3" && isSelectBH3)
        {
            return;
        }
        if (button.Tag is "ys" && isSelectYS)
        {
            return;
        }
        if (button.Tag is "sr" && isSelectSR)
        {
            return;
        }

        var animation = compositor.CreateVector2KeyFrameAnimation();
        animation.Duration = TimeSpan.FromSeconds(0.3);
        if (isSelect)
        {
            animation.InsertKeyFrame(1, new Vector2(8, 8));
        }
        else
        {
            animation.InsertKeyFrame(1, new Vector2(24, 24));
        }
        geometry.StartAnimation(nameof(CompositionRoundedRectangleGeometry.CornerRadius), animation);
    }




    #endregion




    private CancellationTokenSource? cancelSource;


    private async Task ChangeGameInfoAsync()
    {
        if (games is null)
        {
            return;
        }
        var sw = Stopwatch.StartNew();
        try
        {
            cancelSource?.Cancel();
            Grid_GameInfo.Opacity = 0;
            Rectangle_Mask.Opacity = 1;
            var game_info = games.FirstOrDefault(x => x.GameBiz == SelectBiz);
            if (game_info is null)
            {
                _logger.LogInformation("Game info of {GameBiz} is NULL.", SelectBiz);
                return;
            }

            cancelSource = new();
            var source = cancelSource;
            var logoTask = ImageCacheService.Instance.GetFromCacheAsync(new Uri(game_info.Logo));
            var posterTask = ImageCacheService.Instance.GetFromCacheAsync(new Uri(game_info.Poster));
            await Task.WhenAll(logoTask, posterTask);
            var logo = logoTask.Result;
            var poster = posterTask.Result;
            if (logo is null || poster is null)
            {
                _logger.LogInformation("Logo and poster of {GameBiz} download failed.", game_info.Name);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change game logo and poster");
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
