using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Starward.Services.Cache;

public class ImageCacheService : CacheBase<BitmapImage>
{

    [ThreadStatic]
    private static ImageCacheService _instance;


    public static ImageCacheService Instance => _instance ??= new ImageCacheService { CacheDuration = TimeSpan.FromDays(90), RetryCount = 3 };


    private readonly DispatcherQueue _dispatcherQueue;


    public ImageCacheService(DispatcherQueue? dispatcherQueue = null)
    {
        _dispatcherQueue = dispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
    }



    protected override async Task<BitmapImage> InitializeTypeAsync(Stream stream)
    {
        if (stream.Length == 0)
        {
            throw new FileNotFoundException();
        }

        var task = new TaskCompletionSource<BitmapImage>();

        _dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                BitmapImage image = new BitmapImage();
                await image.SetSourceAsync(stream.AsRandomAccessStream()).AsTask().ConfigureAwait(false);
                task.SetResult(image);
            }
            catch (Exception ex)
            {
                task.SetException(ex);
            }
        });

        return await task.Task;
    }



    protected override async Task<BitmapImage> InitializeTypeAsync(StorageFile baseFile)
    {
        using var stream = await baseFile.OpenStreamForReadAsync().ConfigureAwait(false);
        return await InitializeTypeAsync(stream).ConfigureAwait(false);
    }


}
