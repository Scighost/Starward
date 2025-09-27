using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using SharpGameInput.V0;
using Starward.Features.Setting;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Graphics;


namespace Starward.Features.GamepadControl;

[INotifyPropertyChanged]
public sealed partial class SimulateInputTipsWindow : WindowEx
{


    private readonly ILogger<SimulateInputTipsWindow> _logger = AppConfig.GetLogger<SimulateInputTipsWindow>();


    public SimulateInputTipsWindow()
    {
        InitializeComponent();
        InitializeWindow();
        InitializeComposition();
        // 不能删除，防止在 SW_SHOWNOACTIVATE 显示后没有文字
        this.Bindings.Update();
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, _) => this.Bindings.Update());
        this.Closed += SimulateInputTipsWindow_Closed;
    }


    private void InitializeWindow()
    {
        SystemBackdrop = new TransparentBackdrop();
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.IsShownInSwitchers = false;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
            User32.WindowStyles style = (User32.WindowStyles)User32.GetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_STYLE);
            style &= ~User32.WindowStyles.WS_OVERLAPPEDWINDOW;
            User32.SetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_STYLE, (nint)style);
            User32.WindowStylesEx styleEx = (User32.WindowStylesEx)User32.GetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_EXSTYLE);
            styleEx |= User32.WindowStylesEx.WS_EX_TOPMOST;
            styleEx |= User32.WindowStylesEx.WS_EX_LAYERED;
            styleEx |= User32.WindowStylesEx.WS_EX_TRANSPARENT;
            User32.SetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_EXSTYLE, (nint)styleEx);
        }
    }


    private void InitializeComposition()
    {
        _borderVisual = ElementCompositionPreview.GetElementVisual(Border_CursorMask);
        var rect = CanvasGeometry.CreateRectangle(CanvasDevice.GetSharedDevice(), 0, 0, 10000, 10000);
        var circle = CanvasGeometry.CreateCircle(CanvasDevice.GetSharedDevice(), new Vector2(5000), 50);
        var mask = rect.CombineWith(circle, Matrix3x2.Identity, CanvasGeometryCombine.Exclude);
        var geo = _borderVisual.Compositor.CreatePathGeometry(new CompositionPath(mask));
        _geometricClip = _borderVisual.Compositor.CreateGeometricClip(geo);
        _geometricClip.CenterPoint = new Vector2(5000);
        _borderVisual.Clip = _geometricClip;
        _easeInFunction = CompositionEasingFunction.CreateCircleEasingFunction(_borderVisual.Compositor, CompositionEasingFunctionMode.In);
        _easeOutFunction = CompositionEasingFunction.CreateCircleEasingFunction(_borderVisual.Compositor, CompositionEasingFunctionMode.Out);
    }



    private void SimulateInputTipsWindow_Closed(object sender, WindowEventArgs args)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        this.Closed -= SimulateInputTipsWindow_Closed;
        CompositionTarget.Rendering -= CompositionTarget_Rendering;
    }



    private Visual _borderVisual;

    private CompositionGeometricClip _geometricClip;

    private CompositionEasingFunction _easeInFunction;

    private CompositionEasingFunction _easeOutFunction;

    private float _lastLeftTrigger = 0;

    private GameInputGamepadButtons _lastViewButton;

    private void CompositionTarget_Rendering(object? sender, object e)
    {
        try
        {
            if (AppWindow is null)
            {
                return;
            }
            MoveCursorMaskPosition();
            CursorMaskAnimation();
        }
        catch { }
    }


    private void MoveCursorMaskPosition()
    {
        User32.GetCursorPos(out var lpPoint);
        DisplayArea area = DisplayArea.GetFromPoint(new PointInt32(lpPoint.x, lpPoint.y), DisplayAreaFallback.Nearest);
        if (area.OuterBounds.Width != AppWindow.Size.Width || area.OuterBounds.Height != AppWindow.Size.Height)
        {
            AppWindow.MoveAndResize(area.OuterBounds);
        }
        User32.ScreenToClient(WindowHandle, ref lpPoint);
        float scale = User32.GetDpiForWindow(WindowHandle) / 96f;
        _geometricClip.Offset = new Vector2(lpPoint.X / scale - 5000, lpPoint.Y / scale - 5000);
    }


    private void CursorMaskAnimation()
    {
        if (GamepadController.GameInputInstance is null)
        {
            return;
        }
        if (GamepadController.GameInputInstance.GetCurrentReading(GameInputKind.Gamepad, null, out LightIGameInputReading currentReading) is 0)
        {
            if (currentReading.GetGamepadState(out GameInputGamepadState state))
            {
                const float threshold = 0.2f;
                float trigger = state.leftTrigger;
                if (trigger >= threshold && _lastLeftTrigger < threshold)
                {
                    var animation = _geometricClip.Compositor.CreateVector2KeyFrameAnimation();
                    animation.Duration = TimeSpan.FromMilliseconds(600);
                    animation.InsertKeyFrame(1, Vector2.One, _easeOutFunction);
                    _geometricClip.StartAnimation(nameof(_geometricClip.Scale), animation);
                }
                else if (trigger < threshold && _lastLeftTrigger >= threshold)
                {
                    var animation = _geometricClip.Compositor.CreateVector2KeyFrameAnimation();
                    animation.Duration = TimeSpan.FromMilliseconds(600);
                    float scale = (float)Math.Max(Border_CursorMask.ActualWidth, Border_CursorMask.ActualHeight) / 10;
                    animation.InsertKeyFrame(1, new Vector2(scale), _easeInFunction);
                    _geometricClip.StartAnimation(nameof(_geometricClip.Scale), animation);
                }
                _lastLeftTrigger = trigger;

                GameInputGamepadButtons viewButton = state.buttons & GameInputGamepadButtons.View;
                if (viewButton != 0 && (_lastViewButton ^ viewButton).HasFlag(GameInputGamepadButtons.View))
                {
                    Expander_ButtonHints.IsExpanded = !Expander_ButtonHints.IsExpanded;
                }
                _lastViewButton = viewButton;

            }
            currentReading.Dispose();
        }
    }



    public new void Activate()
    {
        try
        {
            _hideWindowCancellationTokenSource?.Cancel();
            User32.GetCursorPos(out var lpPoint);
            DisplayArea area = DisplayArea.GetFromPoint(new PointInt32(lpPoint.x, lpPoint.y), DisplayAreaFallback.Nearest);
            if (area.OuterBounds.Width != AppWindow.Size.Width || area.OuterBounds.Height != AppWindow.Size.Height)
            {
                AppWindow.MoveAndResize(area.OuterBounds);
            }
            User32.ScreenToClient(WindowHandle, ref lpPoint);
            float scale = User32.GetDpiForWindow(WindowHandle) / 96f;
            _geometricClip.Offset = new Vector2(lpPoint.X / scale - 5000, lpPoint.Y / scale - 5000);

            float clipScale = Math.Max(area.OuterBounds.Width, area.OuterBounds.Height) / scale / 10;
            _geometricClip.Scale = new Vector2(clipScale);
            var animation = _geometricClip.Compositor.CreateVector2KeyFrameAnimation();
            animation.Duration = TimeSpan.FromMilliseconds(2000);
            animation.InsertKeyFrame(0.30f, Vector2.One, _easeOutFunction);
            animation.InsertKeyFrame(0.50f, new Vector2(0.6f), _easeOutFunction);
            animation.InsertKeyFrame(0.70f, Vector2.One, _easeInFunction);
            animation.InsertKeyFrame(1.00f, new Vector2(clipScale), _easeInFunction);
            _geometricClip.StartAnimation(nameof(_geometricClip.Scale), animation);
            User32.ShowWindow(WindowHandle, ShowWindowCommand.SW_SHOWNOACTIVATE);

            Expander_ButtonHints.Opacity = 1;
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }
        catch { }
    }



    private CancellationTokenSource _hideWindowCancellationTokenSource;


    public override async void Hide()
    {
        try
        {
            if (AppWindow is null)
            {
                return;
            }
            _hideWindowCancellationTokenSource?.Cancel();
            _hideWindowCancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _hideWindowCancellationTokenSource.Token;
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            Expander_ButtonHints.Opacity = 0;
            var animation = _geometricClip.Compositor.CreateVector2KeyFrameAnimation();
            animation.Duration = TimeSpan.FromMilliseconds(600);
            float scale = (float)Math.Max(Border_CursorMask.ActualWidth, Border_CursorMask.ActualHeight) / 10;
            animation.InsertKeyFrame(1, new Vector2(scale), _easeInFunction);
            _geometricClip.StartAnimation(nameof(_geometricClip.Scale), animation);
            await Task.Delay(600);
            if (token.IsCancellationRequested)
            {
                return;
            }
            AppWindow?.Hide();
        }
        catch { }
    }



}
