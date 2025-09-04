using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Scighost.WinUI.ImageEx;
using Starward.Features.Screenshot;
using Starward.Services.Cache;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
                    return await ImageThumbnail.GetImageThumbnailAsync(imageUri.LocalPath, token);
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


}
