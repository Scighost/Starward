using Scighost.WinUILib.Cache;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Starward.Services;

internal class CacheService : CacheBase<StorageFile>
{

    private static CacheService _instance;


    public static CacheService Instance => _instance ??= InitializeInstance();



    private static CacheService InitializeInstance()
    {
        var service = new CacheService { CacheDuration = TimeSpan.FromDays(90), RetryCount = 3 };
        var folder = Path.Join(AppConfig.ConfigDirectory, "cache");
        Directory.CreateDirectory(folder);
        service.Initialize(StorageFolder.GetFolderFromPathAsync(folder).GetAwaiter().GetResult());
        return service;
    }


    protected override Task<StorageFile> InitializeTypeAsync(Stream stream)
    {
        throw new NotImplementedException();
    }

    protected override Task<StorageFile> InitializeTypeAsync(StorageFile baseFile)
    {
        return Task.FromResult(baseFile);
    }

}
