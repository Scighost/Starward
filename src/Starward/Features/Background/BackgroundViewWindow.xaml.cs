using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Starward.Frameworks;
using System;
using System.ComponentModel;
using System.Diagnostics;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.System;


namespace Starward.Features.Background;

[INotifyPropertyChanged]
public sealed partial class BackgroundViewWindow : WindowEx
{


    public BackgroundViewWindow()
    {
        this.InitializeComponent();
        InitializeWindow();
        WeakReferenceMessenger.Default.Register<AccentColorChangedMessage>(this, OnAccentColorChanged);
    }



    private void InitializeWindow()
    {
        Title = "Starward - Background Viewer";
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
        SystemBackdrop = new DesktopAcrylicBackdrop();
        Content.KeyDown += Content_KeyDown;
        Closed += BackgroundViewWindow_Closed;
        AdaptTitleBarButtonColorToActuallTheme();
        SetIcon();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }
    }



    public override void CenterInScreen(int? width = null, int? height = null)
    {
        width = width <= 0 ? null : width;
        height = height <= 0 ? null : height;
        User32.GetCursorPos(out POINT point);
        DisplayArea display = DisplayArea.GetFromPoint(new PointInt32(point.X, point.Y), DisplayAreaFallback.Nearest);
        double scale = UIScale;
        int w = (int)((width * scale) ?? AppWindow.Size.Width);
        int h = (int)((height * scale) ?? AppWindow.Size.Height);
        int x = display.WorkArea.X + (display.WorkArea.Width - w) / 2;
        int y = display.WorkArea.Y + (display.WorkArea.Height - h) / 2;
        AppWindow.MoveAndResize(new RectInt32(x, y, w, h));
    }



    public override void Show()
    {
        var window = AppWindow.GetFromWindowId(MainWindowId);
        if (window is null)
        {
            CenterInScreen(1200, 676);
        }
        else
        {
            AppWindow.MoveAndResize(new RectInt32(window.Position.X, window.Position.Y, window.Size.Width, window.Size.Height));
        }
        base.Show();
    }



    private void OnAccentColorChanged(object _, AccentColorChangedMessage __)
    {
        FrameworkElement ele = (FrameworkElement)Content;
        ele.RequestedTheme = ele.ActualTheme switch
        {
            ElementTheme.Light => ElementTheme.Dark,
            ElementTheme.Dark => ElementTheme.Light,
            _ => ElementTheme.Default,
        };
        ele.RequestedTheme = ElementTheme.Default;
    }



    private void Content_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Escape)
        {
            Close();
        }
    }


    private void BackgroundViewWindow_Closed(object sender, WindowEventArgs args)
    {
        try
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            Content.KeyDown -= Content_KeyDown;
            Closed -= BackgroundViewWindow_Closed;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }



}
