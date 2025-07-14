using Microsoft.Win32;
using SharpGameInput;
using Starward.Features.Overlay;
using Starward.Features.Screenshot;
using Starward.Features.ViewHost;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using WindowsInput;

namespace Starward.Features.GamepadControl;

internal static class GamepadController
{

    private static IGameInput? _gameInput;

    private static InputSimulator _inputSimulator;

    private static Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;

    private static System.Timers.Timer _inputLoopTimer;


    public static void Initialize()
    {
        try
        {
            if (GameInput.Create(out _gameInput))
            {
                _inputSimulator = new InputSimulator();
                _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                _inputLoopTimer = new System.Timers.Timer(16);
                _inputLoopTimer.Elapsed += _inputLoopTimer_Elapsed;
                _gameInput.RegisterDeviceCallback(null, GameInputKind.Gamepad,
                                                  GameInputDeviceStatus.NoStatus | GameInputDeviceStatus.Connected,
                                                  GameInputEnumerationKind.BlockingEnumeration,
                                                  null, GameInputDeviceCallback, out _, out _);
                _gameInput.RegisterSystemButtonCallback(null, GameInputSystemButtons.Guide | GameInputSystemButtons.Share,
                                                        null, GameInputSystemButtonCallback, out _, out _);
                EnableGamepadSimulateInput = AppConfig.EnableGamepadSimulateInput;
                GamepadGuideButtonMode = AppConfig.GamepadGuideButtonMode;
                GamepadShareButtonMode = AppConfig.GamepadShareButtonMode;
                SetGamepadGuideButtonMapKeys(AppConfig.GamepadGuideButtonMapKeys, out _);
                SetGamepadShareButtonMapKeys(AppConfig.GamepadShareButtonMapKeys, out _);
                Initialized = true;
            }
        }
        catch { }
    }



    public static bool Initialized { get; private set; }


    public static bool EnableGamepadSimulateInput { get; set { field = value; DeviceOrEnableChanged(); } }


    public static bool GamepadConnected { get; set { field = value; DeviceOrEnableChanged(); } }


    public static int GamepadGuideButtonMode
    {
        get; set
        {
            field = value;
            if (value is 1)
            {
                if (RunningGameService.GetRunningGameCount() == 0)
                {
                    RestoreGamepadGuideButtonForGameBarBecauseOfGameExit();
                }
                else
                {
                    DisableGamepadGuideButtonForGameBarBecauseOfGameStart();
                }
            }
            else if (value is 2)
            {
                DisableGamepadGuideButtonForGameBar();
            }
            else
            {
                RestoreGamepadGuideButtonForGameBar();
            }
        }
    }


    public static int GamepadShareButtonMode { get; set; }


    public static (VirtualKeyCode[] Modifiers, VirtualKeyCode[] Keys) GamepadGuideButtonMapKeys { get; set; }


    public static (VirtualKeyCode[] Modifiers, VirtualKeyCode[] Keys) GamepadShareButtonMapKeys { get; set; }



    private static void DeviceOrEnableChanged()
    {
        if (GamepadConnected && EnableGamepadSimulateInput)
        {
            Start();
        }
        else
        {
            Stop();
        }
    }


    public static void Start()
    {
        _inputLoopTimer.Start();
    }


    public static void Stop()
    {
        _inputLoopTimer.Stop();
        _canHandleInput = false;
    }



    #region Key Maps


    private static bool GetKeyMaps(string? value, out string keysTextOrErrorKey, out List<VirtualKeyCode> modifiers, out List<VirtualKeyCode> keysList)
    {
        keysTextOrErrorKey = "";
        modifiers = new();
        keysList = new();
        string[] keys = value?.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.RemoveEmptyEntries) ?? [];
        foreach (string key in keys)
        {
            if (KeyNameToVirtualKey.TryGetValue(key.ToUpper(), out VirtualKey vk))
            {
                if (vk is VirtualKey.Shift or VirtualKey.Control or VirtualKey.Menu or VirtualKey.LeftWindows)
                {
                    modifiers.Add((VirtualKeyCode)vk);
                }
                else
                {
                    keysList.Add((VirtualKeyCode)vk);
                }
            }
            else
            {
                keysTextOrErrorKey = key;
                return false;
            }
        }
        keysTextOrErrorKey = string.Join(" ", modifiers.Concat(keysList).Select(x => VirtualKeyToKeyName.GetValueOrDefault((VirtualKey)x)));
        return true;
    }


    public static bool SetGamepadGuideButtonMapKeys(string? value, [NotNullWhen(true)] out string? keysTextOrErrorKey)
    {
        if (GetKeyMaps(value, out keysTextOrErrorKey, out List<VirtualKeyCode> modifiers, out List<VirtualKeyCode> keysList))
        {
            GamepadGuideButtonMapKeys = (modifiers.ToArray(), keysList.ToArray());
            AppConfig.GamepadGuideButtonMapKeys = keysTextOrErrorKey;
            return true;
        }
        return false;
    }


    public static bool SetGamepadShareButtonMapKeys(string? value, [NotNullWhen(true)] out string? keysTextOrErrorKey)
    {
        if (GetKeyMaps(value, out keysTextOrErrorKey, out List<VirtualKeyCode> modifiers, out List<VirtualKeyCode> keysList))
        {
            GamepadShareButtonMapKeys = (modifiers.ToArray(), keysList.ToArray());
            AppConfig.GamepadShareButtonMapKeys = keysTextOrErrorKey;
            return true;
        }
        return false;
    }


    public static string GetGamepadGuideButtonMapKeysText()
    {
        return string.Join(" ", GamepadGuideButtonMapKeys.Modifiers.Concat(GamepadGuideButtonMapKeys.Keys)
            .Select(x => VirtualKeyToKeyName.GetValueOrDefault((VirtualKey)x)));
    }


    public static string GetGamepadShareButtonMapKeysText()
    {
        return string.Join(" ", GamepadShareButtonMapKeys.Modifiers.Concat(GamepadShareButtonMapKeys.Keys)
            .Select(x => VirtualKeyToKeyName.GetValueOrDefault((VirtualKey)x)));
    }



    public static readonly Dictionary<string, VirtualKey> KeyNameToVirtualKey = new()
    {
        ["SHIFT"] = VirtualKey.Shift,
        ["CTRL"] = VirtualKey.Control,
        ["CONTROL"] = VirtualKey.Control,
        ["ALT"] = VirtualKey.Menu,
        ["MENU"] = VirtualKey.Menu,
        ["WIN"] = VirtualKey.LeftWindows,
        ["WINDOW"] = VirtualKey.LeftWindows,
        ["WINDOWS"] = VirtualKey.LeftWindows,

        ["INS"] = VirtualKey.Insert,
        ["INSERT"] = VirtualKey.Insert,
        ["DEL"] = VirtualKey.Delete,
        ["DELETE"] = VirtualKey.Delete,
        ["HOME"] = VirtualKey.Home,
        ["END"] = VirtualKey.End,
        ["PAGEUP"] = VirtualKey.PageUp,
        ["PGUP"] = VirtualKey.PageUp,
        ["PAGEDOWN"] = VirtualKey.PageDown,
        ["PGDN"] = VirtualKey.PageDown,
        ["←"] = VirtualKey.Left,
        ["↑"] = VirtualKey.Up,
        ["→"] = VirtualKey.Right,
        ["↓"] = VirtualKey.Down,
        ["LEFT"] = VirtualKey.Left,
        ["UP"] = VirtualKey.Up,
        ["RIGHT"] = VirtualKey.Right,
        ["DWON"] = VirtualKey.Down,

        ["0"] = VirtualKey.Number0,
        ["1"] = VirtualKey.Number1,
        ["2"] = VirtualKey.Number2,
        ["3"] = VirtualKey.Number3,
        ["4"] = VirtualKey.Number4,
        ["5"] = VirtualKey.Number5,
        ["6"] = VirtualKey.Number6,
        ["7"] = VirtualKey.Number7,
        ["8"] = VirtualKey.Number8,
        ["9"] = VirtualKey.Number9,

        ["A"] = VirtualKey.A,
        ["B"] = VirtualKey.B,
        ["C"] = VirtualKey.C,
        ["D"] = VirtualKey.D,
        ["E"] = VirtualKey.E,
        ["F"] = VirtualKey.F,
        ["G"] = VirtualKey.G,
        ["H"] = VirtualKey.H,
        ["I"] = VirtualKey.I,
        ["J"] = VirtualKey.J,
        ["K"] = VirtualKey.K,
        ["L"] = VirtualKey.L,
        ["M"] = VirtualKey.M,
        ["N"] = VirtualKey.N,
        ["O"] = VirtualKey.O,
        ["P"] = VirtualKey.P,
        ["Q"] = VirtualKey.Q,
        ["R"] = VirtualKey.R,
        ["S"] = VirtualKey.S,
        ["T"] = VirtualKey.T,
        ["U"] = VirtualKey.U,
        ["V"] = VirtualKey.V,
        ["W"] = VirtualKey.W,
        ["X"] = VirtualKey.X,
        ["Y"] = VirtualKey.Y,
        ["Z"] = VirtualKey.Z,

        ["PAD0"] = VirtualKey.NumberPad0,
        ["PAD1"] = VirtualKey.NumberPad1,
        ["PAD2"] = VirtualKey.NumberPad2,
        ["PAD3"] = VirtualKey.NumberPad3,
        ["PAD4"] = VirtualKey.NumberPad4,
        ["PAD5"] = VirtualKey.NumberPad5,
        ["PAD6"] = VirtualKey.NumberPad6,
        ["PAD7"] = VirtualKey.NumberPad7,
        ["PAD8"] = VirtualKey.NumberPad8,
        ["PAD9"] = VirtualKey.NumberPad9,

        ["PAD+"] = VirtualKey.Add,
        ["PAD-"] = VirtualKey.Subtract,
        ["PAD*"] = VirtualKey.Multiply,
        ["PAD/"] = VirtualKey.Divide,
        ["PAD."] = VirtualKey.Decimal,

        ["F1"] = VirtualKey.F1,
        ["F2"] = VirtualKey.F2,
        ["F3"] = VirtualKey.F3,
        ["F4"] = VirtualKey.F4,
        ["F5"] = VirtualKey.F5,
        ["F6"] = VirtualKey.F6,
        ["F7"] = VirtualKey.F7,
        ["F8"] = VirtualKey.F8,
        ["F9"] = VirtualKey.F9,
        ["F10"] = VirtualKey.F10,
        ["F11"] = VirtualKey.F11,
        ["F12"] = VirtualKey.F12,

        ["`"] = (VirtualKey)192,
        ["-"] = (VirtualKey)189,
        ["="] = (VirtualKey)187,
        ["["] = (VirtualKey)219,
        ["]"] = (VirtualKey)221,
        ["\\"] = (VirtualKey)220,
        [";"] = (VirtualKey)186,
        ["'"] = (VirtualKey)222,
        [","] = (VirtualKey)188,
        ["."] = (VirtualKey)190,
        ["/"] = (VirtualKey)191,

        ["ESC"] = VirtualKey.Escape,
        ["ESCAPE"] = VirtualKey.Escape,
        ["SPACE"] = VirtualKey.Space,
        ["TAB"] = VirtualKey.Tab,
        ["ENTER"] = VirtualKey.Enter,
        ["BACKSPACE"] = VirtualKey.Back,
        ["BACK"] = VirtualKey.Back,
        ["CAPSLOCK"] = VirtualKey.CapitalLock,
        ["CAPITALLOCK"] = VirtualKey.CapitalLock,
        ["NUMLOCK"] = VirtualKey.NumberKeyLock,
        ["NUMLK"] = VirtualKey.NumberKeyLock,
        ["NUMBERKEYLOCK"] = VirtualKey.NumberKeyLock,
        ["SCROLL"] = VirtualKey.Scroll,
        ["SCROLLLOCK"] = VirtualKey.Scroll,
        ["PAUSE"] = VirtualKey.Pause,
        ["PRINT"] = VirtualKey.Print,
        ["PRTSC"] = VirtualKey.Print,
        ["PRINTSCREEN"] = VirtualKey.Print,
    };


    public static readonly Dictionary<VirtualKey, string> VirtualKeyToKeyName = new()
    {
        [VirtualKey.Shift] = "Shift",
        [VirtualKey.Control] = "Ctrl",
        [VirtualKey.Menu] = "Alt",
        [VirtualKey.LeftWindows] = "Win",
        [VirtualKey.RightWindows] = "Win",

        [VirtualKey.Insert] = "Insert",
        [VirtualKey.Delete] = "Delete",
        [VirtualKey.Home] = "Home",
        [VirtualKey.End] = "End",
        [VirtualKey.PageUp] = "PageUp",
        [VirtualKey.PageDown] = "PageDown",
        [VirtualKey.Left] = "←",
        [VirtualKey.Up] = "↑",
        [VirtualKey.Right] = "→",
        [VirtualKey.Down] = "↓",

        [VirtualKey.Number0] = "0",
        [VirtualKey.Number1] = "1",
        [VirtualKey.Number2] = "2",
        [VirtualKey.Number3] = "3",
        [VirtualKey.Number4] = "4",
        [VirtualKey.Number5] = "5",
        [VirtualKey.Number6] = "6",
        [VirtualKey.Number7] = "7",
        [VirtualKey.Number8] = "8",
        [VirtualKey.Number9] = "9",

        [VirtualKey.A] = "A",
        [VirtualKey.B] = "B",
        [VirtualKey.C] = "C",
        [VirtualKey.D] = "D",
        [VirtualKey.E] = "E",
        [VirtualKey.F] = "F",
        [VirtualKey.G] = "G",
        [VirtualKey.H] = "H",
        [VirtualKey.I] = "I",
        [VirtualKey.J] = "J",
        [VirtualKey.K] = "K",
        [VirtualKey.L] = "L",
        [VirtualKey.M] = "M",
        [VirtualKey.N] = "N",
        [VirtualKey.O] = "O",
        [VirtualKey.P] = "P",
        [VirtualKey.Q] = "Q",
        [VirtualKey.R] = "R",
        [VirtualKey.S] = "S",
        [VirtualKey.T] = "T",
        [VirtualKey.U] = "U",
        [VirtualKey.V] = "V",
        [VirtualKey.W] = "W",
        [VirtualKey.X] = "X",
        [VirtualKey.Y] = "Y",
        [VirtualKey.Z] = "Z",

        [VirtualKey.NumberPad0] = "Pad0",
        [VirtualKey.NumberPad1] = "Pad1",
        [VirtualKey.NumberPad2] = "Pad2",
        [VirtualKey.NumberPad3] = "Pad3",
        [VirtualKey.NumberPad4] = "Pad4",
        [VirtualKey.NumberPad5] = "Pad5",
        [VirtualKey.NumberPad6] = "Pad6",
        [VirtualKey.NumberPad7] = "Pad7",
        [VirtualKey.NumberPad8] = "Pad8",
        [VirtualKey.NumberPad9] = "Pad9",

        [VirtualKey.Add] = "Pad+",
        [VirtualKey.Subtract] = "Pad-",
        [VirtualKey.Multiply] = "Pad*",
        [VirtualKey.Divide] = "Pad/",
        [VirtualKey.Decimal] = "Pad.",

        [VirtualKey.F1] = "F1",
        [VirtualKey.F2] = "F2",
        [VirtualKey.F3] = "F3",
        [VirtualKey.F4] = "F4",
        [VirtualKey.F5] = "F5",
        [VirtualKey.F6] = "F6",
        [VirtualKey.F7] = "F7",
        [VirtualKey.F8] = "F8",
        [VirtualKey.F9] = "F9",
        [VirtualKey.F10] = "F10",
        [VirtualKey.F11] = "F11",
        [VirtualKey.F12] = "F12",

        [(VirtualKey)192] = "`",
        [(VirtualKey)189] = "-",
        [(VirtualKey)187] = "=",
        [(VirtualKey)219] = "[",
        [(VirtualKey)221] = "]",
        [(VirtualKey)220] = "\\",
        [(VirtualKey)186] = ";",
        [(VirtualKey)222] = "'",
        [(VirtualKey)188] = ",",
        [(VirtualKey)190] = ".",
        [(VirtualKey)191] = "/",

        [VirtualKey.Escape] = "Esc",
        [VirtualKey.Space] = "Space",
        [VirtualKey.Tab] = "Tab",
        [VirtualKey.Enter] = "Enter",
        [VirtualKey.Back] = "Backspace",
        [VirtualKey.CapitalLock] = "CapsLock",
        [VirtualKey.NumberKeyLock] = "NumberLock",
        [VirtualKey.Scroll] = "ScrollLock",
        [VirtualKey.Pause] = "Pause",
        [VirtualKey.Print] = "PrintScreen",
    };


    #endregion



    #region Device Callback


    private static int gamepadCount = 0;

    private static void GameInputDeviceCallback(LightGameInputCallbackToken callbackToken, object? context, LightIGameInputDevice device, ulong timestamp, GameInputDeviceStatus currentStatus, GameInputDeviceStatus previousStatus)
    {
        if (currentStatus.HasFlag(GameInputDeviceStatus.Connected))
        {
            GamepadConnected = true;
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
                GamepadConnected = false;
            }
        }
    }


    #endregion



    #region System Button Callback


    private static void GameInputSystemButtonCallback(LightGameInputCallbackToken callbackToken, object? context, LightIGameInputDevice device, ulong timestamp, GameInputSystemButtons currentState, GameInputSystemButtons previousState)
    {
        try
        {
            bool currentGuide = currentState.HasFlag(GameInputSystemButtons.Guide);
            bool currentShare = currentState.HasFlag(GameInputSystemButtons.Share);
            bool previousGuide = previousState.HasFlag(GameInputSystemButtons.Guide);
            bool previousShare = previousState.HasFlag(GameInputSystemButtons.Share);
            if (currentGuide ^ previousGuide)
            {
                if (currentGuide)
                {
                    OnGuideDown();
                }
                else
                {
                    OnGuideUp();
                }
            }
            if (currentShare ^ previousShare)
            {
                if (currentShare)
                {
                    OnShareDown();
                }
            }
        }
        catch { }
    }



    private static CancellationTokenSource guideButtonCancellationTokenSource;

    private static bool guideLongPressTriggered;

    private static async void OnGuideDown()
    {
        if (EnableGamepadSimulateInput)
        {
            guideButtonCancellationTokenSource?.Cancel();
            guideButtonCancellationTokenSource = new();
            CancellationToken token = guideButtonCancellationTokenSource.Token;
            guideLongPressTriggered = false;
            await Task.Delay(600);
            if (token.IsCancellationRequested)
            {
                return;
            }
            guideLongPressTriggered = true;
            GuideLongPress();
        }
        else
        {
            GuideClick();
        }
    }


    private static void OnGuideUp()
    {
        if (EnableGamepadSimulateInput)
        {
            guideButtonCancellationTokenSource?.Cancel();
            if (!guideLongPressTriggered)
            {
                GuideClick();
            }
        }
    }


    private static void GuideClick()
    {
        if (GamepadGuideButtonMode is 1)
        {
            _dispatcherQueue.TryEnqueue(ScreenCaptureService.Capture);
        }
        else if (GamepadGuideButtonMode is 2)
        {
            if (GamepadGuideButtonMapKeys.Modifiers.Length > 0 || GamepadGuideButtonMapKeys.Keys.Length > 0)
            {
                _inputSimulator.Keyboard.ModifiedKeyStroke(GamepadGuideButtonMapKeys.Modifiers, GamepadGuideButtonMapKeys.Keys);
            }
        }
    }


    private static void GuideLongPress()
    {
        _dispatcherQueue.TryEnqueue(MainWindow.Current.Show);
    }


    private static void OnShareDown()
    {
        if (GamepadShareButtonMode is 1)
        {
            _dispatcherQueue.TryEnqueue(ScreenCaptureService.Capture);
        }
        else if (GamepadShareButtonMode is 2)
        {
            if (GamepadShareButtonMapKeys.Modifiers.Length > 0 || GamepadShareButtonMapKeys.Keys.Length > 0)
            {
                _inputSimulator.Keyboard.ModifiedKeyStroke(GamepadShareButtonMapKeys.Modifiers, GamepadShareButtonMapKeys.Keys);
            }
        }
    }



    #endregion



    #region Input Loop


    private static void _inputLoopTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            InputLoop();
        }
        catch { }
    }


    private static ulong _lastScrollTimestamp = 0;

    private static ulong _lastButtonsTimestamp = 0;

    private static GameInputGamepadButtons _lastGamepadButtons;

    private static bool _canHandleInput = false;

    private static void InputLoop()
    {
        if (_gameInput is null)
        {
            return;
        }
        if (_gameInput.GetCurrentReading(GameInputKind.Gamepad, null, out LightIGameInputReading currentReading) is not 0)
        {
            return;
        }
        using (currentReading)
        {
            if (!currentReading.GetGamepadState(out GameInputGamepadState currentGamepadState))
            {
                return;
            }

            CheckGamepadThumbSticks(currentGamepadState.buttons);

            if (!_canHandleInput)
            {
                return;
            }

            ulong currentTimestamp = _gameInput.GetCurrentTimestamp();
            ulong currentRreadingTimestamp = currentReading.GetTimestamp();

            MouseMove(currentGamepadState);
            if (currentTimestamp - _lastScrollTimestamp > 100_000)
            {
                MouseScroll(currentGamepadState);
                _lastScrollTimestamp = currentTimestamp;
            }
            if (currentRreadingTimestamp - _lastButtonsTimestamp > 0)
            {
                ButtonClick(currentGamepadState);
                _lastButtonsTimestamp = currentRreadingTimestamp;
            }
        }
    }



    private static GameInputGamepadButtons _lastThumbSticks;


    private static void CheckGamepadThumbSticks(GameInputGamepadButtons gamepadButtons)
    {
        const GameInputGamepadButtons ThumbSticks = GameInputGamepadButtons.LeftThumbstick | GameInputGamepadButtons.RightThumbstick;
        GameInputGamepadButtons currentThumbSticks = gamepadButtons & ThumbSticks;
        bool thumbSticksChanged = currentThumbSticks != _lastThumbSticks;
        if (thumbSticksChanged && currentThumbSticks == ThumbSticks)
        {
            _canHandleInput = !_canHandleInput;
        }
        _lastThumbSticks = currentThumbSticks;
    }




    private static int mouseMoveRepeatCount = 0;


    private static void MouseMove(GameInputGamepadState state)
    {
        mouseMoveRepeatCount++;

        float x = state.leftThumbstickX;
        float y = state.leftThumbstickY;

        float deadZone = 0.1f;
        float speedFactor = 10;
        float exponent = 1.5f;

        float magnitude = MathF.Sqrt(x * x + y * y);
        if (magnitude >= deadZone)
        {
            float exp = MathF.Pow(magnitude, exponent);
            float gain = 1f + MathF.Min(mouseMoveRepeatCount, 250) / 125;
            int dx = (int)(x * speedFactor * exp * gain);
            int dy = (int)(-y * speedFactor * exp * gain);
            _inputSimulator.Mouse.MoveMouseBy(dx, dy);
        }
        else
        {
            mouseMoveRepeatCount = 0;
        }
    }


    private static void MouseScroll(GameInputGamepadState state)
    {
        float x = state.rightThumbstickX;
        float y = state.rightThumbstickY;

        float magnitude = MathF.Sqrt(x * x + y * y);
        if (magnitude < 0.2f)
        {
            return;
        }
        int dx = (int)(x * MathF.Pow(magnitude, 1.5f) * 10);
        int dy = (int)(y * MathF.Pow(magnitude, 1.5f) * 10);
        _inputSimulator.Mouse.HorizontalScroll(dx).VerticalScroll(dy);
    }


    private static void ButtonClick(GameInputGamepadState state)
    {
        GameInputGamepadButtons changedButtons = state.buttons ^ _lastGamepadButtons;
        _lastGamepadButtons = state.buttons;

        if (changedButtons.HasFlag(GameInputGamepadButtons.A))
        {
            if (state.buttons.HasFlag(GameInputGamepadButtons.A))
            {
                _inputSimulator.Mouse.LeftButtonDown();
            }
            else
            {
                _inputSimulator.Mouse.LeftButtonUp();
            }
        }
        if (changedButtons.HasFlag(GameInputGamepadButtons.X))
        {
            if (state.buttons.HasFlag(GameInputGamepadButtons.X))
            {
                _inputSimulator.Mouse.RightButtonDown();
            }
            else
            {
                _inputSimulator.Mouse.RightButtonUp();
            }
        }
        KeyEvent(changedButtons, state.buttons, GameInputGamepadButtons.B, VirtualKeyCode.ESCAPE);
        KeyEvent(changedButtons, state.buttons, GameInputGamepadButtons.DPadLeft, VirtualKeyCode.LEFT);
        KeyEvent(changedButtons, state.buttons, GameInputGamepadButtons.DPadUp, VirtualKeyCode.UP);
        KeyEvent(changedButtons, state.buttons, GameInputGamepadButtons.DPadRight, VirtualKeyCode.RIGHT);
        KeyEvent(changedButtons, state.buttons, GameInputGamepadButtons.DPadDown, VirtualKeyCode.DOWN);
    }



    private static void KeyEvent(GameInputGamepadButtons changedButtons, GameInputGamepadButtons currentButtons, GameInputGamepadButtons keyButton, VirtualKeyCode vk)
    {
        if (changedButtons.HasFlag(keyButton))
        {
            if (currentButtons.HasFlag(keyButton))
            {
                _inputSimulator.Keyboard.KeyDown(vk);
            }
            else
            {
                _inputSimulator.Keyboard.KeyUp(vk);
            }
        }
    }


    #endregion



    #region Xbox Game Bar


    private static bool _gameBarGuideButtonDisabled = false;

    private static bool _gameBarGuideButtonCached = false;


    public static void DisableGamepadGuideButtonForGameBar()
    {
        try
        {
            if (Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", 1) is 1)
            {
                _gameBarGuideButtonCached = true;
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", 0, RegistryValueKind.DWord);
                _gameBarGuideButtonDisabled = true;
            }
            else if (!_gameBarGuideButtonDisabled)
            {
                _gameBarGuideButtonDisabled = true;
                _gameBarGuideButtonCached = false;
            }
        }
        catch { }
    }


    public static void RestoreGamepadGuideButtonForGameBar()
    {
        try
        {
            if (_gameBarGuideButtonDisabled)
            {
                int value = _gameBarGuideButtonCached ? 1 : 0;
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", value, RegistryValueKind.DWord);
                _gameBarGuideButtonDisabled = false;
            }
        }
        catch { }
    }


    public static void DisableGamepadGuideButtonForGameBarBecauseOfGameStart()
    {
        if (GamepadGuideButtonMode is 1 && RunningGameService.GetRunningGameCount() > 0)
        {
            DisableGamepadGuideButtonForGameBar();
        }
    }


    public static void RestoreGamepadGuideButtonForGameBarBecauseOfGameExit()
    {
        if (GamepadGuideButtonMode is 1 && RunningGameService.GetRunningGameCount() == 0)
        {
            RestoreGamepadGuideButtonForGameBar();
        }
    }


    public static void EnableXboxGameBarCapture()
    {
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 1, RegistryValueKind.DWord);
    }


    public static void DisbaleXboxGameBarCapture()
    {
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord);
    }


    #endregion


}
