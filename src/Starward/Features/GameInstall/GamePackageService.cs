using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Core.Hypergryph;
using Starward.Features.GameLauncher;
using Starward.Features.HoYoPlay;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Starward.Features.GameInstall;

internal partial class GamePackageService
{

    private readonly ILogger<GamePackageService> _logger;

    private readonly HoYoPlayService _hoYoPlayService;

    private readonly GameLauncherService _gameLauncherService;

    private readonly HypergryphLauncherClient _hypergryphLauncherClient;


    public GamePackageService(ILogger<GamePackageService> logger, HoYoPlayService hoYoPlayService, GameLauncherService gameLauncherService, HypergryphLauncherClient hypergryphLauncherClient)
    {
        _logger = logger;
        _hoYoPlayService = hoYoPlayService;
        _gameLauncherService = gameLauncherService;
        _hypergryphLauncherClient = hypergryphLauncherClient;
    }



    /// <summary>
    /// 获取游戏安装路径
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public static string? GetGameInstallPath(GameId gameId)
    {
        return GameLauncherService.GetGameInstallPath(gameId);
    }



    /// <summary>
    /// 本地游戏版本
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public async Task<Version?> GetLocalGameVersionAsync(GameId gameId, string? installPath = null)
    {
        return await _gameLauncherService.GetLocalGameVersionAsync(gameId, installPath);
    }



    /// <summary>
    /// 获取鹰角游戏的预下载补丁包
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public async Task<HypergryphGamePatch?> GetHypergryphPreDownloadPatchAsync(GameId gameId, string? installPath = null)
    {
        if (!HypergryphGameConstants.IsHypergryphGame(gameId.GameBiz))
        {
            return null;
        }
        Version? localVersion = await _gameLauncherService.GetLocalGameVersionAsync(gameId, installPath);
        HypergryphLatestGame latest = await _hypergryphLauncherClient.GetLatestGameAsync(
            gameId.GameBiz,
            localVersion?.ToString());
        return latest.PrePatch is { DownloadParts.Count: > 0 } ? latest.PrePatch : null;
    }



    /// <summary>
    /// 检查预下载是否完成
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public async Task<bool> CheckPreDownloadFinishedAsync(GameId gameId, string? installPath = null)
    {
        installPath ??= GameLauncherService.GetGameInstallPath(gameId);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return false;
        }
        if (HypergryphGameConstants.IsHypergryphGame(gameId.GameBiz))
        {
            Version? localVersion = await _gameLauncherService.GetLocalGameVersionAsync(gameId, installPath);
            HypergryphLatestGame latest = await _hypergryphLauncherClient.GetLatestGameAsync(
                gameId.GameBiz,
                localVersion?.ToString());
            HypergryphGamePatch? prePatch = latest.PrePatch;
            if (prePatch is null || prePatch.DownloadParts.Count == 0)
            {
                return false;
            }
            HypergryphInstallMetadata? metadata = await HypergryphInstallMetadata.ReadAsync(installPath);
            if (metadata is null || !metadata.IsFor(gameId.GameBiz)
                || !string.Equals(metadata.PredownloadFingerprint, prePatch.GetFingerprint(), StringComparison.OrdinalIgnoreCase))
            {
                return CheckHypergryphOfficialPreDownload(installPath, prePatch.DownloadParts);
            }
            string downloadDirectory = HypergryphInstallMetadata.GetDownloadsDirectory(installPath);
            if (AreHypergryphPackagePartsComplete(downloadDirectory, prePatch.DownloadParts))
            {
                return true;
            }
            return CheckHypergryphOfficialPreDownload(installPath, prePatch.DownloadParts);
        }
        string? predownloadVersion = null;
        GameConfig? gameConfig = await _hoYoPlayService.GetGameConfigAsync(gameId);
        if (gameConfig is null)
        {
            throw new ArgumentOutOfRangeException($"Game config is null ({gameId.Id}, {gameId.GameBiz}).");
        }
        if (gameConfig.DefaultDownloadMode is DownloadMode.DOWNLOAD_MODE_CHUNK or DownloadMode.DOWNLOAD_MODE_LDIFF)
        {
            GameBranch? gameBranch = await _hoYoPlayService.GetGameBranchAsync(gameId);
            if (gameBranch is null)
            {
                throw new ArgumentOutOfRangeException($"Game branch is null ({gameId.Id}, {gameId.GameBiz}).");
            }
            if (gameBranch.PreDownload is null)
            {
                return false;
            }
            predownloadVersion = gameBranch.PreDownload?.Tag;
        }
        else
        {
            GamePackage package = await _hoYoPlayService.GetGamePackageAsync(gameId);
            if (package.PreDownload.Major is null)
            {
                return false;
            }
            predownloadVersion = package.PreDownload.Major.Version;
        }

        var config = Path.Join(installPath, "config.ini");
        if (File.Exists(config))
        {
            var str = await File.ReadAllTextAsync(config);
            string? localVersion = null;
            var matches = GameVersionRegex().Matches(str);
            if (matches.Count > 0)
            {
                localVersion = matches[^1].Groups[1].Value.Trim();
            }
            string? predownload = PreDownloadRegex().Match(str).Groups[1].Value.Trim();
            AudioLanguage lang = await GetAudioLanguageAsync(gameId, installPath);
            return predownload == $"{localVersion},{predownloadVersion},{lang}";
        }
        return false;
    }


    [GeneratedRegex(@"game_version=(.+)")]
    private static partial Regex GameVersionRegex();

    [GeneratedRegex(@"predownload=(.+)")]
    private static partial Regex PreDownloadRegex();


    private bool CheckHypergryphOfficialPreDownload(string installPath, IReadOnlyList<HypergryphPackagePart> parts)
    {
        try
        {
            string launcherTemporaryDirectory = Path.Combine(installPath, "launcher_tmp");
            if (!Directory.Exists(launcherTemporaryDirectory))
            {
                return false;
            }
            foreach (string taskDirectory in Directory.EnumerateDirectories(launcherTemporaryDirectory))
            {
                string[] cacheDirectories =
                [
                    Path.Combine(taskDirectory, "preload"),
                    Path.Combine(taskDirectory, "predownload"),
                ];
                foreach (string cacheDirectory in cacheDirectories)
                {
                    if (AreHypergryphPackagePartsComplete(cacheDirectory, parts))
                    {
                        _logger.LogDebug("Found completed Hypergryph pre-download in {Directory}.", cacheDirectory);
                        return true;
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug(ex, "Failed to inspect the official Hypergryph pre-download cache.");
        }
        return false;
    }


    private static bool AreHypergryphPackagePartsComplete(string directory, IReadOnlyList<HypergryphPackagePart> parts)
    {
        if (!Directory.Exists(directory) || parts.Count == 0)
        {
            return false;
        }
        foreach (HypergryphPackagePart part in parts)
        {
            string file = Path.Combine(directory, part.GetFileName());
            if (IsHypergryphPackagePartComplete(file, part.PackageSize))
            {
                continue;
            }
            if (!IsHypergryphPackagePartComplete(file + ".tmp", part.PackageSize))
            {
                return false;
            }
        }
        return true;
    }


    private static bool IsHypergryphPackagePartComplete(string path, long expectedSize)
    {
        try
        {
            return expectedSize > 0 && File.Exists(path) && new FileInfo(path).Length == expectedSize;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }



    /// <summary>
    /// 获取语音包语言
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public async Task<AudioLanguage> GetAudioLanguageAsync(GameId gameId, string? installPath = null)
    {
        GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(gameId);
        if (string.IsNullOrWhiteSpace(config?.AudioPackageScanDir))
        {
            return AudioLanguage.None;
        }
        installPath ??= GameLauncherService.GetGameInstallPath(gameId);
        AudioLanguage flag = AudioLanguage.None;
        string file = Path.Join(installPath, config.AudioPackageScanDir);
        if (File.Exists(file))
        {
            var lines = await File.ReadAllLinesAsync(file);
            if (lines.Any(x => x.Contains("Chinese"))) { flag |= AudioLanguage.Chinese; }
            if (lines.Any(x => x.Contains("English(US)"))) { flag |= AudioLanguage.English; }
            if (lines.Any(x => x.Contains("Japanese"))) { flag |= AudioLanguage.Japanese; }
            if (lines.Any(x => x.Contains("Korean"))) { flag |= AudioLanguage.Korean; }
        }
        return flag;
    }



    /// <summary>
    /// 设置语音包语言
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="installPath"></param>
    /// <param name="lang"></param>
    /// <returns></returns>
    public async Task SetAudioLanguageAsync(GameId gameId, string installPath, AudioLanguage lang)
    {
        GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(gameId);
        if (string.IsNullOrWhiteSpace(config?.AudioPackageScanDir))
        {
            return;
        }
        string file = Path.Join(installPath, config.AudioPackageScanDir);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var lines = new List<string>(4);
        if (lang.HasFlag(AudioLanguage.Chinese)) { lines.Add("Chinese"); }
        if (lang.HasFlag(AudioLanguage.English)) { lines.Add("English(US)"); }
        if (lang.HasFlag(AudioLanguage.Japanese)) { lines.Add("Japanese"); }
        if (lang.HasFlag(AudioLanguage.Korean)) { lines.Add("Korean"); }
        await File.WriteAllLinesAsync(file, lines);
    }


}
