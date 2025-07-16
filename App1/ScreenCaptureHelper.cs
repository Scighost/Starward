using Microsoft.Graphics.Canvas;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Foundation.Metadata;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.UI;

namespace App1;

internal class ScreenCaptureHelper
{



    public static readonly bool IsTryCreateFromWindowIdPresent = ApiInformation.IsMethodPresent("Windows.Graphics.Capture.GraphicsCaptureItem", "TryCreateFromWindowId");

    public static readonly bool IsIncludeSecondaryWindowsPresent = ApiInformation.IsPropertyPresent("Windows.Graphics.Capture.GraphicsCaptureSession", "IncludeSecondaryWindows");

    public static readonly bool IsIsBorderRequiredPresent = ApiInformation.IsPropertyPresent("Windows.Graphics.Capture.GraphicsCaptureSession", "IsBorderRequired");

    public static readonly bool IsIsCursorCaptureEnabledPresent = ApiInformation.IsPropertyPresent("Windows.Graphics.Capture.GraphicsCaptureSession", "IsCursorCaptureEnabled");




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



    [ComImport]
    [System.Runtime.InteropServices.Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    private interface IGraphicsCaptureItemInterop
    {
        IntPtr CreateForWindow(
            [In] IntPtr window,
            [In] in Guid iid);

        IntPtr CreateForMonitor(
            [In] IntPtr monitor,
            [In] in Guid iid);
    }






}



internal class ScreenCaptureContext
{


    public nint WindowHandle { get; set; }

    public GraphicsCaptureItem CaptureItem { get; set; }

    public Direct3D11CaptureFramePool FramePool { get; set; }

    public GraphicsCaptureSession CaptureSession { get; set; }


    private SizeInt32 lastSize;

    private TaskCompletionSource<Direct3D11CaptureFrame> frameTaskCompletionSource;


    public ScreenCaptureContext(nint hwnd)
    {
        if (!User32.IsWindow(hwnd))
        {
            throw new ArgumentException("The provided handle is not a valid window handle.", nameof(hwnd));
        }
        WindowHandle = hwnd;
        CaptureItem = ScreenCaptureHelper.CreateGraphicsCaptureItemForWindow(hwnd);
        lastSize = CaptureItem.Size;
        RecreateResource();
    }



    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        if (frameTaskCompletionSource is null)
        {
            return;
        }
        if (sender.TryGetNextFrame() is Direct3D11CaptureFrame frame)
        {
            if (lastSize == frame.ContentSize)
            {
                CaptureSession.Dispose();
                frameTaskCompletionSource.TrySetResult(frame);
            }
            else
            {
                FramePool.Recreate(CanvasDevice.GetSharedDevice(), DirectXPixelFormat.R16G16B16A16Float, 1, frame.ContentSize);
                lastSize = frame.ContentSize;
            }
        }
    }




    public async Task<Direct3D11CaptureFrame> CaptureAsync(CancellationToken cancellationToken)
    {
        try
        {
            long ts = Stopwatch.GetTimestamp();
            frameTaskCompletionSource = new();
            cancellationToken.Register(() => frameTaskCompletionSource.TrySetCanceled());
            CaptureSession.StartCapture();
            var result = await frameTaskCompletionSource.Task;
            long frameTs = Stopwatch.GetTimestamp() - ts;
            var time = (double)frameTs / Stopwatch.Frequency * 1000;
            Debug.WriteLine(time.ToString());
            return result;
        }
        finally
        {
            _ = Task.Run(RecreateResource, CancellationToken.None);
        }
    }




    public void RecreateResource()
    {
        FramePool?.Dispose();
        CaptureSession?.Dispose();
        FramePool = Direct3D11CaptureFramePool.CreateFreeThreaded(CanvasDevice.GetSharedDevice(), DirectXPixelFormat.R16G16B16A16Float, 1, lastSize);
        FramePool.FrameArrived += OnFrameArrived;
        CaptureSession = FramePool.CreateCaptureSession(CaptureItem);
        if (ScreenCaptureHelper.IsIncludeSecondaryWindowsPresent)
        {
            CaptureSession.IncludeSecondaryWindows = true;
        }
        if (ScreenCaptureHelper.IsIsBorderRequiredPresent)
        {
            CaptureSession.IsBorderRequired = false;
        }
        if (ScreenCaptureHelper.IsIsCursorCaptureEnabledPresent)
        {
#pragma warning disable CA1416 // 验证平台兼容性
            CaptureSession.IsCursorCaptureEnabled = false;
#pragma warning restore CA1416 // 验证平台兼容性
        }
    }




}
