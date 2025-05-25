using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;


namespace Starward.Features.Overlay;

public sealed partial class OverlayWindow : WindowEx
{


    private static bool SystemWin11;


    public OverlayWindow()
    {
        this.InitializeComponent();
        SystemWin11 = Environment.OSVersion.Version.Build >= 22000;
        SystemBackdrop = new TransparentBackdrop();
        InitializeWindow();
        this.Activated += OverlayWindow_Activated;
    }





    private void InitializeWindow()
    {
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
            User32.SetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_STYLE, User32.GetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_STYLE) & ~(nint)User32.WindowStyles.WS_DLGFRAME);
        }
    }



    private void OverlayWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState is WindowActivationState.Deactivated)
        {
            this.Hide();
        }
    }


    private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is Windows.System.VirtualKey.Escape)
        {
            this.Hide();
        }
    }



    private CancellationTokenSource? _windowCancellationTokenSource;



    public void ShowActive(RunningGame runningGame)
    {
        try
        {
            _windowCancellationTokenSource?.Cancel();
            var hwnd = runningGame.WindowHandle;
            User32.GetClientRect(hwnd, out RECT clientRect);
            var point = new POINT();
            User32.ClientToScreen(hwnd, ref point);
            AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(point.X, point.Y, clientRect.Width, clientRect.Height));
            if (SystemWin11)
            {
                // 适配圆角窗口
                User32.GetWindowRect(hwnd, out RECT windowRect);
                if (clientRect.Height == windowRect.Height)
                {
                    RootGrid.CornerRadius = new CornerRadius(0);
                }
                else
                {
                    RootGrid.CornerRadius = new CornerRadius(0, 0, 8, 8);
                }
            }
            else
            {
                RootGrid.CornerRadius = new CornerRadius(0);
            }
            User32.SetForegroundWindow(hwnd);
            base.Show();
            RootGrid.Opacity = 1;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }



    public new async void Hide()
    {
        try
        {
            _windowCancellationTokenSource ??= new CancellationTokenSource();
            CancellationToken token = _windowCancellationTokenSource.Token;
            RootGrid.Opacity = 0;
            await Task.Delay(300);
            if (token.IsCancellationRequested)
            {
                return;
            }
            base.Hide();
        }
        catch { }
        finally
        {
            _windowCancellationTokenSource = null;
        }
    }


    public new async void Close()
    {
        try
        {
            RootGrid.Opacity = 0;
            await Task.Delay(300);
            base.Hide();
            base.Close();
        }
        catch { }
    }



}
