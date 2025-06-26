using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Scighost.WinUI.ImageEx;
using Starward.Services.Cache;
using System;
using System.Buffers;
using System.IO;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace Starward.Controls;

public sealed partial class CachedImage : ImageEx
{



    public bool IsThumbnail
    {
        get { return (bool)GetValue(IsThumbnailProperty); }
        set { SetValue(IsThumbnailProperty, value); }
    }

    public static readonly DependencyProperty IsThumbnailProperty =
        DependencyProperty.Register("IsThumbnail", typeof(bool), typeof(CachedImage), new PropertyMetadata(false));




    protected override async Task<ImageSource?> ProvideCachedResourceAsync(Uri imageUri, CancellationToken token)
    {
        try
        {
            if (imageUri.Scheme is "ms-appx")
            {
                return new BitmapImage(imageUri);
            }
            else if (imageUri.Scheme is "file")
            {
                if (IsThumbnail)
                {
                    return await GetImageThumbnailAsync(imageUri.LocalPath, token);
                }
                else
                {
                    return new BitmapImage(imageUri);
                }
            }
            else
            {


                var file = await FileCacheService.Instance.GetFromCacheAsync(imageUri, false, token);
                if (token.IsCancellationRequested)
                {
                    throw new TaskCanceledException("Image source has changed.");
                }
                if (file is null)
                {
                    throw new FileNotFoundException(imageUri.ToString());
                }
                return new BitmapImage(new Uri(file.Path));
            }
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await FileCacheService.Instance.RemoveAsync([imageUri]);
            throw;
        }
    }



    private static readonly string CacheFolder = Path.Combine(AppConfig.CacheFolder, "cache");


    public static async Task<BitmapSource> GetImageThumbnailAsync(string path, CancellationToken cancellationToken = default)
    {
        string fileName = GetCachedThumbnailName(path);
        string cachePath = Path.Combine(CacheFolder, fileName);
        if (File.Exists(cachePath))
        {
            return new BitmapImage(new Uri(cachePath));
        }
        using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream()).AsTask(cancellationToken);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, decoder.BitmapAlphaMode, new BitmapTransform
        {
            ScaledHeight = 230,
            ScaledWidth = 230 * decoder.PixelWidth / decoder.PixelHeight,
            InterpolationMode = BitmapInterpolationMode.Fant,
        }, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.ColorManageToSRgb).AsTask(cancellationToken);
        var writableBitmap = new WriteableBitmap(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight);
        softwareBitmap.CopyToBuffer(writableBitmap.PixelBuffer);
        SaveCachedImage(cachePath, softwareBitmap, cancellationToken);
        return writableBitmap;
    }



    public static BitmapImage? GetCachedThumbnail(string path)
    {
        string fileName = GetCachedThumbnailName(path);
        string cachePath = Path.Combine(CacheFolder, fileName);
        if (File.Exists(cachePath))
        {
            return new BitmapImage(new Uri(cachePath));
        }
        return null;
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



    private static async void SaveCachedImage(string path, SoftwareBitmap bitmap, CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(CacheFolder);
            var tmp = path + "_tmp";
            using var fs = File.Create(tmp);
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, fs.AsRandomAccessStream()).AsTask(cancellationToken).ConfigureAwait(false);
            encoder.SetSoftwareBitmap(bitmap);
            await encoder.FlushAsync().AsTask(cancellationToken).ConfigureAwait(false);
            await fs.DisposeAsync().ConfigureAwait(false);
            File.Move(tmp, path, true);
        }
        catch { }
        finally
        {
            bitmap.Dispose();
        }
    }



}
