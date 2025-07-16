using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using SharpGameInput;
using Starward.Helpers;
using System;
using System.Diagnostics;
using System.Numerics;
using Vanara.PInvoke;
using WindowsInput;


namespace App1;

public sealed partial class MainWindow : Window
{


    public nint WindowHandle { get; set; }

    private static IGameInput _gameInput;

    private static InputSimulator _inputSimulator;

    private DispatcherQueueTimer _inputLoopTimer;


    public MainWindow()
    {
        InitializeComponent();
        WindowHandle = (nint)AppWindow.Id.Value;
        this.ExtendsContentIntoTitleBar = true;
        _inputLoopTimer = DispatcherQueue.CreateTimer();
        _inputLoopTimer.Interval = TimeSpan.FromMilliseconds(8);
        _inputLoopTimer.Tick += _inputLoopTimer_Tick;
        GameInput.Create(out _gameInput);
        _inputSimulator = new InputSimulator();
        _gameInput.RegisterDeviceCallback(null, GameInputKind.Gamepad, GameInputDeviceStatus.NoStatus | GameInputDeviceStatus.Connected, GameInputEnumerationKind.BlockingEnumeration, null, GameInputDeviceCallback, out _, out _);
        CompositionTarget.Rendering += CompositionTarget_Rendering;
        _borderVisual = ElementCompositionPreview.GetElementVisual(Boder_Mask);
        SystemBackdrop = new TransparentBackdrop();
        var rect = CanvasGeometry.CreateRectangle(CanvasDevice.GetSharedDevice(), 0, 0, 10000, 10000);
        var circle = CanvasGeometry.CreateCircle(CanvasDevice.GetSharedDevice(), new Vector2(5000), 20);
        var mask = rect.CombineWith(circle, Matrix3x2.Identity, CanvasGeometryCombine.Exclude);
        var geo = _borderVisual.Compositor.CreatePathGeometry(new CompositionPath(mask));
        clip = _borderVisual.Compositor.CreateGeometricClip(geo);
        clip.CenterPoint = new Vector2(5000);
        _borderVisual.Clip = clip;
        int left, top, right, bottom;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            //presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
            User32.WindowStyles style = (User32.WindowStyles)User32.GetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_STYLE);
            style &= ~User32.WindowStyles.WS_OVERLAPPEDWINDOW;
            //style &= ~User32.WindowStyles.WS_BORDER;
            User32.SetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_STYLE, (nint)style);
            User32.WindowStylesEx styleEx = (User32.WindowStylesEx)User32.GetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_EXSTYLE);
            styleEx |= User32.WindowStylesEx.WS_EX_TOPMOST;
            styleEx |= User32.WindowStylesEx.WS_EX_LAYERED;
            styleEx |= User32.WindowStylesEx.WS_EX_TRANSPARENT;
            User32.SetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_EXSTYLE, (nint)styleEx);
        }
    }

    CompositionGeometricClip clip;



    private Visual _borderVisual;


    private TimeSpan _lastSpan;

    private float _lastTrigger = 0;

    private bool _showAnimation = false;

    private void CompositionTarget_Rendering(object? sender, object e)
    {
        if (AppWindow is null)
        {
            return;
        }
        User32.GetCursorPos(out var lpPoint);
        var area = DisplayArea.GetFromPoint(new Windows.Graphics.PointInt32(lpPoint.x, lpPoint.y), DisplayAreaFallback.Nearest);
        AppWindow.MoveAndResize(area.OuterBounds);
        User32.ScreenToClient(WindowHandle, ref lpPoint);
        TextBlock_Info.Text = lpPoint.ToString();
        float scale = User32.GetDpiForWindow(WindowHandle) / 96f;
        clip.Offset = new Vector2(lpPoint.X / scale - 5000, lpPoint.Y / scale - 5000);
        if (_gameInput.GetCurrentReading(GameInputKind.Gamepad, null, out LightIGameInputReading currentReading) is 0)
        {
            if (currentReading.GetGamepadState(out GameInputGamepadState state))
            {
                float trigger = state.leftTrigger;
                var controller = clip.TryGetAnimationController(nameof(clip.Scale));
                if (controller?.Progress > 0)
                {
                    Debug.WriteLine(controller?.Progress);
                }
                if (trigger > 0.1f && _lastTrigger <= 0.1f)
                {
                    var animation = clip.Compositor.CreateVector2KeyFrameAnimation();
                    animation.Duration = TimeSpan.FromMilliseconds(400);
                    animation.InsertKeyFrame(1, new Vector2(2 - trigger));
                    clip.StartAnimation(nameof(clip.Scale), animation);
                    _showAnimation = true;
                    Debug.WriteLine($"on: {trigger}    {_lastTrigger}");
                }
                else if (trigger <= 0.1f && _lastTrigger > 0.1f)
                {
                    var animation = clip.Compositor.CreateVector2KeyFrameAnimation();
                    animation.Duration = TimeSpan.FromMilliseconds(1000);
                    animation.InsertKeyFrame(1, new Vector2((float)Math.Max(Boder_Mask.ActualWidth, Boder_Mask.ActualHeight) / 10));
                    clip.StartAnimation(nameof(clip.Scale), animation);
                    _showAnimation = false;
                    Debug.WriteLine($"off: {trigger}    {_lastTrigger}");
                }
                _lastTrigger = trigger;
            }
            currentReading.Dispose();
        }
        //var scale = User32.GetDpiForWindow(WindowHandle) / 96f;
        //clip.Offset = new Vector2(lpPoint.X / scale - 5000, lpPoint.Y / scale - 5000);
    }

    long lastts = 0;

    private void _inputLoopTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            long thists = Stopwatch.GetTimestamp();
            lastts = thists;
            //InputLoop();
        }
        catch { }
    }

    private int gamepadCount = 0;
    private bool gamepadConnected = false;


    private unsafe void GameInputDeviceCallback(LightGameInputCallbackToken callbackToken, object? context, LightIGameInputDevice device, ulong timestamp, GameInputDeviceStatus currentStatus, GameInputDeviceStatus previousStatus)
    {
        if (currentStatus.HasFlag(GameInputDeviceStatus.Connected))
        {
            gamepadConnected = true;
            if (previousStatus is GameInputDeviceStatus.NoStatus)
            {
                gamepadCount++;
            }
        }
        if (currentStatus is GameInputDeviceStatus.NoStatus)
        {
            gamepadCount--;
            if (gamepadCount == 0)
            {
                gamepadConnected = false;
            }
        }
        if (gamepadConnected)
        {
            _inputLoopTimer.Start();
        }
        else
        {
            _inputLoopTimer.Stop();
        }
    }




    private ulong _lastTimestamp = 0;

    private GameInputGamepadButtons _lastButtons;

    private void InputLoop()
    {
        if (_gameInput.GetCurrentReading(GameInputKind.AnyKind, null, out LightIGameInputReading currentReading) is not 0)
        {
            return;
        }
        var kind = currentReading.GetInputKind();
        if (!currentReading.GetGamepadState(out GameInputGamepadState currentGamepadState))
        {
            currentReading.Dispose();
            return;
        }
        currentReading.GetDevice(out LightIGameInputDevice currentrReadingDevice);
        _gameInput.GetPreviousReading(currentReading, GameInputKind.Gamepad, currentrReadingDevice.ToComPtr(), out LightIGameInputReading previousReading);
        previousReading.GetGamepadState(out GameInputGamepadState previousGamepadState);


        mouseMoveRepeatCount++;
        float x = currentGamepadState.leftThumbstickX;
        float y = currentGamepadState.leftThumbstickY;

        ulong currentTimestamp = currentReading.GetTimestamp();
        currentReading.Dispose();
        previousReading.Dispose();

        float deadZone = 0.1f;
        float speedFactor = 10;
        float exponent = 1.5f;

        float magnitude = MathF.Sqrt(x * x + y * y);
        if (magnitude < deadZone)
        {
            mouseMoveRepeatCount = 0; // 重置计数
        }
        else
        {
            float gain = MathF.Pow(magnitude, exponent);
            float acceleration = 1f + MathF.Min(mouseMoveRepeatCount, 250f) / 125f;
            int dx = (int)(x * speedFactor * gain * acceleration);
            int dy = (int)(-y * speedFactor * gain * acceleration);
            //Debug.WriteLine($"{dx},{dy}");
            _inputSimulator.Mouse.MoveMouseBy(dx, dy);
        }

        if (currentTimestamp > _lastTimestamp)
        {
            _lastTimestamp = currentTimestamp;
            GameInputGamepadButtons changedButtons = currentGamepadState.buttons ^ _lastButtons;
            _lastButtons = currentGamepadState.buttons;

            if (changedButtons.HasFlag(GameInputGamepadButtons.A))
            {
                if (currentGamepadState.buttons.HasFlag(GameInputGamepadButtons.A))
                {
                    // A button pressed
                    Debug.WriteLine("A button pressed");
                    _inputSimulator.Mouse.LeftButtonDown();
                }
                else
                {
                    // A button released
                    Debug.WriteLine("A button released");
                    _inputSimulator.Mouse.LeftButtonUp();
                }
            }
        }




    }


    private static int mouseMoveRepeatCount = 0;



    RedirectVisual _redirectVisual;





    private void Button_Click(object sender, RoutedEventArgs e)
    {

    }



    bool isPressed = false;

    private void Boder_Mask_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(Content);
        clip.Offset = new Vector2((float)point.Position.X - 5000, (float)point.Position.Y - 5000);
        var animation = clip.Compositor.CreateVector2KeyFrameAnimation();
        animation.Duration = TimeSpan.FromMilliseconds(400);
        animation.InsertKeyFrame(1, Vector2.One);
        clip.StartAnimation(nameof(clip.Scale), animation);
        isPressed = true;
    }

    private void Boder_Mask_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (isPressed)
        {
            var point = e.GetCurrentPoint(Content);
            clip.Offset = new Vector2((float)point.Position.X - 5000, (float)point.Position.Y - 5000);
        }
    }

    private void Boder_Mask_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var animation = clip.Compositor.CreateVector2KeyFrameAnimation();
        animation.Duration = TimeSpan.FromMilliseconds(1000);
        animation.InsertKeyFrame(1, new Vector2((float)Math.Max(Boder_Mask.ActualWidth, Boder_Mask.ActualHeight) / 10));
        clip.StartAnimation(nameof(clip.Scale), animation);
        isPressed = false;
    }
}


