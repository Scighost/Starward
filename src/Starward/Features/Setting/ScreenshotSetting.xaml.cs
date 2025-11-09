using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Display;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Codec.ICC;
using Starward.Features.Codec;
using Starward.Features.Screenshot;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.System;


namespace Starward.Features.Setting;

public sealed partial class ScreenshotSetting : PageBase
{

    private readonly ILogger<ScreenshotSetting> _logger = AppConfig.GetLogger<ScreenshotSetting>();


    public nint WindowHandle => XamlRoot.GetWindowHandle();


    public ScreenshotSetting()
    {
        InitializeComponent();
        InitializeHotkeyInput();
        InitializeScreenshotFolder();
    }



    private ScreenCaptureInfoWindow? _infoWindow;


    protected override void OnUnloaded()
    {
        if (_infoWindow?.AppWindow is not null)
        {
            _infoWindow.Close();
        }
    }



    #region Hotkey


    private void InitializeHotkeyInput()
    {
        try
        {
            HotkeyManager.InitializeHotkeyInput(HotkeyInput_ScreenshotCapture);
        }
        catch { }
    }



    private void HotkeyInput_HotkeyEditing(object sender, HotkeyInputEventArg e)
    {
        try
        {
            HotkeyManager.UnregisterHotkey(e.WindowHandle, e.HotkeyId);
        }
        catch { }
    }


    private void HotkeyInput_HotkeyDeleted(object sender, HotkeyInputEventArg e)
    {
        try
        {
            this.XamlRoot.Content.Focus(FocusState.Programmatic);
            HotkeyManager.DeleteHotkey(e.WindowHandle, e.HotkeyId);
        }
        catch { }
    }



    private void HotkeyInput_HotkeyEditFinished(object sender, HotkeyEditFinishedEventArg e)
    {
        try
        {
            this.XamlRoot.Content.Focus(FocusState.Programmatic);
            if (e.HotkeyAvaliable)
            {
                Win32Error error = HotkeyManager.RegisterHotkey(e.WindowHandle, e.HotkeyId, (User32.HotKeyModifiers)e.fsModifiers, (User32.VK)e.Key);
                if (error.Succeeded && e.HotkeyChanged)
                {
                    ((HotkeyInput)sender).State = HoykeyInputState.Success;
                }
                else
                {
                    if (e.HotkeyChanged)
                    {
                        string? hotkey = HotkeyInput.GetHotkeyText(e.fsModifiers, (uint)e.Key);
                        if (error == Win32Error.ERROR_HOTKEY_ALREADY_REGISTERED)
                        {
                            InAppToast.MainWindow?.Warning(null, string.Format(Lang.HotkeyManager_TheShortcutKeys0IsAlreadyInUse, hotkey), 5000);
                        }
                        else
                        {
                            InAppToast.MainWindow?.Warning(null, string.Format(Lang.HotkeyManager_FailedToRegisterTheShortcutKeys0, hotkey), 5000);
                        }
                        ((HotkeyInput)sender).State = HoykeyInputState.Warning;
                    }
                    else
                    {
                        var info = HotkeyManager.GetHotkeyInfo(e.HotkeyId);
                        if (info?.Error.Failed ?? false)
                        {
                            ((HotkeyInput)sender).State = HoykeyInputState.Warning;
                        }
                    }

                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hotkey edit finished");
            InAppToast.MainWindow?.Error(ex, Lang.HotkeySetting_FailedToRegisterTheShortcutKeys);
        }
    }


    #endregion



    #region Screenshot Folder


    public string ScreenshotFolder { get; set => SetProperty(ref field, value); }


    private void InitializeScreenshotFolder()
    {
        try
        {
            string? folder = AppConfig.ScreenshotFolder;
            if (Directory.Exists(folder))
            {
                ScreenshotFolder = folder;
            }
            else
            {
                ScreenshotFolder = Path.Join(AppConfig.UserDataFolder, "Screenshots");
                Directory.CreateDirectory(ScreenshotFolder);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize screenshot folder");
        }
    }


    [RelayCommand]
    private async Task ChangeScreenshotFolder()
    {
        try
        {
            string? folder = await FileDialogHelper.PickFolderAsync(this.XamlRoot);
            if (Directory.Exists(folder))
            {
                ScreenshotFolder = folder;
                AppConfig.ScreenshotFolder = folder;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change screenshot folder");
        }
    }


    [RelayCommand]
    private async Task OpenScreenshotFolder()
    {
        try
        {
            if (Directory.Exists(ScreenshotFolder))
            {
                await Launcher.LaunchFolderPathAsync(ScreenshotFolder);
            }
        }
        catch { }
    }


    #endregion




    public int ScreenshotForamt
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(ScreenshotQualityVisibility));
                AppConfig.ScreenCaptureSavedFormat = value;
            }
        }
    } = AppConfig.ScreenCaptureSavedFormat;



    public int ScreenshotQuality
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                AppConfig.ScreenCaptureEncodeQuality = value;
            }
        }
    } = AppConfig.ScreenCaptureEncodeQuality;



    public Visibility ScreenshotQualityVisibility => ScreenshotForamt > 0 ? Visibility.Visible : Visibility.Collapsed;



    public bool AutoConvertScreenshotToSDR
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                AppConfig.AutoConvertScreenshotToSDR = value;
            }
        }
    } = AppConfig.AutoConvertScreenshotToSDR;



    public bool AutoCopyScreenshotToClipboard
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                AppConfig.AutoCopyScreenshotToClipboard = value;
            }
        }
    } = AppConfig.AutoCopyScreenshotToClipboard;



    [RelayCommand]
    private async Task TestCaptureAsync()
    {
        try
        {
            HMONITOR monitor = User32.MonitorFromWindow(WindowHandle, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);
            DisplayId displayId = new((ulong)monitor.DangerousGetHandle());
            using DisplayInformation displayInfo = DisplayInformation.CreateForDisplayId(displayId);
            DisplayAdvancedColorInfo colorInfo = displayInfo.GetAdvancedColorInfo();
            DirectXPixelFormat pixelFormat = colorInfo.CurrentAdvancedColorKind is DisplayAdvancedColorKind.HighDynamicRange ? DirectXPixelFormat.R16G16B16A16Float : DirectXPixelFormat.R8G8B8A8UIntNormalized;
            using Direct3D11CaptureFrame frame = await ScreenCaptureHelper.CaptureMonitorAsync(monitor.DangerousGetHandle(), pixelFormat);
            DateTimeOffset frameTime = DateTimeOffset.Now;
            using CanvasBitmap canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(CanvasDevice.GetSharedDevice(), frame.Surface, 96);

            if (_infoWindow?.AppWindow is null)
            {
                _infoWindow = new ScreenCaptureInfoWindow();
            }

            float maxCLL = -1;
            float sdrWhiteLevel = 80;
            bool hdr = false;
            if (canvasBitmap.Format is DirectXPixelFormat.R16G16B16A16Float)
            {
                maxCLL = ScreenCaptureService.GetMaxCLL(canvasBitmap);
                sdrWhiteLevel = (float)colorInfo.SdrWhiteLevelInNits;
                hdr = maxCLL > sdrWhiteLevel + 5;
            }
            int quality = ScreenshotQuality switch
            {
                0 => 80,
                1 => 90,
                2 => 100,
                _ => 90,
            };
            float distance = ScreenshotQuality switch
            {
                0 => 2,
                1 => 1,
                2 => 0,
                _ => 1,
            };

            byte[] xmpData = ScreenCaptureService.BuildXMPMetadata(frameTime);
            ColorPrimaries colorPrimaries = await ScreenCaptureService.GetColorPrimariesFromDisplayInformationAsync(displayInfo);
            Directory.CreateDirectory(Path.Combine(ScreenshotFolder, "Starward"));
            CanvasRenderTarget? renderTarget = null;
            string extension = ScreenshotForamt switch
            {
                1 => "avif",
                2 => "jxl",
                _ => "png",
            };

            string filePath;
            if (hdr)
            {
                _infoWindow.CaptureStart(displayId, canvasBitmap, maxCLL);
                colorPrimaries = ColorPrimaries.BT2020;
                extension = ScreenshotForamt switch
                {
                    2 => "jxl",
                    _ => "avif",
                };
            }
            else if (canvasBitmap.Format is DirectXPixelFormat.R16G16B16A16Float)
            {
                using WhiteLevelAdjustmentEffect whiteLevelEffect = new()
                {
                    Source = canvasBitmap,
                    InputWhiteLevel = 80,
                    OutputWhiteLevel = sdrWhiteLevel,
                    BufferPrecision = CanvasBufferPrecision.Precision16Float,
                };
                using SrgbGammaEffect gammaEffect = new()
                {
                    Source = whiteLevelEffect,
                    GammaMode = SrgbGammaMode.OETF,
                    BufferPrecision = CanvasBufferPrecision.Precision16Float,
                };
                renderTarget = new(CanvasDevice.GetSharedDevice(),
                                   canvasBitmap.SizeInPixels.Width,
                                   canvasBitmap.SizeInPixels.Height,
                                   96,
                                   DirectXPixelFormat.R8G8B8A8UIntNormalized,
                                   CanvasAlphaMode.Premultiplied);
                using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
                {
                    ds.DrawImage(gammaEffect);
                }
                _infoWindow.CaptureStart(displayId, renderTarget, maxCLL);
            }
            else if (canvasBitmap.Format is DirectXPixelFormat.R8G8B8A8UIntNormalized)
            {
                _infoWindow.CaptureStart(displayId, canvasBitmap, maxCLL);
            }
            else
            {
                throw new NotSupportedException($"Unsupported pixel format: {canvasBitmap.Format}");
            }

            filePath = Path.Join(ScreenshotFolder, "Starward", $"Starward_{frameTime:yyyyMMdd_HHmmssff}.{extension}");
            using MemoryStream ms = new();
            if (extension is "png")
            {
                await ImageSaver.SaveAsPngAsync(canvasBitmap, ms, colorPrimaries, xmpData);
            }
            else if (extension is "avif")
            {
                await ImageSaver.SaveAsAvifAsync(canvasBitmap, ms, colorPrimaries, quality, xmpData);
            }
            else if (extension is "jxl")
            {
                await ImageSaver.SaveAsJxlAsync(canvasBitmap, ms, colorPrimaries, distance, xmpData);
            }
            else
            {
                throw new NotSupportedException($"Unsupported image format: {extension}");
            }

            using var fs = File.Create(filePath);
            ms.Seek(0, SeekOrigin.Begin);
            await ms.CopyToAsync(fs);
            await ScreenCaptureService.CopyToClipboardAsync(filePath);
            if (hdr && AutoConvertScreenshotToSDR)
            {
                using var ms2 = new MemoryStream();
                await ImageSaver.SaveAsUhdrAsync(canvasBitmap, ms2, maxCLL, sdrWhiteLevel);
                ms2.Position = 0;
                string sdrPath = Path.ChangeExtension(filePath, ".jpg");
                using var fs2 = File.Create(sdrPath);
                await ms2.CopyToAsync(fs2);
                await ScreenCaptureService.CopyToClipboardAsync(sdrPath);
            }

            _infoWindow.CaptureSuccess(displayId, canvasBitmap, filePath, maxCLL);
            TextBlock_CaptureError.Text = string.Empty;
            TextBlock_CaptureError.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture self window");
            TextBlock_CaptureError.Text = $"{Lang.ScreenCaptureInfoWindow_ScreenshotFailed}: {ex.Message}";
            TextBlock_CaptureError.Visibility = Visibility.Visible;
        }
    }



    [RelayCommand]
    private void ClearThumbnailCache()
    {
        try
        {
            ImageThumbnail.ClearThumbnailCache();
            InAppToast.MainWindow?.Success(Lang.ScreenshotSetting_ClearSuccessfully);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear thumbnail cache");
            InAppToast.MainWindow?.Error(ex, Lang.ScreenshotSetting_ClearFailed);
        }
    }


    private void TextBlock_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        if (sender.FontSize > 12)
        {
            sender.FontSize -= 1;
        }
    }


}
