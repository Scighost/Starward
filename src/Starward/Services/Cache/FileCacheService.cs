using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Starward.Services.Cache;

internal class FileCacheService : CacheBase<StorageFile>
{

    private static FileCacheService _instance;


    public static FileCacheService Instance => _instance ??= new FileCacheService { CacheDuration = TimeSpan.FromDays(90), RetryCount = 3 };



    protected override Task<StorageFile> InitializeTypeAsync(Stream stream)
    {
        throw new NotImplementedException();
    }

    protected override Task<StorageFile> InitializeTypeAsync(StorageFile baseFile)
    {
        return Task.FromResult(baseFile);
    }

}
