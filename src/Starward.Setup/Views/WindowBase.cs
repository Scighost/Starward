using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Starward.Setup.Locale;
using System.Diagnostics;
using Vanara.PInvoke;

namespace Starward.Setup.Views;

public class WindowBase : Window
{

    const double DefaultTitleBarHeight = 32;

    const double ButtonWidth = 40;

    const double ChromeButtonSize = 4;

    static readonly Style ChromeButtonStyle = new(typeof(Button))
    {
        Transitions = [Transition.Create(Control.BackgroundProperty)],
        Setters =
        [
            Setter.Create(Control.BackgroundProperty, t => t.Palette.ButtonFace.WithAlpha(0)),
            Setter.Create(Control.BorderThicknessProperty, 0.0),
            Setter.Create(Control.CornerRadiusProperty, 0.0),
            Setter.Create(Control.PaddingProperty, new Thickness(0)),
        ],
        Triggers =
        [
            new StateTrigger
            {
                Match = VisualStateFlags.Hot,
                Setters = [Setter.Create(Control.BackgroundProperty, t => t.Palette.ButtonFace)],
            },
            new StateTrigger
            {
                Match = VisualStateFlags.Pressed,
                Setters = [Setter.Create(Control.BackgroundProperty, t => t.Palette.ButtonPressedBackground)],
            },
        ],
    };

    static readonly Style CloseButtonStyle = new(typeof(Button))
    {
        Transitions = [Transition.Create(Control.BackgroundProperty)],
        Setters =
        [
            Setter.Create(Control.BackgroundProperty, Color.FromRgb(232, 17, 35).WithAlpha(0)),
            Setter.Create(Control.BorderThicknessProperty, 0.0),
            Setter.Create(Control.CornerRadiusProperty, 0.0),
            Setter.Create(Control.PaddingProperty, new Thickness(0)),
        ],
        Triggers =
        [
            new StateTrigger
            {
                Match = VisualStateFlags.Hot,
                Setters =
                [
                    Setter.Create(Control.BackgroundProperty, Color.FromRgb(232, 17, 35)),
                    Setter.Create(Control.ForegroundProperty, Color.White),
                ],
            },
            new StateTrigger
            {
                Match = VisualStateFlags.Pressed,
                Setters =
                [
                    Setter.Create(Control.BackgroundProperty, Color.FromRgb(200, 12, 28)),
                    Setter.Create(Control.ForegroundProperty, Color.White),
                ],
            },
        ],
    };



    private readonly Border _contentArea;


    public WindowBase()
    {
        this.Padding = 0;
        ExtendClientAreaTitleBarHeight = DefaultTitleBarHeight;

        StyleSheet = new StyleSheet();
        StyleSheet.Define("chrome", ChromeButtonStyle);
        StyleSheet.Define("close", CloseButtonStyle);

        var minimizeBtn = CreateChromeButton(GlyphKind.Minus).OnClick(Minimize);
        minimizeBtn.SetBinding(IsVisibleProperty, this, CanMinimizeProperty);
        var closeBtn = CreateChromeButton(GlyphKind.Cross, isClose: true).OnClick(Close);
        closeBtn.SetBinding(IsVisibleProperty, this, CanCloseProperty);

        var titleBar = new StackPanel().Height(DefaultTitleBarHeight).StretchHorizontal()
                                       .Children(new StackPanel().Horizontal().Right().Children(minimizeBtn, closeBtn));
        _contentArea = new Border();
        base.Content = new Grid().Children(_contentArea, titleBar);

        this.Loaded += OnLoaded;
    }




    protected virtual void OnLoaded()
    {
        DisableDragAndDoubleClick();
        SetWindowIcon();
    }




    public new UIElement? Content
    {
        get => _contentArea.Child;
        set => _contentArea.Child = value;
    }


    private static Button CreateChromeButton(GlyphKind kind, bool isClose = false)
    {
        return new Button
        {
            Content = new GlyphElement().Kind(kind).GlyphSize(ChromeButtonSize),
            MinWidth = ButtonWidth,
            MinHeight = DefaultTitleBarHeight,
            StyleName = isClose ? "close" : "chrome",
        };
    }



    private ComCtl32.SUBCLASSPROC windowSubclassProc;


    private void DisableDragAndDoubleClick()
    {
        // 禁止拖动边框调整窗口大小
        int style = User32.GetWindowLong(this.Handle, User32.WindowLongFlags.GWL_STYLE);
        style &= ~(int)User32.WindowStyles.WS_SIZEBOX;
        User32.SetWindowLong(this.Handle, User32.WindowLongFlags.GWL_STYLE, style);
        windowSubclassProc = new(WindowSubclassProc);
        ComCtl32.SetWindowSubclass(this.Handle, windowSubclassProc, 1001, IntPtr.Zero);
    }



    protected virtual IntPtr WindowSubclassProc(HWND hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, nuint uIdSubclass, IntPtr dwRefData)
    {
        if (uMsg == (uint)User32.WindowMessage.WM_SYSCOMMAND)
        {
            if (wParam == 0xF032)
            {
                // 防止双击标题栏使窗口最大化
                return IntPtr.Zero;
            }
        }
        return ComCtl32.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }



    private void SetWindowIcon()
    {
        nint hInstance = Kernel32.GetModuleHandle(null).DangerousGetHandle();
        nint hIcon = User32.LoadIcon(hInstance, "#32512").DangerousGetHandle();
        User32.SendMessage(this.Handle, User32.WindowMessage.WM_SETICON, 0, hIcon);
        User32.SendMessage(this.Handle, User32.WindowMessage.WM_SETICON, 1, hIcon);
    }



    protected async Task<bool> CheckProcessAsync()
    {
        var ps = Process.GetProcessesByName("Starward").Where(p => p.Id != Environment.ProcessId);
        if (ps.Any())
        {
            bool close = await MessageBox.AskYesNoAsync(string.Format(Lang.StarwardIsRunningForceClose, string.Join(",", ps.Select(x => x.Id))), PromptIconKind.Warning, owner: this);
            if (close)
            {
                foreach (var p in ps)
                {
                    p.Kill();
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return true;
        }
    }



}


public static class CustomWindowExtensions
{
    public static WindowBase Content(this WindowBase w, UIElement? content)
    {
        w.Content = content;
        return w;
    }
}