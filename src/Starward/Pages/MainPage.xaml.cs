// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Composition;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Starward.Core;
using Starward.Helpers;
using Starward.Pages.HoyolabToolbox;
using Starward.Services;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;

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


    private SystemTrayService _systemTrayService = AppConfig.GetService<SystemTrayService>();


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
        set
        {
            MainPage_NavigationView.IsPaneToggleButtonVisible = value;
            Border_TitleText.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }
    }




    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateButtonEffect();
        InitializeSystemTray();
        await UpdateBackgroundImageAsync(true);
        await CheckUpdateAsync();
    }



    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        InitializeTitleBarBackground();
        UpdateDragRectangles();
    }


    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        mediaPlayer?.Dispose();
        softwareBitmap?.Dispose();
        _systemTrayService?.Dispose();
        if (MainWindow.Current?.AppWindow != null)
        {
            MainWindow.Current.AppWindow.Closing -= AppWindow_Closing;
        }
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



    public void ReloadTextForLanguage()
    {
        this.Bindings.Update();
        UpdateNavigationViewItems();
    }



    private void InitializeSystemTray()
    {
        if (AppConfig.EnableSystemTrayIcon)
        {
            _systemTrayService.Initialize();
        }
        MainWindow.Current.AppWindow.Closing += AppWindow_Closing;
    }


    private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (AppConfig.EnableSystemTrayIcon && _systemTrayService.IsCreated && !AppConfig.ExitWhenClosing)
        {
            args.Cancel = true;
            PauseVideo();
            MainWindow.Current.AppWindow.Hide();
        }
    }



    #region Select Game



    private GameBiz lastGameBiz;


    [ObservableProperty]
    private GameBiz currentGameBiz = (GameBiz)int.MaxValue;
    partial void OnCurrentGameBizChanged(GameBiz oldValue, GameBiz newValue)
    {
        lastGameBiz = oldValue;
        AppConfig.SelectGameBiz = newValue;
        UpdateNavigationViewItems();
        CurrentGameBizText = newValue switch
        {
            GameBiz.hk4e_cn or GameBiz.hkrpg_cn or GameBiz.bh3_cn => "China",
            GameBiz.hk4e_global or GameBiz.hkrpg_global => "Global",
            GameBiz.hk4e_cloud => "China Cloud",
            GameBiz.bh3_global => "Europe & Americas",
            GameBiz.bh3_tw => "Traditional Chinese",
            GameBiz.bh3_jp => "Japan",
            GameBiz.bh3_kr => "Korea",
            GameBiz.bh3_overseas => "Southeast Asia",
            _ => ""
        };
    }


    [ObservableProperty]
    private string currentGameBizText;


    private void InitializeSelectGameBiz()
    {
        CurrentGameBiz = AppConfig.SelectGameBiz;
        AppConfig.SetLastRegionOfGame(CurrentGameBiz.ToGame(), CurrentGameBiz);
        _logger.LogInformation("Select game region is {gamebiz}", CurrentGameBiz);
        if (CurrentGameBiz.ToGame() == GameBiz.None)
        {
            MainPage_Frame.Content = new BlankPage();
        }
        else
        {
            NavigateTo(typeof(LauncherPage));
        }
    }



    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ChangeGameBizAsync(string bizStr)
    {
        if (Enum.TryParse<GameBiz>(bizStr, out var biz))
        {
            _logger.LogInformation("Change game region to {gamebiz}", biz);
            CurrentGameBiz = biz;
            AppConfig.SetLastRegionOfGame(biz.ToGame(), biz);
            UpdateButtonEffect();
            NavigateTo(MainPage_Frame.SourcePageType, changeGameBiz: true);
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



    private void Button_SelectGame_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        NavigateTo(typeof(LauncherPage));
        MainPage_NavigationView.SelectedItem = null;
    }



    #endregion



    #region Background Image




    [ObservableProperty]
    private ImageSource backgroundImage;


    [ObservableProperty]
    private int videoBgVolume = AppConfig.VideoBgVolume;
    partial void OnVideoBgVolumeChanged(int value)
    {
        AppConfig.VideoBgVolume = value;
        if (mediaPlayer is not null)
        {
            mediaPlayer.Volume = value / 100d;
        }
    }


    private SoftwareBitmap? softwareBitmap;

    private CanvasImageSource? videoSource;

    private MediaPlayer? mediaPlayer;


    public bool IsPlayingVideo { get; private set; }



    private void InitializeBackgroundImage()
    {
        try
        {
            var file = _launcherService.GetCachedBackgroundImage(CurrentGameBiz);
            if (file != null)
            {
                if (Path.GetExtension(file) is ".flv" or ".mkv" or ".mov" or ".mp4" or ".webm")
                {
                    IsPlayingVideo = true;
                    BackgroundImage = new BitmapImage(new Uri("ms-appx:///Assets/Image/StartUpBG2.png"));
                    MainWindow.Current.ChangeAccentColor(null, null);
                }
                else
                {
                    if (CurrentGameBiz is GameBiz.hk4e_cloud && !AppConfig.GetEnableCustomBg(GameBiz.hk4e_cloud))
                    {
                        Image_Content.HorizontalAlignment = HorizontalAlignment.Left;
                    }
                    else
                    {
                        Image_Content.HorizontalAlignment = HorizontalAlignment.Center;
                    }
                    BackgroundImage = new BitmapImage(new Uri(file));
                    Color? back = null, fore = null;
                    if (AppConfig.EnableDynamicAccentColor)
                    {
                        var hex = AppConfig.AccentColor;
                        if (!string.IsNullOrWhiteSpace(hex))
                        {
                            try
                            {
                                back = ColorHelper.ToColor(hex[0..9]);
                                fore = ColorHelper.ToColor(hex[9..18]);
                            }
                            catch { }
                        }
                    }
                    MainWindow.Current.ChangeAccentColor(back, fore);
                }
            }
            else
            {
                BackgroundImage = new BitmapImage(new Uri("ms-appx:///Assets/Image/StartUpBG2.png"));
                MainWindow.Current.ChangeAccentColor(null, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize background image");
        }
    }


    private CancellationTokenSource? cancelSource;


    public async Task UpdateBackgroundImageAsync(bool force = false)
    {
        if (AppConfig.UseOneBg && !force)
        {
            return;
        }
        try
        {
            ProgressBar_LoadBackground.IsIndeterminate = true;
            ProgressBar_LoadBackground.Visibility = Visibility.Visible;

            mediaPlayer?.Dispose();
            mediaPlayer = null;
            videoSource = null;
            softwareBitmap?.Dispose();
            softwareBitmap = null;

            cancelSource?.Cancel();
            cancelSource = new();
            var source = cancelSource;

            var file = await _launcherService.GetBackgroundImageAsync(CurrentGameBiz);
            if (file != null)
            {
                if (Path.GetExtension(file) is ".flv" or ".mkv" or ".mov" or ".mp4" or ".webm")
                {
                    mediaPlayer = new MediaPlayer();
                    mediaPlayer.IsLoopingEnabled = true;
                    mediaPlayer.Volume = VideoBgVolume / 100d;
                    mediaPlayer.IsVideoFrameServerEnabled = true;
                    mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(file));
                    mediaPlayer.VideoFrameAvailable += MediaPlayer_VideoFrameAvailable;
                    mediaPlayer.Play();
                    IsPlayingVideo = true;
                }
                else
                {
                    IsPlayingVideo = false;
                    using var fs = File.OpenRead(file);
                    var decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream());

                    WriteableBitmap bitmap;
                    double scale = MainWindow.Current.UIScale;
                    int decodeWidth = 0, decodeHeight = 0;
                    double windowWidth = ActualWidth * scale, windowHeight = ActualHeight * scale;

                    if (decoder.PixelWidth <= windowWidth || decoder.PixelHeight <= windowHeight)
                    {
                        decodeWidth = (int)decoder.PixelWidth;
                        decodeHeight = (int)decoder.PixelHeight;
                        bitmap = new WriteableBitmap(decodeWidth, decodeHeight);
                        fs.Position = 0;
                        await bitmap.SetSourceAsync(fs.AsRandomAccessStream());
                    }
                    else
                    {
                        if (windowWidth * decoder.PixelHeight > windowHeight * decoder.PixelWidth)
                        {
                            decodeWidth = (int)windowWidth;
                            decodeHeight = (int)(windowWidth * decoder.PixelHeight / decoder.PixelWidth);
                        }
                        else
                        {
                            decodeHeight = (int)windowHeight;
                            decodeWidth = (int)(windowHeight * decoder.PixelWidth / decoder.PixelHeight);
                        }
                        var data = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8,
                                                                   BitmapAlphaMode.Premultiplied,
                                                                   new BitmapTransform { ScaledWidth = (uint)decodeWidth, ScaledHeight = (uint)decodeHeight, InterpolationMode = BitmapInterpolationMode.Fant },
                                                                   ExifOrientationMode.IgnoreExifOrientation,
                                                                   ColorManagementMode.ColorManageToSRgb);
                        var bytes = data.DetachPixelData();
                        bitmap = new WriteableBitmap(decodeWidth, decodeHeight);
                        await bitmap.PixelBuffer.AsStream().WriteAsync(bytes);
                    }

                    if (AppConfig.EnableDynamicAccentColor)
                    {
                        (Color? back, Color? fore) = AccentColorHelper.GetAccentColor(bitmap.PixelBuffer, decodeWidth, decodeHeight);
                        MainWindow.Current.ChangeAccentColor(back, fore);
                    }
                    else
                    {
                        MainWindow.Current.ChangeAccentColor(null, null);
                    }
                    if (source.IsCancellationRequested)
                    {
                        return;
                    }
                    if (CurrentGameBiz is GameBiz.hk4e_cloud && !AppConfig.GetEnableCustomBg(GameBiz.hk4e_cloud))
                    {
                        Image_Content.HorizontalAlignment = HorizontalAlignment.Left;
                    }
                    else
                    {
                        Image_Content.HorizontalAlignment = HorizontalAlignment.Center;
                    }
                    BackgroundImage = bitmap;
                }
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
        finally
        {
            ProgressBar_LoadBackground.Visibility = Visibility.Collapsed;
            ProgressBar_LoadBackground.IsIndeterminate = false;
        }
    }



    private void MediaPlayer_VideoFrameAvailable(MediaPlayer sender, object args)
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            try
            {
                if (softwareBitmap is null || videoSource is null)
                {
                    int width = (int)sender.PlaybackSession.NaturalVideoWidth;
                    int height = (int)sender.PlaybackSession.NaturalVideoHeight;
                    sender.SystemMediaTransportControls.IsEnabled = false;
                    softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Premultiplied);
                    videoSource = new CanvasImageSource(CanvasDevice.GetSharedDevice(), width, height, 96);
                    BackgroundImage = videoSource;
                }
                using var canvas = CanvasBitmap.CreateFromSoftwareBitmap(CanvasDevice.GetSharedDevice(), softwareBitmap);
                sender.CopyFrameToVideoSurface(canvas);
                using var ds = videoSource.CreateDrawingSession(Microsoft.UI.Colors.Transparent);
                ds.DrawImage(canvas);
            }
            catch { }
        });
    }



    public void PlayVideo()
    {
        mediaPlayer?.Play();
    }


    public void PauseVideo()
    {
        mediaPlayer?.Pause();
    }




    #endregion



    #region Navigate




    private void UpdateNavigationViewItems()
    {
        if (CurrentGameBiz.ToGame() is GameBiz.None)
        {
            NavigationViewItem_Launcher.Visibility = Visibility.Collapsed;
            NavigationViewItem_GameAnnounce.Visibility = Visibility.Collapsed;
            NavigationViewItem_GameSetting.Visibility = Visibility.Collapsed;
            NavigationViewItem_Screenshot.Visibility = Visibility.Collapsed;
            NavigationViewItem_GachaLog.Visibility = Visibility.Collapsed;
            NavigationViewItem_HoyolabToolbox.Visibility = Visibility.Collapsed;
            NavigationViewItem_SelfQuery.Visibility = Visibility.Collapsed;
        }
        else if (CurrentGameBiz.ToGame() is GameBiz.Honkai3rd)
        {
            NavigationViewItem_Launcher.Visibility = Visibility.Visible;
            NavigationViewItem_GameAnnounce.Visibility = Visibility.Visible;
            NavigationViewItem_GameSetting.Visibility = Visibility.Visible;
            NavigationViewItem_Screenshot.Visibility = Visibility.Visible;
            NavigationViewItem_GachaLog.Visibility = Visibility.Collapsed;
            NavigationViewItem_HoyolabToolbox.Visibility = Visibility.Collapsed;
            NavigationViewItem_SelfQuery.Visibility = Visibility.Collapsed;
        }
        else
        {
            NavigationViewItem_Launcher.Visibility = Visibility.Visible;
            NavigationViewItem_GameAnnounce.Visibility = Visibility.Visible;
            NavigationViewItem_GameSetting.Visibility = Visibility.Visible;
            NavigationViewItem_Screenshot.Visibility = Visibility.Visible;
            NavigationViewItem_GachaLog.Visibility = Visibility.Visible;
            NavigationViewItem_HoyolabToolbox.Visibility = Visibility.Visible;
            NavigationViewItem_SelfQuery.Visibility = Visibility.Visible;
        }
        if (CurrentGameBiz.ToGame() is GameBiz.GenshinImpact)
        {
            // 祈愿记录
            NavigationViewItem_GachaLog.Content = Lang.GachaLogService_WishRecords;
        }
        if (CurrentGameBiz.ToGame() is GameBiz.StarRail)
        {
            // 跃迁记录
            NavigationViewItem_GachaLog.Content = Lang.GachaLogService_WarpRecords;
        }
        if (CurrentGameBiz.IsChinaServer())
        {
            NavigationViewItem_HoyolabToolbox.Content = Lang.HyperionToolbox;
        }
        if (CurrentGameBiz.IsGlobalServer())
        {
            NavigationViewItem_HoyolabToolbox.Content = Lang.HoYoLABToolbox;
        }
    }


    private async void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer?.IsSelected ?? false)
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
                if (item.Tag is "Tips")
                {
                    await Launcher.LaunchUriAsync(new("https://github.com/Scighost/Starward/blob/main/docs/Tips.md"));
                    sender.SelectedItem = null;
                    return;
                }
                var type = item.Tag switch
                {
                    nameof(LauncherPage) => typeof(LauncherPage),
                    nameof(GameNoticesPage) => typeof(GameNoticesPage),
                    nameof(GameSettingPage) => typeof(GameSettingPage),
                    nameof(ScreenshotPage) => typeof(ScreenshotPage),
                    nameof(GachaLogPage) => typeof(GachaLogPage),
                    nameof(HoyolabToolboxPage) => typeof(HoyolabToolboxPage),
                    nameof(SettingPage) => typeof(SettingPage),
                    nameof(SelfQueryPage) => typeof(SelfQueryPage),
                    _ => null,
                };
                NavigateTo(type);
            }
        }
    }



    public void NavigateTo(Type? page, object? param = null, NavigationTransitionInfo? infoOverride = null, bool changeGameBiz = false)
    {
        Border_ContentBackground.Visibility = Visibility.Visible;
        string? sourcePage = MainPage_Frame.CurrentSourcePageType?.Name, destPage = page?.Name;
        if (page is null
            || destPage is nameof(BlankPage)
            || (CurrentGameBiz.ToGame() is GameBiz.Honkai3rd && destPage is nameof(GachaLogPage) or nameof(HoyolabToolboxPage)))
        {
            page = typeof(LauncherPage);
            destPage = nameof(LauncherPage);
        }
        else if (!changeGameBiz && (destPage is nameof(GameNoticesPage)
            || (destPage is nameof(LauncherPage) && sourcePage is nameof(GameNoticesPage))))
        {
            infoOverride = new SuppressNavigationTransitionInfo();
        }
        if (page?.Name is nameof(LauncherPage))
        {
            MainPage_NavigationView.SelectedItem = MainPage_NavigationView.MenuItems.FirstOrDefault();
        }
        _logger.LogInformation("Navigate to {page} with param {param}", page!.Name, param ?? CurrentGameBiz);
        infoOverride ??= GetNavigationTransitionInfo(changeGameBiz);
        MainPage_Frame.Navigate(page, param ?? CurrentGameBiz, infoOverride);
        if (destPage is nameof(LauncherPage))
        {
            PlayVideo();
            Border_ContentBackground.Opacity = 0;
            if (sourcePage is nameof(GameNoticesPage))
            {
                Border_ContentBackground.Visibility = Visibility.Collapsed;
            }
        }
        else if (destPage is nameof(BlankPage))
        {
            PauseVideo();
            Border_ContentBackground.Opacity = 0;
        }
        else
        {
            PauseVideo();
            Border_ContentBackground.Opacity = 1;
        }
        IsPaneToggleButtonVisible = true;
    }



    private NavigationTransitionInfo GetNavigationTransitionInfo(bool changeGameBiz)
    {
        GameBiz lastGame = lastGameBiz.ToGame(), currentGame = CurrentGameBiz.ToGame();
        if (changeGameBiz && lastGame != GameBiz.None && lastGame != currentGame)
        {
            return (lastGameBiz.ToGame(), CurrentGameBiz.ToGame()) switch
            {
                (GameBiz.None, _) => new DrillInNavigationTransitionInfo(),
                (_, GameBiz.Honkai3rd) => new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft },
                (GameBiz.Honkai3rd, GameBiz.GenshinImpact) => new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight },
                (GameBiz.StarRail, GameBiz.GenshinImpact) => new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft },
                (_, GameBiz.StarRail) => new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight },
                _ => new DrillInNavigationTransitionInfo(),
            };
        }
        else
        {
            return new DrillInNavigationTransitionInfo();
        }
    }









    #endregion




    #region Shortcut



    private void OpenShortcutPanel()
    {
        Border_Shortcut_Page_1.Visibility = Visibility.Visible;
        Border_Shortcut_Page_2.Visibility = Visibility.Visible;
    }


    private void CloseShortcutPanel()
    {
        Border_Shortcut_Page_1.Visibility = Visibility.Collapsed;
        Border_Shortcut_Page_2.Visibility = Visibility.Collapsed;
    }


    private void Page_PreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        try
        {
            bool ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool alt = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
            if (e.Key == VirtualKey.Control)
            {
                OpenShortcutPanel();
                return;
            }
            if (alt)
            {
                CloseShortcutPanel();
                return;
            }
            if (ctrl)
            {
                ShortcutNavigate((int)e.Key - 48);
            }
        }
        catch { }
    }


    private void Page_PreviewKeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        try
        {
            if (e.Key is VirtualKey.Control or VirtualKey.Menu)
            {
                CloseShortcutPanel();
            }
        }
        catch { }
    }


    private void ShortcutNavigate(int num)
    {
        if (num == 0)
        {
            NavigationViewItem_Setting.IsSelected = true;
            NavigateTo(typeof(SettingPage));
        }
        if (CurrentGameBiz.ToGame() == GameBiz.None)
        {
            return;
        }
        if (num == 1)
        {
            NavigationViewItem_Launcher.IsSelected = true;
            NavigateTo(typeof(LauncherPage));
        }
        if (num == 2)
        {
            NavigationViewItem_GameAnnounce.IsSelected = true;
            NavigateTo(typeof(GameNoticesPage));
        }
        if (num == 3)
        {
            NavigationViewItem_GameSetting.IsSelected = true;
            NavigateTo(typeof(GameSettingPage));
        }
        if (num == 4)
        {
            NavigationViewItem_Screenshot.IsSelected = true;
            NavigateTo(typeof(ScreenshotPage));
        }
        if (num == 5 && CurrentGameBiz.ToGame() != GameBiz.Honkai3rd)
        {
            NavigationViewItem_GachaLog.IsSelected = true;
            NavigateTo(typeof(GachaLogPage));
        }
        if (num == 6 && CurrentGameBiz.ToGame() != GameBiz.Honkai3rd)
        {
            NavigationViewItem_HoyolabToolbox.IsSelected = true;
            NavigateTo(typeof(HoyolabToolboxPage));
        }
        if (num == 7 && CurrentGameBiz.ToGame() != GameBiz.Honkai3rd)
        {
            NavigationViewItem_SelfQuery.IsSelected = true;
            NavigateTo(typeof(SelfQueryPage));
        }
    }


    private void Page_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        try
        {
            CloseShortcutPanel();
        }
        catch { }
    }


    private void Page_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        try
        {
            var point = e.GetPosition(this);
            if (point.Y > 48)
            {
                OpenShortcutPanel();
            }
        }
        catch { }
    }


    private void Button_Navigation_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button)
            {
                if (int.TryParse(button.Tag as string, out int num))
                {
                    ShortcutNavigate(num);
                    CloseShortcutPanel();
                }
            }
        }
        catch { }
    }



    #endregion



}
