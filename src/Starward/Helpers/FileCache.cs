using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Hashing;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Helpers;

internal static class FileCache
{


    private static readonly HttpClient _httpClient;

    private static readonly ConcurrentDictionary<string, Task<string?>> _concurrentTasks;


    static FileCache()
    {
        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            EnableMultipleHttp2Connections = true,
            EnableMultipleHttp3Connections = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        });
        _httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        _concurrentTasks = new();
    }



    public static int RetryCount { get; set; } = 3;

    public static TimeSpan CacheDuration { get; set; } = TimeSpan.FromDays(90);


    public static string CacheFolder { get; private set; }



    public static bool Initialize(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            CacheFolder = folder;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing FileCache: {ex.Message}");
        }
        return false;
    }




    public static async Task<string?> GetFromCacheAsync(Uri uri, bool throwOnError = false, CancellationToken cancellationToken = default)
    {
        return await GetItemAsync(uri, throwOnError, cancellationToken);
    }


    private static async Task<string?> GetItemAsync(Uri uri, bool throwOnError, CancellationToken cancellationToken)
    {
        string fileName = GetCacheFileName(uri);
        if (_concurrentTasks.TryGetValue(fileName, out var request))
        {
            return await request.ConfigureAwait(false);
        }

        request = GetFromCacheOrDownloadAsync(uri, fileName, cancellationToken);
        _concurrentTasks.TryAdd(fileName, request);

        try
        {
            return await request.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error retrieving file from cache: {ex.Message}");
            if (throwOnError)
            {
                throw;
            }
        }
        finally
        {
            _concurrentTasks.TryRemove(fileName, out _);
        }

        return null;
    }



    private static async Task<string?> GetFromCacheOrDownloadAsync(Uri uri, string fileName, CancellationToken cancellationToken)
    {
        if (CacheFolder is null)
        {
            throw new DirectoryNotFoundException("Cache folder not initialized.");
        }

        string filePath = Path.Combine(CacheFolder, fileName);

        await Task.Delay(1, CancellationToken.None).ConfigureAwait(false);
        if (IsFileCacheAvailable(filePath, CacheDuration))
        {
            return filePath;
        }

        uint retries = 0;
        while (retries < RetryCount)
        {
            try
            {
                await DownloadFileAsync(uri, filePath, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException) { }
            retries++;
        }

        return filePath;
    }


    private static async Task DownloadFileAsync(Uri uri, string path, CancellationToken cancellationToken)
    {
        string path_tmp = path + "_tmp";
        using var fs = File.Open(path_tmp, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Range = new RangeHeaderValue(fs.Length, null);
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentRange?.From > 0)
        {
            fs.Position = response.Content.Headers.ContentRange.From.Value;
        }

        using var hs = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await hs.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        await fs.FlushAsync(cancellationToken).ConfigureAwait(false);
        fs.Dispose();

        File.Move(path_tmp, path, true);
    }




    private static string GetCacheFileName(Uri uri)
    {
        byte[] hashBytes = ArrayPool<byte>.Shared.Rent(24);
        try
        {
            ReadOnlySpan<byte> pathSpan = MemoryMarshal.AsBytes(uri.ToString().AsSpan());
            XxHash64.Hash(pathSpan, hashBytes.AsSpan(0, 8));
            MD5.HashData(pathSpan, hashBytes.AsSpan(8, 16));
            return Convert.ToHexString(hashBytes.AsSpan(0, 24));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(hashBytes);
        }
    }



    private static bool IsFileCacheAvailable(string path, TimeSpan duration)
    {
        if (File.Exists(path))
        {
            var fileInfo = new FileInfo(path);
            return fileInfo.Length > 0 && (DateTime.Now - fileInfo.LastWriteTime <= duration);
        }
        return false;
    }



    public static async void DeleteCacheFile(Uri uri)
    {
        await Task.Run(() =>
        {
            string fileName = GetCacheFileName(uri);
            string filePath = Path.Join(CacheFolder, fileName);
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error deleting cache file: {ex.Message}");
                }
            }
        }).ConfigureAwait(false);
    }



}
