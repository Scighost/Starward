using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Core.Hypergryph;
using Starward.GameInstall.Service;
using Starward.Helpers;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.GameLauncher;

internal partial class GameLauncherService
{
    internal static string? GetEndfieldHardLinkSource(GameBiz gameBiz, string installPath)
    {
        GameBiz otherRegion = gameBiz == GameBiz.endfield_cn ? GameBiz.endfield_global : GameBiz.endfield_cn;
        string? path = GetGameInstallPath(Starward.Core.HoYoPlay.GameId.FromGameBiz(otherRegion)!);
        return path is not null
            && string.Equals(Path.GetPathRoot(path), Path.GetPathRoot(installPath), StringComparison.OrdinalIgnoreCase)
            && DriveHelper.GetDriveFormat(path) is "NTFS"
            && File.Exists(Path.Combine(path, HypergryphGameConstants.EndfieldExeName))
            && File.Exists(Path.Combine(path, "game_files"))
            ? path : null;
    }

    private async Task TryShareEndfieldVoiceFilesAsync(GameId gameId, string installPath)
    {
        if (!AppConfig.EnableHardLink)
        {
            return;
        }
        try
        {
            string? donorPath = GetEndfieldHardLinkSource(gameId.GameBiz, installPath);
            if (donorPath is null || !File.Exists(Path.Combine(installPath, "game_files")))
            {
                return;
            }

            var (count, bytes) = await EndfieldVoiceHardLinkService.ShareAsync(donorPath, installPath,
                _ => GetEndfieldVoiceIndexAsync(gameId, installPath), CancellationToken.None);
            _logger.LogInformation("Hard linked {Count} Endfield voice files ({Bytes} bytes) for {GameBiz}.", count, bytes, gameId.GameBiz.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not prepare shared Endfield voice files for {GameBiz}.", gameId.GameBiz.Value);
        }
    }

    private async Task<string?> GetEndfieldVoiceIndexAsync(GameId gameId, string installPath)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            using FileStream marker = File.OpenRead(Path.Combine(installPath, "game_files"));
            string md5 = Convert.ToHexStringLower(await MD5.HashDataAsync(marker, timeout.Token));
            string? online = await _hypergryphLauncherClient.GetEndfieldMainResourceIndexAsync(gameId, md5, timeout.Token);
            if (online is not null)
            {
                return online;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not fetch the Endfield resource index.");
        }
        string cached = Path.Combine(installPath, "Endfield_Data", "Persistent", "index_main.json");
        return File.Exists(cached) ? await File.ReadAllTextAsync(cached) : null;
    }
}
