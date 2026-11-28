using System.Security.Cryptography;
using System.Text.Json;
using Vanara.PInvoke;

namespace Starward.GameInstall.Service;

public static class EndfieldVoiceHardLinkService
{
    private static readonly string[] EndfieldVoiceBlocks = ["E1E7D7CE", "A31457D0", "F668D4EE", "E9D31017"];

    public static async Task<(int Count, long Bytes)> ShareAsync(string donorPath, string installPath,
        Func<CancellationToken, Task<string?>> getTargetIndex, CancellationToken cancellationToken)
    {
        string sourceRoot = GetEndfieldPersistentPath(donorPath);
        string targetRoot = GetEndfieldPersistentPath(installPath);
        var candidates = FindEndfieldVoiceFiles(sourceRoot, targetRoot);
        if (candidates.Count == 0)
        {
            return default;
        }
        string? index = await getTargetIndex(cancellationToken);
        if (index is null)
        {
            return default;
        }

        int count = 0;
        long bytes = 0;
        foreach (var file in ReadEndfieldVoiceIndex(index))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (candidates.TryGetValue(file.Path, out var candidate)
                && await TryLinkEndfieldVoiceFileAsync(candidate.Source, candidate.Target, file.Size, file.MD5, cancellationToken))
            {
                count++;
                bytes += file.Size;
            }
        }
        return (count, bytes);
    }

    private static string GetEndfieldPersistentPath(string installPath)
    {
        string data = Path.Combine(installPath, "Endfield_Data");
        string persistent = Path.Combine(data, "Persistent");
        foreach (string path in new[] { installPath, data, persistent, Path.Combine(persistent, "VFS") })
        {
            if (Directory.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            {
                throw new IOException($"The Endfield voice path contains a junction or symbolic link: {path}.");
            }
        }
        return persistent;
    }

    private static Dictionary<string, (string Source, string Target)> FindEndfieldVoiceFiles(string sourceRoot, string targetRoot)
    {
        var candidates = new Dictionary<string, (string Source, string Target)>(StringComparer.OrdinalIgnoreCase);
        foreach (string block in EndfieldVoiceBlocks)
        {
            string sourceBlock = Path.Combine(sourceRoot, "VFS", block);
            string targetBlock = Path.Combine(targetRoot, "VFS", block);
            if (!Directory.Exists(sourceBlock) || File.GetAttributes(sourceBlock).HasFlag(FileAttributes.ReparsePoint)
                || (Directory.Exists(targetBlock) && File.GetAttributes(targetBlock).HasFlag(FileAttributes.ReparsePoint)))
            {
                continue;
            }
            foreach (string source in Directory.EnumerateFiles(sourceBlock, "*.chk"))
            {
                string name = Path.GetFileName(source);
                string target = Path.Combine(targetBlock, name);
                if (!File.GetAttributes(source).HasFlag(FileAttributes.ReparsePoint)
                    && (!File.Exists(target)
                        || (!File.GetAttributes(target).HasFlag(FileAttributes.ReparsePoint) && !AreSameEndfieldFile(source, target))))
                {
                    candidates.Add($"VFS/{block}/{name}", (source, target));
                }
            }
        }
        return candidates;
    }

    private static bool AreSameEndfieldFile(string source, string target)
    {
        using var first = File.OpenHandle(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var second = File.OpenHandle(target, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return Kernel32.GetFileInformationByHandle(first, out var a)
            && Kernel32.GetFileInformationByHandle(second, out var b)
            && a.dwVolumeSerialNumber == b.dwVolumeSerialNumber
            && a.nFileIndexHigh == b.nFileIndexHigh
            && a.nFileIndexLow == b.nFileIndexLow;
    }

    private static List<(string Path, long Size, string MD5)> ReadEndfieldVoiceIndex(string index)
    {
        const string encodingKey = "Assets/Beyond/DynamicAssets/Gameplay/UI/Fonts/";
        byte[] decoded = Convert.FromBase64String(index);
        for (int i = 0; i < decoded.Length; i++)
        {
            decoded[i] = unchecked((byte)(decoded[i] - encodingKey[i % encodingKey.Length]));
        }
        using JsonDocument document = JsonDocument.Parse(decoded);
        var result = new List<(string Path, long Size, string MD5)>();
        foreach (JsonElement entry in document.RootElement.GetProperty("files").EnumerateArray())
        {
            string? path = entry.GetProperty("name").GetString();
            if (path?.EndsWith(".chk", StringComparison.OrdinalIgnoreCase) is true
                && EndfieldVoiceBlocks.Any(block => path.StartsWith($"VFS/{block}/", StringComparison.OrdinalIgnoreCase))
                && entry.GetProperty("type").TryGetInt32(out int type) && type is >= 101 and <= 104
                && entry.GetProperty("md5").GetString() is { Length: 32 } md5
                && md5.All(Uri.IsHexDigit)
                && entry.GetProperty("size").TryGetInt64(out long size) && size > 0)
            {
                result.Add((path, size, md5));
            }
        }
        return result;
    }

    private static async Task<bool> TryLinkEndfieldVoiceFileAsync(string source, string target, long size, string md5, CancellationToken cancellationToken)
    {
        string temporary = target + "." + Guid.NewGuid().ToString("N") + ".link";
        try
        {
            bool targetExists = File.Exists(target);
            if (new FileInfo(source).Length != size || (targetExists && !await MatchesEndfieldVoiceFileAsync(target, size, md5, cancellationToken)))
            {
                return false;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            if (!Kernel32.CreateHardLink(temporary, source) || !await MatchesEndfieldVoiceFileAsync(temporary, size, md5, cancellationToken))
            {
                return false;
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, target, targetExists);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private static async Task<bool> MatchesEndfieldVoiceFileAsync(string path, long size, string md5, CancellationToken cancellationToken)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete,
            bufferSize: 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return stream.Length == size
            && string.Equals(Convert.ToHexStringLower(await MD5.HashDataAsync(stream, cancellationToken)), md5, StringComparison.OrdinalIgnoreCase);
    }
}
