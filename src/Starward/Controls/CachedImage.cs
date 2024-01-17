using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Starward.Services.Cache;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

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
                    StorageFile file = await StorageFile.GetFileFromPathAsync(imageUri.LocalPath);
                    StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, 240, ThumbnailOptions.UseCurrentScale);
                    BitmapImage image = new BitmapImage();
                    await image.SetSourceAsync(thumbnail);
                    return image;
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
}
