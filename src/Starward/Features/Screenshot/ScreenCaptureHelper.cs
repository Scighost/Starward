using Microsoft.Graphics.Canvas;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.UI;

namespace Starward.Features.Screenshot;

internal partial class ScreenCaptureHelper
{

    public static readonly bool IsTryCreateFromWindowIdPresent = ApiInformation.IsMethodPresent("Windows.Graphics.Capture.GraphicsCaptureItem", "TryCreateFromWindowId");

    public static readonly bool IsIncludeSecondaryWindowsPresent = ApiInformation.IsPropertyPresent("Windows.Graphics.Capture.GraphicsCaptureSession", "IncludeSecondaryWindows");

    public static readonly bool IsIsBorderRequiredPresent = ApiInformation.IsPropertyPresent("Windows.Graphics.Capture.GraphicsCaptureSession", "IsBorderRequired");

    public static readonly bool IsIsCursorCaptureEnabledPresent = ApiInformation.IsPropertyPresent("Windows.Graphics.Capture.GraphicsCaptureSession", "IsCursorCaptureEnabled");



    public static async Task<Direct3D11CaptureFrame> CaptureWindowAsync(nint hwnd, CancellationToken cancellationToken = default)
    {
        if (!User32.IsWindow(hwnd))
        {
            throw new ArgumentException("The provided handle is not a valid window handle.", nameof(hwnd));
        }
        if (User32.IsIconic(hwnd))
        {
            throw new InvalidOperationException("Cannot capture a minimized window.");
        }
        GraphicsCaptureItem item = CreateGraphicsCaptureItemForWindow(hwnd);
        using Direct3D11CaptureFramePool framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(CanvasDevice.GetSharedDevice(), DirectXPixelFormat.R16G16B16A16Float, 1, item.Size);
        using GraphicsCaptureSession session = framePool.CreateCaptureSession(item);
#pragma warning disable CA1416 // 验证平台兼容性
        if (IsIncludeSecondaryWindowsPresent)
        {
            session.IncludeSecondaryWindows = true;
        }
        if (IsIsBorderRequiredPresent)
        {
            session.IsBorderRequired = false;
        }
        if (IsIsCursorCaptureEnabledPresent)
        {
            session.IsCursorCaptureEnabled = false;
        }
#pragma warning restore CA1416 // 验证平台兼容性
        var completionSource = new TaskCompletionSource<Direct3D11CaptureFrame>();
        cancellationToken.Register(() => completionSource.TrySetCanceled());
        framePool.FrameArrived += (s, _) =>
        {
            if (s.TryGetNextFrame() is Direct3D11CaptureFrame frame)
            {
                session.Dispose();
                completionSource.SetResult(frame);
            }
        };
        session.StartCapture();
        return await completionSource.Task.ConfigureAwait(false);
    }



    public static GraphicsCaptureItem CreateGraphicsCaptureItemForWindow(nint hwnd)
    {
        GraphicsCaptureItem graphicsCaptureItem;
        if (IsTryCreateFromWindowIdPresent)
        {
            graphicsCaptureItem = GraphicsCaptureItem.TryCreateFromWindowId(new WindowId((ulong)hwnd));
        }
        else
        {
            Guid GraphicsCaptureItemGuid = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");
            nint abi = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>().CreateForWindow(hwnd, GraphicsCaptureItemGuid);
            graphicsCaptureItem = GraphicsCaptureItem.FromAbi(abi);
        }
        return graphicsCaptureItem;
    }



    [ComVisible(true)]
    [GeneratedComInterface]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [System.Runtime.InteropServices.Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    internal partial interface IGraphicsCaptureItemInterop
    {
        IntPtr CreateForWindow(IntPtr window, in Guid iid);

        IntPtr CreateForMonitor(IntPtr monitor, in Guid iid);
    }

}

