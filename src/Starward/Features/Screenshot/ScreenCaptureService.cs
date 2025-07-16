using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Display;
using Microsoft.UI;
using Starward.Core;
using Starward.Features.GameSetting;
using Starward.Features.Overlay;
using Starward.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using WicNet;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Storage;

namespace Starward.Features.Screenshot;

internal class ScreenCaptureService
{


    private static ScreenCaptureService? _instance;
    private static ScreenCaptureService Instance => _instance ??= AppConfig.GetService<ScreenCaptureService>();

    private readonly ILogger<ScreenCaptureService> _logger;

    private ScreenCaptureInfoWindow? _infoWindow;

    private ConcurrentDictionary<nint, ScreenCaptureContext> _captureContexts = new();



    public ScreenCaptureService(ILogger<ScreenCaptureService> logger)
    {
        _logger = logger;
    }



    public static void Capture()
    {
        Instance.CaptureInternal();
    }



    /// <summary>
    /// 获取窗口所在桌面的高级色彩信息
    /// </summary>
    /// <param name="hwnd"></param>
    /// <returns></returns>
    private static DisplayAdvancedColorInfo GetAdvancedColorInfoFromWindowHandle(nint hwnd)
    {
        Microsoft.UI.DisplayId displayId = Win32Interop.GetDisplayIdFromMonitor(User32.MonitorFromWindow(hwnd, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST).DangerousGetHandle());
        DisplayInformation displayInformation = DisplayInformation.CreateForDisplayId(displayId);
        return displayInformation.GetAdvancedColorInfo();
    }


    private async void CaptureInternal()
    {
        RunningGame? runningGame = RunningGameService.GetLatestActiveGame();
        if (runningGame is null)
        {
            return;
        }
        if (User32.IsIconic(runningGame.WindowHandle))
        {
            _logger.LogWarning("Cannot capture a minimized window.");
            return;
        }
        try
        {
            (CanvasBitmap? renderTarget, string? file, float maxCLL, float sdrWhiteLevel, DateTimeOffset frameTime) = await CaptureAndSaveAsync(runningGame);
            using (renderTarget)
            {
                await CopyToClipboardAsync(file);
                if (_infoWindow?.AppWindow is null)
                {
                    _infoWindow = new ScreenCaptureInfoWindow();
                }
                _infoWindow.CaptureSuccess(runningGame.WindowHandle, renderTarget, file, maxCLL);
                if (maxCLL > sdrWhiteLevel && AppConfig.AutoConvertScreenshotToSDR)
                {
                    string? sdrFilePath = await SaveAsSdrAsync(renderTarget, file, runningGame, maxCLL, sdrWhiteLevel, frameTime).ConfigureAwait(false);
                    await CopyToClipboardAsync(sdrFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while capturing the screen.");
            if (_infoWindow?.AppWindow is null)
            {
                _infoWindow = new ScreenCaptureInfoWindow();
            }
            _infoWindow.CaptureError(runningGame.WindowHandle);
        }
    }



    /// <summary>
    /// 截图并保存
    /// </summary>
    /// <param name="runningGame"></param>
    /// <returns></returns>
    private async Task<(CanvasBitmap CanvasBitmap, string FilePath, float MaxCLL, float SdrWhiteLevel, DateTimeOffset FrameTime)> CaptureAndSaveAsync(RunningGame runningGame)
    {
        if (!_captureContexts.TryGetValue(runningGame.WindowHandle, out ScreenCaptureContext? context))
        {
            context = new ScreenCaptureContext(runningGame.WindowHandle);
            context.CaptureWindowClosed += OnCaptureWindowClosed;
            _captureContexts.TryAdd(runningGame.WindowHandle, context);
        }
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var frame = await context.CaptureAsync(cts.Token).ConfigureAwait(false);
        DateTimeOffset frameTime = DateTimeOffset.Now;
        (CanvasRenderTarget renderTarget, float maxCLL, float sdrWhiteLevel) = await ProceedImageAsync(frame, runningGame).ConfigureAwait(false);
        string filePath = await SaveImageAsync(renderTarget, runningGame, frameTime).ConfigureAwait(false);
        return (renderTarget, filePath, maxCLL, sdrWhiteLevel, frameTime);
    }


    /// <summary>
    /// 处理图片
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="runningGame"></param>
    /// <returns></returns>
    private static async Task<(CanvasRenderTarget CanvasRenderTarget, float MaxCLL, float SdrWhiteLevel)> ProceedImageAsync(Direct3D11CaptureFrame frame, RunningGame runningGame)
    {
        return await Task.Run(() =>
        {
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
            using CanvasDrawingSession ds = renderTarget.CreateDrawingSession();
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
            return (renderTarget, maxCLL, (float)colorInfo.SdrWhiteLevelInNits);
        }).ConfigureAwait(false);
    }


    /// <summary>
    /// 保存为 SDR 图像
    /// </summary>
    /// <param name="canvasImage"></param>
    /// <param name="filePath"></param>
    /// <param name="runningGame"></param>
    /// <param name="maxCLL"></param>
    /// <param name="sdrWhiteLevel"></param>
    /// <param name="frameTime"></param>
    /// <returns></returns>
    private static async Task<string?> SaveAsSdrAsync(CanvasBitmap canvasImage, string filePath, RunningGame runningGame, float maxCLL, float sdrWhiteLevel, DateTimeOffset frameTime)
    {
        if (canvasImage.Format is DirectXPixelFormat.R16G16B16A16Float)
        {
            await Task.Delay(1).ConfigureAwait(false);
            float outputMaxLuminance = sdrWhiteLevel;
            if (runningGame.GameBiz.Game is GameBiz.hk4e)
            {
                (_, outputMaxLuminance, _) = GameSettingService.GetGenshinHDRLuminance(runningGame.GameBiz);
            }
            using HdrToneMapEffect toneMapEffect = new()
            {
                Source = canvasImage,
                InputMaxLuminance = maxCLL,
                OutputMaxLuminance = outputMaxLuminance,
                BufferPrecision = CanvasBufferPrecision.Precision16Float,
            };
            using WhiteLevelAdjustmentEffect whiteLevelEffect = new()
            {
                Source = toneMapEffect,
                InputWhiteLevel = 80,
                OutputWhiteLevel = outputMaxLuminance,
                BufferPrecision = CanvasBufferPrecision.Precision16Float,
            };
            SrgbGammaEffect gammaEffect = new()
            {
                Source = whiteLevelEffect,
                GammaMode = SrgbGammaMode.OETF,
                BufferPrecision = CanvasBufferPrecision.Precision16Float,
            };
            using CanvasRenderTarget renderTarget = new(CanvasDevice.GetSharedDevice(),
                                                    canvasImage.SizeInPixels.Width,
                                                    canvasImage.SizeInPixels.Height,
                                                    96,
                                                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                                                    CanvasAlphaMode.Premultiplied);
            using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
            {
                ds.Clear(Colors.Transparent);
                ds.DrawImage(gammaEffect);
            }

            filePath = Path.ChangeExtension(filePath, ".png");
            await SaveImageAsync(renderTarget, filePath, frameTime).ConfigureAwait(false);
            return filePath;
        }
        return null;
    }


    /// <summary>
    /// 保存为文件
    /// </summary>
    /// <param name="renderTarget"></param>
    /// <param name="runningGame"></param>
    /// <param name="frameTime"></param>
    /// <returns></returns>
    private static async Task<string> SaveImageAsync(CanvasRenderTarget renderTarget, RunningGame runningGame, DateTimeOffset frameTime)
    {
        return await Task.Run(async () =>
        {
            string screenshotFolder;
            string? targetFolder = AppConfig.ScreenshotFolder;
            if (Directory.Exists(targetFolder))
            {
                screenshotFolder = Path.GetFullPath(Path.Join(targetFolder, runningGame.Process.ProcessName));
            }
            else
            {
                screenshotFolder = Path.GetFullPath(Path.Join(AppConfig.UserDataFolder, "Screenshots", runningGame.Process.ProcessName));
            }
            Directory.CreateDirectory(screenshotFolder);

            bool hdr = renderTarget.Format is not DirectXPixelFormat.B8G8R8A8UIntNormalized;
            string fileName = $"{runningGame.Process.ProcessName}_{frameTime:yyyyMMdd_HHmmssff}.{(hdr ? "jxr" : "png")}";
            string filePath = Path.Combine(screenshotFolder, fileName);
            await SaveImageAsync(renderTarget, filePath, frameTime).ConfigureAwait(false);
            return filePath;
        });
    }


    /// <summary>
    /// 保存图片
    /// </summary>
    /// <param name="canvasBitmap"></param>
    /// <param name="filePath"></param>
    /// <param name="frameTime"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static async Task SaveImageAsync(CanvasBitmap canvasBitmap, string filePath, DateTimeOffset frameTime)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Guid containerGuid = canvasBitmap.Format is DirectXPixelFormat.B8G8R8A8UIntNormalized ? WicCodec.GUID_ContainerFormatPng : WicCodec.GUID_ContainerFormatWmp;
        Guid pixelFormatGuid = canvasBitmap.Format switch
        {
            DirectXPixelFormat.B8G8R8A8UIntNormalized => WicPixelFormat.GUID_WICPixelFormat32bppBGRA,
            DirectXPixelFormat.R16G16B16A16Float => WicPixelFormat.GUID_WICPixelFormat64bppRGBAHalf,
            DirectXPixelFormat.R32G32B32A32Float => WicPixelFormat.GUID_WICPixelFormat128bppRGBAFloat,
            _ => throw new NotSupportedException($"Unsupported pixel format: {canvasBitmap.Format}"),
        };

        byte[] pixelBytes = canvasBitmap.GetPixelBytes();
        int width = (int)canvasBitmap.SizeInPixels.Width;
        int height = (int)canvasBitmap.SizeInPixels.Height;
        using WicBitmapSource wicBitmapSource = WicBitmapSource.FromMemory(width, height, pixelFormatGuid, pixelBytes.Length / height, pixelBytes);
        var metaList = new List<WicMetadataKeyValue>
        {
           new(new WicMetadataKey(WicCodec.CLSID_WICXMPMetadataWriter, "System.ApplicationName"), "Starward Launcher", DirectN.PropertyType.VT_LPWSTR),
           new(new WicMetadataKey(WicCodec.CLSID_WICXMPMetadataWriter, "System.Photo.DateTaken"), frameTime, DirectN.PropertyType.VT_FILETIME),
        };

        using var ms = new MemoryStream();
        wicBitmapSource.Save(ms, containerGuid, encoderOptions: new Dictionary<string, object> { ["Lossless"] = true }, metadata: metaList);
        using var fs = File.Create(filePath);
        ms.Seek(0, SeekOrigin.Begin);
        await ms.CopyToAsync(fs).ConfigureAwait(false);
    }


    public static async Task CopyToClipboardAsync(string? filePath)
    {
        // 0x800401D0
        const int CLIPBRD_E_CANT_OPEN = -2147221040;
        if (AppConfig.AutoCopyScreenshotToClipboard)
        {
            if (File.Exists(filePath))
            {
                var file = await StorageFile.GetFileFromPathAsync(filePath);
                try
                {
                    ClipboardHelper.SetStorageItems(DataPackageOperation.Copy, file);
                }
                catch (COMException ex) when (ex.HResult == CLIPBRD_E_CANT_OPEN)
                {
                    ClipboardHelper.SetStorageItems(DataPackageOperation.Copy, file);
                }
            }
        }
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
    public static float GetMaxCLL(CanvasBitmap canvasBitmap)
    {
        float pixelScale = MathF.Min(0.5f, 2048f / MathF.Max(canvasBitmap.SizeInPixels.Width, canvasBitmap.SizeInPixels.Height));
        using var scaleEfect = new ScaleEffect
        {
            Source = canvasBitmap,
            Scale = new Vector2(pixelScale, pixelScale),
            BufferPrecision = CanvasBufferPrecision.Precision16Float,
        };
        using var colorEffect = new ColorMatrixEffect
        {
            Source = scaleEfect,
            ColorMatrix = new Matrix5x4(
                0.2126f / 125, 0, 0, 0,
                0.7152f / 125, 0, 0, 0,
                0.0722f / 125, 0, 0, 0,
                0, 0, 0, 1,
                0, 0, 0, 0),
            BufferPrecision = CanvasBufferPrecision.Precision16Float,
        };
        using var gammaEffect = new GammaTransferEffect
        {
            Source = colorEffect,
            RedExponent = 0.1f,
            GreenDisable = true,
            BlueDisable = true,
            AlphaDisable = true,
            BufferPrecision = CanvasBufferPrecision.Precision16Float,
        };
        using var histogramEffect = new HistogramEffect
        {
            Source = gammaEffect,
            NumBins = 512,
            ChannelSelect = HistogramEffectChannelSelector.R,
            BufferPrecision = CanvasBufferPrecision.Precision16Float,
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
                _captureContexts.TryRemove(context.WindowHandle, out _);
            }
            if (_captureContexts.Count == 0)
            {
                _infoWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    _infoWindow?.Close();
                    _infoWindow = null;
                });
            }
        }
        catch { }
    }


}
