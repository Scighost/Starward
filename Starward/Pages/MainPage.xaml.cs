// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Starward.Core;
using Starward.Services;
using Starward.Services.Gacha;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class MainPage : Page
{

    public static MainPage Current { get; private set; }


    private readonly ILogger<MainPage> _logger = AppConfig.GetLogger<MainPage>();


    private readonly LauncherService _launcherService = AppConfig.GetService<LauncherService>();


    private readonly UpdateService _updateService = AppConfig.GetService<UpdateService>();


    private readonly Compositor compositor;


    public MainPage()
    {
        Current = this;
        this.InitializeComponent();
        compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

        InitializeSelectGameBiz();
        InitializeBackgroundImage();
    }




    public bool IsPaneToggleButtonVisible
    {
        get => MainPage_NavigationView.IsPaneToggleButtonVisible;
        set => MainPage_NavigationView.IsPaneToggleButtonVisible = value;
    }




    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateButtonEffect();
        await UpdateBackgroundImageAsync();
        await CheckUpdateAsync();
    }



    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        InitializeTitleBarBackground();
        UpdateDragRectangles();
    }




    private void InitializeTitleBarBackground()
    {
        var surface = compositor.CreateVisualSurface();
        surface.SourceOffset = Vector2.Zero;
        surface.SourceVisual = ElementCompositionPreview.GetElementVisual(Border_ContentImage);
        surface.SourceSize = new Vector2((float)Border_TitleBar.ActualWidth, 12);
        var visual = compositor.CreateSpriteVisual();
        visual.Size = new Vector2((float)Border_TitleBar.ActualWidth, (float)Border_TitleBar.ActualHeight);
        var brush = compositor.CreateSurfaceBrush(surface);
        brush.Stretch = CompositionStretch.Fill;
        visual.Brush = brush;
        ElementCompositionPreview.SetElementChildVisual(Border_TitleBar, visual);
    }



    private async Task CheckUpdateAsync()
    {
        try
        {
            var release = await _updateService.CheckUpdateAsync(false);
            if (release != null)
            {
                MainWindow.Current.OverlayFrameNavigateTo(typeof(UpdatePage), release, new DrillInNavigationTransitionInfo());
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Check update: {exception}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check update");
        }
    }




    #region Select Game

    [ObservableProperty]
    private GameBiz currentGameBiz;
    partial void OnCurrentGameBizChanged(GameBiz value)
    {
        NavigationViewItem_GachaLog.Content = GachaLogService.GetGachaLogText(value);
        CurrentGameBizText = value switch
        {
            GameBiz.hk4e_cn or GameBiz.hkrpg_cn => "China",
            GameBiz.hk4e_global or GameBiz.hkrpg_global => "Global",
            GameBiz.hk4e_cloud => "Cloud",
            _ => ""
        };
    }


    [ObservableProperty]
    private string currentGameBizText;


    private void InitializeSelectGameBiz()
    {
        CurrentGameBiz = AppConfig.SelectGameBiz;
        _logger.LogInformation("Select game region is {gamebiz}", CurrentGameBiz);
        if (CurrentGameBiz.ToGame() != GameBiz.None)
        {
            NavigateTo(typeof(LauncherPage));
        }
    }



    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ChangeGameBizAsync(string bizStr)
    {
        if (Enum.TryParse<GameBiz>(bizStr, out var biz))
        {
            _logger.LogInformation("Change game region to {gamebiz}", CurrentGameBiz);
            CurrentGameBiz = biz;
            AppConfig.SelectGameBiz = CurrentGameBiz;
            AppConfig.SetLastRegionOfGame(biz.ToGame(), biz);
            UpdateButtonEffect();
            NavigateTo(MainPage_Frame.SourcePageType);
            await UpdateBackgroundImageAsync();
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
        if (CurrentGameBiz.ToGame() is GameBiz.Honkai3rd)
        {
            UpdateButtonCornerRadius(Button_BH3, true);
            UpdateButtonCornerRadius(Button_YS, false);
            UpdateButtonCornerRadius(Button_SR, false);
            Border_Mask_BH3.Opacity = 0;
            isSelectBH3 = true;
            return;
        }
        if (CurrentGameBiz.ToGame() is GameBiz.GenshinImpact)
        {
            UpdateButtonCornerRadius(Button_BH3, false);
            UpdateButtonCornerRadius(Button_YS, true);
            UpdateButtonCornerRadius(Button_SR, false);
            Border_Mask_YS.Opacity = 0;
            isSelectYS = true;
            return;
        }
        if (CurrentGameBiz.ToGame() is GameBiz.StarRail)
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

    private async void Button_BH3_Click(object sender, RoutedEventArgs e)
    {
        var biz = AppConfig.GetLastRegionOfGame(GameBiz.Honkai3rd) switch
        {
            GameBiz.bh3_cn => GameBiz.bh3_cn,
            GameBiz.bh3_global => GameBiz.bh3_global,
            GameBiz.bh3_jp => GameBiz.bh3_jp,
            GameBiz.bh3_kr => GameBiz.bh3_kr,
            GameBiz.bh3_overseas => GameBiz.bh3_overseas,
            GameBiz.bh3_tw => GameBiz.bh3_tw,
            _ => GameBiz.bh3_cn,
        };
        if (biz != CurrentGameBiz)
        {
            await ChangeGameBizAsync(biz.ToString());
        }
    }

    private void Button_BH3_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        isSelectBH3 = true;
    }

    private async void Button_YS_Click(object sender, RoutedEventArgs e)
    {
        var biz = AppConfig.GetLastRegionOfGame(GameBiz.GenshinImpact) switch
        {
            GameBiz.hk4e_cn => GameBiz.hk4e_cn,
            GameBiz.hk4e_global => GameBiz.hk4e_global,
            GameBiz.hk4e_cloud => GameBiz.hk4e_cloud,
            _ => GameBiz.hk4e_cn,
        };
        if (biz != CurrentGameBiz)
        {
            await ChangeGameBizAsync(biz.ToString());
        }
    }

    private void Button_YS_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        isSelectYS = true;
    }

    private async void Button_SR_Click(object sender, RoutedEventArgs e)
    {
        var biz = AppConfig.GetLastRegionOfGame(GameBiz.StarRail) switch
        {
            GameBiz.hkrpg_cn => GameBiz.hkrpg_cn,
            GameBiz.hkrpg_global => GameBiz.hkrpg_global,
            _ => GameBiz.hkrpg_cn,
        };
        if (biz != CurrentGameBiz)
        {
            await ChangeGameBizAsync(biz.ToString());
        }
    }

    private void Button_SR_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
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
            animation.InsertKeyFrame(1, new Vector2(18, 18));
        }
        geometry.StartAnimation(nameof(CompositionRoundedRectangleGeometry.CornerRadius), animation);
    }




    private void Grid_SelectGame_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateDragRectangles();
    }



    public void UpdateDragRectangles()
    {
        try
        {
            var scale = MainWindow.Current.UIScale;
            var point = Grid_SelectGame.TransformToVisual(this).TransformPoint(new Windows.Foundation.Point());
            var width = Grid_SelectGame.ActualWidth;
            var height = Grid_SelectGame.ActualHeight;
            int len = (int)(48 * scale);
            var rect1 = new RectInt32(len, 0, (int)((point.X - 48) * scale), len);
            var rect2 = new RectInt32((int)((point.X + width) * scale), 0, 100000, len);
            MainWindow.Current.SetDragRectangles(rect1, rect2);
        }
        catch { }
    }



    #endregion



    #region Background Image




    [ObservableProperty]
    private BitmapSource backgroundImage;





    private void InitializeBackgroundImage()
    {
        try
        {
            var file = _launcherService.GetCachedBackgroundImage(CurrentGameBiz);
            if (file != null)
            {
                BackgroundImage = new BitmapImage(new Uri(file));
                Color? color = null;
                if (AppConfig.EnableDynamicAccentColor)
                {
                    var hex = AppConfig.AccentColor;
                    if (!string.IsNullOrWhiteSpace(hex))
                    {
                        try
                        {
                            color = ColorHelper.ToColor(hex);
                        }
                        catch { }
                    }
                }
                MainWindow.Current.ChangeAccentColor(color);
            }
            else
            {
                BackgroundImage = new BitmapImage(new Uri("ms-appx:///Assets/Image/StartUpBG2.png"));
                MainWindow.Current.ChangeAccentColor(null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize background image");
        }
    }


    private CancellationTokenSource? source;


    public async Task UpdateBackgroundImageAsync()
    {
        try
        {
            source?.Cancel();
            source = new();
            var file = await _launcherService.GetBackgroundImageAsync(CurrentGameBiz);
            if (file != null)
            {
                using var fs = File.OpenRead(file);
                var decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream());
                var bitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                fs.Position = 0;
                await bitmap.SetSourceAsync(fs.AsRandomAccessStream());
                var bytes = new byte[bitmap.PixelBuffer.Length];
                var ms = new MemoryStream(bytes);
                await bitmap.PixelBuffer.AsStream().CopyToAsync(ms);
                var color = GetPrimaryColor(bytes);
                if (source.IsCancellationRequested)
                {
                    return;
                }
                if (AppConfig.EnableDynamicAccentColor)
                {
                    MainWindow.Current.ChangeAccentColor(color);
                }
                BackgroundImage = bitmap;
            }
        }
        catch (COMException ex)
        {
            _logger.LogWarning(ex, "Update background image");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update background image");
        }
    }



    private Color? GetPrimaryColor(byte[] bytes)
    {
        if (bytes.Length % 4 == 0)
        {
            long b = 0, g = 0, r = 0, a = 0;
            for (int i = 0; i < bytes.Length; i += 4)
            {
                b += bytes[i];
                g += bytes[i + 1];
                r += bytes[i + 2];
                a += bytes[i + 3];
            }
            var color = Color.FromArgb((byte)(a * 4 / bytes.Length), (byte)(r * 4 / bytes.Length), (byte)(g * 4 / bytes.Length), (byte)(b * 4 / bytes.Length));
            var hsv = color.ToHsv();
            return ColorHelper.FromHsv(hsv.H, 0.5, hsv.V);

        }
        return null;
    }



    #endregion



    #region Navigate



    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer.IsSelected)
        {
            return;
        }
        if (args.IsSettingsInvoked)
        {
        }
        else
        {
            var item = args.InvokedItemContainer as NavigationViewItem;
            if (item != null)
            {
                var type = item.Tag switch
                {
                    nameof(LauncherPage) => typeof(LauncherPage),
                    nameof(ScreenshotPage) => typeof(ScreenshotPage),
                    nameof(GachaLogPage) => typeof(GachaLogPage),
                    nameof(SettingPage) => typeof(SettingPage),
                    _ => null,
                };
                NavigateTo(type);
            }
        }
    }



    public void NavigateTo(Type? page, object? param = null)
    {
        page ??= typeof(LauncherPage);
        _logger.LogInformation("Navigate to {page} with param {param}", page.Name, param);
        MainPage_Frame.Navigate(page, param ?? CurrentGameBiz, new DrillInNavigationTransitionInfo());
        if (page.Name is "LauncherPage")
        {
            Border_ContentBackground.Opacity = 0;
        }
        else
        {
            Border_ContentBackground.Opacity = 1;
        }
    }












    #endregion


}
