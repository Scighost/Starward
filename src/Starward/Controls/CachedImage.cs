using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Scighost.WinUI.ImageEx;
using Starward.Features.Background;
using Starward.Features.Codec;
using Starward.Helpers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

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


    public bool PngThumbnail
    {
        get { return (bool)GetValue(PngThumbnailProperty); }
        set { SetValue(PngThumbnailProperty, value); }
    }

    public static readonly DependencyProperty PngThumbnailProperty =
        DependencyProperty.Register(nameof(PngThumbnail), typeof(bool), typeof(CachedImage), new PropertyMetadata(false));



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
                    if (BackgroundService.FileIsSupportedVideo(imageUri.OriginalString))
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(imageUri.OriginalString);
                        var thumbnail = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 412);
                        BitmapImage bitmap = new BitmapImage();
                        await bitmap.SetSourceAsync(thumbnail);
                        return bitmap;
                    }
                    else
                    {
                        return await ImageThumbnail.GetImageThumbnailAsync(imageUri.LocalPath, PngThumbnail, token);
                    }
                }
                else
                {
                    return new BitmapImage(imageUri);
                }
            }
            else
            {
                var file = await FileCache.GetFromCacheAsync(imageUri, false, token);
                if (token.IsCancellationRequested)
                {
                    throw new TaskCanceledException("Image source has changed.");
                }
                if (file is null)
                {
                    throw new FileNotFoundException(imageUri.ToString());
                }
                var bitmap = new BitmapImage(new Uri(file));
                bitmap.ImageOpened += BitmapImage_ImageOpened;
                bitmap.ImageFailed += BitmapImage_ImageFailed;
                return bitmap;
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
            throw;
        }
    }



    private void BitmapImage_ImageOpened(object sender, RoutedEventArgs e)
    {
        if (sender is BitmapImage image)
        {
            image.ImageOpened -= BitmapImage_ImageOpened;
            image.ImageFailed -= BitmapImage_ImageFailed;
        }
    }


    private void BitmapImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is BitmapImage image)
        {
            image.ImageOpened -= BitmapImage_ImageOpened;
            image.ImageFailed -= BitmapImage_ImageFailed;
            FileCache.DeleteCacheFile(image.UriSource);
        }
    }


}
