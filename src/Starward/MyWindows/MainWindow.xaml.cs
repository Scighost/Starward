// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Starward.Pages;
using Starward.Pages.Welcome;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.MyWindows;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : WindowEx
{


    public static new MainWindow Current { get; private set; }


    public MainWindow()
    {
        Current = this;
        this.InitializeComponent();
        InitializeMainWindow();
    }



    private void InitializeMainWindow()
    {
        Title = "Starward";
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
        var len = (int)(48 * UIScale);
        ChangeWindowSize();
        AdaptTitleBarButtonColorToActuallTheme();
        SetDragRectangles(new RectInt32(0, 0, 100000, len));
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, @"Assets\logo.ico"));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }
        if (AppConfig.UserDataFolder is null)
        {
            MainWindow_Frame.Content = new WelcomePage();
        }
        else
        {
            MainWindow_Frame.Content = new MainPage();
        }
    }



    public void ChangeWindowSize(int width = 0, int height = 0)
    {
        if (width * height == 0)
        {
            (width, height) = (AppConfig.WindowSizeMode, AppConfig.EnableNavigationViewLeftCompact) switch
            {
                (0, true) => (1280, 740),
                (0, false) => (1280, 768),
                (_, true) => (1092, 636),
                (_, false) => (1064, 648),
            };
        }
        CenterInScreen(width, height);
    }




    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {

    }



    public void NavigateTo(Type page, object? parameter, NavigationTransitionInfo infoOverride)
    {
        MainWindow_Frame.Navigate(page, parameter!, infoOverride);
    }



    public void OverlayFrameNavigateTo(Type page, object? parameter, NavigationTransitionInfo infoOverride)
    {
        MainPage.Current?.PauseVideo();
        Frame_Overlay.Visibility = Visibility.Visible;
        Frame_Overlay.Navigate(page, parameter!, infoOverride);
        var len = (int)(48 * UIScale);
        SetDragRectangles(new RectInt32[] { new RectInt32(0, 0, 100000, len) });
    }


    public void CloseOverlayPage()
    {
        MainPage.Current?.PlayVideo();
        Frame_Overlay.Visibility = Visibility.Collapsed;
        Frame_Overlay.Content = null;
        MainPage.Current?.UpdateDragRectangles();
    }




    [DllImport("wtsapi32.dll")]
    private static extern bool WTSRegisterSessionNotification(IntPtr hWnd, int dwFlags);


    public IntPtr SUBCLASSPROC(HWND hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, nuint uIdSubclass, IntPtr dwRefData)
    {
        if (uMsg == (uint)User32.WindowMessage.WM_WTSSESSION_CHANGE)
        {
            // WTS_SESSION_LOCK
            if (wParam == 0x7)
            {
                MainPage.Current?.PauseVideo(true);
            }
            // WTS_SESSION_UNLOCK 
            if (wParam == 0x8)
            {
                MainPage.Current?.PlayVideo(true);
            }
        }
        if (uMsg == (uint)User32.WindowMessage.WM_SYSCOMMAND)
        {
            // SC_MAXIMIZE
            if (wParam == 0xF030)
            {
                return IntPtr.Zero;
            }
        }
        return ComCtl32.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }


    private void RootGrid_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        try
        {
            if (e.Key is Windows.System.VirtualKey.Escape)
            {
                if (AppConfig.EnableSystemTrayIcon)
                {
                    MainPage.Current?.PauseVideo();
                    AppWindow.Hide();
                    GC.Collect();
                }
            }
        }
        catch { }
    }



    public override nint WindowSubclassProc(HWND hWnd, uint uMsg, nint wParam, nint lParam, nuint uIdSubclass, nint dwRefData)
    {
        if (uMsg == (uint)User32.WindowMessage.WM_SYSCOMMAND)
        {
            // SC_MAXIMIZE
            if (wParam == 0xF030)
            {
                return IntPtr.Zero;
            }
        }
        return base.WindowSubclassProc(hWnd, uMsg, wParam, lParam, uIdSubclass, dwRefData);
    }


}
