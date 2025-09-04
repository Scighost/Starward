using Microsoft.Graphics.Canvas;
using Starward.Codec.AVIF;
using Starward.Codec.JpegXL;
using Starward.Codec.JpegXL.CMS;
using Starward.Codec.JpegXL.Decode;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.DirectX;

namespace Starward.Features.Screenshot;

internal static class ImageLoader
{



    public static async Task<CanvasBitmap> LoadCanvasBitmapAsync(string filePath, CancellationToken cancellation = default)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension is ".avif")
        {
            return await LoadAvifAsync(filePath, cancellation);
        }
        else if (extension is ".jxl")
        {
            return await LoadJxlAsync(filePath, cancellation);
        }
        else
        {
            return await CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice(), filePath).AsTask(cancellation);
        }
    }



    private static async Task<CanvasBitmap> LoadAvifAsync(string path, CancellationToken cancellationToken = default)
    {
        using var decoder = avifDecoderLite.Create(await File.ReadAllBytesAsync(path, cancellationToken));
        int width = (int)decoder.Width;
        int height = (int)decoder.Height;
        uint depth = decoder.Depth > 8u ? 16u : 8u;
        bool hdr10 = decoder.ColorPrimaries is avifColorPrimaries.BT2020 && decoder.TransferCharacteristics is avifTransferCharacteristics.PQ && depth == 16;
        var pixelBytes = await decoder.GetAvifRGBPixelBytesAsync(depth, avifRGBFormat.RGBA, cancellationToken);

        if (hdr10)
        {
            using var bitmap = CanvasBitmap.CreateFromBytes(CanvasDevice.GetSharedDevice(), pixelBytes, width, height, DirectXPixelFormat.R16G16B16A16UIntNormalized);
            var renderTarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96, DirectXPixelFormat.R16G16B16A16Float, CanvasAlphaMode.Premultiplied);
            using var ds = renderTarget.CreateDrawingSession();
            var effect = new HDR10ToScRGBEffect
            {
                Source = bitmap,
                BufferPrecision = CanvasBufferPrecision.Precision16Float,
            };
            ds.DrawImage(effect);
            return renderTarget;
        }
        else
        {
            return CanvasBitmap.CreateFromBytes(CanvasDevice.GetSharedDevice(), pixelBytes, width, height, depth == 8 ? DirectXPixelFormat.R8G8B8A8UIntNormalized : DirectXPixelFormat.R16G16B16A16UIntNormalized);
        }
    }




    private static async Task<CanvasBitmap> LoadJxlAsync(string path, CancellationToken cancellation = default)
    {
        using var decoder = JxlDecoderLite.Create(await File.ReadAllBytesAsync(path, cancellation));
        int width = (int)decoder.Width;
        int height = (int)decoder.Height;
        var color = decoder.ColorEncoding;
        var pixelFormat = decoder.PixelFormat;
        bool hdr10 = color == JxlColorEncoding.HDR10 && pixelFormat.DataType != JxlDataType.UInt8;

        if (pixelFormat.DataType is JxlDataType.UInt8)
        {
            var pixelBytes = await decoder.GetJxlPixelBytesAsync(JxlPixelFormat.R8G8B8A8UInt, cancellation);
            return CanvasBitmap.CreateFromBytes(CanvasDevice.GetSharedDevice(), pixelBytes, width, height, DirectXPixelFormat.R8G8B8A8UIntNormalized);
        }
        else if (pixelFormat.DataType is JxlDataType.UInt16)
        {
            var pixelBytes = await decoder.GetJxlPixelBytesAsync(JxlPixelFormat.R16G16B16A16UInt, cancellation);
            if (hdr10)
            {
                using var bitmap = CanvasBitmap.CreateFromBytes(CanvasDevice.GetSharedDevice(), pixelBytes, width, height, DirectXPixelFormat.R16G16B16A16UIntNormalized);
                var renderTarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96, DirectXPixelFormat.R16G16B16A16Float, CanvasAlphaMode.Premultiplied);
                using var ds = renderTarget.CreateDrawingSession();
                var effect = new HDR10ToScRGBEffect
                {
                    Source = bitmap,
                    BufferPrecision = CanvasBufferPrecision.Precision16Float,
                };
                ds.DrawImage(effect);
                return renderTarget;
            }
            else
            {
                return CanvasBitmap.CreateFromBytes(CanvasDevice.GetSharedDevice(), pixelBytes, width, height, DirectXPixelFormat.R16G16B16A16UIntNormalized);
            }
        }
        else if (pixelFormat.DataType is JxlDataType.Float or JxlDataType.Float16)
        {
            var pixelBytes = await decoder.GetJxlPixelBytesAsync(JxlPixelFormat.R16G16B16A16Float, cancellation);
            if (hdr10)
            {
                using var bitmap = CanvasBitmap.CreateFromBytes(CanvasDevice.GetSharedDevice(), pixelBytes, width, height, DirectXPixelFormat.R16G16B16A16Float);
                var renderTarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96, DirectXPixelFormat.R16G16B16A16Float, CanvasAlphaMode.Premultiplied);
                using var ds = renderTarget.CreateDrawingSession();
                var effect = new HDR10ToScRGBEffect
                {
                    Source = bitmap,
                    BufferPrecision = CanvasBufferPrecision.Precision16Float,
                };
                ds.DrawImage(effect);
                return renderTarget;
            }
            else
            {
                return CanvasBitmap.CreateFromBytes(CanvasDevice.GetSharedDevice(), pixelBytes, width, height, DirectXPixelFormat.R16G16B16A16Float);
            }
        }
        else
        {
            throw new NotSupportedException("Unsupported JXL pixel format: " + pixelFormat);
        }
    }




    private static async Task<byte[]> GetAvifRGBPixelBytesAsync(this avifDecoderLite decoder, uint depth, avifRGBFormat format, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() =>
        {
            using var image = decoder.GetNextImage();
            using var rgb = image.ToRGBImage(depth, format);
            return rgb.GetPixelBytes().ToArray();
        }, cancellationToken);
    }



    private static async Task<byte[]> GetJxlPixelBytesAsync(this JxlDecoderLite decoder, JxlPixelFormat pixelFormat, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(() => decoder.GetPixelBytes(pixelFormat), cancellationToken);
    }





}
