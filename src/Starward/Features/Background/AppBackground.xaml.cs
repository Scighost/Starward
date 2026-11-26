using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Starward.Core.HoYoPlay;
using Starward.Features.Codec;
using Starward.Features.ViewHost;
using Starward.Helpers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using WinRT;


namespace Starward.Features.Background;

[INotifyPropertyChanged]
public sealed partial class AppBackground : UserControl
{

    public static AppBackground Current { get; private set; }


    private readonly ILogger<AppBackground> _logger = AppConfig.GetLogger<AppBackground>();

    private readonly BackgroundService _backgroundService = AppConfig.GetService<BackgroundService>();


    public AppBackground()
    {
        Current = this;
        this.InitializeComponent();
        this.Unloaded += AppBackground_Unloaded;
        WeakReferenceMessenger.Default.Register<MainWindowStateChangedMessage>(this, OnMainWindowStateChanged);
    }



    private void AppBackground_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        this.Unloaded -= AppBackground_Unloaded;
        CanvasDevice.GetSharedDevice().DeviceLost -= CanvasDevice_DeviceLost;
        DisposeVideoResource();
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }



    public GameId CurrentGameId
    {
        get; set
        {
            if (field is null)
            {
                field = value;
                InitializeBackgroundImage();
            }
            field = value;
            _ = UpdateBackgroundAsync();
        }
    }

    public ImageSource? PlaceholderImageSource
    {
        get; set
        {
            if (field is SoftwareBitmapSource bitmapSource)
            {
                bitmapSource.Dispose();
            }
            SetProperty(ref field, value);
        }
    }

    public ImageSource? BackgroundImageSource
    {
        get; set
        {
            if (value is null && field is not null)
            {
                PlaceholderImageSource = field;
            }
            SetProperty(ref field, value);
        }
    }

    public string? ThemeImageUrl { get; set => SetProperty(ref field, value); }

    public bool IsUpdateBackgroundRunning { get; set => SetProperty(ref field, value); }

    public ObservableCollection<GameBackgroundModel>? GameBackgrounds { get; set => SetProperty(ref field, value); }

    public GameBackgroundModel? CurrentGameBackground { get; set => SetProperty(ref field, value); }


    private string? _lastBackgroundUrl;

    private string? _lastBackgroundFile;

    private CancellationTokenSource? _updateCancelTokenSource;


    private CancellationToken RefreshCancellationToken()
    {
        _updateCancelTokenSource?.Cancel();
        _updateCancelTokenSource = new();
        return _updateCancelTokenSource.Token;
    }



    private void InitializeBackgroundImage()
    {
        try
        {
            var file = BackgroundService.GetLastAppBackgroundFile(CurrentGameId);
            if (file != null)
            {
                if (!BackgroundService.FileIsSupportedVideo(file))
                {
                    BackgroundImageSource = new BitmapImage(new Uri(file));
                }
                try
                {
                    string? hex = AppConfig.AccentColor;
                    if (!string.IsNullOrWhiteSpace(hex))
                    {
                        Color color = ColorHelper.ToColor(hex);
                        AccentColorHelper.ChangeAppAccentColor(color);
                    }
                }
                catch { }
            }
            else
            {
                BackgroundImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Image/UI_CutScene_1130320101A.png"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize background image");
        }

    }


    public async Task UpdateBackgroundAsync()
    {
        CanvasDevice.GetSharedDevice().DeviceLost -= CanvasDevice_DeviceLost;
        CanvasDevice.GetSharedDevice().DeviceLost += CanvasDevice_DeviceLost;
        CancellationToken cancellationToken = RefreshCancellationToken();
        try
        {
            IsUpdateBackgroundRunning = true;
            GameBackgrounds = null;
            CurrentGameBackground = null;
            ThemeImageUrl = null;

            if (CurrentGameId is null)
            {
                DisposeVideoResource();
                BackgroundImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Image/UI_CutScene_1130320101A.png"));
                return;
            }

            try
            {
                if (BackgroundService.TryGetCustomBgFilePath(CurrentGameId, out string? path))
                {
                    if (BackgroundService.FileIsSupportedVideo(path))
                    {
                        DisposeVideoResource();
                        StartMediaPlayer(path);
                        _lastBackgroundFile = path;
                    }
                    else
                    {
                        await SetImageBackgroundAsync(path, cancellationToken);
                    }
                    return;
                }
            }
            catch { }

            var timeoutToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, new CancellationTokenSource(1000).Token).Token;
            try
            {
                await UpdateGameBackgroundAsync(false, timeoutToken);
                return;
            }
            catch (OperationCanceledException) when (timeoutToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                string? path = BackgroundService.GetLastAppBackgroundFile(CurrentGameId);
                if (path is not null)
                {
                    await SetImageBackgroundAsync(path, cancellationToken);
                }
            }

            await UpdateGameBackgroundAsync(true, cancellationToken);

        }
        catch (OperationCanceledException) { }
        catch (COMException ex) when (ex.HResult == -2003292277)
        {
            // 0x88982F8B
            if (Path.GetExtension(_lastBackgroundUrl)?.StartsWith(".webp", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                InAppToast.MainWindow?.ShowWithButton(InfoBarSeverity.Warning,
                                                      Lang.AppBackground_ImageDecodingFailed,
                                                      Lang.AppBackground_PleaseInstallTheWebPImageExtension,
                                                      Lang.SettingPage_Download,
                                                      async () => await Launcher.LaunchUriAsync(new("https://apps.microsoft.com/detail/9pg2dk419drg")));
            }
            else
            {
                InAppToast.MainWindow?.Warning(Lang.AppBackground_ImageDecodingFailed);
            }
            _logger.LogError(ex, "Cannot decode image: '{path}'", _lastBackgroundUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update background image");
            try
            {
                if (BackgroundService.GetFallbackBackgroundImage() is string path)
                {
                    await SetImageBackgroundAsync(path, cancellationToken);
                }
                else
                {
                    BackgroundImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Image/UI_CutScene_1130320101A.png"));
                }
            }
            catch (Exception)
            {
                BackgroundImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Image/UI_CutScene_1130320101A.png"));
            }
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                IsUpdateBackgroundRunning = false;
            }
        }
    }


    private async Task UpdateGameBackgroundAsync(bool imageBeforeVideo, CancellationToken cancellationToken)
    {
        (var models, var selected) = await _backgroundService.GetGameBackgroundsAsync(CurrentGameId, cancellationToken);
        GameBackgrounds = new(models);
        if (selected is not null)
        {
            if (selected.Type is 1 && selected.GameBackground.Type is GameBackground.BACKGROUND_TYPE_VIDEO)
            {
                if (imageBeforeVideo)
                {
                    _lastBackgroundUrl = selected.GameBackground.Background.Url;
                    await SetImageBackgroundAsync(selected.GameBackground, cancellationToken);
                }
                _lastBackgroundUrl = selected.GameBackground.Video.Url;
                await SetVideoBackgroundAsync(selected.GameBackground, cancellationToken);
            }
            else
            {
                _lastBackgroundUrl = selected.GameBackground.Background.Url;
                await SetImageBackgroundAsync(selected.GameBackground, cancellationToken);
            }
        }
        else
        {
            string? fallback = BackgroundService.GetFallbackBackgroundImage();
            if (fallback is not null)
            {
                await SetImageBackgroundAsync(fallback, cancellationToken);
            }
        }
        CurrentGameBackground = selected;
        SaveGameBackgroundSetting();
    }


    private async Task SetImageBackgroundAsync(GameBackground gameBackground, CancellationToken cancellationToken = default)
    {
        CancellationToken token = cancellationToken.CanBeCanceled ? cancellationToken : RefreshCancellationToken();
        try
        {
            if (!cancellationToken.CanBeCanceled)
            {
                IsUpdateBackgroundRunning = true;
            }
            string file = await _backgroundService.GetBackgroundFileAsync(gameBackground.Background.Url, token);
            await SetImageBackgroundAsync(file, token);
            ThemeImageUrl = string.IsNullOrWhiteSpace(gameBackground.Theme.Url) ? null : gameBackground.Theme.Url;
        }
        finally
        {
            if (!cancellationToken.CanBeCanceled)
            {
                IsUpdateBackgroundRunning = false;
            }
        }
    }


    private async Task SetImageBackgroundAsync(string file, CancellationToken cancellationToken)
    {
        if (_lastBackgroundFile == file)
        {
            return;
        }
        DisposeVideoResource();
        using var fs = File.OpenRead(file);
        var decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream());
        using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        var bitmapSource = new SoftwareBitmapSource();
        await bitmapSource.SetBitmapAsync(bitmap);
        if (cancellationToken.IsCancellationRequested)
        {
            bitmapSource.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
        }
        BackgroundImageSource = bitmapSource;

        using BitmapBuffer bitmapBuffer = bitmap.LockBuffer(BitmapBufferAccessMode.Read);
        using IMemoryBufferReference memoryBufferReference = bitmapBuffer.CreateReference();
        memoryBufferReference.As<AccentColorHelper.IMemoryBufferByteAccess>().GetBuffer(out nint bufferPtr, out uint capacity);
        Color? color = AccentColorHelper.GetAccentColor(bufferPtr, capacity, bitmap.PixelWidth, bitmap.PixelHeight);
        AccentColorHelper.ChangeAppAccentColor(color);
        AppConfig.AccentColor = color?.ToHex();
        _lastBackgroundFile = file;
        AppConfig.SetBg(CurrentGameId, Path.GetFileName(file));
    }


    private async Task SetVideoBackgroundAsync(GameBackground gameBackground, CancellationToken cancellationToken = default)
    {
        CancellationToken token = cancellationToken.CanBeCanceled ? cancellationToken : RefreshCancellationToken();
        try
        {
            if (!cancellationToken.CanBeCanceled)
            {
                IsUpdateBackgroundRunning = true;
            }
            string filePath = await _backgroundService.GetBackgroundFileAsync(gameBackground.Video.Url, token);
            if (_lastBackgroundFile == filePath)
            {
                return;
            }
            DisposeVideoResource();
            StartMediaPlayer(filePath);
            await ChangeAccentColorToImageFileAsync(gameBackground.Background.Url, token);
            ThemeImageUrl = string.IsNullOrWhiteSpace(gameBackground.Theme.Url) ? null : gameBackground.Theme.Url;
            _lastBackgroundFile = filePath;
        }
        finally
        {
            if (!cancellationToken.CanBeCanceled)
            {
                IsUpdateBackgroundRunning = false;
            }
        }

    }


    private void SaveGameBackgroundSetting()
    {
        if (GameBackgrounds != null && CurrentGameBackground != null)
        {
            int index = GameBackgrounds.IndexOf(CurrentGameBackground);
            if (index >= 0)
            {
                string content = $"{index};{string.Join(';', GameBackgrounds.Select(x => $"{x.Id},{x.Type}"))}";
                AppConfig.SetGameBackground(CurrentGameId, content);
            }
        }
        else
        {
            AppConfig.SetGameBackground(CurrentGameId, null);
        }
    }


    private void CanvasDevice_DeviceLost(CanvasDevice sender, object args)
    {
        try
        {
            sender.DeviceLost -= CanvasDevice_DeviceLost;
            _logger.LogWarning("Canvas device lost ({reason})", sender.GetDeviceLostReason());
            DispatcherQueue?.TryEnqueue(() =>
            {
                _lastBackgroundUrl = null;
                _lastBackgroundFile = null;
                _ = UpdateBackgroundAsync();

            });
        }
        catch { }
    }


    #region Video


    private MediaPlayer? _mediaPlayer;

    private CanvasRenderTarget? _videoSurface;

    private CanvasImageSource? _videoImageSource;

    private SemaphoreSlim _videoSemaphore = new SemaphoreSlim(1, 1);

    public bool IsVideoPlaying { get; set => SetProperty(ref field, value); }

    public int VideoBgVolume
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                _mediaPlayer?.Volume = Math.Clamp(value / 100.0, 0, 1);
                AppConfig.VideoBgVolume = value;
            }
        }
    } = Math.Clamp(AppConfig.VideoBgVolume, 0, 100);


    public async void PlayPauseVideo()
    {
        try
        {
            if (CurrentGameBackground is null)
            {
                return;
            }
            if (CurrentGameBackground.GameBackground.Type is not GameBackground.BACKGROUND_TYPE_VIDEO)
            {
                return;
            }
            CurrentGameBackground.Type = CurrentGameBackground.Type is 1 ? 0 : 1;
            SaveGameBackgroundSetting();
            if (CurrentGameBackground.Type is 1)
            {
                await SetVideoBackgroundAsync(CurrentGameBackground.GameBackground);
            }
            else
            {
                await SetImageBackgroundAsync(CurrentGameBackground.GameBackground);
            }
        }
        catch { }
    }


    private void StartMediaPlayer(string file)
    {
        if (Path.GetExtension(file).Equals(".webm", StringComparison.OrdinalIgnoreCase))
        {
            bool decoderInstalled = VP9Helper.IsVP9DecoderInstalled();
            bool vp8 = VP9Helper.IsVP8VideoFile(file);
            if (vp8)
            {
                if (!decoderInstalled)
                {
                    _needToInstallVp9VideoExtension = true;
                }
            }
            else
            {
                bool highProfileOrRgb = VP9Helper.IsVP9HighProfileOrRGB(file);
                if (!decoderInstalled || highProfileOrRgb)
                {
                    VP9Helper.RegisterVP9Decoder(true);
                }
                if (!decoderInstalled && !highProfileOrRgb)
                {
                    SuggestToInstallVP9Decoder();
                }
            }
        }
        VP9Helper.RegisterVorbisDecoder();
        _mediaPlayer = new MediaPlayer
        {
            IsLoopingEnabled = true,
            Volume = VideoBgVolume / 100.0,
            IsMuted = false,
            IsVideoFrameServerEnabled = true,
            Source = MediaSource.CreateFromUri(new Uri(file)),
        };
        _mediaPlayer.CommandManager.IsEnabled = false;
        _mediaPlayer.SystemMediaTransportControls.IsEnabled = false;
        _mediaPlayer.VideoFrameAvailable += MediaPlayer_VideoFrameAvailable;
        _mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
        _mediaPlayer.Play();
        IsVideoPlaying = true;
    }


    private bool _needToInstallVp9VideoExtension;


    private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        _logger.LogError(args.ExtendedErrorCode, "Media player failed.");
        if (_needToInstallVp9VideoExtension)
        {
            InAppToast.MainWindow?.ShowWithButton(InfoBarSeverity.Warning, null, Lang.AppBackground_VideoDecodingFailedPleaseInstallTheVP9VideoExtensions, Lang.SettingPage_Download, async () => await Launcher.LaunchUriAsync(new("https://apps.microsoft.com/detail/9n4d0msmp0pt")));
            _needToInstallVp9VideoExtension = false;
        }
        else
        {
            InAppToast.MainWindow?.Warning(Lang.AppBackground_VideoDecodingFailed);
        }

        DispatcherQueue?.TryEnqueue(async () =>
        {
            if (!ReferenceEquals(sender, _mediaPlayer))
            {
                return;
            }
            try
            {
                if (CurrentGameBackground is not null)
                {
                    CurrentGameBackground.Type = 0;
                    SaveGameBackgroundSetting();
                    await SetImageBackgroundAsync(CurrentGameBackground.GameBackground);
                }
                else
                {
                    DisposeVideoResource();
                }
            }
            catch { }
        });
    }


    private void MediaPlayer_VideoFrameAvailable(MediaPlayer sender, object args)
    {
        if (!_videoSemaphore.Wait(0))
        {
            return;
        }
        bool queue = DispatcherQueue?.TryEnqueue(() =>
        {
            try
            {
                if (!ReferenceEquals(sender, _mediaPlayer))
                {
                    return;
                }
                if (_videoSurface is null || _videoImageSource is null)
                {
                    _videoSurface?.Dispose();
                    int width = (int)sender.PlaybackSession.NaturalVideoWidth;
                    int height = (int)sender.PlaybackSession.NaturalVideoHeight;
                    _videoSurface = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96);
                    _videoImageSource = new CanvasImageSource(CanvasDevice.GetSharedDevice(), width, height, 96);
                    BackgroundImageSource = _videoImageSource;
                }
                sender.CopyFrameToVideoSurface(_videoSurface);
                using var ds = _videoImageSource.CreateDrawingSession(Microsoft.UI.Colors.Transparent);
                ds.DrawImage(_videoSurface);
            }
            catch { }
            finally
            {
                _videoSemaphore.Release();
            }
        }) ?? false;
        if (!queue)
        {
            _videoSemaphore.Release();
        }
    }


    private async Task ChangeAccentColorToImageFileAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            Color? color = await Task.Run(async () =>
            {
                string filePath = await _backgroundService.GetBackgroundFileAsync(url, cancellationToken);
                using var fs = File.OpenRead(filePath);
                var decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream());
                var pixelData = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, new BitmapTransform(), ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
                Color? color = AccentColorHelper.GetAccentColor(pixelData.DetachPixelData(), (int)decoder.PixelWidth, (int)decoder.PixelHeight);
                return color;
            });
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            if (color is not null)
            {
                AccentColorHelper.ChangeAppAccentColor(color);
                AppConfig.AccentColor = color?.ToHex();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Prepare video overlay image");
        }
    }


    private void DisposeVideoResource()
    {
        _mediaPlayer?.MediaFailed -= MediaPlayer_MediaFailed;
        _mediaPlayer?.VideoFrameAvailable -= MediaPlayer_VideoFrameAvailable;
        _mediaPlayer?.Dispose();
        _mediaPlayer = null;
        _videoSurface?.Dispose();
        _videoSurface = null;
        _videoImageSource = null;
        VP9Helper.UnregisterVP9Decoder(true);
        VP9Helper.UnregisterVorbisDecoder();
        IsVideoPlaying = false;
        BackgroundImageSource = null;
    }


    #endregion


    public async Task ChangeCurrentBackgroundIndexAsync(int value)
    {
        try
        {
            if (GameBackgrounds is null)
            {
                return;
            }
            if (value < 0 || value >= GameBackgrounds.Count)
            {
                return;
            }
            GameBackgroundModel model = GameBackgrounds[value];
            if (CurrentGameBackground == model)
            {
                return;
            }
            if (model.Type is 1)
            {
                await SetVideoBackgroundAsync(model.GameBackground);
            }
            else
            {
                await SetImageBackgroundAsync(model.GameBackground);
            }
            CurrentGameBackground = model;
            SaveGameBackgroundSetting();
        }
        catch { }
    }



    private void OnMainWindowStateChanged(object _, MainWindowStateChangedMessage message)
    {
        try
        {
            if (_mediaPlayer is not null)
            {
                var state = _mediaPlayer.PlaybackSession.PlaybackState;
                if (message.Activate && state is not MediaPlaybackState.Playing)
                {
                    _mediaPlayer.Play();
                }
                else if (message.Hide || message.SessionLock)
                {
                    _mediaPlayer.Pause();
                }
            }
        }
        catch { }
    }



    public async Task CopyCurrentBackgroundAsync()
    {
        try
        {
            if (CurrentGameBackground is null)
            {
                return;
            }
            string path;
            if (CurrentGameBackground.GameBackground.Type is GameBackground.BACKGROUND_TYPE_VIDEO && IsVideoPlaying)
            {
                path = await _backgroundService.GetBackgroundFileAsync(CurrentGameBackground.GameBackground.Video.Url);
            }
            else
            {
                path = await _backgroundService.GetBackgroundFileAsync(CurrentGameBackground.GameBackground.Background.Url);
            }
            var file = await StorageFile.GetFileFromPathAsync(path);
            ClipboardHelper.SetStorageItems(DataPackageOperation.Copy, file);
            InAppToast.MainWindow?.Information(Lang.Common_CopiedToClipboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy current background image {GameId}", CurrentGameId);
        }
    }



    public async Task SaveCurrentBackgroundAsync()
    {
        try
        {
            if (CurrentGameBackground is null)
            {
                return;
            }
            string path;
            if (CurrentGameBackground.GameBackground.Type is GameBackground.BACKGROUND_TYPE_VIDEO && IsVideoPlaying)
            {
                path = await _backgroundService.GetBackgroundFileAsync(CurrentGameBackground.GameBackground.Video.Url);
            }
            else
            {
                path = await _backgroundService.GetBackgroundFileAsync(CurrentGameBackground.GameBackground.Background.Url);
            }
            var savePath = await FileDialogHelper.OpenSaveFileDialogAsync(this.XamlRoot, Path.GetFileName(path));
            if (!string.IsNullOrWhiteSpace(savePath))
            {
                File.Copy(path, savePath, true);
                var file = await StorageFile.GetFileFromPathAsync(savePath);
                var options = new FolderLauncherOptions();
                options.ItemsToSelect.Add(file);
                await Launcher.LaunchFolderAsync(await file.GetParentAsync(), options);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save as current background image {GameId}", CurrentGameId);
        }
    }



    private bool _vp9DecoderSuggested;


    private void SuggestToInstallVP9Decoder()
    {
        if (!_vp9DecoderSuggested)
        {
            InAppToast.MainWindow?.ShowWithButton(InfoBarSeverity.Warning, null, Lang.ItIsRecommendedToInstallTheVP9VideoExtensionsToReduceCPUUsage, Lang.SettingPage_Download, async () => await Launcher.LaunchUriAsync(new("https://apps.microsoft.com/detail/9n4d0msmp0pt")));
            _vp9DecoderSuggested = true;
        }
    }


}
