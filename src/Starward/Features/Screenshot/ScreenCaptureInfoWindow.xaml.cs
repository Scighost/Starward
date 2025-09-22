using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Starward.Features.Setting;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.DirectX;
using Windows.Storage;
using Windows.System;


namespace Starward.Features.Screenshot;

[INotifyPropertyChanged]
public sealed partial class ScreenCaptureInfoWindow : WindowEx
{

    private const int WindowWidth = 320;

    private const int WindowHeight = 100;

    private readonly ILogger<ScreenCaptureInfoWindow> _logger = AppConfig.GetLogger<ScreenCaptureInfoWindow>();


    public ScreenCaptureInfoWindow()
    {
        InitializeComponent();
        SystemBackdrop = new TransparentBackdrop();
        InitializeWindow();
        // 不能删除，防止在 SW_SHOWNOACTIVATE 显示后没有文字
        this.Bindings.Update();
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, _) => this.Bindings.Update());
        this.Closed += ScreenCaptureInfoWindow_Closed;
    }



    private void InitializeWindow()
    {
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.IsShownInSwitchers = false;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
            User32.WindowStyles style = (User32.WindowStyles)User32.GetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_STYLE);
            style &= ~User32.WindowStyles.WS_DLGFRAME;
            User32.SetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_STYLE, (nint)style);
            User32.WindowStylesEx styleEx = (User32.WindowStylesEx)User32.GetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_EXSTYLE);
            styleEx |= User32.WindowStylesEx.WS_EX_TOPMOST;
            User32.SetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_EXSTYLE, (nint)styleEx);
        }
    }


    private void ScreenCaptureInfoWindow_Closed(object sender, WindowEventArgs args)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        this.Closed -= ScreenCaptureInfoWindow_Closed;
        Button_OpenImage.Click -= Button_OpenImage_Click;
        Button_OpenLog.Click -= Button_OpenLog_Click;
        _imageViewWindow2 = null;
    }


    public bool IsSuccess { get; set => SetProperty(ref field, value); }

    public bool IsError { get; set => SetProperty(ref field, value); }



    private int _captureImageCount;

    private int _finishedImageCount;

    private string? _lastFile;

    private CanvasImageSource _imageSource;

    private CancellationTokenSource? _cancellationTokenSource;

    private CancellationToken _openImageCancellationToken;


    private ImageViewWindow2? _imageViewWindow2;



    /// <summary>
    /// 开始截图后调用此方法显示信息
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="bitmap"></param>
    /// <param name="maxCLL"></param>
    public void CaptureStart(nint hwnd, CanvasBitmap bitmap, float maxCLL = -1)
    {
        IsSuccess = true;
        IsError = false;
        _captureImageCount++;
        CropImage(bitmap, User32.GetDpiForWindow(hwnd), maxCLL);
        _cancellationTokenSource?.Cancel();
        DisplayWindow(hwnd, true);
    }


    /// <summary>
    /// 开始截图后调用此方法显示信息
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="bitmap"></param>
    /// <param name="maxCLL"></param>
    public void CaptureStart(Microsoft.UI.DisplayId displayId, CanvasBitmap bitmap, float maxCLL = -1)
    {
        IsSuccess = true;
        IsError = false;
        _captureImageCount++;
        CropImage(bitmap, GetDpiForMonitor(displayId), maxCLL);
        _cancellationTokenSource?.Cancel();
        DisplayWindow(displayId, true);
    }



    /// <summary>
    /// 截图成功后调用此方法来显示截图信息窗口
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="bitmap"></param>
    /// <param name="file"></param>
    /// <param name="maxCLL"></param>
    public void CaptureSuccess(nint hwnd, CanvasBitmap bitmap, string file, float maxCLL = -1)
    {
        try
        {
            if (IsError)
            {
                CropImage(bitmap, User32.GetDpiForWindow(hwnd), maxCLL);
            }
            IsSuccess = true;
            IsError = false;
            _lastFile = file;
            _finishedImageCount++;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            _openImageCancellationToken = _cancellationTokenSource.Token;
            DisplayWindow(hwnd, false, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CaptureSuccess");
        }
    }


    public void CaptureSuccess(Microsoft.UI.DisplayId displayId, CanvasBitmap bitmap, string file, float maxCLL = -1)
    {
        try
        {
            if (IsError)
            {
                CropImage(bitmap, GetDpiForMonitor(displayId), maxCLL);
            }
            IsSuccess = true;
            IsError = false;
            _lastFile = file;
            _finishedImageCount++;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            _openImageCancellationToken = _cancellationTokenSource.Token;
            DisplayWindow(displayId, false, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CaptureSuccess");
        }
    }


    [LibraryImport("Shcore.dll")]
    private static partial int GetDpiForMonitor(nint hmonitor, int dpiType, out uint dpiX, out uint dpiY);


    private static uint GetDpiForMonitor(Microsoft.UI.DisplayId displayId)
    {
        GetDpiForMonitor((nint)displayId.Value, 0, out uint dpiX, out uint dpiY);
        return dpiX;
    }


    /// <summary>
    /// 剪裁并缩放至正方形大小
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="bitmap"></param>
    /// <param name="maxCLL"></param>
    private void CropImage(CanvasBitmap bitmap, uint dpi, float maxCLL = -1)
    {
        float dpiScale = dpi / 96f;
        float targetSize = 72 * dpiScale;

        float cropSize = Math.Min(bitmap.SizeInPixels.Width, bitmap.SizeInPixels.Height);
        float cropX = (bitmap.SizeInPixels.Width - cropSize) / 2f;
        float cropY = (bitmap.SizeInPixels.Height - cropSize) / 2f;
        using CropEffect cropEffect = new()
        {
            Source = bitmap,
            SourceRectangle = new Rect(cropX, cropY, cropSize, cropSize),
        };

        float imageScale = targetSize / cropSize;
        using ScaleEffect scaleEffect = new()
        {
            Source = cropEffect,
            Scale = new Vector2(imageScale, imageScale),
            InterpolationMode = CanvasImageInterpolation.HighQualityCubic,
        };

        ICanvasImage output = scaleEffect;
        if (bitmap.Format is DirectXPixelFormat.R16G16B16A16Float)
        {
            HdrToneMapEffect toneMapEffect = new()
            {
                Source = scaleEffect,
                InputMaxLuminance = maxCLL,
                OutputMaxLuminance = 300,
                DisplayMode = HdrToneMapEffectDisplayMode.Hdr,
            };
            WhiteLevelAdjustmentEffect whiteLevelEffect = new()
            {
                Source = toneMapEffect,
                InputWhiteLevel = 80,
                OutputWhiteLevel = 300,
            };
            GammaTransferEffect gammaEffect = new()
            {
                Source = whiteLevelEffect,
                RedExponent = 0.4545f,
                GreenExponent = 0.4545f,
                BlueExponent = 0.4545f,
            };
            output = gammaEffect;
        }

        if (_imageSource?.Size.Width != targetSize)
        {
            _imageSource = new CanvasImageSource(CanvasDevice.GetSharedDevice(), targetSize, targetSize, 96);
        }
        using (CanvasDrawingSession ds = _imageSource.CreateDrawingSession(Colors.Transparent))
        {
            ds.DrawImage(output, -cropX * imageScale, -cropY * imageScale);
        }
        ThumbnailImage.Source = _imageSource;
    }


    /// <summary>
    /// 显示错误信息
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="ex"></param>
    public void CaptureError(nint hwnd, bool captureStarted)
    {
        try
        {
            IsError = true;
            IsSuccess = false;
            if (captureStarted)
            {
                _finishedImageCount++;
            }
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            _openImageCancellationToken = _cancellationTokenSource.Token;
            DisplayWindow(hwnd, false, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CaptureError");
        }
    }



    private Visual _contentVisual;


    private async void DisplayWindow(nint hwnd, bool doNotClose = false, CancellationToken cancellationToken = default)
    {
        try
        {
            float dpiScale = User32.GetDpiForWindow(hwnd) / 96f;
            int width = (int)(WindowWidth * dpiScale);
            int height = (int)(WindowHeight * dpiScale);

            if (!User32.GetClientRect(hwnd, out var clientRect))
            {
                return;
            }
            int clientWidth = clientRect.right - clientRect.left;
            int clientHeight = clientRect.bottom - clientRect.top;
            POINT clientTopLeft = new POINT { x = 0, y = 0 };
            if (!User32.ClientToScreen(hwnd, ref clientTopLeft))
            {
                return;
            }

            int targetX = clientTopLeft.x + clientWidth - width;
            int targetY = clientTopLeft.y + (int)(clientHeight * 0.25) - height / 2;

            HMONITOR monitor = User32.MonitorFromWindow(hwnd, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);
            User32.MONITORINFOEX monitorInfo = new() { cbSize = (uint)Marshal.SizeOf<User32.MONITORINFOEX>() };
            User32.GetMonitorInfo(monitor, ref monitorInfo);
            var err = Kernel32.GetLastError();
            var rcMonitor = monitorInfo.rcMonitor;

            bool exceedsClient = targetX < clientTopLeft.x
                                 || targetX + width > clientTopLeft.x + clientWidth
                                 || targetY < clientTopLeft.y
                                 || targetY + height > clientTopLeft.y + clientHeight;

            bool exceedsScreen = targetX < rcMonitor.left
                                 || targetX + width > rcMonitor.right
                                 || targetY < rcMonitor.top
                                 || targetY + height > rcMonitor.bottom;

            bool fallbackToScreen = exceedsClient || exceedsScreen || User32.GetForegroundWindow() != hwnd;

            if (fallbackToScreen)
            {
                targetX = rcMonitor.right - width;
                targetY = rcMonitor.top + (int)(rcMonitor.Height * 0.25) - height / 2;
            }

            bool complete = _finishedImageCount == _captureImageCount;
            TextBlock_State.Text = complete ? Lang.ScreenCaptureInfoWindow_ScreenshotSaved : Lang.ScreenCaptureInfoWindow_ProcessingImage;
            ProgressRing_Process.Visibility = complete ? Visibility.Collapsed : Visibility.Visible;
            FontIcon_Complete.Visibility = complete ? Visibility.Visible : Visibility.Collapsed;
            if (_captureImageCount > 1)
            {
                TextBlock_Repeat.Visibility = Visibility.Visible;
                TextBlock_Repeat.Text = $"{_finishedImageCount}/{_captureImageCount}";
            }
            Button_OpenImage.Visibility = IsSuccess && _finishedImageCount > 0 ? Visibility.Visible : Visibility.Collapsed;

            ShowWindow(new RectInt32(targetX, targetY, width, height));
            if (doNotClose)
            {
                return;
            }
            await Task.Delay(2000, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                await HideWindowAsync(cancellationToken);
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to display screenshot info window.");
        }
    }


    private async void DisplayWindow(Microsoft.UI.DisplayId displayId, bool doNotClose, CancellationToken cancellationToken = default)
    {
        try
        {
            float dpiScale = GetDpiForMonitor(displayId) / 96f;
            int width = (int)(WindowWidth * dpiScale);
            int height = (int)(WindowHeight * dpiScale);
            var area = DisplayArea.GetFromDisplayId(displayId);
            int targetX = area.OuterBounds.X + area.OuterBounds.Width - width;
            int targetY = area.OuterBounds.Y + (int)(area.OuterBounds.Height * 0.25) - height / 2;

            bool complete = _finishedImageCount == _captureImageCount;
            TextBlock_State.Text = complete ? Lang.ScreenCaptureInfoWindow_ScreenshotSaved : Lang.ScreenCaptureInfoWindow_ProcessingImage;
            ProgressRing_Process.Visibility = complete ? Visibility.Collapsed : Visibility.Visible;
            FontIcon_Complete.Visibility = complete ? Visibility.Visible : Visibility.Collapsed;
            if (_captureImageCount > 1)
            {
                TextBlock_Repeat.Visibility = Visibility.Visible;
                TextBlock_Repeat.Text = $"{_finishedImageCount}/{_captureImageCount}";
            }
            Button_OpenImage.Visibility = IsSuccess && _finishedImageCount > 0 ? Visibility.Visible : Visibility.Collapsed;

            ShowWindow(new RectInt32(targetX, targetY, width, height));
            if (doNotClose)
            {
                return;
            }
            await Task.Delay(2000, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                await HideWindowAsync(cancellationToken);
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to display screenshot info window.");
        }
    }


    private void ShowWindow(RectInt32 rect)
    {
        AppWindow.MoveAndResize(rect);
        AppWindow.Show(false);
        StartShowAnimation();
    }


    private async Task HideWindowAsync(CancellationToken cancellationToken)
    {
        if (_finishedImageCount < _captureImageCount)
        {
            return;
        }
        StartHideAnimation();
        await Task.Delay(600, cancellationToken);
        AppWindow?.Hide();
        _captureImageCount = 0;
        _finishedImageCount = 0;
        _lastFile = null;
        TextBlock_Repeat.Text = "";
        TextBlock_Repeat.Visibility = Visibility.Collapsed;
    }



    private Vector3KeyFrameAnimation _showAnimation;

    private Vector3KeyFrameAnimation _hideAnimation;

    private long _lastShowAnimationTs;

    private void StartShowAnimation()
    {
        long ts = Stopwatch.GetTimestamp();
        if (ts - _lastShowAnimationTs < 0.6 * Stopwatch.Frequency)
        {
            return;
        }
        _contentVisual ??= ElementCompositionPreview.GetElementVisual(RootGrid);
        if (_showAnimation is null)
        {
            _showAnimation = _contentVisual.Compositor.CreateVector3KeyFrameAnimation();
            _showAnimation.InsertKeyFrame(1.0f, new Vector3(-320, 0, 0));
            _showAnimation.Duration = TimeSpan.FromSeconds(0.6);
        }
        _contentVisual.StartAnimation(nameof(_contentVisual.Offset), _showAnimation);
        _lastShowAnimationTs = ts;
    }


    private void StartHideAnimation()
    {
        _contentVisual ??= ElementCompositionPreview.GetElementVisual(RootGrid);
        if (_hideAnimation is null)
        {
            _hideAnimation = _contentVisual.Compositor.CreateVector3KeyFrameAnimation();
            _hideAnimation.InsertKeyFrame(1.0f, new Vector3(0, 0, 0));
            _hideAnimation.Duration = TimeSpan.FromSeconds(0.6);
        }
        _contentVisual.StartAnimation(nameof(_contentVisual.Offset), _hideAnimation);
    }


    private async void Button_OpenImage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_lastFile))
            {
                return;
            }
            if (_imageViewWindow2?.AppWindow is null)
            {
                _imageViewWindow2 = new();
            }
            await _imageViewWindow2.ShowWindowAsync(AppWindow.Id, _lastFile, true);
            if (_finishedImageCount == _captureImageCount)
            {
                await HideWindowAsync(_openImageCancellationToken);
            }
        }
        catch { }
    }


    private async void Button_OpenLog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(AppConfig.LogFile);
            if (file is not null)
            {
                var options = new FolderLauncherOptions();
                options.ItemsToSelect.Add(file);
                await Launcher.LaunchFolderAsync(await file.GetParentAsync(), options);
                if (_finishedImageCount == _captureImageCount)
                {
                    await HideWindowAsync(_openImageCancellationToken);
                }
            }
        }
        catch { }
    }


}
