using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Display;
using Microsoft.UI;
using Starward.Features.Overlay;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.DirectX;

namespace Starward.Features.Screenshot;

internal class ScreenCaptureService
{


    private static ScreenCaptureService? _instance;
    private static ScreenCaptureService Instance => _instance ??= AppConfig.GetService<ScreenCaptureService>();


    private readonly ILogger<ScreenCaptureService> _logger;


    private ScreenCaptureInfoWindow? _infoWindow;


    private List<ScreenCaptureContext> captureContexts;



    public ScreenCaptureService(ILogger<ScreenCaptureService> logger)
    {
        _logger = logger;
        captureContexts = new List<ScreenCaptureContext>();
    }



    public static void Capture()
    {
        Instance.CaptureInternal();
    }



    private static DisplayAdvancedColorInfo GetAdvancedColorInfoFromWindowHandle(nint hwnd)
    {
        Microsoft.UI.DisplayId displayId = Win32Interop.GetDisplayIdFromMonitor(User32.MonitorFromWindow(hwnd, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST).DangerousGetHandle());
        DisplayInformation displayInformation = DisplayInformation.CreateForDisplayId(displayId);
        return displayInformation.GetAdvancedColorInfo();
    }




    private async void CaptureInternal()
    {
        try
        {
            RunningGame? runningGame = RunningGameService.GetLatestActiveGame();
            if (runningGame is null)
            {
                _logger.LogWarning("No active game found for screen capture.");
                return;
            }
            if (User32.IsIconic(runningGame.WindowHandle))
            {
                _logger.LogWarning("Cannot capture a minimized window.");
                return;
            }
            (CanvasBitmap? renderTarget, string? file, float maxCLL) = await CaptureAndSaveAsync(runningGame);
            using (renderTarget)
            {
                if (_infoWindow?.AppWindow is null)
                {
                    _infoWindow = new ScreenCaptureInfoWindow();
                }
                _infoWindow.CaptureSuccess(runningGame.WindowHandle, renderTarget, file, maxCLL);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while capturing the screen.");
        }
    }




    private async Task<(CanvasBitmap CanvasBitmap, string FilePath, float MaxCLL)> CaptureAndSaveAsync(RunningGame runningGame)
    {
        if (captureContexts.FirstOrDefault(x => x.WindowHandle == runningGame.WindowHandle) is not ScreenCaptureContext context)
        {
            context = new ScreenCaptureContext(runningGame.WindowHandle);
            context.CaptureWindowClosed += OnCaptureWindowClosed;
            captureContexts.Add(context);
        }
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        using var frame = await context.CaptureAsync(cts.Token).ConfigureAwait(false);
        using CanvasBitmap canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(CanvasDevice.GetSharedDevice(), frame.Surface, 96);
        DisplayAdvancedColorInfo colorInfo = GetAdvancedColorInfoFromWindowHandle(runningGame.WindowHandle);
        float maxCLL = GetMaxCLL(canvasBitmap);
        bool hdr = maxCLL > colorInfo.SdrWhiteLevelInNits;
        bool clip = TryClipClient(runningGame.WindowHandle, frame.ContentSize, out Rect clientRect);
        if (!clip)
        {
            clientRect = new Rect(0, 0, frame.ContentSize.Width, frame.ContentSize.Height);
        }
        CanvasRenderTarget renderTarget = new(CanvasDevice.GetSharedDevice(),
                                               (float)clientRect.Width,
                                               (float)clientRect.Height,
                                               96,
                                               hdr ? DirectXPixelFormat.R16G16B16A16Float : DirectXPixelFormat.B8G8R8A8UIntNormalized,
                                               CanvasAlphaMode.Premultiplied);
        using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
        {
            ICanvasImage output = canvasBitmap;
            if (!hdr)
            {
                WhiteLevelAdjustmentEffect whiteLevelEffect = new()
                {
                    Source = canvasBitmap,
                    InputWhiteLevel = 80,
                    OutputWhiteLevel = (float)colorInfo.SdrWhiteLevelInNits,
                    BufferPrecision = CanvasBufferPrecision.Precision16Float,
                };
                SrgbGammaEffect gammaEffect = new()
                {
                    Source = whiteLevelEffect,
                    GammaMode = SrgbGammaMode.OETF,
                    BufferPrecision = CanvasBufferPrecision.Precision16Float,
                };
                output = gammaEffect;
            }
            ds.Clear(Colors.Transparent);
            ds.DrawImage(output, 0, 0, clientRect);
        }
        string folder = Path.Combine(AppConfig.UserDataFolder!, "Screenshots", runningGame.GameBiz.Game);
        Directory.CreateDirectory(folder);
        string fileName = $"{runningGame.Process.ProcessName}_{DateTimeOffset.Now:yyyyMMdd_HHmmssff}.{(hdr ? "jxr" : "png")}";
        string file = Path.Combine(folder, fileName);
        await renderTarget.SaveAsync(file, hdr ? CanvasBitmapFileFormat.JpegXR : CanvasBitmapFileFormat.Png, 1);
        return (renderTarget, file, maxCLL);
    }




    /// <summary>
    /// 客户区的实际内容区域
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="contentSize"></param>
    /// <param name="clipRect"></param>
    /// <returns></returns>
    public static bool TryClipClient(nint hwnd, SizeInt32 contentSize, out Rect clipRect)
    {
        // todo 窗口超出屏幕范围警告
        clipRect = default;
        if (!(User32.GetClientRect(hwnd, out RECT clientSize) && clientSize is { Width: > 0, Height: > 0 }))
        {
            return false;
        }
        if (clientSize.Width == contentSize.Width && clientSize.Height == contentSize.Height)
        {
            return false;
        }
        if (DwmApi.DwmGetWindowAttribute(hwnd, DwmApi.DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, out RECT windowRect) != HRESULT.S_OK)
        {
            return false;
        }
        POINT clientPoint = default;
        if (!User32.ClientToScreen(hwnd, ref clientPoint))
        {
            return false;
        }
        double left = clipRect.X = clientPoint.x > windowRect.left ? (clientPoint.x - windowRect.left) : 0;
        double top = clipRect.Y = clientPoint.y > windowRect.top ? (clientPoint.y - windowRect.top) : 0;
        clipRect.Width = contentSize.Width > left ? Math.Min(contentSize.Width - left, clientSize.Width) : 1;
        clipRect.Height = contentSize.Height > top ? Math.Min(contentSize.Height - top, clientSize.Height) : 1;
        return clipRect.Right <= contentSize.Width && clipRect.Bottom <= contentSize.Height;
    }




    /// <summary>
    /// 图片最大亮度
    /// </summary>
    /// <param name="canvasBitmap"></param>
    /// <returns></returns>
    private static float GetMaxCLL(CanvasBitmap canvasBitmap)
    {
        float pixelScale = MathF.Min(0.5f, 2048f / MathF.Max(canvasBitmap.SizeInPixels.Width, canvasBitmap.SizeInPixels.Height));
        var scaleEfect = new ScaleEffect
        {
            Source = canvasBitmap,
            Scale = new Vector2(pixelScale, pixelScale),
            BufferPrecision = CanvasBufferPrecision.Precision16Float,
        };
        var colorEffect = new ColorMatrixEffect
        {
            Source = scaleEfect,
            ColorMatrix = new Matrix5x4(
                0.2126f / 125, 0, 0, 0,
                0.7152f / 125, 0, 0, 0,
                0.0722f / 125, 0, 0, 0,
                0, 0, 0, 1,
                0, 0, 0, 0),
        };
        var gammaEffect = new GammaTransferEffect
        {
            Source = colorEffect,
            RedExponent = 0.1f,
            GreenDisable = true,
            BlueDisable = true,
            AlphaDisable = true,
        };
        var histogramEffect = new HistogramEffect
        {
            Source = gammaEffect,
            NumBins = 512,
            ChannelSelect = HistogramEffectChannelSelector.R,
        };
        using CanvasRenderTarget renderTarget = new(CanvasDevice.GetSharedDevice(), 1, 1, 96);
        using var ds = renderTarget.CreateDrawingSession();
        ds.DrawImage(histogramEffect);
        ds.Dispose();
        float[] histogram = new float[512];
        histogramEffect.GetHistogramOutput(histogram);
        int maxBinIndex = 0;
        float cumulative = 0;
        for (int i = histogram.Length - 1; i >= 0; i--)
        {
            cumulative += histogram[i];
            if (cumulative >= 0.0001f)
            {
                maxBinIndex = i;
                break;
            }
        }
        return MathF.Pow((maxBinIndex + 0.5f) / histogram.Length, 10) * 10000;
    }




    private void OnCaptureWindowClosed(object? sender, EventArgs e)
    {
        try
        {
            if (sender is ScreenCaptureContext context)
            {
                captureContexts.Remove(context);
            }
            if (captureContexts.Count == 0)
            {
                _infoWindow?.Close();
                _infoWindow = null;
            }
        }
        catch { }

    }





}
