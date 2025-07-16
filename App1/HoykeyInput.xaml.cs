using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.System;
using Windows.UI.Core;


namespace App1;

[INotifyPropertyChanged]
public sealed partial class HoykeyInput : UserControl
{


    public HoykeyInput()
    {
        InitializeComponent();
    }



    public nint WindowHandle { get; set; }


    public int HotkeyId { get; set; }


    public VirtualKeyModifiers Modifiers { get; private set; }


    public VirtualKey Key { get; private set; }


    public HoykeyInputState State
    {
        get; set
        {
            field = value;
            if (value is HoykeyInputState.None)
            {
                TextBlock_EditingText.Visibility = Visibility.Collapsed;
                TextBlock_HotkeyText_Warning.Visibility = Visibility.Collapsed;
                Button_Success.Visibility = Visibility.Collapsed;
                if (string.IsNullOrWhiteSpace(HotkeyText))
                {
                    TextBlock_ClickToSetHotkey.Visibility = Visibility.Visible;
                    TextBlock_HotkeyText.Visibility = Visibility.Collapsed;
                    Button_DeleteHotkey.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TextBlock_ClickToSetHotkey.Visibility = Visibility.Collapsed;
                    TextBlock_HotkeyText.Visibility = Visibility.Visible;
                    Button_DeleteHotkey.Visibility = Visibility.Visible;
                }
            }
            else if (value is HoykeyInputState.Edit)
            {
                TextBlock_ClickToSetHotkey.Visibility = Visibility.Collapsed;
                TextBlock_HotkeyText.Visibility = Visibility.Collapsed;
                TextBlock_EditingText.Visibility = Visibility.Visible;
                TextBlock_HotkeyText_Warning.Visibility = Visibility.Collapsed;
                Button_Success.Visibility = Visibility.Collapsed;
                Button_DeleteHotkey.Visibility = Visibility.Visible;
            }
            else if (value is HoykeyInputState.Success)
            {
                TextBlock_ClickToSetHotkey.Visibility = Visibility.Collapsed;
                TextBlock_HotkeyText.Visibility = Visibility.Visible;
                TextBlock_EditingText.Visibility = Visibility.Collapsed;
                TextBlock_HotkeyText_Warning.Visibility = Visibility.Collapsed;
                Button_Success.Visibility = Visibility.Visible;
                Button_DeleteHotkey.Visibility = Visibility.Collapsed;
            }
            else if (value is HoykeyInputState.Warning)
            {
                TextBlock_ClickToSetHotkey.Visibility = Visibility.Collapsed;
                TextBlock_HotkeyText.Visibility = Visibility.Collapsed;
                TextBlock_EditingText.Visibility = Visibility.Collapsed;
                TextBlock_HotkeyText_Warning.Visibility = Visibility.Visible;
                Button_Success.Visibility = Visibility.Collapsed;
                Button_DeleteHotkey.Visibility = Visibility.Visible;
            }
        }
    }



    public event EventHandler<HotkeyInputEventArg> HotkeyEditing;

    public event EventHandler<HotkeyEditFinishedEventArg> HotkeyEditFinished;

    public event EventHandler<HotkeyInputEventArg> HotkeyDeleted;


    public bool SetHotkey(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        if (IsHotkeyAvaliable(modifiers, key))
        {
            Modifiers = modifiers;
            Key = key;
            UpdateText();
            return true;
        }
        else
        {
            return false;
        }
    }


    public bool SetHotkey(uint fsModifiers, VirtualKey key)
    {
        return SetHotkey((VirtualKeyModifiers)ChangeModifiesBit(fsModifiers), key);
    }


    public string? EditingText { get; private set => SetProperty(ref field, value); }


    public string? HotkeyText { get; private set => SetProperty(ref field, value); }


    private void Button_EditHotkey_Click(object sender, RoutedEventArgs e)
    {
        OnHotkeyEditing();
        _pressedKeys.Clear();
        _editingModifiers = VirtualKeyModifiers.None;
        _editingKey = VirtualKey.None;
        State = HoykeyInputState.Edit;
        UpdateText();
    }


    private void Grid_EditHotkey_LostFocus(object sender, RoutedEventArgs e)
    {
        _pressedKeys.Clear();
        if (State is HoykeyInputState.Edit)
        {
            OnHotkeyEditFinished();
        }
    }


    private void Grid_EditHotkey_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        try
        {
            if (e.KeyStatus.WasKeyDown)
            {
                return;
            }
            State = HoykeyInputState.Edit;
            if (!_pressedKeys.Contains(e.Key))
            {
                _pressedKeys.Add(e.Key);
            }
            _editingModifiers = GetKeyModifiers();
            if (e.Key is VirtualKey.Control or VirtualKey.Shift or VirtualKey.Menu or VirtualKey.LeftWindows or VirtualKey.RightWindows)
            {
                _editingKey = VirtualKey.None;
            }
            else if (AvaliableKeyDict.TryGetValue(e.Key, out _))
            {
                _editingKey = e.Key;
            }
            else
            {
                _editingKey = VirtualKey.None;
            }
            UpdateText();
        }
        catch { }
    }


    private void Grid_EditHotkey_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        try
        {
            if (_pressedKeys.Contains(e.Key))
            {
                _pressedKeys.Remove(e.Key);
            }
            if (IsHotkeyAvaliable(_editingModifiers, _editingKey))
            {
                if (_pressedKeys.Count == 0)
                {
                    OnHotkeyEditFinished();
                }
                return;
            }

            if (e.Key is VirtualKey.Control or VirtualKey.Shift or VirtualKey.Menu or VirtualKey.LeftWindows or VirtualKey.RightWindows)
            {
                _editingModifiers &= e.Key switch
                {
                    VirtualKey.Control => ~VirtualKeyModifiers.Control,
                    VirtualKey.Shift => ~VirtualKeyModifiers.Shift,
                    VirtualKey.Menu => ~VirtualKeyModifiers.Menu,
                    VirtualKey.LeftWindows or VirtualKey.RightWindows => ~VirtualKeyModifiers.Windows,
                    _ => ~VirtualKeyModifiers.None,
                };
            }
            else if (e.Key == _editingKey)
            {
                _editingKey = VirtualKey.None;
            }
            UpdateText();
        }
        catch { }
    }


    private void Button_DeleteHotkey_Click(object sender, RoutedEventArgs e)
    {
        Modifiers = VirtualKeyModifiers.None;
        Key = VirtualKey.None;
        _editingModifiers = VirtualKeyModifiers.None;
        _editingKey = VirtualKey.None;
        HotkeyDeleted?.Invoke(this, new HotkeyInputEventArg
        {
            WindowHandle = WindowHandle,
            HotkeyId = HotkeyId,
            Modifiers = 0,
            Key = 0,
            fsModifiers = 0,
        });
        UpdateText();
        State = HoykeyInputState.None;
    }



    private List<VirtualKey> _pressedKeys = new();

    private VirtualKeyModifiers _editingModifiers;

    private VirtualKey _editingKey;


    private void UpdateText()
    {
        HotkeyText = GetHotkeyText(Modifiers, Key);
        EditingText = GetHotkeyText(_editingModifiers, _editingKey) ?? "Press Hotkey";
    }


    private static string? GetHotkeyText(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        if (modifiers == 0 && key >= VirtualKey.F1 && key <= VirtualKey.F10)
        {
            return AvaliableKeyDict.GetValueOrDefault(key)!;
        }
        else if (AvaliableKeyDict.TryGetValue(key, out string? name) || modifiers > 0)
        {
            var sb = new StringBuilder();
            if (modifiers.HasFlag(VirtualKeyModifiers.Windows))
            {
                sb.Append("Win + ");
            }
            if (modifiers.HasFlag(VirtualKeyModifiers.Control))
            {
                sb.Append("Ctrl + ");
            }
            if (modifiers.HasFlag(VirtualKeyModifiers.Menu))
            {
                sb.Append("Alt + ");
            }
            if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
            {
                sb.Append("Shift + ");
            }
            if (!string.IsNullOrWhiteSpace(name))
            {
                sb.Append($"{name} + ");
            }
            return sb.ToString(0, sb.Length - 3);
        }
        else
        {
            return null;
        }
    }


    private static bool IsHotkeyAvaliable(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        if (modifiers == 0 && key >= VirtualKey.F1 && key <= VirtualKey.F10)
        {
            return true;
        }
        else if (modifiers > 0 && AvaliableKeyDict.ContainsKey(key))
        {
            return true;
        }
        return false;
    }


    private static VirtualKeyModifiers GetKeyModifiers()
    {
        VirtualKeyModifiers modifiers = 0;
        if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
        {
            modifiers |= VirtualKeyModifiers.Control;
        }
        if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
        {
            modifiers |= VirtualKeyModifiers.Shift;
        }
        if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down))
        {
            modifiers |= VirtualKeyModifiers.Menu;
        }
        if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftWindows).HasFlag(CoreVirtualKeyStates.Down)
            || InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.RightWindows).HasFlag(CoreVirtualKeyStates.Down))
        {
            modifiers |= VirtualKeyModifiers.Windows;
        }
        return modifiers;
    }


    private void OnHotkeyEditing()
    {
        HotkeyEditing?.Invoke(this, new HotkeyInputEventArg
        {
            WindowHandle = WindowHandle,
            HotkeyId = HotkeyId,
            Modifiers = Modifiers,
            Key = Key,
            fsModifiers = ChangeModifiesBit((uint)Modifiers),
        });
    }


    private void OnHotkeyEditFinished()
    {
        if (IsHotkeyAvaliable(_editingModifiers, _editingKey))
        {
            var e = new HotkeyEditFinishedEventArg
            {
                WindowHandle = WindowHandle,
                HotkeyId = HotkeyId,
                Modifiers = _editingModifiers,
                Key = _editingKey,
                fsModifiers = ChangeModifiesBit((uint)_editingModifiers),
                HotkeyAvaliable = true,
                HotkeyChanged = Modifiers != _editingModifiers || Key != _editingKey,
            };
            Modifiers = _editingModifiers;
            Key = _editingKey;
            UpdateText();
            State = HoykeyInputState.None;
            HotkeyEditFinished?.Invoke(this, e);
        }
        else
        {
            var e = new HotkeyEditFinishedEventArg
            {
                WindowHandle = WindowHandle,
                HotkeyId = HotkeyId,
                Modifiers = Modifiers,
                Key = Key,
                fsModifiers = ChangeModifiesBit((uint)Modifiers),
                HotkeyAvaliable = IsHotkeyAvaliable(Modifiers, Key),
                HotkeyChanged = false,
            };
            UpdateText();
            State = HoykeyInputState.None;
            HotkeyEditFinished?.Invoke(this, e);
        }

    }


    private static uint ChangeModifiesBit(uint modifiers)
    {
        uint bit0 = (modifiers & 0b01);
        uint bit1 = (modifiers & 0b10);
        uint mod = (uint)(modifiers & ~0b11);
        return mod | (bit0 << 1) | (bit1 >> 1);
    }


    private static readonly Dictionary<VirtualKey, string> AvaliableKeyDict = new()
    {
        [VirtualKey.Shift] = "Shift",
        [VirtualKey.Control] = "Ctrl",
        [VirtualKey.Menu] = "Alt",
        [VirtualKey.LeftWindows] = "Win",
        [VirtualKey.RightWindows] = "Win",

        [VirtualKey.PageUp] = "PageUp",
        [VirtualKey.PageDown] = "PageDown",
        [VirtualKey.End] = "End",
        [VirtualKey.Home] = "Home",
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
    };

}


public enum HoykeyInputState
{
    None = 0,
    Edit = 1,
    Success = 2,
    Warning = 3,
}


public class HotkeyInputEventArg : EventArgs
{
    public required nint WindowHandle { get; init; }

    public required int HotkeyId { get; init; }

    public required VirtualKeyModifiers Modifiers { get; init; }

    public required VirtualKey Key { get; init; }

    public required uint fsModifiers { get; init; }
}



public class HotkeyEditFinishedEventArg : HotkeyInputEventArg
{
    public required bool HotkeyAvaliable { get; set; }

    public required bool HotkeyChanged { get; set; }
}