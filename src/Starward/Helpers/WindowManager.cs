using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Linq;

namespace Starward.Helpers;

public static class WindowManager
{

    private static object lockObj = new();

    private static List<Window> _windows = new();

    private static Dictionary<string, Window> _dic = new();


    public static void Active(Window window)
    {
        window.Closed -= Window_Closed;
        window.Closed += Window_Closed;
        _windows.Add(window);
        window.Activate();
    }



    public static bool TryGetWindow(string key, out Window? window)
    {
        return _dic.TryGetValue(key, out window);
    }



    public static void Active(string key, Window window)
    {
        window.Closed -= Window_Dictionary_Closed;
        window.Closed += Window_Dictionary_Closed;
        window.Activate();
        if (_dic.TryGetValue(key, out var oldWindow))
        {
            oldWindow.Close();
        }
        _dic[key] = window;
    }


    public static void CloseAll()
    {
        foreach (var window in _windows.ToList())
        {
            window.Close();
        }
        foreach (var window in _dic.Values.ToList())
        {
            window.Close();
        }
    }



    private static void Window_Closed(object sender, WindowEventArgs args)
    {
        lock (lockObj)
        {
            if (sender is Window window)
            {
                window.Closed -= Window_Closed;
                if (_windows.Contains(window))
                {
                    _windows.Remove(window);
                }
            }
        }
    }



    private static void Window_Dictionary_Closed(object sender, WindowEventArgs args)
    {
        lock (lockObj)
        {
            if (sender is Window window)
            {
                window.Closed -= Window_Dictionary_Closed;
                if (_dic.ContainsValue(window))
                {
                    var kv = _dic.First(x => x.Value == window);
                    _dic.Remove(kv.Key);
                }
            }
        }
    }




}
