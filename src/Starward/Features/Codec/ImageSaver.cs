using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI;
using Starward.Codec;
using Starward.Codec.AVIF;
using Starward.Codec.ICC;
using Starward.Codec.JpegXL;
using Starward.Codec.JpegXL.CMS;
using Starward.Codec.JpegXL.CodeStream;
using Starward.Codec.JpegXL.Encode;
using Starward.Codec.PNG;
using Starward.Codec.UltraHdr;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics.DirectX;
using Windows.Graphics.Imaging;

namespace Starward.Features.Codec;

internal static class ImageSaver
{

    private static int GetSuggestedThreads()
    {
        int threads = Environment.ProcessorCount;
        if (threads >= 16)
        {
            return threads - 4;
        }
        else if (threads >= 8)
        {
            return threads - 2;
        }
        else
        {
            return threads;
        }
    }




    public static async Task SaveAsPngAsync(CanvasBitmap bitmap, Stream stream, ColorPrimaries colorPrimaries, byte[]? xmpData = null)
    {
        uint width = bitmap.SizeInPixels.Width;
        uint height = bitmap.SizeInPixels.Height;

        if (bitmap.Format is DirectXPixelFormat.R8G8B8A8UIntNormalized or DirectXPixelFormat.B8G8R8A8UIntNormalized)
        {
            byte[] pixelBytes = bitmap.GetPixelBytes();
            await SaveAsPngAsync(stream, width, height, bitmap.Format, pixelBytes, colorPrimaries, xmpData).ConfigureAwait(false);
        }
        else if (bitmap.Format is DirectXPixelFormat.R16G16B16A16Float or DirectXPixelFormat.R32G32B32A32Float)
        {
            using var renderTarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96, DirectXPixelFormat.R16G16B16A16UIntNormalized, CanvasAlphaMode.Premultiplied);
            using (var ds = renderTarget.CreateDrawingSession())
            {
                var effect = new ScRGBToHDR10Effect
                {
                    Source = bitmap,
                    BufferPrecision = CanvasBufferPrecision.Precision16Float,
                };
                ds.DrawImage(effect);
            }
            byte[] pixelBytes = renderTarget.GetPixelBytes();
            await SaveAsPngAsync(stream, width, height, DirectXPixelFormat.R16G16B16A16UIntNormalized, pixelBytes, ColorPrimaries.BT2020, xmpData).ConfigureAwait(false);
        }
        else
        {
            throw new NotSupportedException($"{bitmap.Format} is not supported for PNG encoding.");
        }
    }


    public static async Task SaveAsPngAsync(Stream stream, uint width, uint height, DirectXPixelFormat pixelFormat, byte[] pixelBytes, ColorPrimaries colorPrimaries, byte[]? xmpData = null)
    {
        BitmapPixelFormat format = pixelFormat switch
        {
            DirectXPixelFormat.R8G8B8A8UIntNormalized => BitmapPixelFormat.Rgba8,
            DirectXPixelFormat.B8G8R8A8UIntNormalized => BitmapPixelFormat.Bgra8,
            DirectXPixelFormat.R16G16B16A16UIntNormalized => BitmapPixelFormat.Rgba16,
            _ => throw new NotSupportedException($"{pixelFormat} is not supported for PNG encoding."),
        };

        PngChunk? cicpChunk = null;
        PngChunk? iccpChunk = null;
        PngChunk? srgbChunk = null;
        PngChunk? chrmChunk = null;
        PngChunk? itxtChunk = null;

        if (colorPrimaries.TryGetDefinedPrimaries(out int id))
        {
            if (id == 1)
            {
                srgbChunk = new PngChunk(1, PngChunkType.sRGB);
            }
            cicpChunk = new PngChunk(4, PngChunkType.cICP);
            ref PngcICPChunk cicp = ref cicpChunk.GetcICPChunk();
            cicp.ColorPrimaries = (byte)id;
            cicp.TransferFunction = (byte)(id == 9 ? 16 : 13);
            cicp.MatrixCoefficients = 0;
            cicp.FullRangeFlag = 1;
            cicpChunk.UpdateCrc32();
        }
        else
        {
            chrmChunk = new PngChunk(32, PngChunkType.cHRM);
            ref PngcHRMChunk chrm = ref chrmChunk.GetcHRMChunk();
            chrm.WhitePointX = colorPrimaries.White.X;
            chrm.WhitePointY = colorPrimaries.White.Y;
            chrm.RedX = colorPrimaries.Red.X;
            chrm.RedY = colorPrimaries.Red.Y;
            chrm.GreenX = colorPrimaries.Green.X;
            chrm.GreenY = colorPrimaries.Green.Y;
            chrm.BlueX = colorPrimaries.Blue.X;
            chrm.BlueY = colorPrimaries.Blue.Y;
            chrmChunk.UpdateCrc32();

            using var iccdata = new MemoryStream();
            using var zlib = new ZLibStream(iccdata, CompressionMode.Compress);
            zlib.Write(ICCHelper.CreateIccData(colorPrimaries));
            zlib.Flush();
            Span<byte> chunkContent = new byte[13 + iccdata.Length];
            "ICC Profile"u8.CopyTo(chunkContent);
            iccdata.Position = 0;
            iccdata.ReadExactly(chunkContent[13..]);
            iccpChunk = new PngChunk(PngChunkType.iCCP, chunkContent);
        }
        if (xmpData is not null)
        {
            Span<byte> chunkContent = new byte[22 + xmpData.Length];
            "XML:com.adobe.xmp"u8.CopyTo(chunkContent);
            xmpData.CopyTo(chunkContent[22..]);
            itxtChunk = new PngChunk(PngChunkType.iTXt, chunkContent);
        }

        using var ms = new MemoryStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, ms.AsRandomAccessStream());
        encoder.SetPixelData(format, BitmapAlphaMode.Premultiplied, width, height, 96, 96, pixelBytes);
        await encoder.FlushAsync();

        stream.Write(PngReader.PngSignature);

        ms.Position = 0;
        using var reader = new PngReader(ms);
        PngChunk currentChunk;
        bool write = false;
        while ((currentChunk = reader.GetNextChunk()).Type != PngChunkType.IEND)
        {
            if (!write && (currentChunk.Type == PngChunkType.sRGB
                           || (currentChunk.Type == PngChunkType.gAMA)
                           || currentChunk.Type == PngChunkType.PLTE
                           || currentChunk.Type == PngChunkType.IDAT))
            {
                if (cicpChunk is not null)
                {
                    stream.Write(cicpChunk.ChunkData.Span);
                }
                if (iccpChunk is not null)
                {
                    stream.Write(iccpChunk.ChunkData.Span);
                }
                if (srgbChunk is not null)
                {
                    stream.Write(srgbChunk.ChunkData.Span);
                }
                if (chrmChunk is not null)
                {
                    stream.Write(chrmChunk.ChunkData.Span);
                }
                if (itxtChunk is not null)
                {
                    stream.Write(itxtChunk.ChunkData.Span);
                }
                write = true;
            }
            if (currentChunk.Type != PngChunkType.sRGB && currentChunk.Type != PngChunkType.gAMA)
            {
                stream.Write(currentChunk.ChunkData.Span);
            }
        }

        stream.Write(PngReader.IENDSignature);
    }




    public static async Task SaveAsAvifAsync(CanvasBitmap bitmap, Stream stream, ColorPrimaries colorPrimaries, int quality, byte[]? xmpData = null)
    {
        uint width = bitmap.SizeInPixels.Width;
        uint height = bitmap.SizeInPixels.Height;

        if (bitmap.Format is DirectXPixelFormat.R8G8B8A8UIntNormalized or DirectXPixelFormat.B8G8R8A8UIntNormalized)
        {
            byte[] pixelBytes = bitmap.GetPixelBytes();
            await SaveAsAvifAsync(stream, width, height, bitmap.Format, pixelBytes, colorPrimaries, quality, xmpData).ConfigureAwait(false);
        }
        else if (bitmap.Format is DirectXPixelFormat.R16G16B16A16Float or DirectXPixelFormat.R32G32B32A32Float)
        {
            using var renderTarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96, DirectXPixelFormat.R16G16B16A16UIntNormalized, CanvasAlphaMode.Premultiplied);
            using (var ds = renderTarget.CreateDrawingSession())
            {
                var effect = new ScRGBToHDR10Effect
                {
                    Source = bitmap,
                    BufferPrecision = CanvasBufferPrecision.Precision16Float,
                };
                ds.DrawImage(effect);
            }
            byte[] pixelBytes = renderTarget.GetPixelBytes();
            await SaveAsAvifAsync(stream, width, height, DirectXPixelFormat.R16G16B16A16UIntNormalized, pixelBytes, ColorPrimaries.BT2020, quality, xmpData).ConfigureAwait(false);
        }
        else
        {
            throw new NotSupportedException($"{bitmap.Format} is not supported for AVIF encoding.");
        }
    }


    public static async Task SaveAsAvifAsync(Stream stream, uint width, uint height, DirectXPixelFormat pixelFormat, byte[] pixelBytes, ColorPrimaries colorPrimaries, int quality, byte[]? xmpData = null)
    {
        quality = Math.Clamp(quality, 0, 100);
        bool floatPixel = pixelFormat is DirectXPixelFormat.R16G16B16A16Float or DirectXPixelFormat.R32G32B32A32Float;
        avifRGBFormat rgbFormat = pixelFormat switch
        {
            DirectXPixelFormat.R8G8B8A8UIntNormalized => avifRGBFormat.RGBA,
            DirectXPixelFormat.B8G8R8A8UIntNormalized => avifRGBFormat.BGRA,
            DirectXPixelFormat.R16G16B16A16UIntNormalized => avifRGBFormat.RGBA,
            //DirectXPixelFormat.R16G16B16A16Float => avifRGBFormat.RGBA,
            _ => throw new NotSupportedException($"{pixelFormat} is not supported for AVIF encoding."),
        };
        uint depth = pixelFormat switch
        {
            DirectXPixelFormat.R8G8B8A8UIntNormalized => 8,
            DirectXPixelFormat.B8G8R8A8UIntNormalized => 8,
            DirectXPixelFormat.R16G16B16A16UIntNormalized => 16,
            //DirectXPixelFormat.R16G16B16A16Float => 16,
            _ => throw new NotSupportedException($"{pixelFormat} is not supported for AVIF encoding."),
        };

        await Task.Run(() =>
        {
            int maxThreads = GetSuggestedThreads();
            using var encoder = new avifEncoderLite();
            encoder.Quality = quality;
            encoder.QualityAlpha = quality;
            encoder.MaxThreads = maxThreads;
            using var rgb = new avifRGBImageWrapper(width, height, depth, rgbFormat);
            rgb.MaxThreads = maxThreads;
            rgb.IsFloat = floatPixel;
            rgb.SetPixelBytes(pixelBytes);
            using var image = new avifImageWrapper(width, height, Math.Clamp(depth, 8, 12), avifPixelFormat.YUV444);

            if (colorPrimaries.TryGetDefinedPrimaries(out int id))
            {
                if (id == 9)
                {
                    image.ColorPrimaries = avifColorPrimaries.BT2020;
                    image.TransferCharacteristics = avifTransferCharacteristics.SMPTE2084;
                    image.MatrixCoefficients = avifMatrixCoefficients.BT2020_NCL;
                }
                else
                {
                    image.ColorPrimaries = (avifColorPrimaries)id;
                    image.TransferCharacteristics = avifTransferCharacteristics.SRGB;
                    image.MatrixCoefficients = avifMatrixCoefficients.BT709;
                }
            }
            else
            {
                image.ColorPrimaries = avifColorPrimaries.Unspecified;
                image.TransferCharacteristics = avifTransferCharacteristics.Unspecified;
                image.MatrixCoefficients = avifMatrixCoefficients.Unspecified;
                image.SetProfileICC(ICCHelper.CreateIccData(colorPrimaries));
            }

            if (xmpData is not null)
            {
                image.SetXMPMetadata(xmpData);
            }
            image.FromRGBImage(rgb);
            encoder.AddImage(image, 1, avifAddImageFlag.Single);
            stream.Write(encoder.Encode());
        }).ConfigureAwait(false);
    }




    public static async Task SaveAsJxlAsync(CanvasBitmap bitmap, Stream stream, ColorPrimaries colorPrimaries, float distance, byte[]? xmpData = null)
    {
        uint width = bitmap.SizeInPixels.Width;
        uint height = bitmap.SizeInPixels.Height;

        if (bitmap.Format is DirectXPixelFormat.R8G8B8A8UIntNormalized or DirectXPixelFormat.B8G8R8A8UIntNormalized)
        {
            byte[] pixelBytes;
            if (bitmap.Format is DirectXPixelFormat.B8G8R8A8UIntNormalized)
            {
                using var renderTarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96, DirectXPixelFormat.R8G8B8A8UIntNormalized, CanvasAlphaMode.Premultiplied);
                using (var ds = renderTarget.CreateDrawingSession())
                {
                    ds.DrawImage(bitmap);
                }
                pixelBytes = renderTarget.GetPixelBytes();
            }
            else
            {
                pixelBytes = bitmap.GetPixelBytes();
            }
            await SaveAsJxlAsync(stream, width, height, DirectXPixelFormat.R8G8B8A8UIntNormalized, pixelBytes, colorPrimaries, distance, xmpData).ConfigureAwait(false);
        }
        else if (bitmap.Format is DirectXPixelFormat.R16G16B16A16Float or DirectXPixelFormat.R32G32B32A32Float)
        {
            using var renderTarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96, DirectXPixelFormat.R16G16B16A16UIntNormalized, CanvasAlphaMode.Premultiplied);
            using (var ds = renderTarget.CreateDrawingSession())
            {
                var effect = new ScRGBToHDR10Effect
                {
                    Source = bitmap,
                    BufferPrecision = CanvasBufferPrecision.Precision16Float,
                };
                ds.DrawImage(effect);
            }
            byte[] pixelBytes = renderTarget.GetPixelBytes();
            await SaveAsJxlAsync(stream, width, height, DirectXPixelFormat.R16G16B16A16UIntNormalized, pixelBytes, ColorPrimaries.BT2020, distance, xmpData).ConfigureAwait(false);
        }
        else
        {
            throw new NotSupportedException($"{bitmap.Format} is not supported for JPEG XL encoding.");
        }
    }


    public static async Task SaveAsJxlAsync(Stream stream, uint width, uint height, DirectXPixelFormat pixelFormat, byte[] pixelBytes, ColorPrimaries colorPrimaries, float distance, byte[]? xmpData = null)
    {
        distance = Math.Clamp(distance, 0, 25);
        bool lossless = distance == 0;
        bool floatPixel = pixelFormat is DirectXPixelFormat.R16G16B16A16Float or DirectXPixelFormat.R32G32B32A32Float;
        JxlPixelFormat format = pixelFormat switch
        {
            DirectXPixelFormat.R8G8B8A8UIntNormalized => JxlPixelFormat.R8G8B8A8UInt,
            DirectXPixelFormat.R16G16B16A16UIntNormalized => JxlPixelFormat.R16G16B16A16UInt,
            DirectXPixelFormat.R16G16B16A16Float => JxlPixelFormat.R16G16B16A16Float,
            DirectXPixelFormat.R32G32B32A32Float => JxlPixelFormat.R32G32B32A32Float,
            _ => throw new NotSupportedException($"{pixelFormat} is not supported for JXL encoding."),
        };
        uint depth = pixelFormat switch
        {
            DirectXPixelFormat.R8G8B8A8UIntNormalized => 8,
            DirectXPixelFormat.R16G16B16A16UIntNormalized => 16,
            DirectXPixelFormat.R16G16B16A16Float => 16,
            DirectXPixelFormat.R32G32B32A32Float => 32,
            _ => throw new NotSupportedException($"{pixelFormat} is not supported for AVIF encoding."),
        };

        JxlColorEncoding colorEncoding = default;
        if (colorPrimaries.TryGetDefinedPrimaries(out int id))
        {
            if (id == 9)
            {
                colorEncoding = JxlColorEncoding.HDR10;
            }
            else
            {
                colorEncoding.Primaries = (JxlPrimaries)id;
                colorEncoding.TransferFunction = JxlTransferFunction.sRGB;
            }
            colorEncoding.WhitePoint = JxlWhitePoint.D65;
        }
        else
        {
            colorEncoding.Primaries = JxlPrimaries.Custom;
            colorEncoding.PrimariesRedXY = new JxlPoint(colorPrimaries.Red.X, colorPrimaries.Red.Y);
            colorEncoding.PrimariesGreenXY = new JxlPoint(colorPrimaries.Green.X, colorPrimaries.Green.Y);
            colorEncoding.PrimariesBlueXY = new JxlPoint(colorPrimaries.Blue.X, colorPrimaries.Blue.Y);
            colorEncoding.WhitePoint = JxlWhitePoint.Custom;
            colorEncoding.WhitePointXY = new JxlPoint(colorPrimaries.White.X, colorPrimaries.White.Y);
            colorEncoding.TransferFunction = JxlTransferFunction.sRGB;
        }
        if (floatPixel)
        {
            colorEncoding.TransferFunction = JxlTransferFunction.Linear;
        }

        await Task.Run(() =>
        {
            using var encoder = new JxlEncoder();
            encoder.SetBasicInfo(new JxlBasicInfo(width, height, format, true) { UsesOriginalProfile = lossless });
            encoder.SetColorEncoding(colorEncoding);
            if (xmpData is not null)
            {
                encoder.AddBox(JxlBoxType.XMP, xmpData, false);
            }
            encoder.RunnerThreads = (uint)GetSuggestedThreads();
            var frameSettings = encoder.CreateFrameSettings();
            frameSettings.Distance = distance;
            frameSettings.Lossless = lossless;
            frameSettings.AddImageFrame(format, pixelBytes);
            encoder.Encode(stream);
        }).ConfigureAwait(false);
    }




    public static async Task SaveAsUhdrAsync(CanvasBitmap canvasImage, Stream stream, float maxCLL, float sdrWhiteLevel)
    {
        if (canvasImage.Format is DirectXPixelFormat.R16G16B16A16Float)
        {
            await Task.Delay(1).ConfigureAwait(false);
            using HdrToneMapEffect toneMapEffect = new()
            {
                Source = canvasImage,
                InputMaxLuminance = maxCLL,
                OutputMaxLuminance = sdrWhiteLevel,
                DisplayMode = HdrToneMapEffectDisplayMode.Hdr,
                BufferPrecision = CanvasBufferPrecision.Precision16Float,
            };
            using WhiteLevelAdjustmentEffect whiteLevelEffect = new()
            {
                Source = toneMapEffect,
                InputWhiteLevel = 80,
                OutputWhiteLevel = sdrWhiteLevel,
                BufferPrecision = CanvasBufferPrecision.Precision16Float,
            };
            using SrgbGammaEffect gammaEffect = new()
            {
                Source = whiteLevelEffect,
                GammaMode = SrgbGammaMode.OETF,
                BufferPrecision = CanvasBufferPrecision.Precision16Float,
            };
            using UhdrPixelGainEffect uhdrPixelGainEffect = new()
            {
                SdrSource = toneMapEffect,
                HdrSource = canvasImage,
            };

            using CanvasRenderTarget renderTarget_gain = new(CanvasDevice.GetSharedDevice(),
                                                    canvasImage.SizeInPixels.Width,
                                                    canvasImage.SizeInPixels.Height,
                                                    96,
                                                    DirectXPixelFormat.R32G32B32A32Float,
                                                    CanvasAlphaMode.Premultiplied);
            using (CanvasDrawingSession ds = renderTarget_gain.CreateDrawingSession())
            {
                ds.Units = CanvasUnits.Pixels;
                ds.Clear(Colors.Transparent);
                ds.DrawImage(uhdrPixelGainEffect);
            }
            byte[] gainPixelBytes = renderTarget_gain.GetPixelBytes();
            float[] contentBoost = GetContentMinMaxBoost(gainPixelBytes);


            using UhdrGainmapEffect uhdrGainmapEffect = new()
            {
                PixelGainSource = renderTarget_gain,
                MinContentBoost = MemoryMarshal.Cast<float, float3>(contentBoost)[0],
                MaxContentBoost = MemoryMarshal.Cast<float, float3>(contentBoost)[1],
            };
            using CanvasRenderTarget renderTarget_gainmap = new(CanvasDevice.GetSharedDevice(),
                                                    canvasImage.SizeInPixels.Width,
                                                    canvasImage.SizeInPixels.Height,
                                                    96,
                                                    DirectXPixelFormat.R8G8B8A8UIntNormalized,
                                                    CanvasAlphaMode.Premultiplied);
            using (CanvasDrawingSession ds = renderTarget_gainmap.CreateDrawingSession())
            {
                ds.Units = CanvasUnits.Pixels;
                ds.Clear(Colors.Transparent);
                ds.DrawImage(uhdrGainmapEffect);
            }

            using CanvasRenderTarget renderTarget_sdr = new(CanvasDevice.GetSharedDevice(),
                                                   canvasImage.SizeInPixels.Width,
                                                   canvasImage.SizeInPixels.Height,
                                                   96,
                                                   DirectXPixelFormat.R8G8B8A8UIntNormalized,
                                                   CanvasAlphaMode.Premultiplied);
            using (CanvasDrawingSession ds = renderTarget_sdr.CreateDrawingSession())
            {
                ds.Units = CanvasUnits.Pixels;
                ds.Clear(Colors.Transparent);
                ds.DrawImage(gammaEffect);
            }

            MemoryStream ms_base = new();
            MemoryStream ms_gainmap = new();
            await renderTarget_sdr.SaveAsync(ms_base.AsRandomAccessStream(), CanvasBitmapFileFormat.Jpeg);
            await renderTarget_gainmap.SaveAsync(ms_gainmap.AsRandomAccessStream(), CanvasBitmapFileFormat.Jpeg);

            using var encoder = new UhdrEncoder();
            unsafe
            {
                fixed (byte* b = ms_base.ToArray(), g = ms_gainmap.ToArray())
                {
                    UhdrCompressedImage baseImage = new UhdrCompressedImage
                    {
                        Data = (nint)b,
                        DataSize = (uint)ms_base.Length,
                        Capacity = (uint)ms_base.Length,
                        ColorGamut = UhdrColorGamut.BT709,
                        ColorRange = UhdrColorRange.FullRange,
                        ColorTransfer = UhdrColorTransfer.SRGB,
                    };
                    UhdrCompressedImage gainmapImage = new UhdrCompressedImage
                    {
                        Data = (nint)g,
                        DataSize = (uint)ms_gainmap.Length,
                        Capacity = (uint)ms_gainmap.Length,
                        ColorGamut = UhdrColorGamut.BT709,
                        ColorRange = UhdrColorRange.FullRange,
                        ColorTransfer = UhdrColorTransfer.SRGB,
                    };
                    encoder.SetCompressedImage(baseImage, UhdrImageLabel.Base);
                    UhdrGainmapMetadata metadata = new UhdrGainmapMetadata
                    {
                        Gamma = new FixedArray3<float>(1),
                        OffsetSdr = new FixedArray3<float>(0.015625f),
                        OffsetHdr = new FixedArray3<float>(0.015625f),
                        HdrCapacityMin = 1,
                        HdrCapacityMax = MathF.Max(MathF.Max(contentBoost[3], contentBoost[4]), MathF.Max(contentBoost[5], 1)),
                        UseBaseColorSpace = 1,
                    };
                    metadata.MinContentBoost[0] = contentBoost[0];
                    metadata.MinContentBoost[1] = contentBoost[1];
                    metadata.MinContentBoost[2] = contentBoost[2];
                    metadata.MaxContentBoost[0] = contentBoost[3];
                    metadata.MaxContentBoost[1] = contentBoost[4];
                    metadata.MaxContentBoost[2] = contentBoost[5];
                    encoder.SetGainmapImage(gainmapImage, metadata);
                }
            }
            encoder.Encode();
            stream.Write(encoder.GetEncodedBytes());
        }
    }


    /// <summary>
    /// return min rgb, max rgb
    /// </summary>
    /// <param name="pixelBytes"></param>
    /// <returns></returns>
    public static float[] GetContentMinMaxBoost(byte[] pixelBytes)
    {
        const float PQ_MAX = 10000f / 203;
        float[] contentBoost = [PQ_MAX, PQ_MAX, PQ_MAX, 0, 0, 0];
        var span = MemoryMarshal.Cast<byte, float>(pixelBytes);
        if (Vector.IsHardwareAccelerated && Vector<float>.Count % 4 == 0)
        {
            Vector<float> minBoost = new Vector<float>(PQ_MAX);
            Vector<float> maxBoost = new Vector<float>(0);
            int remaining = span.Length % Vector<float>.Count;
            for (int i = 0; i < span.Length - remaining; i += Vector<float>.Count)
            {
                var value = new Vector<float>(span.Slice(i, Vector<float>.Count));
                minBoost = Vector.Min(minBoost, value);
                maxBoost = Vector.Max(maxBoost, value);
            }
            for (int i = 0; i < Vector<float>.Count; i += 4)
            {
                contentBoost[0] = MathF.Min(contentBoost[0], minBoost[i]);
                contentBoost[1] = MathF.Min(contentBoost[1], minBoost[i + 1]);
                contentBoost[2] = MathF.Min(contentBoost[2], minBoost[i + 2]);
                contentBoost[3] = MathF.Max(contentBoost[3], maxBoost[i]);
                contentBoost[4] = MathF.Max(contentBoost[4], maxBoost[i + 1]);
                contentBoost[5] = MathF.Max(contentBoost[5], maxBoost[i + 2]);
            }
            for (int i = span.Length - remaining; i < span.Length; i += 4)
            {
                contentBoost[0] = MathF.Min(contentBoost[0], span[i]);
                contentBoost[1] = MathF.Min(contentBoost[1], span[i + 1]);
                contentBoost[2] = MathF.Min(contentBoost[2], span[i + 2]);
                contentBoost[3] = MathF.Max(contentBoost[3], span[i]);
                contentBoost[4] = MathF.Max(contentBoost[4], span[i + 1]);
                contentBoost[5] = MathF.Max(contentBoost[5], span[i + 2]);
            }
        }
        else
        {
            for (int i = 0; i < span.Length; i += 4)
            {
                contentBoost[0] = MathF.Min(contentBoost[0], span[i]);
                contentBoost[1] = MathF.Min(contentBoost[1], span[i + 1]);
                contentBoost[2] = MathF.Min(contentBoost[2], span[i + 2]);
                contentBoost[3] = MathF.Max(contentBoost[3], span[i]);
                contentBoost[4] = MathF.Max(contentBoost[4], span[i + 1]);
                contentBoost[5] = MathF.Max(contentBoost[5], span[i + 2]);
            }
        }
        return contentBoost;
    }



}
