using Microsoft.Graphics.Canvas;
using Starward.Codec.ICC;
using System;

namespace Starward.Features.Codec;

internal class ImageInfo : IDisposable
{

    public CanvasBitmap CanvasBitmap { get; set; }

    public bool HDR { get; set; }

    public ColorPrimaries ColorPrimaries { get; set; }

    public byte[]? IccData { get; set; }



    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
            }
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