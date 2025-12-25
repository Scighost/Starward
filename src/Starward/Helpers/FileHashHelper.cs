using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Helpers;

internal static class FileHashHelper
{


    public static async Task<bool> CheckSHA256Async(string path, string hash, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return false;
        }
        using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return await CheckSHA256Async(fs, hash, cancellationToken);
    }



    public static async Task<bool> CheckSHA256Async(Stream stream, string hash, CancellationToken cancellationToken = default)
    {
        byte[] bytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return hash.Equals(Convert.ToHexStringLower(bytes), StringComparison.OrdinalIgnoreCase);
    }


    public static async Task<bool> CheckSHA256Async(string path, long size, string hash, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return false;
        }
        if (new FileInfo(path).Length != size)
        {
            return false;
        }
        using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return await CheckSHA256Async(fs, hash, cancellationToken);
    }



    public static async Task<bool> CheckSHA256Async(Stream stream, long size, string hash, CancellationToken cancellationToken = default)
    {
        if (stream.Length != size)
        {
            return false;
        }
        byte[] bytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return hash.Equals(Convert.ToHexStringLower(bytes), StringComparison.OrdinalIgnoreCase);
    }



}
