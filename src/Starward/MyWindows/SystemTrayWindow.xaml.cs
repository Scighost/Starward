using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Starward.Helpers;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Vanara.PInvoke;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.MyWindows;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class SystemTrayWindow : WindowEx
{


    public static new SystemTrayWindow Current { get; private set; }



    public SystemTrayWindow()
    {
        Current = this;
        this.InitializeComponent();
        InitializeWindow();
        try
        {
            string icon = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.ico");
            if (File.Exists(icon))
            {
                TaskbarIcon.Icon = new(icon);
            }
        }
        catch { }
    }




    private unsafe void InitializeWindow()
    {
        new SystemBackdropHelper(this, SystemBackdropProperty.AcrylicDefault with
        {
            TintColorLight = 0xFFE7E7E7,
            TintColorDark = 0xFF404040
        }).TrySetAcrylic(true);
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
        var flag = User32.GetWindowLongPtr(WindowHandle, User32.WindowLongFlags.GWL_STYLE);
        flag &= ~(nint)User32.WindowStyles.WS_CAPTION;
        flag &= ~(nint)User32.WindowStyles.WS_BORDER;
        User32.SetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_STYLE, flag);
        var p = DwmApi.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
        DwmApi.DwmSetWindowAttribute(WindowHandle, DwmApi.DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, (nint)(&p), sizeof(DwmApi.DWM_WINDOW_CORNER_PREFERENCE));
        RootGrid.RequestedTheme = ShouldSystemUseDarkMode() ? ElementTheme.Dark : ElementTheme.Light;
        Show();
        Hide();
    }



    private void SystemTrayWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState is WindowActivationState.Deactivated)
        {
            Hide();
        }
    }



    [RelayCommand]
    public override void Show()
    {
        RootGrid.RequestedTheme = ShouldSystemUseDarkMode() ? ElementTheme.Dark : ElementTheme.Light;
        tray.UpdateContent();
        tray.UpdateLayout();
        User32.GetCursorPos(out POINT point);
        SIZE windowSize = new()
        {
            Width = (int)(tray.ActualWidth * UIScale),
            Height = (int)(tray.ActualHeight * UIScale)
        };
        User32.CalculatePopupWindowPosition(point, windowSize, User32.TrackPopupMenuFlags.TPM_RIGHTALIGN | User32.TrackPopupMenuFlags.TPM_BOTTOMALIGN | User32.TrackPopupMenuFlags.TPM_WORKAREA, null, out RECT windowPos);
        User32.MoveWindow(WindowHandle, windowPos.X, windowPos.Y, windowPos.Width, windowPos.Height, true);
        base.Show();
    }



    [RelayCommand]
    public override void Hide()
    {
        try
        {
            base.Hide();
            AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1000, 1000));
        }
        catch { }
    }



    [return: MarshalAs(UnmanagedType.Bool)]
    [LibraryImport("uxtheme.dll", EntryPoint = "#138", SetLastError = true)]
    private static partial bool ShouldSystemUseDarkMode();



    [RelayCommand]
    public void ShowMainWindow()
    {
        App.Current.EnsureMainWindow();
    }


    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        Current = null!;
        TaskbarIcon?.Dispose();
    }


}
