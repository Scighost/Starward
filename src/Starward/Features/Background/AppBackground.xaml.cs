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
using Starward.Features.ViewHost;
using Starward.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.Playback;
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
        WeakReferenceMessenger.Default.Register<BackgroundChangedMessage>(this, OnBackgroundChanged);
        WeakReferenceMessenger.Default.Register<MainWindowStateChangedMessage>(this, OnMainWindowStateChanged);
        WeakReferenceMessenger.Default.Register<VideoBgVolumeChangedMessage>(this, OnVideoBgVolumeChanged);
        this.Loaded += AppBackground_Loaded;
        this.Unloaded += AppBackground_Unloaded;
    }


    private void AppBackground_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        this.XamlRoot.Changed -= XamlRoot_Changed;
        this.XamlRoot.Changed += XamlRoot_Changed;
    }


    private void AppBackground_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        DisposeVideoResource();
        this.XamlRoot?.Changed -= XamlRoot_Changed;
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    private void XamlRoot_Changed(Microsoft.UI.Xaml.XamlRoot sender, Microsoft.UI.Xaml.XamlRootChangedEventArgs args)
    {
        if (_lastScale != sender.RasterizationScale)
        {
            _ = UpdateBackgroundAsync();
        }
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


    public ImageSource? PlacehoderImageSource { get; set => SetProperty(ref field, value); }

    public ImageSource? BackgroundImageSource
    {
        get; set
        {
            if (value is null && field is not null)
            {
                PlacehoderImageSource = field;
            }
            SetProperty(ref field, value);
        }
    }

    public bool IsUpdateBackgroundRunning { get; set => SetProperty(ref field, value); }

    public GameBackground? CurrentGameBackground { get; private set; }

    private string? _lastBackgroundFile;

    private double _lastScale = 1;


    private void InitializeBackgroundImage()
    {
        try
        {
            var file = BackgroundService.GetCachedBackgroundFile(CurrentGameId);
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



    private CancellationTokenSource? updateBackgroundCts;


    public async Task UpdateBackgroundAsync(GameBackground? background = null)
    {
        try
        {
            IsUpdateBackgroundRunning = true;

            updateBackgroundCts?.Cancel();
            updateBackgroundCts = new();
            CancellationToken cancellationToken = updateBackgroundCts.Token;

            if (CurrentGameId is null)
            {
                DisposeVideoResource();
                BackgroundImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Image/UI_CutScene_1130320101A.png"));
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                bool apiCancelled = false;
                string? filePath = null;
                GameBackground? gameBackground = null;
                try
                {
                    bool timeout = i == 0 && background is null;
                    CancellationToken apiCancellationToken = timeout ? new CancellationTokenSource(1000).Token : CancellationToken.None;
                    CancellationToken downloadCancellationToken = timeout ? new CancellationTokenSource(3000).Token : CancellationToken.None;
                    gameBackground = background ?? await _backgroundService.GetSuggestedGameBackgroundAsync(CurrentGameId, apiCancellationToken);
                    if (gameBackground is null)
                    {
                        filePath = BackgroundService.GetFallbackBackgroundImage(CurrentGameId);
                    }
                    else if (gameBackground.Type is GameBackground.BACKGROUND_TYPE_CUSTOM)
                    {
                        filePath = gameBackground.Background.Url;
                    }
                    else if (gameBackground.Type is GameBackground.BACKGROUND_TYPE_VIDEO && !gameBackground.StopVideo)
                    {
                        filePath = await _backgroundService.GetBackgroundFileAsync(gameBackground.Video.Url, downloadCancellationToken);
                    }
                    else
                    {
                        filePath = await _backgroundService.GetBackgroundFileAsync(gameBackground.Background.Url, downloadCancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    apiCancelled = true;
                    filePath = BackgroundService.GetFallbackBackgroundImage(CurrentGameId);
                }
                catch (Exception ex)
                {
                    filePath = BackgroundService.GetFallbackBackgroundImage(CurrentGameId);
                    _logger.LogError(ex, "Update background image");
                }
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                if (filePath == _lastBackgroundFile)
                {
                    if (BackgroundService.FileIsSupportedVideo(filePath))
                    {
                        continue;
                    }
                    if (_lastScale == this.XamlRoot.GetUIScaleFactor())
                    {
                        continue;
                    }
                }
                DisposeVideoResource();
                BackgroundImageSource = null;
                if (filePath != null)
                {
                    if (gameBackground?.Type is GameBackground.BACKGROUND_TYPE_VIDEO)
                    {
                        await SetVideoBackgroundAsync(gameBackground, filePath, cancellationToken);
                    }
                    else if (BackgroundService.FileIsSupportedVideo(filePath))
                    {
                        StartMediaPlayer(filePath);
                    }
                    else
                    {
                        await ChangeBackgroundImageAsync(filePath, cancellationToken);
                    }
                    _lastBackgroundFile = filePath;
                    _lastScale = this.XamlRoot.GetUIScaleFactor();
                    CurrentGameBackground = gameBackground;
                    if (!apiCancelled && gameBackground?.Type is not GameBackground.BACKGROUND_TYPE_CUSTOM)
                    {
                        AppConfig.SetBg(CurrentGameId.GameBiz, Path.GetFileName(filePath));
                        var list = await _backgroundService.GetGameBackgroundsAsync(CurrentGameId);
                        AppConfig.SetGameBackgroundIds(CurrentGameId.GameBiz, string.Join(',', list.Select(x => x.Id)));
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update background image");
        }
        finally
        {
            IsUpdateBackgroundRunning = false;
        }
    }


    private async Task ChangeBackgroundImageAsync(string file, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var fs = File.OpenRead(file);
        var decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream());

        double scale = this.XamlRoot.GetUIScaleFactor();
        int decodeWidth = 0, decodeHeight = 0;
        double windowWidth = ActualWidth * scale, windowHeight = ActualHeight * scale;

        if (decoder.PixelWidth <= windowWidth || decoder.PixelHeight <= windowHeight)
        {
            decodeWidth = (int)decoder.PixelWidth;
            decodeHeight = (int)decoder.PixelHeight;
            var writeableBitmap = new WriteableBitmap(decodeWidth, decodeHeight);
            fs.Position = 0;
            await writeableBitmap.SetSourceAsync(fs.AsRandomAccessStream());
            cancellationToken.ThrowIfCancellationRequested();
            Color? color = AccentColorHelper.GetAccentColor(writeableBitmap.PixelBuffer, decodeWidth, decodeHeight);
            AccentColorHelper.ChangeAppAccentColor(color);
            AppConfig.AccentColor = color?.ToHex() ?? null;
            BackgroundImageSource = writeableBitmap;
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
            using var soft = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8,
                                                                  BitmapAlphaMode.Premultiplied,
                                                                  new BitmapTransform
                                                                  {
                                                                      ScaledWidth = (uint)decodeWidth,
                                                                      ScaledHeight = (uint)decodeHeight,
                                                                      InterpolationMode = BitmapInterpolationMode.Fant
                                                                  },
                                                                  ExifOrientationMode.IgnoreExifOrientation,
                                                                  ColorManagementMode.DoNotColorManage);
            var softwareBitmapSource = new SoftwareBitmapSource();
            await softwareBitmapSource.SetBitmapAsync(soft);

            cancellationToken.ThrowIfCancellationRequested();

            using BitmapBuffer bitmapBuffer = soft.LockBuffer(BitmapBufferAccessMode.Read);
            using IMemoryBufferReference memoryBufferReference = bitmapBuffer.CreateReference();
            memoryBufferReference.As<AccentColorHelper.IMemoryBufferByteAccess>().GetBuffer(out nint bufferPtr, out uint capacity);
            Color? color = AccentColorHelper.GetAccentColor(bufferPtr, capacity, decodeWidth, decodeHeight);
            AccentColorHelper.ChangeAppAccentColor(color);
            AppConfig.AccentColor = color?.ToHex() ?? null;
            BackgroundImageSource = softwareBitmapSource;
        }
    }



    #region Video



    private MediaPlayer? _mediaPlayer;

    private CanvasRenderTarget? _videoSurface;

    private CanvasBitmap? _videoOverlayImage;

    private CanvasImageSource? _videoImageSource;

    private int videoBgVolume = AppConfig.VideoBgVolume;

    private SemaphoreSlim _videoSemaphore = new SemaphoreSlim(1, 1);


    private void StartMediaPlayer(string file)
    {
        _mediaPlayer = new MediaPlayer
        {
            IsLoopingEnabled = true,
            Volume = videoBgVolume / 100.0,
            IsMuted = false,
            IsVideoFrameServerEnabled = true,
            Source = MediaSource.CreateFromUri(new Uri(file))
        };
        _mediaPlayer.CommandManager.IsEnabled = false;
        _mediaPlayer.SystemMediaTransportControls.IsEnabled = false;
        _mediaPlayer.VideoFrameAvailable += MediaPlayer_VideoFrameAvailable;
        _mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
        _mediaPlayer.Play();
    }


    private async Task SetVideoBackgroundAsync(GameBackground gameBackground, string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (BackgroundService.FileIsSupportedVideo(filePath))
        {
            StartMediaPlayer(filePath);
            _ = PrepareVideoOverlayImageAsync(gameBackground.Theme.Url, cancellationToken);
            _ = ChangeAccentColorToImageFileAsync(gameBackground.Background.Url, cancellationToken);
        }
        else
        {
            string overlayPath = await _backgroundService.GetBackgroundFileAsync(gameBackground.Theme.Url, cancellationToken);
            using var fs1 = File.OpenRead(filePath);
            using var bitmap = await CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice(), fs1.AsRandomAccessStream(), 96);
            using var fs2 = File.OpenRead(overlayPath);
            using var overlay = await CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice(), fs2.AsRandomAccessStream(), 96);
            var imageSource = new CanvasImageSource(CanvasDevice.GetSharedDevice(), bitmap.SizeInPixels.Width, bitmap.SizeInPixels.Height, 96);
            using (var ds = imageSource.CreateDrawingSession(Microsoft.UI.Colors.Transparent))
            {
                ds.DrawImage(bitmap);
                Rect source = new Rect(0, 0, overlay.SizeInPixels.Width, overlay.SizeInPixels.Height);
                Rect dest = new Rect(0, 0, imageSource.SizeInPixels.Width, imageSource.SizeInPixels.Height);
                ds.DrawImage(overlay, dest, source, 1, CanvasImageInterpolation.HighQualityCubic);
            }
            BackgroundImageSource = imageSource;
            if (bitmap.Format is Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized)
            {
                try
                {
                    Color? color = await Task.Run(() =>
                    {
                        Color? color = AccentColorHelper.GetAccentColor(bitmap.GetPixelBytes(), (int)bitmap.SizeInPixels.Width, (int)bitmap.SizeInPixels.Height);
                        return color;
                    });
                    if (color is not null)
                    {
                        AccentColorHelper.ChangeAppAccentColor(color);
                        AppConfig.AccentColor = color?.ToHex() ?? null;
                    }
                }
                catch { }
            }

        }
    }




    private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {

    }

    private void MediaPlayer_VideoFrameAvailable(MediaPlayer sender, object args)
    {
        if (_videoSemaphore.CurrentCount == 0)
        {
            return;
        }
        _videoSemaphore.Wait();
        DispatcherQueue?.TryEnqueue(() =>
        {
            try
            {
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
                if (_videoOverlayImage is not null)
                {
                    Rect source = new Rect(0, 0, _videoOverlayImage.SizeInPixels.Width, _videoOverlayImage.SizeInPixels.Height);
                    Rect dest = new Rect(0, 0, _videoImageSource.SizeInPixels.Width, _videoImageSource.SizeInPixels.Height);
                    ds.DrawImage(_videoOverlayImage, dest, source, 1, CanvasImageInterpolation.HighQualityCubic);
                }
            }
            catch { }
            finally
            {
                _videoSemaphore.Release();
            }
        });
    }


    private async Task PrepareVideoOverlayImageAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            string filePath = await _backgroundService.GetBackgroundFileAsync(url, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            _videoOverlayImage?.Dispose();
            using var fs = File.OpenRead(filePath);
            _videoOverlayImage = await CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice(), fs.AsRandomAccessStream(), 96);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Prepare video overlay image");
        }
    }


    private async Task ChangeAccentColorToImageFileAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            Color? color = await Task.Run(async () =>
            {
                string filePath = await _backgroundService.GetBackgroundFileAsync(url, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }
                using var fs = File.OpenRead(filePath);
                var decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream());
                var pixelData = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, new BitmapTransform(), ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
                Color? color = AccentColorHelper.GetAccentColor(pixelData.DetachPixelData(), (int)decoder.PixelWidth, (int)decoder.PixelHeight);
                return color;
            });
            if (color is not null)
            {
                AccentColorHelper.ChangeAppAccentColor(color);
                AppConfig.AccentColor = color?.ToHex() ?? null;
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
        _mediaPlayer?.Dispose();
        _mediaPlayer = null;
        _videoSurface?.Dispose();
        _videoSurface = null;
        _videoImageSource = null;
        _videoOverlayImage?.Dispose();
        _videoOverlayImage = null;
    }


    #endregion



    private void OnBackgroundChanged(object _, BackgroundChangedMessage message)
    {
        _ = UpdateBackgroundAsync(message.GameBackground);
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


    private void OnVideoBgVolumeChanged(object _, VideoBgVolumeChangedMessage message)
    {
        try
        {
            videoBgVolume = message.Volume;
            _mediaPlayer?.Volume = message.Volume / 100d;
        }
        catch { }
    }


}
