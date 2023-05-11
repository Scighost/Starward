using Starward.Service.Cache;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Starward.Service;

internal class CacheService : CacheBase<StorageFile>
{

    private static CacheService _instance;


    public static CacheService Instance => _instance ??= new CacheService { CacheDuration = TimeSpan.FromDays(90), RetryCount = 3 };



    protected override Task<StorageFile> InitializeTypeAsync(Stream stream)
    {
        throw new NotImplementedException();
    }

    protected override Task<StorageFile> InitializeTypeAsync(StorageFile baseFile)
    {
        return Task.FromResult(baseFile);
    }

}
