using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Starward.Services.Cache;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace Starward.Controls;

public sealed class CachedImage : ImageEx
{


    private static readonly ConcurrentDictionary<Uri, Uri> fileCache = new();


    public static void ClearCache()
    {
        fileCache.Clear();
    }



    public bool IsThumbnail
    {
        get { return (bool)GetValue(IsThumbnailProperty); }
        set { SetValue(IsThumbnailProperty, value); }
    }

    public static readonly DependencyProperty IsThumbnailProperty =
        DependencyProperty.Register("IsThumbnail", typeof(bool), typeof(CachedImage), new PropertyMetadata(false));




    protected override async Task<ImageSource> ProvideCachedResourceAsync(Uri imageUri, CancellationToken token)
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
                    if (fileCache.TryGetValue(imageUri, out var uri))
                    {
                        return new BitmapImage(uri);
                    }
                    else
                    {
                        string path = await GetImageThumbnailAsync(imageUri.LocalPath);
                        uri = new Uri(path);
                        fileCache[imageUri] = uri;
                        return new BitmapImage(uri);
                    }
                }
                else
                {
                    return new BitmapImage(imageUri);
                }
            }
            else
            {
                if (fileCache.TryGetValue(imageUri, out var uri))
                {
                    return new BitmapImage(uri);
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
                    uri = new Uri(file.Path);
                    fileCache[imageUri] = uri;
                    return new BitmapImage(uri);
                }
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
        catch (Exception)
        {
            await FileCacheService.Instance.RemoveAsync(new[] { imageUri });
            throw;
        }
    }



    private static readonly string CacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward", "cache");


    public static async Task<string> GetImageThumbnailAsync(string path)
    {
        return await Task.Run(async () =>
        {
            string fileName = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(path)));
            string outPath = Path.Combine(CacheFolder, fileName);
            if (File.Exists(outPath))
            {
                return outPath;
            }
            Directory.CreateDirectory(CacheFolder);
            using var fs = File.OpenRead(path);
            using var ms = new MemoryStream();
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream());
            var bitmap = await decoder.GetSoftwareBitmapAsync(decoder.BitmapPixelFormat, decoder.BitmapAlphaMode, new BitmapTransform
            {
                ScaledHeight = 240,
                ScaledWidth = 240 * decoder.PixelWidth / decoder.PixelHeight,
                InterpolationMode = BitmapInterpolationMode.Fant,
            }, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, ms.AsRandomAccessStream());
            encoder.SetSoftwareBitmap(bitmap);
            await encoder.FlushAsync();
            await File.WriteAllBytesAsync(outPath, ms.ToArray());
            return outPath;
        });
    }


}
