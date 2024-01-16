using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Starward.Messages;
using System;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.UI;

namespace Starward.MyWindows;

public abstract class WindowEx : Window
{


    public IntPtr WindowHandle { get; private init; }

    public IntPtr BridgeHandle { get; private init; }


    public double UIScale => User32.GetDpiForWindow(WindowHandle) / 96d;




    public WindowEx()
    {
        WindowHandle = (IntPtr)AppWindow.Id.Value;
        BridgeHandle = (IntPtr)User32.FindWindowEx(WindowHandle, IntPtr.Zero, "Microsoft.UI.Content.DesktopChildSiteBridge", null);
        windowSubclassProc = new(WindowSubclassProc);
        bridgeSubclassProc = new(BridgeSubclassProc);
        ComCtl32.SetWindowSubclass(WindowHandle, windowSubclassProc, 1001, IntPtr.Zero);
        ComCtl32.SetWindowSubclass(BridgeHandle, bridgeSubclassProc, 1002, IntPtr.Zero);
    }




    #region Message Loop



    private readonly ComCtl32.SUBCLASSPROC windowSubclassProc;

    private readonly ComCtl32.SUBCLASSPROC bridgeSubclassProc;



    public unsafe virtual IntPtr WindowSubclassProc(HWND hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, nuint uIdSubclass, IntPtr dwRefData)
    {
        return ComCtl32.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }



    public unsafe virtual IntPtr BridgeSubclassProc(HWND hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, nuint uIdSubclass, IntPtr dwRefData)
    {
        return ComCtl32.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }




    #endregion




    #region Window Method



    public virtual void Show()
    {
        AppWindow.Show(true);
        AppWindow.MoveInZOrderAtTop();
        User32.SetForegroundWindow(WindowHandle);
        WeakReferenceMessenger.Default.Send(new WindowStateChangedMessage(false));
    }



    public virtual void Hide()
    {
        AppWindow.Hide();
        WeakReferenceMessenger.Default.Send(new WindowStateChangedMessage(true));
    }



    public virtual void Minimize()
    {
        User32.ShowWindow(WindowHandle, ShowWindowCommand.SW_MINIMIZE);
    }



    public virtual void CenterInScreen(int? width = null, int? height = null)
    {
        width = width <= 0 ? null : width;
        height = height <= 0 ? null : height;
        DisplayArea display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        double scale = UIScale;
        int w = (int)((width * scale) ?? AppWindow.Size.Width);
        int h = (int)((height * scale) ?? AppWindow.Size.Height);
        int x = display.WorkArea.X + (display.WorkArea.Width - w) / 2;
        int y = display.WorkArea.Y + (display.WorkArea.Height - h) / 2;
        AppWindow.MoveAndResize(new RectInt32(x, y, w, h));
    }




    public bool IsTopMost
    {
        get
        {
            User32.WindowStylesEx flags = (User32.WindowStylesEx)User32.GetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_EXSTYLE);
            return flags.HasFlag(User32.WindowStylesEx.WS_EX_TOPMOST);
        }
        set
        {
            User32.WindowStylesEx flags = (User32.WindowStylesEx)User32.GetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_EXSTYLE);
            if (value)
            {
                flags |= User32.WindowStylesEx.WS_EX_TOPMOST;
            }
            else
            {
                flags &= ~User32.WindowStylesEx.WS_EX_TOPMOST;
            }
            User32.SetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_EXSTYLE, (nint)flags);
        }
    }




    #endregion




    #region Title Bar



    public void AdaptTitleBarButtonColorToActuallTheme()
    {
        if (AppWindowTitleBar.IsCustomizationSupported() && AppWindow.TitleBar.ExtendsContentIntoTitleBar == true)
        {
            var titleBar = AppWindow.TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            if (Content is FrameworkElement element)
            {
                switch (element.ActualTheme)
                {
                    case ElementTheme.Default:
                        break;
                    case ElementTheme.Light:
                        titleBar.ButtonForegroundColor = Colors.Black;
                        titleBar.ButtonHoverForegroundColor = Colors.Black;
                        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x20, 0x00, 0x00, 0x00);
                        titleBar.ButtonInactiveForegroundColor = Color.FromArgb(0xFF, 0x99, 0x99, 0x99);
                        break;
                    case ElementTheme.Dark:
                        titleBar.ButtonForegroundColor = Colors.White;
                        titleBar.ButtonHoverForegroundColor = Colors.White;
                        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF);
                        titleBar.ButtonInactiveForegroundColor = Color.FromArgb(0xFF, 0x99, 0x99, 0x99);
                        break;
                    default:
                        break;
                }
            }
        }
    }



    public void SetDragRectangles(params RectInt32[] value)
    {
        if (AppWindowTitleBar.IsCustomizationSupported() && AppWindow.TitleBar.ExtendsContentIntoTitleBar == true)
        {
            AppWindow.TitleBar.SetDragRectangles(value);
        }
    }



    #endregion




    #region Accent Color


    public virtual void ChangeAccentColor(Color? backColor = null, Color? foreColor = null)
    {
        if (Content is FrameworkElement element)
        {
            App.ChangeAccentColor(element.ActualTheme, backColor, foreColor);
            if (element.ActualTheme is ElementTheme.Dark)
            {
                element.RequestedTheme = ElementTheme.Light;
                element.RequestedTheme = ElementTheme.Default;
            }
            if (element.ActualTheme is ElementTheme.Light)
            {
                element.RequestedTheme = ElementTheme.Dark;
                element.RequestedTheme = ElementTheme.Default;
            }
        }
    }


    #endregion


}
