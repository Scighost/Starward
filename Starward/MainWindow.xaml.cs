// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{


    public static new MainWindow Current { get; private set; }

    public IntPtr HWND { get; private set; }


    public double UIScale => User32.GetDpiForWindow(HWND) / 96d;





    public MainWindow()
    {
        Current = this;
        this.InitializeComponent();
        InitializeMainWindow();
    }




    private void InitializeMainWindow()
    {
        HWND = WindowNative.GetWindowHandle(this);
        //if (MicaController.IsSupported())
        //{
        //    RootGrid.Background = null;
        //    this.SystemBackdrop = new MicaBackdrop();
        //}
        var titleBar = this.AppWindow.TitleBar;
        var len = (int)(48 * UIScale);
        titleBar.ExtendsContentIntoTitleBar = true;
        titleBar.SetDragRectangles(new RectInt32[] { new RectInt32(len, 0, 100000, len) });
        ChangeTitleBarButtonColor();

        ResizeToCertainSize();

        //if (AppConfig.IsMainWindowMaximum)
        //{
        //    User32.ShowWindow(HWND, ShowWindowCommand.SW_SHOWMAXIMIZED);
        //    return;
        //}

        //var windowRectValue = AppConfig.MainWindowRect;
        //if (windowRectValue > 0)
        //{
        //    var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        //    var workAreaWidth = display.WorkArea.Width;
        //    var workAreaHeight = display.WorkArea.Height;
        //    var rect = new WindowRect(windowRectValue);
        //    if (rect.Left > 0 && rect.Top > 0 && rect.Right < workAreaWidth && rect.Bottom < workAreaHeight)
        //    {
        //        AppWindow.MoveAndResize(rect.ToRectInt32());
        //    }
        //}
    }



    private void ResizeToCertainSize()
    {
        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var scale = UIScale;
        var width = (int)(1280 * scale);
        var height = (int)(768 * scale);
        var x = (display.WorkArea.Width - width) / 2;
        var y = (display.WorkArea.Height - height) / 2;
        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }
    }





    private void RootGrid_ActualThemeChanged(FrameworkElement sender, object args)
    {
        ChangeTitleBarButtonColor();
    }


    private void ChangeTitleBarButtonColor()
    {
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = AppWindow.TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            switch (RootGrid.ActualTheme)
            {
                case ElementTheme.Default:
                    break;
                case ElementTheme.Light:
                    titleBar.ButtonForegroundColor = Colors.Black;
                    titleBar.ButtonHoverForegroundColor = Colors.Black;
                    titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x20, 0x00, 0x00, 0x00);
                    break;
                case ElementTheme.Dark:
                    titleBar.ButtonForegroundColor = Colors.White;
                    titleBar.ButtonHoverForegroundColor = Colors.White;
                    titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF);
                    break;
                default:
                    break;
            }
        }
    }



    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        //SaveWindowState();
    }



    /// <summary>
    /// 保存窗口状态
    /// </summary>
    private void SaveWindowState()
    {
        var wpl = new User32.WINDOWPLACEMENT();
        if (User32.GetWindowPlacement(HWND, ref wpl))
        {
            AppConfig.IsMainWindowMaximum = wpl.showCmd == ShowWindowCommand.SW_MAXIMIZE;
            var p = AppWindow.Position;
            var s = AppWindow.Size;
            var rect = new WindowRect(p.X, p.Y, s.Width, s.Height);
            AppConfig.MainWindowRect = rect.Value;
        }
    }









    [StructLayout(LayoutKind.Explicit)]
    private struct WindowRect
    {
        [FieldOffset(0)] public short X;
        [FieldOffset(2)] public short Y;
        [FieldOffset(4)] public short Width;
        [FieldOffset(6)] public short Height;
        [FieldOffset(0)] public ulong Value;

        public int Left => X;
        public int Top => Y;
        public int Right => X + Width;
        public int Bottom => Y + Height;

        public WindowRect(int x, int y, int width, int height)
        {
            Value = 0;
            X = (short)x;
            Y = (short)y;
            Width = (short)width;
            Height = (short)height;
        }

        public WindowRect(ulong value)
        {
            X = 0;
            Y = 0;
            Width = 0;
            Height = 0;
            Value = value;
        }

        public RectInt32 ToRectInt32()
        {
            return new RectInt32(X, Y, Width, Height);
        }

    }




}
