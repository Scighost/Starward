﻿// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Starward.Pages;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
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



    private CancellationTokenSource overlayCancelTokenSource;


    public void OverlayFrameNavigateTo(Type page, object? parameter)
    {
        try
        {
            overlayCancelTokenSource?.Cancel();
            overlayCancelTokenSource = new CancellationTokenSource();
            MainPage.Current?.PauseVideo();
            Frame_Overlay.Visibility = Visibility.Visible;
            Frame_Overlay.Navigate(page, parameter!, new SuppressNavigationTransitionInfo());
            if (Frame_Overlay.Content is UIElement element)
            {
                element.Transitions = [new EntranceThemeTransition { FromVerticalOffset = 600 }];
            }
            SetDragRectangles(new RectInt32(0, 0, 100000, (int)(48 * UIScale)));
        }
        catch { }
    }


    public void CloseOverlayPage()
    {
        try
        {
            if (Frame_Overlay.Content is UIElement element)
            {
                var token = overlayCancelTokenSource?.Token ?? CancellationToken.None;
                var tt = new TranslateTransform();
                element.RenderTransform = tt;
                var da = new DoubleAnimation
                {
                    To = Frame_Overlay.ActualHeight,
                    Duration = new Duration(TimeSpan.FromMilliseconds(600)),
                    EasingFunction = new BackEase(),
                };
                Storyboard.SetTarget(da, tt);
                Storyboard.SetTargetProperty(da, nameof(tt.Y));
                var sb = new Storyboard { Duration = new Duration(TimeSpan.FromMilliseconds(600)) };
                sb.Children.Add(da);
                sb.Completed += (_, _) =>
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    Frame_Overlay.Visibility = Visibility.Collapsed;
                    Frame_Overlay.Content = null;
                    MainPage.Current?.UpdateDragRectangles();
                };
                sb.Begin();
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