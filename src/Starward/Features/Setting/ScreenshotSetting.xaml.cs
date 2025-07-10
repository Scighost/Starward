using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using Vanara.PInvoke;


namespace Starward.Features.Setting;

public sealed partial class ScreenshotSetting : PageBase
{

    private readonly ILogger<ScreenshotSetting> _logger = AppConfig.GetLogger<ScreenshotSetting>();


    public nint WindowHandle => XamlRoot.GetWindowHandle();


    public ScreenshotSetting()
    {
        InitializeComponent();
        InitializeHotkeyInput();
    }



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
                        ((HotkeyInput)sender).State = HoykeyInputState.Success;
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



}
