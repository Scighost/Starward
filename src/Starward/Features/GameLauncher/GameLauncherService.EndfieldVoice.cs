using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Hypergryph;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace Starward.Features.GameLauncher;

internal partial class GameLauncherService
{
    private static readonly string[] EndfieldVoiceBlocks = ["E1E7D7CE", "A31457D0", "F668D4EE", "E9D31017"];

    internal static string? GetEndfieldHardLinkSource(GameBiz gameBiz, string installPath)
    {
        GameBiz otherRegion = gameBiz == GameBiz.endfield_cn ? GameBiz.endfield_global : GameBiz.endfield_cn;
        string? path = GetGameInstallPath(otherRegion);
        return path is not null
            && string.Equals(Path.GetPathRoot(path), Path.GetPathRoot(installPath), StringComparison.OrdinalIgnoreCase)
            && DriveHelper.GetDriveFormat(path) is "NTFS"
            && File.Exists(Path.Combine(path, HypergryphGameConstants.EndfieldExeName))
            && File.Exists(Path.Combine(path, "game_files"))
            ? path : null;
    }

    private async Task TryShareEndfieldVoiceFilesAsync(GameBiz gameBiz, string installPath)
    {
        if (!AppConfig.EnableHardLink)
        {
            return;
        }
        try
        {
            string? donorPath = GetEndfieldHardLinkSource(gameBiz, installPath);
            if (donorPath is null || !File.Exists(Path.Combine(installPath, "game_files")))
            {
                return;
            }

            string targetRoot = GetEndfieldPersistentPath(installPath);
            string sourceRoot = GetEndfieldPersistentPath(donorPath);
            var candidates = FindEndfieldVoiceFiles(sourceRoot, targetRoot);
            if (candidates.Count == 0)
            {
                return;
            }
            string? index = await GetEndfieldVoiceIndexAsync(gameBiz, installPath, targetRoot);
            if (index is null)
            {
                return;
            }

            int count = 0;
            long bytes = 0;
            foreach (var file in ReadEndfieldVoiceIndex(index))
            {
                if (candidates.TryGetValue(file.Path, out var candidate)
                    && await TryLinkEndfieldVoiceFileAsync(candidate.Source, candidate.Target, file.Size, file.MD5))
                {
                    count++;
                    bytes += file.Size;
                }
            }
            _logger.LogInformation("Hard linked {Count} Endfield voice files ({Bytes} bytes) for {GameBiz}.", count, bytes, gameBiz.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not prepare shared Endfield voice files for {GameBiz}.", gameBiz.Value);
        }
    }

    private async Task<string?> GetEndfieldVoiceIndexAsync(GameBiz gameBiz, string installPath, string persistentPath)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            using FileStream marker = File.OpenRead(Path.Combine(installPath, "game_files"));
            string md5 = Convert.ToHexStringLower(await MD5.HashDataAsync(marker, timeout.Token));
            string? online = await _hypergryphLauncherClient.GetEndfieldMainResourceIndexAsync(gameBiz, md5, timeout.Token);
            if (online is not null)
            {
                return online;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not fetch the Endfield resource index.");
        }
        string cached = Path.Combine(persistentPath, "index_main.json");
        return File.Exists(cached) ? await File.ReadAllTextAsync(cached) : null;
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
                && entry.GetProperty("md5").GetString() is { Length: 32 } md5
                && md5.All(Uri.IsHexDigit)
                && entry.GetProperty("size").TryGetInt64(out long size) && size > 0)
            {
                result.Add((path, size, md5));
            }
        }
        return result;
    }

    private static async Task<bool> TryLinkEndfieldVoiceFileAsync(string source, string target, long size, string md5)
    {
        string temporary = target + "." + Guid.NewGuid().ToString("N") + ".link";
        try
        {
            bool targetExists = File.Exists(target);
            if (new FileInfo(source).Length != size || (targetExists && !await MatchesEndfieldVoiceFileAsync(target, size, md5)))
            {
                return false;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            if (!Kernel32.CreateHardLink(temporary, source) || !await MatchesEndfieldVoiceFileAsync(temporary, size, md5))
            {
                return false;
            }
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

    private static async Task<bool> MatchesEndfieldVoiceFileAsync(string path, long size, string md5)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete,
            bufferSize: 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return stream.Length == size
            && string.Equals(Convert.ToHexStringLower(await MD5.HashDataAsync(stream)), md5, StringComparison.OrdinalIgnoreCase);
    }
}
