using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Helpers;
using Starward.Pages.SystemTray;
using System;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class SystemTrayWindow : Window, IDisposable
{


    private readonly IntPtr HWND;


    private double Scale => User32.GetDpiForWindow(HWND) / 96d;


    public SystemTrayWindow()
    {
        this.InitializeComponent();
        HWND = WindowNative.GetWindowHandle(this);
        InitializeWindow();
    }


    public SystemTrayWindow(Page page)
    {
        this.InitializeComponent();
        HWND = WindowNative.GetWindowHandle(this);
        InitializeWindow();
        frame.Content = page;
    }



    private unsafe void InitializeWindow()
    {
        new SystemBackdropHelper(this, SystemBackdropProperty.AcrylicDefault with { TintColorLight = 0xFFE7E7E7, TintColorDark = 0xFF404040 }).TrySetAcrylic(true);
        AppWindow.IsShownInSwitchers = false;
        AppWindow.Closing += (s, e) => e.Cancel = true;
        this.Activated += SystemTrayWindow_Activated;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.IsAlwaysOnTop = true;
        }
        var flag = User32.GetWindowLongPtr(HWND, User32.WindowLongFlags.GWL_STYLE);
        flag &= ~(nint)User32.WindowStyles.WS_CAPTION;
        flag &= ~(nint)User32.WindowStyles.WS_BORDER;
        User32.SetWindowLong(HWND, User32.WindowLongFlags.GWL_STYLE, flag);
        var p = DwmApi.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
        DwmApi.DwmSetWindowAttribute(HWND, DwmApi.DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, (nint)(&p), sizeof(DwmApi.DWM_WINDOW_CORNER_PREFERENCE));
        RootGrid.RequestedTheme = ShouldSystemUseDarkMode() ? ElementTheme.Dark : ElementTheme.Light;
        User32.ShowWindow(HWND, ShowWindowCommand.SW_HIDE);
    }



    private void SystemTrayWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState is WindowActivationState.Deactivated)
        {
            Hide();
        }
    }



    [RelayCommand]
    public void Show()
    {
        RootGrid.RequestedTheme = ShouldSystemUseDarkMode() ? ElementTheme.Dark : ElementTheme.Light;
        User32.GetCursorPos(out POINT point);
        SIZE windowSize = new(400, 600);
        if (frame.Content is MainMenuSystemTrayPage page1)
        {
            page1.UpdateContent();
            windowSize.Width = (int)(page1.ContentGrid.ActualWidth * Scale);
            windowSize.Height = (int)(page1.ContentGrid.ActualHeight * Scale);
        }
        if (frame.Content is InstallGameSystemTrayPage page2)
        {
            windowSize.Width = (int)(page2.ContentGrid.ActualWidth * Scale);
            windowSize.Height = (int)(page2.ContentGrid.ActualHeight * Scale);
        }
        User32.CalculatePopupWindowPosition(point, windowSize, User32.TrackPopupMenuFlags.TPM_RIGHTALIGN | User32.TrackPopupMenuFlags.TPM_BOTTOMALIGN | User32.TrackPopupMenuFlags.TPM_WORKAREA, null, out RECT windowPos);
        User32.MoveWindow(HWND, windowPos.X, windowPos.Y, windowPos.Width, windowPos.Height, true);
        User32.ShowWindow(HWND, ShowWindowCommand.SW_SHOWNORMAL);
        User32.SetForegroundWindow(HWND);
    }



    [RelayCommand]
    public void Hide()
    {
        User32.ShowWindow(HWND, ShowWindowCommand.SW_HIDE);
        AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1000, 1000));
    }



    [DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#138")]
    private static extern bool ShouldSystemUseDarkMode();



    private bool disposedValue;


    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {

            }
            this.Hide();
            this.Close();
            disposedValue = true;
        }
    }

    ~SystemTrayWindow()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

}
