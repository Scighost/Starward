using System.Buffers;
using System.Security.Cryptography;

namespace Starward.GameManagement.Service;

internal static class HashService
{

    private const int MD5_BUFFER_SIZE = 1 << 16;


    /// <summary>
    /// 校验文件md5
    /// </summary>
    /// <param name="path"></param>
    /// <param name="size"></param>
    /// <param name="md5"></param>
    /// <param name="context"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    public static async Task<bool> CheckFileMD5Async(string path, long size, string md5, CancellationToken cancellation = default)
    {
        if (!File.Exists(path))
        {
            return false;
        }
        if (new FileInfo(path).Length != size)
        {
            return false;
        }
        using FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(MD5_BUFFER_SIZE);
        try
        {
            using MD5 md5Hash = MD5.Create();
            int read;
            while ((read = await fs.ReadAsync(buffer, cancellation)) > 0)
            {
                md5Hash.TransformBlock(buffer, 0, read, null, 0);
            }
            md5Hash.TransformFinalBlock(buffer, 0, 0);
            if (md5Hash.Hash is null)
            {
                return false;
            }
            return string.Equals(md5, Convert.ToHexStringLower(md5Hash.Hash), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }



    /// <summary>
    /// 校验流 MD5，从当前位置开始读取 size 长度
    /// </summary>
    public static async Task<bool> CheckStreamMD5Async(Stream stream, long size, string md5, CancellationToken cancellation = default)
    {
        if (stream.Length - stream.Position < size)
        {
            return false;
        }
        byte[] buffer = ArrayPool<byte>.Shared.Rent(MD5_BUFFER_SIZE);
        try
        {
            using MD5 md5Hash = MD5.Create();
            long remaining = size;
            while (remaining > 0)
            {
                int toRead = (int)Math.Min(buffer.Length, remaining);
                int read = await stream.ReadAsync(buffer.AsMemory(0, toRead), cancellation);
                if (read <= 0)
                {
                    break;
                }
                md5Hash.TransformBlock(buffer, 0, read, null, 0);
                remaining -= read;
            }
            md5Hash.TransformFinalBlock(buffer, 0, 0);
            if (md5Hash.Hash is null)
            {
                return false;
            }
            return string.Equals(md5, Convert.ToHexStringLower(md5Hash.Hash), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }


    public static async Task<string?> GetFileMD5Async(string path, CancellationToken cancellation = default)
    {
        using FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(MD5_BUFFER_SIZE);
        try
        {
            using MD5 md5Hash = MD5.Create();
            int read;
            while ((read = await fs.ReadAsync(buffer, cancellation)) > 0)
            {
                md5Hash.TransformBlock(buffer, 0, read, null, 0);
            }
            md5Hash.TransformFinalBlock(buffer, 0, 0);
            if (md5Hash.Hash is null)
            {
                return null;
            }
            return Convert.ToHexStringLower(md5Hash.Hash);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }


}