using Microsoft.UI.Xaml.Controls;
using System;
using WinRT;
using WinRT.Interop;

namespace Starward.Features.Screenshot;


/// <summary>
/// 用于在 ScrollViewer 的缩放率大于 1 时忽略鼠标滚轮操作
/// </summary>
public static class ScrollViewerExtensions
{

    private static readonly Guid IScrollViewerPrivateGuid = new Guid("7cc9bcb8-947e-582a-ae91-6a76597477c9");


    public static bool GetArePointerWheelEventsIgnored(this ScrollViewer scrollViewer)
    {
        IObjectReference iScrollViewerPrivate = ((IWinRTObject)scrollViewer).NativeObject.As<IUnknownVftbl>(IScrollViewerPrivateGuid);
        return get_ArePointerWheelEventsIgnored(iScrollViewerPrivate);
    }

    public unsafe static void SetArePointerWheelEventsIgnored(this ScrollViewer scrollViewer, bool value)
    {
        IObjectReference iScrollViewerPrivate = ((IWinRTObject)scrollViewer).NativeObject.As<IUnknownVftbl>(IScrollViewerPrivateGuid);
        set_ArePointerWheelEventsIgnored(iScrollViewerPrivate, value);
    }

    private unsafe static bool get_ArePointerWheelEventsIgnored(IObjectReference _obj)
    {
        IntPtr thisPtr = _obj.ThisPtr;
        byte b = 0;
        ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, byte*, int>)(*(IntPtr*)((nint)(*(IntPtr*)(void*)thisPtr) + (nint)7 * (nint)sizeof(delegate* unmanaged[Stdcall]<IntPtr, byte*, int>))))(thisPtr, &b));
        return b != 0;
    }

    private unsafe static void set_ArePointerWheelEventsIgnored(IObjectReference _obj, bool value)
    {
        IntPtr thisPtr = _obj.ThisPtr;
        ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, byte, int>)(*(IntPtr*)((nint)(*(IntPtr*)(void*)thisPtr) + (nint)8 * (nint)sizeof(delegate* unmanaged[Stdcall]<IntPtr, byte, int>))))(thisPtr, value ? ((byte)1) : ((byte)0)));
    }

}
