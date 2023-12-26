// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Starward.Pages;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
#if DEBUG
        if (Debugger.IsAttached)
        {
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                DispatcherQueue.TryEnqueue(App.Current.InitializeSystemTray);
            });
        }
        else
        {
            App.Current.InitializeSystemTray();
        }
#else
        App.Current.InitializeSystemTray();
#endif
    }



    private void InitializeMainWindow()
    {
        Title = "Starward";
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
        AppWindow.Closing += AppWindow_Closing;
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


    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        args.Cancel = true;
        // todo
        App.Current.Exit();
    }



    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        Current = null!;
    }


    public void OverlayFrameNavigateTo(Type page, object? parameter, NavigationTransitionInfo infoOverride)
    {
        MainPage.Current?.PauseVideo();
        Frame_Overlay.Visibility = Visibility.Visible;
        Frame_Overlay.Navigate(page, parameter!, infoOverride);
        var len = (int)(48 * UIScale);
        SetDragRectangles(new RectInt32(0, 0, 100000, len));
    }


    public void CloseOverlayPage()
    {
        MainPage.Current?.PlayVideo();
        Frame_Overlay.Visibility = Visibility.Collapsed;
        Frame_Overlay.Content = null;
        MainPage.Current?.UpdateDragRectangles();
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
