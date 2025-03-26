using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
using System.Runtime.InteropServices;
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


    private readonly ILogger<AppBackground> _logger = AppConfig.GetLogger<AppBackground>();


    private readonly BackgroundService _backgroundService = AppConfig.GetService<BackgroundService>();


    public AppBackground()
    {
        this.InitializeComponent();
        WeakReferenceMessenger.Default.Register<BackgroundChangedMessage>(this, OnBackgroundChanged);
        WeakReferenceMessenger.Default.Register<MainWindowStateChangedMessage>(this, OnMainWindowStateChanged);
        WeakReferenceMessenger.Default.Register<VideoBgVolumeChangedMessage>(this, OnVideoBgVolumeChanged);
        Unloaded += (_, _) => { DisposeVideoResource(); WeakReferenceMessenger.Default.UnregisterAll(this); };
    }


    public GameId CurrentGameId
    {
        get;
        set
        {
            if (field is null)
            {
                field = value;
                InitializeBackgroundImage();
            }
            field = value;
            UpdateBackgroundCommand.Execute(null);
        }
    }



    public ImageSource? PlacehoderImageSource { get; set => SetProperty(ref field, value); }


    public ImageSource? BackgroundImageSource
    {
        get;
        set
        {
            if (value is null && field is not null)
            {
                PlacehoderImageSource = field;
            }
            SetProperty(ref field, value);
        }
    }


    private MediaPlayer? mediaPlayer;

    private CanvasRenderTarget? canvasRenderTarget;

    private CanvasImageSource? canvasImageSource;

    private int videoBgVolume = AppConfig.VideoBgVolume;

    private string? lastBackgroundFile;


    private void DisposeVideoResource()
    {
        mediaPlayer?.Dispose();
        mediaPlayer = null;
        canvasRenderTarget?.Dispose();
        canvasRenderTarget = null;
        canvasImageSource = null;
    }



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


    [RelayCommand]
    public async Task UpdateBackgroundAsync()
    {
        try
        {
            updateBackgroundCts?.Cancel();
            updateBackgroundCts = new();
            CancellationToken cancellationToken = updateBackgroundCts.Token;

            if (CurrentGameId is null)
            {
                DisposeVideoResource();
                BackgroundImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Image/UI_CutScene_1130320101A.png"));
                return;
            }

            await foreach (string? file in _backgroundService.GetBackgroundFileAsync(CurrentGameId))
            {
                // 重复两次获取背景图
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                if (file == lastBackgroundFile)
                {
                    continue;
                }

                DisposeVideoResource();
                BackgroundImageSource = null;
                if (file != null)
                {
                    if (BackgroundService.FileIsSupportedVideo(file))
                    {
                        StartMediaPlayer(file);
                    }
                    else
                    {
                        await ChangeBackgroundImageAsync(file, cancellationToken);
                    }
                    lastBackgroundFile = file;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (COMException ex)
        {
            _logger.LogWarning(ex, "Update background image");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update background image");
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
            AppConfig.AccentColor = color?.ToHex() ?? null;
            AccentColorHelper.ChangeAppAccentColor(color);
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
            var soft = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8,
                                                            BitmapAlphaMode.Premultiplied,
                                                            new BitmapTransform
                                                            {
                                                                ScaledWidth = (uint)decodeWidth,
                                                                ScaledHeight = (uint)decodeHeight,
                                                                InterpolationMode = BitmapInterpolationMode.Fant
                                                            },
                                                            ExifOrientationMode.RespectExifOrientation,
                                                            ColorManagementMode.ColorManageToSRgb);
            var softwareBitmapSource = new SoftwareBitmapSource();
            await softwareBitmapSource.SetBitmapAsync(soft);

            cancellationToken.ThrowIfCancellationRequested();

            using BitmapBuffer bitmapBuffer = soft.LockBuffer(BitmapBufferAccessMode.Read);
            using IMemoryBufferReference memoryBufferReference = bitmapBuffer.CreateReference();
            memoryBufferReference.As<AccentColorHelper.IMemoryBufferByteAccess>().GetBuffer(out nint bufferPtr, out uint capacity);
            Color? color = AccentColorHelper.GetAccentColor(bufferPtr, capacity, decodeWidth, decodeHeight);
            AppConfig.AccentColor = color?.ToHex() ?? null;
            AccentColorHelper.ChangeAppAccentColor(color);
            BackgroundImageSource = softwareBitmapSource;
        }
    }



    private void StartMediaPlayer(string file)
    {
        mediaPlayer = new MediaPlayer
        {
            IsLoopingEnabled = true,
            Volume = videoBgVolume / 100d,
            IsMuted = false,
            IsVideoFrameServerEnabled = true,
            Source = MediaSource.CreateFromUri(new Uri(file))
        };
        mediaPlayer.CommandManager.IsEnabled = false;
        mediaPlayer.SystemMediaTransportControls.IsEnabled = false;
        mediaPlayer.VideoFrameAvailable += MediaPlayer_VideoFrameAvailable;
        mediaPlayer.Play();
    }



    private void MediaPlayer_VideoFrameAvailable(MediaPlayer sender, object args)
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            try
            {
                if (canvasRenderTarget is null || canvasImageSource is null)
                {
                    int height = (int)sender.PlaybackSession.NaturalVideoHeight;
                    int width = (int)sender.PlaybackSession.NaturalVideoWidth;
                    canvasRenderTarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96);
                    canvasImageSource = new CanvasImageSource(CanvasDevice.GetSharedDevice(), width, height, 96);
                    BackgroundImageSource = canvasImageSource;
                }
                sender.CopyFrameToVideoSurface(canvasRenderTarget);
                using var ds = canvasImageSource.CreateDrawingSession(Microsoft.UI.Colors.Transparent);
                ds.DrawImage(canvasRenderTarget);
            }
            catch { }
        });
    }



    private async void OnBackgroundChanged(object _, BackgroundChangedMessage message)
    {
        await UpdateBackgroundAsync();
    }




    private void OnMainWindowStateChanged(object _, MainWindowStateChangedMessage message)
    {
        try
        {
            if (mediaPlayer is not null)
            {
                var state = mediaPlayer.PlaybackSession.PlaybackState;
                if (message.Activate && state is not MediaPlaybackState.Playing)
                {
                    mediaPlayer.Play();
                }
                else if (message.Hide || message.SessionLock)
                {
                    mediaPlayer.Pause();
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
            if (mediaPlayer is not null)
            {
                mediaPlayer.Volume = message.Volume / 100d;
            }
        }
        catch { }
    }



}
