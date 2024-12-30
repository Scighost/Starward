using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Starward.Features.Setting;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.IO;
using Vanara.PInvoke;
using Windows.Foundation;


namespace Starward.Features.ViewHost;

[INotifyPropertyChanged]
public sealed partial class SystemTrayWindow : WindowEx
{




    public SystemTrayWindow()
    {
        this.InitializeComponent();
        InitializeWindow();
        SetTrayIcon();
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, _) => this.Bindings.Update());
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

        Show();
        Hide();
    }



    private void SetTrayIcon()
    {
        try
        {
            string icon = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.ico");
            if (File.Exists(icon))
            {
                trayIcon.Icon = new(icon);
            }
        }
        catch { }
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
        RootGrid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        SIZE windowSize = new()
        {
            Width = (int)(RootGrid.DesiredSize.Width * UIScale),
            Height = (int)(RootGrid.DesiredSize.Height * UIScale)
        };
        User32.GetCursorPos(out POINT point);
        User32.CalculatePopupWindowPosition(point, windowSize, User32.TrackPopupMenuFlags.TPM_RIGHTALIGN | User32.TrackPopupMenuFlags.TPM_BOTTOMALIGN | User32.TrackPopupMenuFlags.TPM_WORKAREA, null, out RECT windowPos);
        User32.MoveWindow(WindowHandle, windowPos.X, windowPos.Y, windowPos.Width, windowPos.Height, true);
        base.Show();
    }



    [RelayCommand]
    public override void Hide()
    {
        base.Hide();
    }



    [RelayCommand]
    public void ShowMainWindow()
    {
        App.Current.EnsureMainWindow();
    }


    [RelayCommand]
    private void Exit()
    {
        App.Current.Exit();
    }


    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        trayIcon?.Dispose();
    }


}
