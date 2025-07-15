using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Display;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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




    [RelayCommand]
    private async Task TestCaptureAsync()
    {
        try
        {
            HMONITOR monitor = User32.MonitorFromWindow(WindowHandle, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);
            using Direct3D11CaptureFrame frame = await ScreenCaptureHelper.CaptureMonitorAsync(monitor.DangerousGetHandle());
            DateTimeOffset frameTime = DateTimeOffset.Now;
            using CanvasBitmap canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(CanvasDevice.GetSharedDevice(), frame.Surface, 96);
            DisplayId displayId = new DisplayId((ulong)monitor.DangerousGetHandle());
            DisplayAdvancedColorInfo colorInfo = DisplayInformation.CreateForDisplayId(displayId).GetAdvancedColorInfo();
            float maxCLL = ScreenCaptureService.GetMaxCLL(canvasBitmap);
            float sdrWhiteLevel = (float)colorInfo.SdrWhiteLevelInNits;
            bool hdr = maxCLL > sdrWhiteLevel;
            Directory.CreateDirectory(Path.Combine(ScreenshotFolder, "Starward"));
            string filePath = Path.Join(ScreenshotFolder, "Starward", $"Starward_{frameTime:yyyyMMdd_HHmmssff}.{(hdr ? "jxr" : "png")}");
            if (hdr)
            {
                await ScreenCaptureService.SaveImageAsync(canvasBitmap, filePath, frameTime);
                if (AutoConvertScreenshotToSDR)
                {
                    using HdrToneMapEffect toneMapEffect = new()
                    {
                        Source = canvasBitmap,
                        InputMaxLuminance = maxCLL,
                        OutputMaxLuminance = sdrWhiteLevel,
                        BufferPrecision = CanvasBufferPrecision.Precision16Float,
                    };
                    using WhiteLevelAdjustmentEffect whiteLevelEffect = new()
                    {
                        Source = toneMapEffect,
                        InputWhiteLevel = 80,
                        OutputWhiteLevel = sdrWhiteLevel,
                        BufferPrecision = CanvasBufferPrecision.Precision16Float,
                    };
                    SrgbGammaEffect gammaEffect = new()
                    {
                        Source = whiteLevelEffect,
                        GammaMode = SrgbGammaMode.OETF,
                        BufferPrecision = CanvasBufferPrecision.Precision16Float,
                    };
                    using CanvasRenderTarget renderTarget = new(CanvasDevice.GetSharedDevice(),
                                                            canvasBitmap.SizeInPixels.Width,
                                                            canvasBitmap.SizeInPixels.Height,
                                                            96,
                                                            DirectXPixelFormat.B8G8R8A8UIntNormalized,
                                                            CanvasAlphaMode.Premultiplied);
                    using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
                    {
                        ds.Clear(Colors.Transparent);
                        ds.DrawImage(gammaEffect);
                    }

                    string sdrPath = Path.ChangeExtension(filePath, ".png");
                    await ScreenCaptureService.SaveImageAsync(renderTarget, sdrPath, frameTime);
                }
            }
            else
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
                CanvasRenderTarget renderTarget = new(CanvasDevice.GetSharedDevice(),
                                                   canvasBitmap.SizeInPixels.Width,
                                                   canvasBitmap.SizeInPixels.Height,
                                                   96,
                                                   DirectXPixelFormat.B8G8R8A8UIntNormalized,
                                                   CanvasAlphaMode.Premultiplied);
                using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
                {
                    ds.Clear(Colors.Transparent);
                    ds.DrawImage(gammaEffect);
                }
                await ScreenCaptureService.SaveImageAsync(renderTarget, filePath, frameTime);
            }
            if (_infoWindow?.AppWindow is null)
            {
                _infoWindow = new ScreenCaptureInfoWindow();
            }
            _infoWindow.CaptureSuccess(displayId, canvasBitmap, filePath, maxCLL);
            TextBlock_CaptureError.Text = string.Empty;
            TextBlock_CaptureError.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture self window");
            TextBlock_CaptureError.Text = ex.Message;
            TextBlock_CaptureError.Visibility = Visibility.Visible;
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
