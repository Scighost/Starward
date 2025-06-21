using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;

namespace Starward.Features.Screenshot;

internal class ScreenCaptureContext
{


    public nint WindowHandle { get; private set; }

    public GraphicsCaptureItem CaptureItem { get; private set; }

    public Direct3D11CaptureFramePool? FramePool { get; private set; }

    public GraphicsCaptureSession? CaptureSession { get; private set; }



    public event EventHandler CaptureWindowClosed;



    private Lock _lock = new();

    private SizeInt32 lastSize;

    private bool isStartCapture;

    public bool _windowClosed;

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
        CaptureSession?.Dispose();
        CaptureSession = null;
        FramePool?.Dispose();
        FramePool = null;
        _windowClosed = true;
        CaptureWindowClosed?.Invoke(this, EventArgs.Empty);
    }


    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        Debug.WriteLine("Frame arrived");
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
                    CaptureSession?.Dispose();
                    CaptureSession = null;
                    isStartCapture = false;
                    RecreateResource();
                }
            }
            else
            {
                FramePool?.Recreate(CanvasDevice.GetSharedDevice(), DirectXPixelFormat.R16G16B16A16Float, 2, frame.ContentSize);
                lastSize = frame.ContentSize;
                frame.Dispose();
            }
        }
    }





    public async Task<Direct3D11CaptureFrame> CaptureAsync(CancellationToken cancellationToken = default)
    {
        if (_windowClosed)
        {
            throw new InvalidOperationException("The capture context has been closed and cannot be used.");
        }
        if (frameCompletionQueue.Count >= 5)
        {
            throw new Exception("Too many pending capture requests, please try again later.");
        }
        var completionSource = new TaskCompletionSource<Direct3D11CaptureFrame>();
        cancellationToken.Register(() => completionSource.TrySetCanceled());
        frameCompletionQueue.Enqueue(completionSource);
        if (CaptureSession is null)
        {
            RecreateResource();
        }
        if (!isStartCapture)
        {
            CaptureSession!.StartCapture();
            isStartCapture = true;
        }
        return await completionSource.Task.ConfigureAwait(false);
    }



    public void RecreateResource()
    {
        lock (_lock)
        {
            if (CaptureSession is not null)
            {
                return;
            }
            FramePool?.Dispose();
            FramePool = null;
            if (_windowClosed)
            {
                return;
            }
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



}




