// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Starward.Core;
using Starward.Helpers;
using Starward.Messages;
using Starward.Pages.HoyolabToolbox;
using Starward.Pages.Setting;
using Starward.Services;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.System;
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
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, OnLanguageChanged);
        WeakReferenceMessenger.Default.Register<NavigationViewCompactChangedMessage>(this, InitializeNavigationViewPaneDisplayMode);
    }



    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        MainWindow.Current.KeyDown += MainPage_KeyDown;
        InitializeNavigationViewPaneDisplayMode();
        await UpdateBackgroundImageAsync(true);
#if !CI
        await CheckUpdateAsync();
#endif
    }



    private void InitializeNavigationViewPaneDisplayMode(object? sender = null, NavigationViewCompactChangedMessage? message = null)
    {
        try
        {
            if (AppConfig.EnableNavigationViewLeftCompact)
            {
                MainPage_NavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                Grid_FrameContent.CornerRadius = new CornerRadius(8, 0, 0, 0);
            }
            else
            {
                MainPage_NavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
                Grid_FrameContent.CornerRadius = new CornerRadius();
            }
        }
        catch { }
    }


    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateDragRectangles();
    }


    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        Current = null!;
        mediaPlayer?.Dispose();
        softwareBitmap?.Dispose();
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }




    private async Task CheckUpdateAsync()
    {
        try
        {
            await Task.Delay(1000);
            var release = await _updateService.CheckUpdateAsync(false);
            if (release != null)
            {
                MainWindow.Current.OverlayFrameNavigateTo(typeof(UpdatePage), release);
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



    public void OnLanguageChanged(object? sender, LanguageChangedMessage message)
    {
        if (message.Completed)
        {
            this.Bindings.Update();
            UpdateNavigationViewItems();
            NavigateTo(MainPage_Frame.SourcePageType, changeGameBiz: false);
        }
    }




    #region Select Game



    private GameBiz lastGameBiz;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentGameServerText))]
    private GameBiz currentGameBiz = (GameBiz)int.MaxValue;
    partial void OnCurrentGameBizChanged(GameBiz oldValue, GameBiz newValue)
    {
        lastGameBiz = oldValue;
        AppConfig.SelectGameBiz = newValue;
        UpdateNavigationViewItems();
    }


    public string CurrentGameServerText => CurrentGameBiz switch
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


    private void InitializeSelectGameBiz()
    {
        CurrentGameBiz = AppConfig.SelectGameBiz;
        AppConfig.SetLastRegionOfGame(CurrentGameBiz.ToGame(), CurrentGameBiz);
        _logger.LogInformation("Select game region is {gamebiz}", CurrentGameBiz);
        UpdateGameIcon();
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
            NavigateTo(MainPage_Frame.SourcePageType, changeGameBiz: true);
            await UpdateBackgroundImageAsync();
        }
    }


    [RelayCommand]
    private void ChangeGameBiz1(GameBiz biz)
    {
        if (biz.ToGame() is GameBiz.None)
        {
            GameBiz b = AppConfig.GetLastRegionOfGame(biz);
            if (b.ToGame() is GameBiz.None)
            {
                biz++;
            }
            else
            {
                biz = b;
            }
        }
        else
        {
            AppConfig.SetLastRegionOfGame(biz.ToGame(), biz);
        }
        CurrentGameBiz = biz;
        UpdateGameIcon();
        NavigateTo(MainPage_Frame.SourcePageType, changeGameBiz: true);
        _ = UpdateBackgroundImageAsync();
    }


    private void UpdateGameIcon()
    {
        GameIcon_BH3.Select(CurrentGameBiz);
        GameIcon_YS.Select(CurrentGameBiz);
        GameIcon_SR.Select(CurrentGameBiz);
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
                    if (!AppConfig.UseSystemThemeColor)
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
                    if (source.IsCancellationRequested)
                    {
                        return;
                    }
                    Color? back = null, fore = null;
                    if (!AppConfig.UseSystemThemeColor)
                    {
                        (back, fore) = AccentColorHelper.GetAccentColor(bitmap.PixelBuffer, decodeWidth, decodeHeight);
                    }
                    if (source.IsCancellationRequested)
                    {
                        return;
                    }
                    MainWindow.Current.ChangeAccentColor(back, fore);
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


    private bool pausedBySessionLocked;


    public void PlayVideo(bool sessionUnlock = false)
    {
        if (!sessionUnlock)
        {
            mediaPlayer?.Play();
        }
        if (sessionUnlock && pausedBySessionLocked)
        {
            mediaPlayer?.Play();
        }
    }


    public void PauseVideo(bool sessionLock = false)
    {
        if (mediaPlayer?.PlaybackSession?.PlaybackState is MediaPlaybackState.Playing)
        {
            pausedBySessionLocked = sessionLock;
        }
        mediaPlayer?.Pause();
    }




    #endregion



    #region Navigate




    private void UpdateNavigationViewItems()
    {
        if (CurrentGameBiz.ToGame() is GameBiz.None)
        {
            NavigationViewItem_Launcher.Visibility = Visibility.Collapsed;
            NavigationViewItem_GameSetting.Visibility = Visibility.Collapsed;
            NavigationViewItem_Screenshot.Visibility = Visibility.Collapsed;
            NavigationViewItem_GachaLog.Visibility = Visibility.Collapsed;
            NavigationViewItem_HoyolabToolbox.Visibility = Visibility.Collapsed;
            NavigationViewItem_SelfQuery.Visibility = Visibility.Collapsed;
        }
        else if (CurrentGameBiz.ToGame() is GameBiz.Honkai3rd)
        {
            NavigationViewItem_Launcher.Visibility = Visibility.Visible;
            NavigationViewItem_GameSetting.Visibility = Visibility.Visible;
            NavigationViewItem_Screenshot.Visibility = Visibility.Visible;
            NavigationViewItem_GachaLog.Visibility = Visibility.Collapsed;
            NavigationViewItem_HoyolabToolbox.Visibility = Visibility.Collapsed;
            NavigationViewItem_SelfQuery.Visibility = Visibility.Collapsed;
        }
        else
        {
            NavigationViewItem_Launcher.Visibility = Visibility.Visible;
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
                if (item.Tag is nameof(SettingPage))
                {
                    MainWindow.Current.OverlayFrameNavigateTo(typeof(SettingPage), null);
                    await Task.Delay(1);
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
                    nameof(SelfQueryPage) => typeof(SelfQueryPage),
                    _ => null,
                };
                NavigateTo(type);
            }
        }
    }



    public void NavigateTo(Type? page, object? param = null, NavigationTransitionInfo? infoOverride = null, bool changeGameBiz = false)
    {
        string? sourcePage = MainPage_Frame.CurrentSourcePageType?.Name, destPage = page?.Name;
        if (page is null
            || destPage is nameof(BlankPage)
            || (CurrentGameBiz.ToGame() is GameBiz.Honkai3rd && destPage is nameof(GachaLogPage) or nameof(HoyolabToolboxPage) or nameof(SelfQueryPage)))
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
            Image_FrameBackground.Opacity = 1;
        }
        else if (destPage is nameof(GameNoticesPage) or nameof(BlankPage))
        {
            PauseVideo();
            Image_FrameBackground.Opacity = 1;
        }
        else
        {
            PauseVideo();
            Image_FrameBackground.Opacity = 0;
        }
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



    private void MainPage_KeyDown(object? sender, MainWindow.KeyDownEventArgs e)
    {
        try
        {
            if (e.Handled)
            {
                return;
            }
            if (e.wParam == (nint)User32.VK.VK_ESCAPE)
            {
                PauseVideo();
                MainWindow.Current.Hide();
                e.Handled = true;
            }
        }
        catch { }
    }



    #endregion






}
