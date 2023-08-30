using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Starward.Services.Cache;

public abstract class CacheBase<T>
{

    public bool Initialized => _cacheFolder is not null;

    private class ConcurrentRequest
    {
        public Task<T?> Task { get; set; }

        public Progress<DownloadProgress> Progress { get; set; }

        public bool EnsureCachedCopy { get; set; }
    }

    private readonly SemaphoreSlim _cacheFolderSemaphore = new SemaphoreSlim(1);
    private StorageFolder? _baseFolder;
    private string? _cacheFolderName;

    private StorageFolder _cacheFolder;
    private InMemoryStorage<T> _inMemoryFileStorage;

    private ConcurrentDictionary<string, ConcurrentRequest> _concurrentTasks = new ConcurrentDictionary<string, ConcurrentRequest>();

    protected HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheBase{T}"/> class.
    /// </summary>
    protected CacheBase()
    {
        CacheDuration = TimeSpan.FromDays(1);
        _inMemoryFileStorage = new InMemoryStorage<T>();
        RetryCount = 1;
    }

    /// <summary>
    /// Gets or sets the life duration of every cache entry.
    /// </summary>
    public TimeSpan CacheDuration { get; set; }

    /// <summary>
    /// Gets or sets the number of retries trying to ensure the file is cached.
    /// </summary>
    public uint RetryCount { get; set; }

    /// <summary>
    /// Gets or sets max in-memory item storage count
    /// </summary>
    public int MaxMemoryCacheCount
    {
        get
        {
            return _inMemoryFileStorage.MaxItemCount;
        }

        set
        {
            _inMemoryFileStorage.MaxItemCount = value;
        }
    }

    /// <summary>
    /// Gets instance of <see cref="HttpClient"/>
    /// </summary>
    protected HttpClient HttpClient => _httpClient ??= new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { DefaultRequestVersion = HttpVersion.Version20 };


    /// <summary>
    /// Initializes FileCache and provides root folder and cache folder name
    /// </summary>
    /// <param name="folder">Folder for cache</param>
    /// <param name="httpClient">instance of <see cref="System.Net.Http.HttpClient"/></param>
    /// <returns>awaitable task</returns>
    public virtual void Initialize(StorageFolder folder, HttpClient? httpClient = null)
    {
        _cacheFolder ??= folder;

        if (httpClient != null)
        {
            _httpClient = httpClient;
        }
    }

    /// <summary>
    /// Initializes FileCache and provides root folder and cache folder name
    /// </summary>
    /// <param name="baseFolder">Folder that is used as root for cache</param>
    /// <param name="folderName">Cache folder name</param>
    /// <param name="httpClient">instance of <see cref="System.Net.Http.HttpClient"/></param>
    /// <returns>awaitable task</returns>
    public virtual async Task InitializeAsync(StorageFolder? baseFolder = null, string? folderName = null, HttpClient? httpClient = null)
    {
        _baseFolder = baseFolder;
        _cacheFolderName = folderName;

        _cacheFolder = await GetCacheFolderAsync().ConfigureAwait(false);

        if (httpClient != null)
        {
            _httpClient = httpClient;
        }
    }

    /// <summary>
    /// Clears all files in the cache
    /// </summary>
    /// <returns>awaitable task</returns>
    public async Task ClearAsync()
    {
        var folder = await GetCacheFolderAsync().ConfigureAwait(false);
        var files = await folder.GetFilesAsync().AsTask().ConfigureAwait(false);

        await CacheBase<T>.InternalClearAsync(files).ConfigureAwait(false);

        _inMemoryFileStorage.Clear();
    }

    /// <summary>
    /// Clears file if it has expired
    /// </summary>
    /// <param name="duration">timespan to compute whether file has expired or not</param>
    /// <returns>awaitable task</returns>
    public Task ClearAsync(TimeSpan duration)
    {
        return RemoveExpiredAsync(duration);
    }

    /// <summary>
    /// Removes cached files that have expired
    /// </summary>
    /// <param name="duration">Optional timespan to compute whether file has expired or not. If no value is supplied, <see cref="CacheDuration"/> is used.</param>
    /// <returns>awaitable task</returns>
    public async Task RemoveExpiredAsync(TimeSpan? duration = null)
    {
        TimeSpan expiryDuration = duration ?? CacheDuration;

        var folder = await GetCacheFolderAsync().ConfigureAwait(false);
        var files = await folder.GetFilesAsync().AsTask().ConfigureAwait(false);

        var filesToDelete = new List<StorageFile>();

        foreach (var file in files)
        {
            if (file == null)
            {
                continue;
            }

            if (await IsFileOutOfDateAsync(file, expiryDuration, false).ConfigureAwait(false))
            {
                filesToDelete.Add(file);
            }
        }

        await CacheBase<T>.InternalClearAsync(filesToDelete).ConfigureAwait(false);

        _inMemoryFileStorage.Clear(expiryDuration);
    }

    /// <summary>
    /// Removed items based on uri list passed
    /// </summary>
    /// <param name="uriForCachedItems">Enumerable uri list</param>
    /// <returns>awaitable Task</returns>
    public async Task RemoveAsync(IEnumerable<Uri> uriForCachedItems)
    {
        if (uriForCachedItems == null || !uriForCachedItems.Any())
        {
            return;
        }

        var folder = await GetCacheFolderAsync().ConfigureAwait(false);
        var files = await folder.GetFilesAsync().AsTask().ConfigureAwait(false);

        var filesToDelete = new List<StorageFile>();
        var keys = new List<string>();

        Dictionary<string, StorageFile> hashDictionary = new();

        foreach (var file in files)
        {
            hashDictionary.Add(file.Name, file);
        }

        foreach (var uri in uriForCachedItems)
        {
            string fileName = GetCacheFileName(uri);
            if (hashDictionary.TryGetValue(fileName, out var file))
            {
                filesToDelete.Add(file);
                keys.Add(fileName);
            }
        }

        await CacheBase<T>.InternalClearAsync(filesToDelete).ConfigureAwait(false);

        _inMemoryFileStorage.Remove(keys);
    }

    /// <summary>
    /// Assures that item represented by Uri is cached.
    /// </summary>
    /// <param name="uri">Uri of the item</param>
    /// <param name="throwOnError">Indicates whether or not exception should be thrown if item cannot be cached</param>
    /// <param name="storeToMemoryCache">Indicates if item should be loaded into the in-memory storage</param>
    /// <param name="cancellationToken">instance of <see cref="CancellationToken"/></param>
    /// <returns>Awaitable Task</returns>
    public Task PreCacheAsync(Uri uri, bool throwOnError = false, bool storeToMemoryCache = false, CancellationToken cancellationToken = default)
    {
        return GetItemAsync(uri, throwOnError, !storeToMemoryCache, cancellationToken);
    }

    /// <summary>
    /// Retrieves item represented by Uri from the cache. If the item is not found in the cache, it will try to downloaded and saved before returning it to the caller.
    /// </summary>
    /// <param name="uri">Uri of the item.</param>
    /// <param name="throwOnError">Indicates whether or not exception should be thrown if item cannot be found / downloaded.</param>
    /// <param name="cancellationToken">instance of <see cref="CancellationToken"/></param>
    /// <returns>an instance of Generic type</returns>
    public Task<T?> GetFromCacheAsync(Uri uri, bool throwOnError = false, CancellationToken cancellationToken = default)
    {
        return GetItemAsync(uri, throwOnError, false, cancellationToken);
    }

    /// <summary>
    /// Gets the StorageFile containing cached item for given Uri
    /// </summary>
    /// <param name="uri">Uri of the item.</param>
    /// <returns>a StorageFile</returns>
    public async Task<StorageFile?> GetFileFromCacheAsync(Uri uri)
    {
        var folder = await GetCacheFolderAsync().ConfigureAwait(false);

        string fileName = GetCacheFileName(uri);

        var item = await folder.TryGetItemAsync(fileName).AsTask().ConfigureAwait(false);

        return item as StorageFile;
    }

    /// <summary>
    /// Retrieves item represented by Uri from the in-memory cache if it exists and is not out of date. If item is not found or is out of date, default instance of the generic type is returned.
    /// </summary>
    /// <param name="uri">Uri of the item.</param>
    /// <returns>an instance of Generic type</returns>
    public T? GetFromMemoryCache(Uri uri)
    {
        T? instance = default;

        string fileName = GetCacheFileName(uri);

        if (_inMemoryFileStorage.MaxItemCount > 0)
        {
            var msi = _inMemoryFileStorage.GetItem(fileName, CacheDuration);
            if (msi != null)
            {
                instance = msi.Item;
            }
        }

        return instance;
    }


    public Progress<DownloadProgress>? GetProgress(Uri uri)
    {
        string fileName = GetCacheFileName(uri);
        _concurrentTasks.TryGetValue(fileName, out var request);

        if (request != null)
        {
            return request.Progress;
        }
        else
        {
            return null;
        }

    }

    /// <summary>
    /// Cache specific hooks to process items from HTTP response
    /// </summary>
    /// <param name="stream">input stream</param>
    /// <returns>awaitable task</returns>
    protected abstract Task<T> InitializeTypeAsync(Stream stream);

    /// <summary>
    /// Cache specific hooks to process items from HTTP response
    /// </summary>
    /// <param name="baseFile">storage file</param>
    /// <returns>awaitable task</returns>
    protected abstract Task<T> InitializeTypeAsync(StorageFile baseFile);

    /// <summary>
    /// Override-able method that checks whether file is valid or not.
    /// </summary>
    /// <param name="file">storage file</param>
    /// <param name="duration">cache duration</param>
    /// <param name="treatNullFileAsOutOfDate">option to mark uninitialized file as expired</param>
    /// <returns>bool indicate whether file has expired or not</returns>
    protected virtual async Task<bool> IsFileOutOfDateAsync(StorageFile file, TimeSpan duration, bool treatNullFileAsOutOfDate = true)
    {
        if (file == null)
        {
            return treatNullFileAsOutOfDate;
        }

        var properties = await file.GetBasicPropertiesAsync().AsTask().ConfigureAwait(false);

        return properties.Size == 0 || DateTime.Now.Subtract(properties.DateModified.DateTime) > duration;
    }

    /// <summary>
    /// Get cache file name for given Uri
    /// </summary>
    /// <param name="uri">Uri of the item.</param>
    /// <returns>file name</returns>
    protected virtual string GetCacheFileName(Uri uri)
    {
        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(uri.ToString())));
    }


    private async Task<T?> GetItemAsync(Uri uri, bool throwOnError, bool preCacheOnly, CancellationToken cancellationToken)
    {
        T? instance = default;

        string fileName = GetCacheFileName(uri);
        _concurrentTasks.TryGetValue(fileName, out var request);

        // if similar request exists check if it was preCacheOnly and validate that current request isn't preCacheOnly
        if (request != null && request.EnsureCachedCopy && !preCacheOnly)
        {
            await request.Task.ConfigureAwait(false);
            request = null;
        }

        if (request == null)
        {
            var progress = new Progress<DownloadProgress>();
            request = new ConcurrentRequest()
            {
                Task = GetFromCacheOrDownloadAsync(uri, fileName, preCacheOnly, cancellationToken, progress),
                Progress = progress,
                EnsureCachedCopy = preCacheOnly,
            };

            _concurrentTasks[fileName] = request;
        }

        try
        {
            instance = await request.Task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            if (throwOnError)
            {
                throw;
            }
        }
        finally
        {
            _concurrentTasks.TryRemove(fileName, out _);
        }

        return instance;
    }




    private async Task<T?> GetFromCacheOrDownloadAsync(Uri uri, string fileName, bool preCacheOnly, CancellationToken cancellationToken, IProgress<DownloadProgress>? progress = null)
    {
        T? instance = default;

        if (_inMemoryFileStorage.MaxItemCount > 0)
        {
            var msi = _inMemoryFileStorage.GetItem(fileName, CacheDuration);
            if (msi != null)
            {
                instance = msi.Item;
            }
        }

        if (instance != null)
        {
            return instance;
        }

        var folder = await GetCacheFolderAsync().ConfigureAwait(false);
        var baseFile = await folder.TryGetItemAsync(fileName) as StorageFile;

        bool downloadDataFile = baseFile == null || await IsFileOutOfDateAsync(baseFile, CacheDuration).ConfigureAwait(false);

        if (baseFile == null)
        {
            baseFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting).AsTask().ConfigureAwait(false);
        }

        if (downloadDataFile)
        {
            uint retries = 0;
            try
            {
                while (retries < RetryCount)
                {
                    try
                    {
                        instance = await DownloadFileAsync(uri, baseFile, preCacheOnly, cancellationToken, progress).ConfigureAwait(false);

                        if (instance != null)
                        {
                            break;
                        }
                    }
                    catch (HttpRequestException) { }
                    catch (FileNotFoundException) { }

                    retries++;
                }
            }
            catch (TaskCanceledException)
            {
                await baseFile.DeleteAsync().AsTask().ConfigureAwait(false);
                progress?.Report(new DownloadProgress(DownloadState.Canceled, 0, -1));
                throw; // re-throwing the exception changes the stack trace. just throw
            }
            catch (Exception)
            {
                await baseFile.DeleteAsync().AsTask().ConfigureAwait(false);
                throw; // re-throwing the exception changes the stack trace. just throw
            }
        }

        if (EqualityComparer<T>.Default.Equals(instance, default) && !preCacheOnly)
        {
            instance = await InitializeTypeAsync(baseFile).ConfigureAwait(false);

            if (_inMemoryFileStorage.MaxItemCount > 0)
            {
                var properties = await baseFile.GetBasicPropertiesAsync().AsTask().ConfigureAwait(false);

                var msi = new InMemoryStorageItem<T>(fileName, properties.DateModified.DateTime, instance);
                _inMemoryFileStorage.SetItem(msi);
            }
        }

        return instance;
    }

    private async Task<T?> DownloadFileAsync(Uri uri, StorageFile baseFile, bool preCacheOnly, CancellationToken cancellationToken, IProgress<DownloadProgress>? progress = null)
    {
        T? instance = default;
        const int bufferSize = 8192;

        var request = GetHttpRequestMessage(uri);

        progress?.Report(new DownloadProgress(DownloadState.Pending, 0, -1));

        var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        long? contentLength = response.Content.Headers.ContentLength;

        progress?.Report(new DownloadProgress(DownloadState.Pending, 0, contentLength));

        long bytesRecieved = 0;

        using var hs = await response.Content.ReadAsStreamAsync(cancellationToken);

        byte[] buffer = new byte[bufferSize];

        var ms = new MemoryStream();

        int read = 0;

        var sw = Stopwatch.StartNew();
        long lastMs = 0;

        do
        {
            read = await hs.ReadAsync(buffer, 0, bufferSize).ConfigureAwait(false);
            await ms.WriteAsync(buffer, 0, read).ConfigureAwait(false);
            bytesRecieved += read;
            var nowMs = sw.ElapsedMilliseconds;
            if (nowMs - lastMs > 100)
            {
                progress?.Report(new DownloadProgress(DownloadState.Downloading, bytesRecieved, contentLength));
                lastMs = nowMs;
            }
        } while (read > 0);

        sw.Stop();

        await ms.FlushAsync().ConfigureAwait(false);
        ms.Position = 0;

        progress?.Report(new DownloadProgress(DownloadState.Downloading, bytesRecieved, contentLength));

        using var fs = await baseFile.OpenStreamForWriteAsync();
        await ms.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        await fs.FlushAsync().ConfigureAwait(false);

        progress?.Report(new DownloadProgress(DownloadState.Completed, bytesRecieved, contentLength));

        // if its pre-cache we aren't looking to load items in memory
        if (!preCacheOnly)
        {
            instance = await InitializeTypeAsync(baseFile).ConfigureAwait(false);
        }

        return instance;
    }



    protected virtual HttpRequestMessage GetHttpRequestMessage(Uri uri)
    {
        return new HttpRequestMessage(HttpMethod.Get, uri);
    }




    private static async Task InternalClearAsync(IEnumerable<StorageFile> files)
    {
        foreach (var file in files)
        {
            try
            {
                await file.DeleteAsync().AsTask().ConfigureAwait(false);
            }
            catch
            {
                // Just ignore errors for now}
            }
        }
    }

    /// <summary>
    /// Initializes with default values if user has not initialized explicitly
    /// </summary>
    /// <returns>awaitable task</returns>
    private async Task ForceInitialiseAsync()
    {
        if (_cacheFolder != null)
        {
            return;
        }

        await _cacheFolderSemaphore.WaitAsync().ConfigureAwait(false);

        _inMemoryFileStorage = new InMemoryStorage<T>();

        if (_baseFolder == null)
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward");
            Directory.CreateDirectory(folder);
            _baseFolder = await StorageFolder.GetFolderFromPathAsync(folder);
        }

        if (string.IsNullOrWhiteSpace(_cacheFolderName))
        {
            _cacheFolderName = "cache";
        }

        try
        {
            _cacheFolder = await _baseFolder.CreateFolderAsync(_cacheFolderName, CreationCollisionOption.OpenIfExists).AsTask().ConfigureAwait(false);
        }
        finally
        {
            _cacheFolderSemaphore.Release();
        }
    }

    /// <summary>
    /// Get cache folder, create if not exists.
    /// </summary>
    /// <returns></returns>
    public async Task<StorageFolder> GetCacheFolderAsync()
    {
        if (_cacheFolder == null)
        {
            await ForceInitialiseAsync().ConfigureAwait(false);
        }

        return _cacheFolder!;
    }

    /// <summary>
    /// Get cache file path whether or not cached.
    /// </summary>
    /// <param name="uri"></param>
    /// <returns></returns>
    public string GetCacheFilePath(Uri uri)
    {
        if (_cacheFolder == null)
        {
            ForceInitialiseAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        return Path.Combine(_cacheFolder!.Path, GetCacheFileName(uri));
    }

}




