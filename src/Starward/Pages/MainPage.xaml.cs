// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
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
using NuGet.Versioning;
using Starward.Core;
using Starward.Helpers;
using Starward.Messages;
using Starward.Pages.HoyolabToolbox;
using Starward.Pages.Setting;
using Starward.Services;
using Starward.Services.Launcher;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
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
public sealed partial class MainPage : PageBase
{


    private readonly ILogger<MainPage> _logger = AppConfig.GetLogger<MainPage>();


    //private readonly LauncherContentService _launcherContentService = AppConfig.GetService<LauncherContentService>();

    private readonly LauncherBackgroundService _launcherBackgroundService = AppConfig.GetService<LauncherBackgroundService>();


    private readonly UpdateService _updateService = AppConfig.GetService<UpdateService>();


    private readonly HoYoPlayService _hoyoPlayService = AppConfig.GetService<HoYoPlayService>();


    private readonly Compositor compositor;


    public MainPage()
    {
        this.InitializeComponent();
        compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
        InitializeGameBiz();
        RegisterMessage();
    }



    protected override void OnLoaded()
    {
        MainWindow.Current.KeyDown += MainPage_KeyDown;
        InitializeBackgroundImage();
        _ = UpdateBackgroundImageAsync(true);
        _ = ShowRecentUpdateContentAsync();
        _ = CheckUpdateAsync();
        _ = PrepareHoYoPlayDataAsync();
    }


    private async Task PrepareHoYoPlayDataAsync()
    {
        await Task.Delay(3000);
        await _hoyoPlayService.PrepareDataAsync();
    }


    protected override void OnUnloaded()
    {
        mediaPlayer?.Dispose();
        softwareBitmap?.Dispose();
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }



    private void RegisterMessage()
    {
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, OnLanguageChanged);
        WeakReferenceMessenger.Default.Register<GameStartMessage>(this, (_, _) => PauseVideo());
        WeakReferenceMessenger.Default.Register<UpdateBackgroundImageMessage>(this, (_, m) => _ = UpdateBackgroundImageAsync(m.Force));
        WeakReferenceMessenger.Default.Register<MainPageNavigateMessage>(this, (_, m) => NavigateTo(m.Page, m.Param, m.TransitionInfo));
    }



    private async Task CheckUpdateAsync()
    {
        try
        {
#if CI || DEBUG
            return;
#endif
#pragma warning disable CS0162 // 检测到无法访问的代码
            await Task.Delay(1000);
            var release = await _updateService.CheckUpdateAsync(false);
            if (release != null)
            {
                MainWindow.Current.OverlayFrameNavigateTo(typeof(UpdatePage), release);
            }
#pragma warning restore CS0162 // 检测到无法访问的代码
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
            UpdateNavigationViewItemsText();
            NavigateTo(MainPage_Frame.SourcePageType);
        }
    }



    private async Task ShowRecentUpdateContentAsync()
    {
#if !CI
        try
        {
            await Task.Delay(600);
            if (NuGetVersion.TryParse(AppConfig.AppVersion, out var version))
            {
                NuGetVersion.TryParse(AppConfig.LastAppVersion, out var lastVersion);
                if (version != lastVersion)
                {
                    new UpdateContentWindow().Activate();
                }
            }
        }
        catch { }
#endif
    }




    #region  GameBiz





    private void InitializeGameBiz()
    {
        CurrentGameBiz = AppConfig.CurrentGameBiz;
        if (CurrentGameBiz.ToGame() is GameBiz.None)
        {
            CurrentGameBiz = GameBiz.None;
        }
        _logger.LogInformation("Last game region is {gamebiz}", CurrentGameBiz);

        GameBizSelector.InitializeGameBiz(CurrentGameBiz);

        UpdateNavigationViewItemsText();
        if (CurrentGameBiz.ToGame() == GameBiz.None)
        {
            MainPage_Frame.Content = new BlankPage();
        }
        else
        {
            NavigateTo(typeof(GameLauncherPage));
        }
    }



    private void GameBizSelector_GameBizChanged(object sender, (GameBiz biz, bool doubleTapped) args)
    {
        _logger.LogInformation("Change game region to {gamebiz}", args.biz);
        CurrentGameBiz = args.biz;
        UpdateNavigationViewItemsText();
        if (args.doubleTapped)
        {
            NavigateTo(typeof(GameLauncherPage));
        }
        else
        {
            NavigateTo(MainPage_Frame.SourcePageType);
        }
        _ = UpdateBackgroundImageAsync();
        AppConfig.CurrentGameBiz = args.biz;
    }



    public void UpdateDragRectangles()
    {
        GameBizSelector.UpdateDragRectangles();
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



    private void InitializeBackgroundImage()
    {
        try
        {
            var file = _launcherBackgroundService.GetCachedBackgroundImage(CurrentGameBiz);
            if (file != null)
            {
                if (Path.GetExtension(file) is ".flv" or ".mkv" or ".mov" or ".mp4" or ".webm")
                {
                    AppConfig.IsPlayingVideo = true;
                    BackgroundImage = new BitmapImage(new Uri("ms-appx:///Assets/Image/UI_CutScene_1130320101A.png"));
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
                BackgroundImage = new BitmapImage(new Uri("ms-appx:///Assets/Image/UI_CutScene_1130320101A.png"));
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

            var file = await _launcherBackgroundService.GetBackgroundImageAsync(CurrentGameBiz);
            if (file != null)
            {
                if (Path.GetExtension(file) is ".flv" or ".mkv" or ".mov" or ".mp4" or ".webm")
                {
                    mediaPlayer = new MediaPlayer
                    {
                        IsLoopingEnabled = true,
                        Volume = VideoBgVolume / 100d,
                        IsVideoFrameServerEnabled = true,
                        Source = MediaSource.CreateFromUri(new Uri(file))
                    };
                    mediaPlayer.VideoFrameAvailable += MediaPlayer_VideoFrameAvailable;
                    mediaPlayer.Play();
                    AppConfig.IsPlayingVideo = true;
                    WeakReferenceMessenger.Default.Send(new VideoPlayStateChangedMessage(true));
                }
                else
                {
                    AppConfig.IsPlayingVideo = false;
                    WeakReferenceMessenger.Default.Send(new VideoPlayStateChangedMessage(false));
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




    private void UpdateNavigationViewItemsText()
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
        else if (CurrentGameBiz.ToGame() is GameBiz.ZZZ)
        {
            NavigationViewItem_Launcher.Visibility = Visibility.Visible;
            NavigationViewItem_GameSetting.Visibility = Visibility.Visible;
            NavigationViewItem_Screenshot.Visibility = Visibility.Visible;
            NavigationViewItem_GachaLog.Visibility = Visibility.Visible;
            NavigationViewItem_HoyolabToolbox.Visibility = Visibility.Visible;
            NavigationViewItem_SelfQuery.Visibility = Visibility.Visible;
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
            ToolTipService.SetToolTip(NavigationViewItem_GachaLog, Lang.GachaLogService_WishRecords);
            TextBlock_GachaLog.Text = Lang.GachaLogService_WishRecords;
        }
        if (CurrentGameBiz.ToGame() is GameBiz.StarRail)
        {
            // 跃迁记录
            ToolTipService.SetToolTip(NavigationViewItem_GachaLog, Lang.GachaLogService_WarpRecords);
            TextBlock_GachaLog.Text = Lang.GachaLogService_WarpRecords;
        }
        if (CurrentGameBiz.ToGame() is GameBiz.ZZZ)
        {
            // 调频记录
            ToolTipService.SetToolTip(NavigationViewItem_GachaLog, Lang.GachaLogService_SignalSearchRecords);
            TextBlock_GachaLog.Text = Lang.GachaLogService_SignalSearchRecords;
        }
        if (CurrentGameBiz.IsChinaServer())
        {
            ToolTipService.SetToolTip(NavigationViewItem_HoyolabToolbox, Lang.HyperionToolbox);
            TextBlock_HoyolabToolbox.Text = Lang.HyperionToolbox;
        }
        if (CurrentGameBiz.IsGlobalServer())
        {
            ToolTipService.SetToolTip(NavigationViewItem_HoyolabToolbox, Lang.HoYoLABToolbox);
            TextBlock_HoyolabToolbox.Text = Lang.HoYoLABToolbox;
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
            if (args.InvokedItemContainer is NavigationViewItem item)
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
                    nameof(GameLauncherPage) => typeof(GameLauncherPage),
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


    public void NavigateTo(Type? page, object? param = null, NavigationTransitionInfo? infoOverride = null)
    {
        string? destPage = page?.Name;
        if (destPage is null or nameof(BlankPage)
            || (CurrentGameBiz.ToGame() is GameBiz.Honkai3rd && destPage is not nameof(GameLauncherPage) and not nameof(GameSettingPage) and not nameof(ScreenshotPage)))
        {
            page = typeof(GameLauncherPage);
            destPage = nameof(GameLauncherPage);
        }
        if (destPage is nameof(GameLauncherPage))
        {
            MainPage_NavigationView.SelectedItem = NavigationViewItem_Launcher;
        }
        _logger.LogInformation("Navigate to {page} with param {param}", destPage, param ?? CurrentGameBiz);
        MainPage_Frame.Navigate(page, param ?? CurrentGameBiz);
        if (destPage is nameof(GameLauncherPage))
        {
            PlayVideo();
            Border_OverlayMask.Opacity = 0;
        }
        else if (destPage is nameof(GameNoticesPage) or nameof(BlankPage))
        {
            PauseVideo();
            Border_OverlayMask.Opacity = 0;
        }
        else
        {
            PauseVideo();
            Border_OverlayMask.Opacity = 1;
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
