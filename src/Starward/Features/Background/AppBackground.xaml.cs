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
using Starward.Frameworks;
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


    private readonly ILogger<AppBackground> _logger = AppService.GetLogger<AppBackground>();


    private readonly BackgroundService _backgroundService = AppService.GetService<BackgroundService>();


    public AppBackground()
    {
        this.InitializeComponent();
        WeakReferenceMessenger.Default.Register<BackgroundChangedMessage>(this, OnBackgroundChanged);
        WeakReferenceMessenger.Default.Register<MainWindowStateChangedMessage>(this, OnMainWindowStateChanged);
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

    private SoftwareBitmap? softwareBitmap;

    private CanvasImageSource? canvasImageSource;



    private void DisposeVideoResource()
    {
        mediaPlayer?.Dispose();
        mediaPlayer = null;
        softwareBitmap?.Dispose();
        softwareBitmap = null;
        canvasImageSource = null;
    }



    private void InitializeBackgroundImage()
    {
        try
        {
            var file = _backgroundService.GetCachedBackgroundFile(CurrentGameId);
            if (file != null)
            {
                if (!BackgroundService.FileIsSupportedVideo(file))
                {
                    BackgroundImageSource = new BitmapImage(new Uri(file));
                    try
                    {
                        string? hex = AppSetting.AccentColor;
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



    private CancellationTokenSource? cancelSource;


    [RelayCommand]
    public async Task UpdateBackgroundAsync()
    {
        try
        {
            cancelSource?.Cancel();
            cancelSource = new(TimeSpan.FromSeconds(5));

            if (CurrentGameId is null)
            {
                DisposeVideoResource();
                BackgroundImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Image/UI_CutScene_1130320101A.png"));
                return;
            }

            var file = await _backgroundService.GetBackgroundFileAsync(CurrentGameId);
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
                    await ChangeBackgroundImageAsync(file, cancelSource.Token);
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
    }



    private async Task ChangeBackgroundImageAsync(string file, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

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

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Color? color = AccentColorHelper.GetAccentColor(writeableBitmap.PixelBuffer, decodeWidth, decodeHeight);
            AppSetting.AccentColor = color?.ToHex() ?? null;
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

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            using BitmapBuffer bitmapBuffer = soft.LockBuffer(BitmapBufferAccessMode.Read);
            using IMemoryBufferReference memoryBufferReference = bitmapBuffer.CreateReference();
            memoryBufferReference.As<AccentColorHelper.IMemoryBufferByteAccess>().GetBuffer(out nint bufferPtr, out uint capacity);
            Color? color = AccentColorHelper.GetAccentColor(bufferPtr, capacity, decodeWidth, decodeHeight);
            AppSetting.AccentColor = color?.ToHex() ?? null;
            AccentColorHelper.ChangeAppAccentColor(color);
            BackgroundImageSource = softwareBitmapSource;
        }
    }



    private void StartMediaPlayer(string file)
    {
        mediaPlayer = new MediaPlayer
        {
            IsLoopingEnabled = true,
            Volume = 0,
            IsMuted = true,
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
                if (softwareBitmap is null || canvasImageSource is null)
                {
                    int width = (int)sender.PlaybackSession.NaturalVideoWidth;
                    int height = (int)sender.PlaybackSession.NaturalVideoHeight;
                    softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Premultiplied);
                    canvasImageSource = new CanvasImageSource(CanvasDevice.GetSharedDevice(), width, height, 96);
                    BackgroundImageSource = canvasImageSource;
                }
                using var canvas = CanvasBitmap.CreateFromSoftwareBitmap(CanvasDevice.GetSharedDevice(), softwareBitmap);
                sender.CopyFrameToVideoSurface(canvas);
                using var ds = canvasImageSource.CreateDrawingSession(Microsoft.UI.Colors.Transparent);
                ds.DrawImage(canvas);
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



}
