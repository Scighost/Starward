using Microsoft.Graphics.Canvas;
using Starward.Codec.ICC;
using System;

namespace Starward.Features.Screenshot;

internal class ScreenCaptureItem : IDisposable
{

    public CanvasBitmap CanvasBitmap { get; set; }

    public bool HDR { get; set; }

    public float MaxCLL { get; set; }

    public float SdrWhiteLevel { get; set; }

    public DateTimeOffset FrameTime { get; set; }

    public ColorPrimaries ColorPrimaries { get; set; }



    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
            }
            ColorPrimaries = null!;
            CanvasBitmap.Dispose();
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}