using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;

namespace Starward.Features.Screenshot;

internal class ScreenCaptureContext : IDisposable
{

    public nint WindowHandle { get; private set; }

    public GraphicsCaptureItem CaptureItem { get; private set; }

    public Direct3D11CaptureFramePool? FramePool { get; private set; }

    public GraphicsCaptureSession? CaptureSession { get; private set; }


    public event EventHandler CaptureWindowClosed;


    private Lock _lock = new();

    private SizeInt32 lastSize;

    private bool _windowClosed;

    private readonly ConcurrentQueue<TaskCompletionSource<Direct3D11CaptureFrame>> frameCompletionQueue = new();



    public ScreenCaptureContext(nint hwnd)
    {
        if (!User32.IsWindow(hwnd))
        {
            throw new ArgumentException("The provided handle is not a valid window handle.", nameof(hwnd));
        }
        WindowHandle = hwnd;
        CaptureItem = ScreenCaptureHelper.CreateGraphicsCaptureItemForWindow(hwnd);
        lastSize = CaptureItem.Size;
        CaptureItem.Closed += OnCaptureItemClosed;
        RecreateResource();
    }


    private void OnCaptureItemClosed(GraphicsCaptureItem sender, object args)
    {
        Dispose();
        _windowClosed = true;
        CaptureWindowClosed?.Invoke(this, EventArgs.Empty);
    }


    public async Task<Direct3D11CaptureFrame> CaptureAsync(CancellationToken cancellationToken = default)
    {
        if (_windowClosed)
        {
            throw new InvalidOperationException("The capture context has been closed and cannot be used.");
        }
        var completionSource = new TaskCompletionSource<Direct3D11CaptureFrame>();
        cancellationToken.Register(() => completionSource.TrySetCanceled());
        frameCompletionQueue.Enqueue(completionSource);
        StartCapture();
        return await completionSource.Task.ConfigureAwait(false);
    }


    private void StartCapture()
    {
        try
        {
            if (CaptureSession is null)
            {
                Dispose();
                RecreateResource();
            }
            CaptureSession!.StartCapture();
        }
        catch (ObjectDisposedException)
        {
            RecreateResource();
            CaptureSession!.StartCapture();
        }
    }


    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        if (sender.TryGetNextFrame() is Direct3D11CaptureFrame frame)
        {
            if (lastSize == frame.ContentSize)
            {
                TaskCompletionSource<Direct3D11CaptureFrame>? completionSource;
                while (frameCompletionQueue.TryDequeue(out completionSource))
                {
                    if (!completionSource.Task.IsCompleted)
                    {
                        break;
                    }
                }
                completionSource?.TrySetResult(frame);
                if (frameCompletionQueue.IsEmpty)
                {
                    Dispose();
                    RecreateResource();
                }
            }
            else
            {
                SizeInt32 contentSize = frame.ContentSize;
                frame.Dispose();
                sender.Recreate(CanvasDevice.GetSharedDevice(), DirectXPixelFormat.R16G16B16A16Float, 2, contentSize);
                lastSize = contentSize;
            }
        }
    }


    public void RecreateResource()
    {
        lock (_lock)
        {
            FramePool = Direct3D11CaptureFramePool.CreateFreeThreaded(CanvasDevice.GetSharedDevice(), DirectXPixelFormat.R16G16B16A16Float, 2, lastSize);
            FramePool.FrameArrived += OnFrameArrived;
            CaptureSession = FramePool.CreateCaptureSession(CaptureItem);
#pragma warning disable CA1416 // 验证平台兼容性
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
                CaptureSession.IsCursorCaptureEnabled = false;
            }
#pragma warning restore CA1416 // 验证平台兼容性
        }
    }

    public void Dispose()
    {
        FramePool?.Dispose();
        CaptureSession?.Dispose();
        FramePool = null;
        CaptureSession = null;
    }

}




