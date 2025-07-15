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


    private int _repeatCount;

    private string _lastFile;

    private CanvasImageSource _imageSource;

    private CancellationTokenSource? _cancellationTokenSource;

    private CancellationToken _openImageCancellationToken;


    private ImageViewWindow2? _imageViewWindow2;


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
            IsSuccess = true;
            IsError = false;
            _lastFile = file;
            CropImage(bitmap, User32.GetDpiForWindow(hwnd), maxCLL);
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            _openImageCancellationToken = _cancellationTokenSource.Token;
            DisplayWindow(hwnd, _cancellationTokenSource.Token);
        }
        catch { }
    }


    public void CaptureSuccess(Microsoft.UI.DisplayId displayId, CanvasBitmap bitmap, string file, float maxCLL = -1)
    {
        try
        {
            IsSuccess = true;
            IsError = false;
            _lastFile = file;
            CropImage(bitmap, GetDpiForMonitor(displayId), maxCLL);
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            _openImageCancellationToken = _cancellationTokenSource.Token;
            DisplayWindow(displayId, _cancellationTokenSource.Token);
        }
        catch { }
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
    public void CaptureError(nint hwnd)
    {
        try
        {
            IsError = true;
            IsSuccess = false;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            _openImageCancellationToken = _cancellationTokenSource.Token;
            DisplayWindow(hwnd, _cancellationTokenSource.Token);
        }
        catch { }
    }



    private Visual _contentVisual;


    private async void DisplayWindow(nint hwnd, CancellationToken cancellationToken)
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

            _repeatCount++;
            if (_repeatCount > 1)
            {
                TextBlock_Repeat.Visibility = Visibility.Visible;
                TextBlock_Repeat.Text = $"+{_repeatCount}";
            }

            ShowWindow(new RectInt32(targetX, targetY, width, height));
            await Task.Delay(3000, cancellationToken);
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


    private async void DisplayWindow(Microsoft.UI.DisplayId displayId, CancellationToken cancellationToken)
    {
        try
        {
            float dpiScale = GetDpiForMonitor(displayId) / 96f;
            int width = (int)(WindowWidth * dpiScale);
            int height = (int)(WindowHeight * dpiScale);
            var area = DisplayArea.GetFromDisplayId(displayId);
            int targetX = area.OuterBounds.X + area.OuterBounds.Width - width;
            int targetY = area.OuterBounds.Y + (int)(area.OuterBounds.Height * 0.25) - height / 2;

            _repeatCount++;
            if (_repeatCount > 1)
            {
                TextBlock_Repeat.Visibility = Visibility.Visible;
                TextBlock_Repeat.Text = $"+{_repeatCount}";
            }

            ShowWindow(new RectInt32(targetX, targetY, width, height));
            await Task.Delay(3000, cancellationToken);
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
        User32.ShowWindow(WindowHandle, ShowWindowCommand.SW_SHOWNOACTIVATE);
        StartShowAnimation();
    }


    private async Task HideWindowAsync(CancellationToken cancellationToken)
    {
        StartHideAnimation();
        await Task.Delay(600, cancellationToken);
        AppWindow?.Hide();
        _repeatCount = 0;
        TextBlock_Repeat.Text = "";
        TextBlock_Repeat.Visibility = Visibility.Collapsed;
    }



    private Vector3KeyFrameAnimation _showAnimation;

    private Vector3KeyFrameAnimation _hideAnimation;


    private void StartShowAnimation()
    {
        _contentVisual ??= ElementCompositionPreview.GetElementVisual(RootGrid);
        if (_showAnimation is null)
        {
            _showAnimation = _contentVisual.Compositor.CreateVector3KeyFrameAnimation();
            _showAnimation.InsertKeyFrame(1.0f, new Vector3(-320, 0, 0));
            _showAnimation.Duration = TimeSpan.FromSeconds(0.6);
        }
        _contentVisual.StartAnimation(nameof(_contentVisual.Offset), _showAnimation);
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
            if (_imageViewWindow2?.AppWindow is null)
            {
                _imageViewWindow2 = new();
            }
            await _imageViewWindow2.ShowWindowAsync(AppWindow.Id, _lastFile, true);
            await HideWindowAsync(_openImageCancellationToken);
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
                await HideWindowAsync(_openImageCancellationToken);
            }
        }
        catch { }
    }


}
