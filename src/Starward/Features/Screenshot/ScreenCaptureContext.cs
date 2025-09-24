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


    private DirectXPixelFormat _pixelFormat;

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


    public async Task<Direct3D11CaptureFrame> CaptureAsync(DirectXPixelFormat pixelFormat, CancellationToken cancellationToken = default)
    {
        if (_windowClosed)
        {
            throw new InvalidOperationException("The capture context has been closed and cannot be used.");
        }
        pixelFormat = ScreenCaptureHelper.IsWin10 ? DirectXPixelFormat.R8G8B8A8UIntNormalized : pixelFormat;
        if (pixelFormat != _pixelFormat)
        {
            _pixelFormat = pixelFormat;
            Dispose();
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
                    RecreateResource();
                }
            }
            else
            {
                SizeInt32 contentSize = frame.ContentSize;
                frame.Dispose();
                sender.Recreate(CanvasDevice.GetSharedDevice(), _pixelFormat, 2, contentSize);
                lastSize = contentSize;
            }
        }
    }


    public void RecreateResource()
    {
        lock (_lock)
        {
            Dispose();
            FramePool = ScreenCaptureHelper.IsWin10 ? Direct3D11CaptureFramePool.Create(CanvasDevice.GetSharedDevice(), DirectXPixelFormat.R8G8B8A8UIntNormalized, 2, lastSize)
                                                    : Direct3D11CaptureFramePool.CreateFreeThreaded(CanvasDevice.GetSharedDevice(), _pixelFormat, 2, lastSize);
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
        if (CaptureSession is not null)
        {
            CaptureSession?.Dispose();
            CaptureSession = null;
        }
        if (FramePool is not null)
        {
            FramePool.Dispose();
            FramePool = null;
        }
    }

}




