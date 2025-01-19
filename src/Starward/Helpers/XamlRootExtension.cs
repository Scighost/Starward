using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Linq;
using Vanara.PInvoke;
using Windows.Foundation;
using Windows.Graphics;

namespace Starward.Helpers;

internal static class XamlRootExtension
{



    public static void SetWindowDragRectangles(this XamlRoot xamlRoot, params RectInt32[] rects)
    {
        WindowId id = xamlRoot.ContentIslandEnvironment.AppWindowId;
        AppWindow appWindow = AppWindow.GetFromWindowId(id);
        appWindow.TitleBar.SetDragRectangles(rects);
    }



    public static void SetWindowDragRectangles(this XamlRoot xamlRoot, params Rect[] rects)
    {
        if (xamlRoot is null)
        {
            return;
        }
        WindowId id = xamlRoot.ContentIslandEnvironment.AppWindowId;
        AppWindow appWindow = AppWindow.GetFromWindowId(id);
        double scale = User32.GetDpiForWindow((nint)id.Value) / 96d;
        var value = rects.Select(rect => RectToRectInt32(rect, scale)).ToArray();
        appWindow.TitleBar.SetDragRectangles(value);
    }



    private static RectInt32 RectToRectInt32(Rect rect, double scale = 1)
    {
        return new RectInt32((int)(rect.X * scale), (int)(rect.Y * scale), (int)(rect.Width * scale), (int)(rect.Height * scale));
    }



    public static double GetUIScaleFactor(this XamlRoot xamlRoot)
    {
        if (xamlRoot is null)
        {
            return 1;
        }

        WindowId id = xamlRoot.ContentIslandEnvironment.AppWindowId;
        return User32.GetDpiForWindow((nint)id.Value) / 96d;
    }



    public static AppWindow GetAppWindow(this XamlRoot xamlRoot)
    {
        return AppWindow.GetFromWindowId(xamlRoot.ContentIslandEnvironment.AppWindowId);
    }



    public static nint GetWindowHandle(this XamlRoot xamlRoot)
    {
        return (nint)xamlRoot.GetAppWindow().Id.Value;
    }


}
