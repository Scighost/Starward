using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml.Media.Imaging;
using Starward.Codec.AVIF;
using Starward.Codec.JpegXL;
using Starward.Codec.JpegXL.Decode;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using WinRT;

namespace Starward.Features.Codec;

internal static class ImageThumbnail
{



    private static readonly string CacheFolder = Path.Combine(AppConfig.CacheFolder, "thumb");


    public static readonly bool AvifDecoderSupported;

    public static readonly bool JxlDecoderSupported;


    private const string AvifImageBase64 = "AAAAHGZ0eXBhdmlmAAAAAGF2aWZtaWYxbWlhZgAAAOltZXRhAAAAAAAAACFoZGxyAAAAAAAAAABwaWN0AAAAAAAAAAAAAAAAAAAAAA5waXRtAAAAAAABAAAAHmlsb2MAAAAARAAAAQABAAAAAQAAAQ0AAAASAAAAKGlpbmYAAAAAAAEAAAAaaW5mZQIAAAAAAQAAYXYwMUNvbG9yAAAAAGhpcHJwAAAASWlwY28AAAAUaXNwZQAAAAAAAAABAAAAAQAAAA5waXhpAAAAAAEIAAAADGF2MUOBABwAAAAAE2NvbHJuY2x4AAEAAAABgAAAABdpcG1hAAAAAAAAAAEAAQQBAoMEAAAAGm1kYXQSAAoEGAAGVTIIH/AAAQACH8A=";

    private const string JxlImageBase64 = "/woAEBAUNwIIBAEAGABLGIsVggE=";


    private static List<KeyValuePair<string, BitmapTypedValue>> ImageQuality = [new KeyValuePair<string, BitmapTypedValue>("ImageQuality", new BitmapTypedValue(0.6f, Windows.Foundation.PropertyType.Single))];


    private static readonly SemaphoreSlim Semaphore = new(Math.Clamp(Environment.ProcessorCount / 2, 1, int.MaxValue));


    static ImageThumbnail()
    {
        try
        {
            var bytes = Convert.FromBase64String(AvifImageBase64);
            BitmapDecoder.CreateAsync(new MemoryStream(bytes).AsRandomAccessStream()).AsTask().Wait();
            AvifDecoderSupported = true;
        }
        catch { }
        try
        {
            var bytes = Convert.FromBase64String(JxlImageBase64);
            BitmapDecoder.CreateAsync(new MemoryStream(bytes).AsRandomAccessStream()).AsTask().Wait();
            JxlDecoderSupported = true;
        }
        catch { }
    }




    public static async Task<BitmapSource> GetImageThumbnailAsync(string path, bool png, CancellationToken cancellationToken = default)
    {
        string fileName = GetCachedThumbnailName(path);
        string cachePath = Path.Combine(CacheFolder, fileName);
        if (File.Exists(cachePath))
        {
            return new BitmapImage(new Uri(cachePath));
        }
        await Semaphore.WaitAsync(cancellationToken);
        try
        {
            await Task.Delay(300, cancellationToken);
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (!AvifDecoderSupported && extension is ".avif")
            {
                return await LoadAvifImageAsync(path, cachePath, cancellationToken);
            }
            else if (!JxlDecoderSupported && extension is ".jxl")
            {
                return await LoadJxlImageAsync(path, cachePath, cancellationToken);
            }
            else
            {
                return await LoadImageAsync(path, cachePath, cancellationToken);
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }


    private static string GetCachedThumbnailName(string path)
    {
        byte[] hashBytes = ArrayPool<byte>.Shared.Rent(24);
        try
        {
            ReadOnlySpan<byte> pathSpan = MemoryMarshal.AsBytes(path.AsSpan());
            XxHash64.Hash(pathSpan, hashBytes.AsSpan(0, 8));
            MD5.HashData(pathSpan, hashBytes.AsSpan(8, 16));
            return Convert.ToHexString(hashBytes.AsSpan(0, 24));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(hashBytes);
        }
    }



    private static async Task<BitmapSource> LoadAvifImageAsync(string filePath, string cachePath, CancellationToken cancellationToken = default)
    {
        await Task.Run(async () =>
        {
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var decoder = await avifDecoderLite.CreateAsync(fs);
            cancellationToken.ThrowIfCancellationRequested();

            uint width = decoder.Width;
            uint height = decoder.Height;
            uint depth = decoder.Depth > 8u ? 16u : 8u;

            using var image = decoder.GetNextImage();
            using var rgb = image.ToRGBImage(depth, avifRGBFormat.RGBA);
            var pixelBytes = rgb.GetPixelBytes();

            using var softwareBitmap = new SoftwareBitmap(depth == 8 ? BitmapPixelFormat.Rgba8 : BitmapPixelFormat.Rgba16, (int)rgb.Width, (int)rgb.Height);
            using (var buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write))
            {
                using var refer = buffer.CreateReference();
                refer.As<IMemoryBufferByteAccess>().GetBuffer(out nint ptr, out uint capacity);
                if (capacity < pixelBytes.Length)
                {
                    throw new InvalidOperationException("Buffer size is smaller than pixel data size.");
                }
                unsafe
                {
                    NativeMemory.Copy(Unsafe.AsPointer(ref MemoryMarshal.GetReference(pixelBytes)), ptr.ToPointer(), (nuint)pixelBytes.Length);
                }
            }
            cancellationToken.ThrowIfCancellationRequested();

            Directory.CreateDirectory(CacheFolder);
            var tmp = cachePath + "_tmp";
            using var fs_tmp = File.Create(tmp);
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, fs.AsRandomAccessStream(), ImageQuality).AsTask(cancellationToken).ConfigureAwait(false);
            encoder.SetSoftwareBitmap(softwareBitmap);
            (uint scaledWidth, uint scaledHeight) = GetThumbnailSize(rgb.Width, rgb.Height);
            encoder.BitmapTransform.ScaledWidth = scaledWidth;
            encoder.BitmapTransform.ScaledHeight = scaledHeight;
            encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
            await encoder.FlushAsync();
            fs_tmp.Dispose();
            File.Move(tmp, cachePath, true);
        }, cancellationToken);
        return new BitmapImage(new Uri(cachePath));
    }



    private static async Task<BitmapSource> LoadJxlImageAsync(string filePath, string cachePath, CancellationToken cancellationToken = default)
    {
        await Task.Run(async () =>
        {
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var decoder = await JxlDecoderLite.CreateAsync(fs);
            cancellationToken.ThrowIfCancellationRequested();

            var pixelBytes = decoder.GetPixelBytes(JxlPixelFormat.R8G8B8A8UInt);
            using var softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Rgba8, (int)decoder.Width, (int)decoder.Height);
            using (var buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write))
            {
                using var refer = buffer.CreateReference();
                refer.As<IMemoryBufferByteAccess>().GetBuffer(out nint ptr, out uint capacity);
                if (capacity < pixelBytes.Length)
                {
                    throw new InvalidOperationException("Buffer size is smaller than pixel data size.");
                }
                Marshal.Copy(pixelBytes, 0, ptr, pixelBytes.Length);
            }
            cancellationToken.ThrowIfCancellationRequested();

            Directory.CreateDirectory(CacheFolder);
            var tmp = cachePath + "_tmp";
            using var fs_tmp = File.Create(tmp);
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, fs.AsRandomAccessStream(), ImageQuality).AsTask(cancellationToken).ConfigureAwait(false);
            encoder.SetSoftwareBitmap(softwareBitmap);
            (uint width, uint height) = GetThumbnailSize(decoder.Width, decoder.Height);
            encoder.BitmapTransform.ScaledWidth = width;
            encoder.BitmapTransform.ScaledHeight = height;
            encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
            await encoder.FlushAsync();
            fs_tmp.Dispose();
            File.Move(tmp, cachePath, true);
        }, cancellationToken);
        return new BitmapImage(new Uri(cachePath));
    }




    private static async Task<BitmapSource> LoadImageAsync(string filePath, string cachePath, CancellationToken cancellationToken = default)
    {
        using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        Span<byte> buffer = stackalloc byte[21];
        fs.ReadExactly(buffer);
        fs.Position = 0;
        bool png = buffer.Slice(12, 4).SequenceEqual("VP8X"u8) && ((buffer[20] & 0x10) == 0x10);
        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream()).AsTask(cancellationToken);
        (uint width, uint height) = GetThumbnailSize(decoder.PixelWidth, decoder.PixelHeight);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, decoder.BitmapAlphaMode, new BitmapTransform
        {
            ScaledWidth = width,
            ScaledHeight = height,
            InterpolationMode = BitmapInterpolationMode.Fant,
        }, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.ColorManageToSRgb);
        var writableBitmap = new WriteableBitmap(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight);
        softwareBitmap.CopyToBuffer(writableBitmap.PixelBuffer);
        SaveCachedImage(cachePath, softwareBitmap, png, cancellationToken);
        return writableBitmap;
    }




    private static async void SaveCachedImage(string path, SoftwareBitmap bitmap, bool png, CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(CacheFolder);
            var tmp = path + "_tmp";
            using var fs = File.Create(tmp);
            BitmapEncoder encoder = png ? await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fs.AsRandomAccessStream()).AsTask(cancellationToken).ConfigureAwait(false)
                                        : await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, fs.AsRandomAccessStream(), ImageQuality).AsTask(cancellationToken).ConfigureAwait(false);
            encoder.SetSoftwareBitmap(bitmap);
            await encoder.FlushAsync().AsTask(cancellationToken).ConfigureAwait(false);
            fs.Dispose();
            File.Move(tmp, path, true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            bitmap.Dispose();
        }
    }



    private static (uint Width, uint Height) GetThumbnailSize(uint width, uint height)
    {
        const uint TargetWidth = 412;
        const uint TargetHeight = 232;
        if (width * TargetHeight > height * TargetWidth)
        {
            width = TargetHeight * width / height;
            height = TargetHeight;
        }
        else
        {
            height = TargetWidth * height / width;
            width = TargetWidth;
        }
        return (width, height);
    }



    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer([Out] out nint buffer, [Out] out uint capacity);
    }



    public static void ClearThumbnailCache()
    {
        if (Directory.Exists(CacheFolder))
        {
            Directory.Delete(CacheFolder, true);
        }
    }





    private static readonly SemaphoreSlim VideoSemaphore = new(1);


    public static async Task<BitmapSource> GetVideoThumbnailAsync(string path, CancellationToken cancellation = default)
    {
        string fileName = GetCachedThumbnailName(path);
        string cachePath = Path.Combine(CacheFolder, fileName);
        if (File.Exists(cachePath))
        {
            return new BitmapImage(new Uri(cachePath));
        }
        await VideoSemaphore.WaitAsync(cancellation);
        try
        {
            if (Path.GetExtension(path).Equals(".webm", StringComparison.OrdinalIgnoreCase))
            {
                bool decoderInstalled = VP9Helper.IsVP9DecoderInstalled();
                bool highProfile = VP9Helper.IsWebmButNotProfile0(path);
                if (!decoderInstalled || highProfile)
                {
                    return await GetVideoThumbnailFromMediaPlayerAsync(path, cachePath, cancellation);
                }
            }
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, 216);
            await SaveStreamToFileAsync(cachePath, thumbnail.AsStream(), cancellation);
            return new BitmapImage(new(cachePath));
        }
        finally
        {
            VideoSemaphore.Release();
        }
    }


    private static async Task SaveStreamToFileAsync(string path, Stream stream, CancellationToken cancellation)
    {
        Directory.CreateDirectory(CacheFolder);
        var tmp = path + "_tmp";
        using var fs = File.Create(tmp);
        await stream.CopyToAsync(fs, cancellation);
        fs.Dispose();
        File.Move(tmp, path, true);
    }


    private static async Task<BitmapSource> GetVideoThumbnailFromMediaPlayerAsync(string path, string cachePath, CancellationToken cancellation = default)
    {
        MediaPlayer? player = null;
        try
        {
            Debug.WriteLine(path);
            VP9Helper.RegisterVP9Decoder();
            var completion = new TaskCompletionSource();
            player = new MediaPlayer
            {
                IsMuted = true,
                IsVideoFrameServerEnabled = true,
                Source = MediaSource.CreateFromUri(new Uri(path))
            };
            player.CommandManager.IsEnabled = false;
            player.SystemMediaTransportControls.IsEnabled = false;
            player.VideoFrameAvailable += (s, e) => { s.Pause(); completion.TrySetResult(); };
            player.MediaFailed += (s, e) => completion.TrySetException(e.ExtendedErrorCode);
            player.Play();

            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            tokenSource.CancelAfter(TimeSpan.FromSeconds(3));
            await completion.Task.WaitAsync(tokenSource.Token);

            int width = (int)player.PlaybackSession.NaturalVideoWidth;
            int height = (int)player.PlaybackSession.NaturalVideoHeight;
            using var renderTarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96);
            player.CopyFrameToVideoSurface(renderTarget);
            player.Dispose();
            player = null;

            var ms = new MemoryStream();
            await renderTarget.SaveAsync(ms.AsRandomAccessStream(), CanvasBitmapFileFormat.Jpeg, 0.6f);
            ms.Position = 0;
            await SaveStreamToFileAsync(cachePath, ms, cancellation);
            return new BitmapImage(new Uri(cachePath));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            throw;
        }
        finally
        {
            VP9Helper.UnregisterVP9Decoder();
            player?.Dispose();
        }
    }


}
