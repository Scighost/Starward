using Microsoft.Graphics.Canvas;
using Starward.Codec.AVIF;
using Starward.Codec.ICC;
using Starward.Codec.JpegXL;
using Starward.Codec.JpegXL.CMS;
using Starward.Codec.JpegXL.Decode;
using Starward.Codec.PNG;
using Starward.Codec.UltraHdr;
using System;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.DirectX;
using Windows.Graphics.Imaging;

namespace Starward.Features.Codec;

internal static class ImageLoader
{



    public static async Task<ImageInfo> LoadImageAsync(string filePath, CancellationToken cancellation = default)
    {
        ImageInfo info;
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension is ".avif")
        {
            info = await LoadAvifAsync(filePath, cancellation);
        }
        else if (extension is ".jxl")
        {
            info = await LoadJxlAsync(filePath, cancellation);
        }
        else if (extension is ".png")
        {
            info = await LoadPngAsync(filePath, cancellation);
        }
        else if (extension is ".jpg")
        {
            info = await LoadJpegAsync(filePath, cancellation);
        }
        else
        {
            using var fs = File.OpenRead(filePath);
            info = new()
            {
                ColorPrimaries = ColorPrimaries.BT709,
                CanvasBitmap = await CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice(), fs.AsRandomAccessStream()).AsTask(cancellation),
            };
            info.HDR = info.CanvasBitmap.Format is DirectXPixelFormat.R16G16B16A16Float or DirectXPixelFormat.R32G32B32A32Float;
            return info;
        }
        if (info.ColorPrimaries is null || !info.ColorPrimaries.IsValid)
        {
            info.ColorPrimaries = ColorPrimaries.BT709;
        }
        return info;
    }



    private static async Task<ImageInfo> LoadAvifAsync(string path, CancellationToken cancellationToken = default)
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
            return new ImageInfo
            {
                CanvasBitmap = renderTarget,
                HDR = true,
                ColorPrimaries = ColorPrimaries.BT709,
            };
        }
        else
        {
            ReadOnlySpan<byte> iccData = decoder.GetIccData();
            ColorPrimaries? colorPrimaries = null;
            try
            {
                if (iccData.Length > 0)
                {
                    colorPrimaries = ICCHelper.GetColorPrimariesFromIccData(iccData);
                }
                else
                {
                    colorPrimaries = decoder.ColorPrimaries switch
                    {
                        avifColorPrimaries.BT709 => ColorPrimaries.BT709,
                        avifColorPrimaries.DCI_P3 => ColorPrimaries.DisplayP3,
                        _ => null,
                    };
                }
            }
            catch { }

            var bitmap = CanvasBitmap.CreateFromBytes(CanvasDevice.GetSharedDevice(), pixelBytes, width, height, depth == 8 ? DirectXPixelFormat.R8G8B8A8UIntNormalized : DirectXPixelFormat.R16G16B16A16UIntNormalized);
            return new ImageInfo
            {
                CanvasBitmap = bitmap,
                HDR = false,
                ColorPrimaries = colorPrimaries ?? ColorPrimaries.BT709,
                IccData = iccData.Length > 0 ? iccData.ToArray() : null,
            };
        }
    }


    private static async Task<ImageInfo> LoadJxlAsync(string path, CancellationToken cancellation = default)
    {
        using var decoder = JxlDecoderLite.Create(await File.ReadAllBytesAsync(path, cancellation));
        int width = (int)decoder.Width;
        int height = (int)decoder.Height;
        var color = decoder.ColorEncoding;
        var pixelFormat = decoder.PixelFormat;
        bool hdr10 = color == JxlColorEncoding.HDR10 && pixelFormat.DataType != JxlDataType.UInt8;
        ImageInfo info = new() { HDR = hdr10 };
        if (hdr10)
        {
            info.ColorPrimaries = ColorPrimaries.BT709;
        }
        else
        {
            try
            {
                var icc = decoder.GetICCData();
                if (icc is not null)
                {
                    info.IccData = icc;
                    info.ColorPrimaries = ICCHelper.GetColorPrimariesFromIccData(icc);
                }
                else
                {
                    info.ColorPrimaries = color.Primaries switch
                    {
                        JxlPrimaries.sRGB => ColorPrimaries.BT709,
                        JxlPrimaries.P3 => ColorPrimaries.DisplayP3,
                        JxlPrimaries.BT2100 => ColorPrimaries.BT2020,
                        JxlPrimaries.Custom => new ColorPrimaries
                        {
                            Red = new Vector2((float)color.PrimariesRedXY.X, (float)color.PrimariesRedXY.Y),
                            Green = new Vector2((float)color.PrimariesGreenXY.X, (float)color.PrimariesGreenXY.Y),
                            Blue = new Vector2((float)color.PrimariesBlueXY.X, (float)color.PrimariesBlueXY.Y),
                            White = new Vector2((float)color.WhitePointXY.X, (float)color.WhitePointXY.Y),
                        },
                        _ => ColorPrimaries.BT709,
                    };
                }
            }
            catch { }
        }

        if (pixelFormat.DataType is JxlDataType.UInt8)
        {
            var pixelBytes = await decoder.GetJxlPixelBytesAsync(JxlPixelFormat.R8G8B8A8UInt, cancellation);
            info.CanvasBitmap = CanvasBitmap.CreateFromBytes(CanvasDevice.GetSharedDevice(), pixelBytes, width, height, DirectXPixelFormat.R8G8B8A8UIntNormalized);
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
                info.CanvasBitmap = renderTarget;
            }
            else
            {
                info.CanvasBitmap = CanvasBitmap.CreateFromBytes(CanvasDevice.GetSharedDevice(), pixelBytes, width, height, DirectXPixelFormat.R16G16B16A16UIntNormalized);
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
                info.CanvasBitmap = renderTarget;
            }
            else
            {
                info.CanvasBitmap = CanvasBitmap.CreateFromBytes(CanvasDevice.GetSharedDevice(), pixelBytes, width, height, DirectXPixelFormat.R16G16B16A16Float);
            }
        }
        else
        {
            throw new NotSupportedException("Unsupported JXL pixel format: " + pixelFormat);
        }
        return info;
    }


    private static async Task<ImageInfo> LoadPngAsync(string path, CancellationToken cancellationToken = default)
    {
        var info = new ImageInfo();
        using var fs = File.OpenRead(path);
        var reader = new PngReader(fs);
        PngChunk chunk;
        ColorPrimaries? colorPrimaries = null;
        while ((chunk = reader.GetNextChunk()).Type != PngChunkType.IEND)
        {
            try
            {
                // https://www.w3.org/TR/png-3/#4Concepts.ColourSpaces
                if (chunk.Type == PngChunkType.cICP)
                {
                    PngcICPChunk cicp = chunk.GetcICPChunk();
                    colorPrimaries = cicp.ColorPrimaries switch
                    {
                        1 => ColorPrimaries.BT709,
                        9 => ColorPrimaries.BT2020,
                        12 => ColorPrimaries.DisplayP3,
                        _ => null,
                    };
                    break;
                }
                if (chunk.Type == PngChunkType.iCCP)
                {
                    byte[] icc = chunk.GetiCCPChunk(out string profileName);
                    info.IccData = icc;
                    colorPrimaries = ICCHelper.GetColorPrimariesFromIccData(icc);
                    break;
                }
                if (chunk.Type == PngChunkType.sRGB)
                {
                    colorPrimaries = ColorPrimaries.BT709;
                    break;
                }
                if (chunk.Type == PngChunkType.cHRM)
                {
                    PngcHRMChunk chrm = chunk.GetcHRMChunk();
                    colorPrimaries = new ColorPrimaries
                    {
                        Red = new Vector2(chrm.RedX, chrm.RedY),
                        Green = new Vector2(chrm.GreenX, chrm.GreenY),
                        Blue = new Vector2(chrm.BlueX, chrm.BlueY),
                        White = new Vector2(chrm.WhitePointX, chrm.WhitePointY)
                    };
                    break;
                }
            }
            catch { }
        }

        info.ColorPrimaries = colorPrimaries ?? ColorPrimaries.BT709;
        fs.Position = 0;
        if (info.ColorPrimaries.Id == 9)
        {
            using var bitmap = await CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice(), fs.AsRandomAccessStream()).AsTask(cancellationToken);
            var renderTarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), bitmap.SizeInPixels.Width, bitmap.SizeInPixels.Height, 96, DirectXPixelFormat.R16G16B16A16Float, CanvasAlphaMode.Premultiplied);
            using var ds = renderTarget.CreateDrawingSession();
            var effect = new HDR10ToScRGBEffect
            {
                Source = bitmap,
                BufferPrecision = CanvasBufferPrecision.Precision16Float,
            };
            ds.DrawImage(effect);
            info.CanvasBitmap = renderTarget;
            info.HDR = true;
            info.ColorPrimaries = ColorPrimaries.BT709;
        }
        else
        {
            info.CanvasBitmap = await CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice(), fs.AsRandomAccessStream()).AsTask(cancellationToken);
        }
        return info;
    }


    private static async Task<ImageInfo> LoadJpegAsync(string path, CancellationToken cancellationToken = default)
    {
        var info = new ImageInfo();
        byte[] bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        try
        {
            using var decoder = UhdrDecoder.Create(bytes);
            ReadOnlySpan<byte> icc = decoder.GetIccData();
            if (icc.Length > 14)
            {
                info.ColorPrimaries = ICCHelper.GetColorPrimariesFromIccData(icc[14..]);
            }
            else
            {
                info.ColorPrimaries = ColorPrimaries.BT709;
            }
        }
        catch { }
        info.CanvasBitmap = await CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice(), new MemoryStream(bytes).AsRandomAccessStream()).AsTask(cancellationToken);
        return info;
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



    public static async Task<(uint Width, uint Height)> GetImagePixelSizeAsync(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        using var fs = File.OpenRead(filePath);
        if (extension is ".avif" && !ImageThumbnail.AvifDecoderSupported)
        {
            using var decoder = await avifDecoderLite.CreateAsync(fs);
            return (decoder.Width, decoder.Height);
        }
        else if (extension is ".jxl" && !ImageThumbnail.JxlDecoderSupported)
        {
            using var decoder = await JxlDecoderLite.CreateAsync(fs);
            return (decoder.Width, decoder.Height);
        }
        else
        {
            var decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream());
            return (decoder.PixelWidth, decoder.PixelHeight);
        }
    }


}
