using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace Starward.Helpers;

public partial class TransparentBackdrop : SystemBackdrop
{

    private readonly WindowsSystemDispatcherQueueHelper dispatcherQueueHelper = new();

    private readonly nuint _subclassId = (nuint)Random.Shared.Next(100000, 999999);

    ComCtl32.SUBCLASSPROC _wndProcHandler;

    private nint _hwnd;


    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);

        dispatcherQueueHelper.EnsureWindowsSystemDispatcherQueueController();
        var com = new Windows.UI.Composition.Compositor();
        connectedTarget.SystemBackdrop = com.CreateColorBrush(Windows.UI.Color.FromArgb(0, 255, 255, 255));

        _hwnd = (nint)xamlRoot.ContentIslandEnvironment.AppWindowId.Value;
        _wndProcHandler = new ComCtl32.SUBCLASSPROC(WndProc);
        ComCtl32.SetWindowSubclass(_hwnd, _wndProcHandler, _subclassId, IntPtr.Zero);

        using var rgn = Gdi32.CreateRectRgn(-2, -2, -1, -1);
        DwmApi.DwmEnableBlurBehindWindow(_hwnd, new DwmApi.DWM_BLURBEHIND()
        {
            dwFlags = DwmApi.DWM_BLURBEHIND_Mask.DWM_BB_ENABLE | DwmApi.DWM_BLURBEHIND_Mask.DWM_BB_BLURREGION,
            fEnable = true,
            hRgnBlur = rgn,
        });
    }


    protected override void OnDefaultSystemBackdropConfigurationChanged(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
    {

    }


    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        base.OnTargetDisconnected(disconnectedTarget);
        disconnectedTarget.SystemBackdrop = null;
        ComCtl32.RemoveWindowSubclass(_hwnd, _wndProcHandler, _subclassId);
        DwmApi.DwmEnableBlurBehindWindow(_hwnd, new DwmApi.DWM_BLURBEHIND()
        {
            dwFlags = DwmApi.DWM_BLURBEHIND_Mask.DWM_BB_ENABLE | DwmApi.DWM_BLURBEHIND_Mask.DWM_BB_BLURREGION,
            fEnable = false,
        });
    }


    private unsafe IntPtr WndProc(HWND hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, nuint uIdSubclass, IntPtr dwRefData)
    {
        if (uMsg == (uint)User32.WindowMessage.WM_PAINT)
        {
            var hdc = User32.BeginPaint(hWnd, out var ps);
            if (hdc.IsNull) return new IntPtr(0);

            var brush = Gdi32.GetStockObject(Gdi32.StockObjectType.BLACK_BRUSH);
            User32.FillRect(hdc, ps.rcPaint, brush);
            return new IntPtr(1);
        }

        return ComCtl32.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }


    public partial class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [LibraryImport("CoreMessaging.dll")]
        private static partial int CreateDispatcherQueueController(in DispatcherQueueOptions options, out nint dispatcherQueueController);

        nint m_dispatcherQueueController;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == 0)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                _ = CreateDispatcherQueueController(options, out m_dispatcherQueueController);
            }
        }
    }


}
