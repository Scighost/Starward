using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Core.Hypergryph;
using Starward.Features.Hypergryph;
using Starward.Features.GameSelector;
using Starward.Features.GameSetting;
using Starward.Features.Passport;
using Starward.Features.PlayTime;
using Starward.GameInstall;
using Starward.GameInstall.HotUpdate;
using Starward.GameInstall.Model;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.GameLauncher;

internal partial class GameLauncherService
{
    private readonly ILogger<GameLauncherService> _logger;

    private readonly HoYoPlayService _hoYoPlayService;

    private readonly GameConfigService _gameConfigService;

    private readonly PassportService _passportService;

    private readonly PlayTimeRecordService _playTimeRecorderService;

    private readonly GenshinHotUpdateService _genshinHotUpdateService;

    private readonly HypergryphLauncherClient _hypergryphLauncherClient;

    public GameLauncherService(ILogger<GameLauncherService> logger,
                               HoYoPlayService hoYoPlayService,
                               PassportService passportService,
                               GameConfigService gameConfigService,
                               PlayTimeRecordService playTimeRecorderService,
                               GenshinHotUpdateService genshinHotUpdateService,
                               HypergryphLauncherClient hypergryphLauncherClient)
    {
        _logger = logger;
        _hoYoPlayService = hoYoPlayService;
        _gameConfigService = gameConfigService;
        _passportService = passportService;
        _playTimeRecorderService = playTimeRecorderService;
        _genshinHotUpdateService = genshinHotUpdateService;
        _hypergryphLauncherClient = hypergryphLauncherClient;
    }


    #region Install Path


    /// <summary>
    /// 游戏安装目录，为空时未找到
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public static string? GetGameInstallPath(GameId gameId)
    {
        GameEntry? entry = GameEntryService.GetFromId(gameId);
        if (string.IsNullOrWhiteSpace(entry?.InstallPath))
        {
            return null;
        }
        string path = GetFullPathIfRelativePath(entry.InstallPath);
        if (Directory.Exists(path))
        {
            return Path.GetFullPath(path);
        }
        else if (entry.Removable)
        {
            return path;
        }
        else
        {
            GameEntryService.SetInstallPathNull(gameId);
            return null;
        }
    }


    /// <summary>
    /// 游戏安装目录，为空时未找到
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="storageRemoved">可移动存储设备已移除</param>
    /// <returns></returns>
    public static string? GetGameInstallPath(GameId gameId, out bool storageRemoved)
    {
        storageRemoved = false;
        GameEntry? entry = GameEntryService.GetFromId(gameId);
        if (string.IsNullOrWhiteSpace(entry?.InstallPath))
        {
            return null;
        }
        string path = GetFullPathIfRelativePath(entry.InstallPath);
        if (Directory.Exists(path))
        {
            return path;
        }
        else if (entry.Removable)
        {
            storageRemoved = true;
            return path;
        }
        else
        {
            GameEntryService.SetInstallPathNull(gameId);
            return null;
        }
    }

    /// <summary>
    /// 修改游戏安装目录
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public string? ChangeGameInstallPath(GameId gameId, string? path)
    {
        LauncherConfig? model = _hoYoPlayService.GetLauncherConfig(gameId);
        if (model is null)
        {
            throw new ArgumentNullException($"Cannot get LauncherConfig from GameId ({gameId}).");
        }
        if (Directory.Exists(path))
        {
            path = Path.GetFullPath(path);
            string relativePath = GetRelativePathIfInRemovableStorage(path, out bool removable);
            GameEntryService.SetInstallPath(gameId, model.Id, relativePath, removable);
            WeakReferenceMessenger.Default.Send(new PinGameIconMessage(gameId, model, true));
        }
        else
        {
            path = null;
            GameEntryService.SetInstallPath(gameId, model.Id, null, false);
        }
        return path;
    }


    /// <summary>
    /// 如果安装在可移动存储设备中，获取相对路径
    /// </summary>
    /// <param name="path"></param>
    /// <param name="removableStorage"></param>
    /// <returns></returns>
    public static string GetRelativePathIfInRemovableStorage(string path, out bool removableStorage)
    {
        removableStorage = DriveHelper.IsDeviceRemovableOrOnUSB(path);
        if (removableStorage && Path.GetPathRoot(AppConfig.StarwardExecutePath) == Path.GetPathRoot(path))
        {
            path = Path.GetRelativePath(Path.GetDirectoryName(AppConfig.ConfigPath)!, path);
        }
        return path;
    }


    /// <summary>
    /// 如果安装在可移动存储设备中，获取完整路径
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string GetFullPathIfRelativePath(string path)
    {
        if (Path.IsPathFullyQualified(path))
        {
            return Path.GetFullPath(path);
        }
        else
        {
            return Path.GetFullPath(path, Path.GetDirectoryName(AppConfig.ConfigPath)!);
        }
    }


    #endregion



    #region Game Version


    /// <summary>
    /// 本地游戏版本
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public async Task<Version?> GetLocalGameVersionAsync(GameId gameId, string? installPath = null, CancellationToken cancellation = default)
    {
        installPath ??= GetGameInstallPath(gameId);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return null;
        }
        GameBiz gameBiz = gameId.GameBiz;
        if (HypergryphGameConstants.IsHypergryphGame(gameBiz))
        {
            HypergryphGameProfile profile = HypergryphGameConstants.GetGameProfile(gameBiz);
            HypergryphInstallMetadata? metadata = await HypergryphInstallMetadata.ReadAsync(installPath);
            Version? metadataVersion = null;
            if (metadata?.IsFor(gameBiz) is true
                && Version.TryParse(metadata.Version, out metadataVersion))
            {
                // The official launcher updates the game in place and does not update
                // Starward's sidecar metadata. Verify the small game_files marker before
                // trusting the cached version, otherwise an official update looks stale.
                string cachedGameFilesPath = Path.Combine(installPath, "game_files");
                if (!File.Exists(cachedGameFilesPath)
                    || string.IsNullOrWhiteSpace(metadata.GameFilesMD5))
                {
                    return metadataVersion;
                }

                await using FileStream stream = File.OpenRead(cachedGameFilesPath);
                string currentGameFilesMD5 = Convert.ToHexStringLower(await MD5.HashDataAsync(stream));
                if (string.Equals(currentGameFilesMD5, metadata.GameFilesMD5, StringComparison.OrdinalIgnoreCase))
                {
                    return metadataVersion;
                }

                _logger.LogInformation(
                    "Hypergryph game marker changed for {GameBiz}; refreshing cached version metadata.",
                    gameBiz.Value);
            }

            string exePath = Path.Combine(installPath, profile.ExeName);
            if (!File.Exists(exePath))
            {
                return null;
            }

            string gameFilesPath = Path.Combine(installPath, "game_files");
            if (File.Exists(gameFilesPath))
            {
                HypergryphLatestGame latest = await _hypergryphLauncherClient.GetLatestGameAsync(gameBiz, null);
                await using FileStream stream = File.OpenRead(gameFilesPath);
                string md5 = Convert.ToHexStringLower(await MD5.HashDataAsync(stream));
                if (string.Equals(md5, latest.Package.GameFilesMD5, StringComparison.OrdinalIgnoreCase)
                    && Version.TryParse(latest.Version, out Version? latestVersion))
                {
                    await new HypergryphInstallMetadata
                    {
                        AppCode = profile.GameAppCode,
                        Version = latest.Version,
                        GameFilesMD5 = latest.Package.GameFilesMD5,
                    }.WriteAsync(installPath);
                    return latestVersion;
                }
            }

            // The game is importable, but its encrypted launcher metadata does not expose a version.
            return metadataVersion ?? new Version(0, 0, 0);
        }
        return (await GameConfigService.GetGameConfigIniAsync(installPath, cancellation))?.GameVersion;
    }


    /// <summary>
    /// 最新游戏版本
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <returns></returns>
    public async Task<(Version? Latest, Version? Predownload)> GetLatestGameVersionAsync(GameId gameId, CancellationToken cancellation)
    {
        if (HypergryphGameConstants.IsHypergryphGame(gameId.GameBiz))
        {
            Version? localVersion = await GetLocalGameVersionAsync(gameId);
            HypergryphLatestGame latest = await _hypergryphLauncherClient.GetLatestGameAsync(
                gameId.GameBiz,
                localVersion?.ToString());
            _ = Version.TryParse(latest.Version, out Version? latestVersion);
            Version? predownloadVersion = null;
            if (latest.PrePatch is not null && latest.PrePatch.DownloadParts.Count > 0 && latestVersion is not null)
            {
                string targetVersion = string.IsNullOrWhiteSpace(latest.PrePatch.TargetVersion)
                    ? latest.PrePatch.Version
                    : latest.PrePatch.TargetVersion;
                if (!Version.TryParse(targetVersion, out predownloadVersion) || predownloadVersion <= latestVersion)
                {
                    predownloadVersion = new Version(latestVersion.Major, latestVersion.Minor, Math.Max(0, latestVersion.Build), Math.Max(0, latestVersion.Revision) + 1);
                }
            }
            return (latestVersion, predownloadVersion);
        }
        LauncherConfig? launcher = _hoYoPlayService.GetLauncherConfig(gameId);
        GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(gameId, cancellation);
        if (config is null)
        {
            throw new ArgumentOutOfRangeException($"Game config is null ({gameId.Id}, {gameId.GameBiz}).");
        }
        if (config.DefaultDownloadMode is DownloadMode.DOWNLOAD_MODE_FILE)
        {
            GamePackage? package = await _hoYoPlayService.GetGamePackageAsync(gameId, cancellation);
            _ = Version.TryParse(package?.Main.Major?.Version, out Version? latestVersion);
            _ = Version.TryParse(package?.PreDownload.Major?.Version, out Version? predownloadVersion);
            return (latestVersion, predownloadVersion);
        }
        else
        {
            GameBranch? gameBranch = await _hoYoPlayService.GetGameBranchAsync(gameId, cancellation);
            if (gameBranch is null)
            {
                throw new ArgumentOutOfRangeException($"Game branch is null ({gameId.Id}, {gameId.GameBiz}).");
            }
            if (launcher?.IsBeta is true)
            {
                await _hoYoPlayService.GetGameSophonChunkBuildAsync(gameId, gameBranch.Main, null, cancellation);
            }
            _ = Version.TryParse(gameBranch.Main.Tag, out Version? latestVersion);
            _ = Version.TryParse(gameBranch.PreDownload?.Tag, out Version? predownloadVersion);
            return (latestVersion, predownloadVersion);
        }
    }


    /// <summary>
    /// 检查预下载是否完成
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public async Task<bool> CheckPreDownloadFinishedAsync(GameId gameId, string? installPath = null, CancellationToken cancellation = default)
    {
        installPath ??= GetGameInstallPath(gameId);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return false;
        }
        if (HypergryphGameConstants.IsHypergryphGame(gameId.GameBiz))
        {
            Version? localVersion = await GetLocalGameVersionAsync(gameId, installPath, cancellation);
            HypergryphLatestGame latest = await _hypergryphLauncherClient.GetLatestGameAsync(
                gameId.GameBiz,
                localVersion?.ToString(), cancellation);
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
        var config = await GameConfigService.GetGameConfigIniAsync(installPath, cancellation);
        if (config is null)
        {
            return false;
        }
        (_, Version? pre) = await GetLatestGameVersionAsync(gameId, cancellation);
        AudioLanguage language = await _gameConfigService.GetAudioLanguageAsync(gameId, installPath, cancellation);
        return config.Settings.GetValueOrDefault("predownload") == $"{config.GameVersion},{pre},{language}";
    }


    /// <summary>
    /// 检查插件更新
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="installPath"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    public async Task<bool> CheckPluginUpdateAsync(GameId gameId, string? installPath = null, CancellationToken cancellation = default)
    {
        installPath ??= GetGameInstallPath(gameId);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return false;
        }
        GamePluginRelease? pluginRelease = await _hoYoPlayService.GetGamePluginAsync(gameId, cancellation);
        if (pluginRelease is null)
        {
            return false;
        }
        GameConfigIni? configIni = await GameConfigService.GetGameConfigIniAsync(installPath, cancellation);
        foreach (var item in pluginRelease.Plugins)
        {
            if (item.PluginPackage.Url.Contains("DXSETUP", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            string key = $"plugin_{item.PluginId}_version";
            if (configIni?.Settings.GetValueOrDefault(key) == item.Version)
            {
                continue;
            }
            if (string.IsNullOrWhiteSpace(item.PluginPackage.Validation))
            {
                return true;
            }
            else if (!await GameResourceService.VerifyPluginAsync(installPath, item.PluginPackage, cancellation))
            {
                return true;
            }
        }
        return false;
    }



    public async Task<bool> CheckGenshinHotUpdateAsync(GameId gameId, string? installPath = null, CancellationToken cancellation = default)
    {
        if (gameId.GameBiz.Game is not GameBiz.hk4e || !AppConfig.GetEnableCutsceneManage(gameId))
        {
            return false;
        }
        var launcher = _hoYoPlayService.GetLauncherConfig(gameId);
        if (launcher is null || launcher.IsBeta)
        {
            return false;
        }
        installPath ??= GetGameInstallPath(gameId);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return false;
        }
        return await _genshinHotUpdateService.CheckUpdateAsync(gameId, installPath, cancellation);
    }



    #endregion



    #region Start Game


    /// <summary>
    /// 游戏进程名，带 .exe 扩展名
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public async Task<string> GetGameExeNameAsync(GameId gameId, CancellationToken cancellation)
    {
        string? name = GetGameExeName(gameId.GameBiz);
        if (string.IsNullOrWhiteSpace(name))
        {
            var config = await _hoYoPlayService.GetGameConfigAsync(gameId, cancellation);
            name = config?.ExeFileName;
        }
        return name ?? throw new ArgumentOutOfRangeException($"Unknown game ({gameId.Id}, {gameId.GameBiz}).");
    }


    /// <summary>
    /// 游戏进程名，带 .exe 扩展名
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public static string? GetGameExeName(GameBiz gameBiz)
    {
        string? name = gameBiz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => "YuanShen.exe",
            GameBiz.hk4e_global => "GenshinImpact.exe",
            GameBiz.arknights_cn => HypergryphGameConstants.ArknightsExeName,
            GameBiz.endfield_cn or GameBiz.endfield_global => HypergryphGameConstants.EndfieldExeName,
            _ => gameBiz.Game switch
            {
                GameBiz.hkrpg => "StarRail.exe",
                GameBiz.bh3 => "BH3.exe",
                GameBiz.nap => "ZenlessZoneZero.exe",
                _ => null,
            },
        };
        return name;
    }


    /// <summary>
    /// 游戏进程文件是否存在
    /// </summary>
    /// <param name="biz"></param>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public async Task<bool> IsGameExeExistsAsync(GameId gameId, string? installPath = null, CancellationToken cancellation = default)
    {
        installPath ??= GetGameInstallPath(gameId);
        if (!string.IsNullOrWhiteSpace(installPath))
        {
            var exe = Path.Join(installPath, await GetGameExeNameAsync(gameId, cancellation));
            return File.Exists(exe);
        }
        return false;
    }


    /// <summary>
    /// 获取游戏进程
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public async Task<Process?> GetGameProcessAsync(GameId gameId, CancellationToken cancellation)
    {
        int currentSessionId = Process.GetCurrentProcess().SessionId;
        var name = (await GetGameExeNameAsync(gameId, cancellation)).Replace(".exe", "");
        return Process.GetProcessesByName(name).Where(x => x.SessionId == currentSessionId && !IsProcessPending(x)).FirstOrDefault();
    }


    /// <summary>
    /// 检测进程挂起
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    public static bool IsProcessPending(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return false;
            }
            foreach (ProcessThread thread in process.Threads)
            {
                if (thread.ThreadState is not System.Diagnostics.ThreadState.Wait)
                {
                    return false;
                }
                else if (thread.WaitReason is not ThreadWaitReason.Suspended)
                {
                    return false;
                }
            }
            return true;
        }
        catch { }
        return true;
    }


    /// <summary>
    /// 启动游戏
    /// </summary>
    /// <returns></returns>
    public async Task<Process?> StartGameAsync(GameId gameId, string? installPath = null, CancellationToken cancellation = default)
    {
        const int ERROR_CANCELLED = 0x000004C7;
        try
        {
            if (await GetGameProcessAsync(gameId, cancellation) is Process existingProcess)
            {
                throw new Exception($"Game is running: {existingProcess.ProcessName}.exe ({existingProcess.Id}).");
            }
            string? exe = null, verb = null;
            if (Directory.Exists(installPath))
            {
                var e = Path.Join(installPath, await GetGameExeNameAsync(gameId, cancellation));
                if (File.Exists(e))
                {
                    exe = e;
                }
            }
            bool thirdPartyTool = false;
            if (string.IsNullOrWhiteSpace(exe) && AppConfig.GetEnableThirdPartyTool(gameId))
            {
                exe = GetThirdPartyToolPath(gameId);
                if (File.Exists(exe))
                {
                    thirdPartyTool = true;
                    verb = Path.GetExtension(exe) is ".exe" or ".bat" ? "runas" : "";
                }
                else
                {
                    exe = null;
                    SetThirdPartyToolPath(gameId, null);
                    _logger.LogWarning("Third party tool not found: {path}", exe);
                }
            }
            if (string.IsNullOrWhiteSpace(exe))
            {
                var folder = GetGameInstallPath(gameId);
                var name = await GetGameExeNameAsync(gameId, cancellation);
                exe = Path.Join(folder, name);
                verb = "runas";
                if (!File.Exists(exe))
                {
                    _logger.LogWarning("Game exe not found: {path}", exe);
                    throw new FileNotFoundException("Game exe not found", name);
                }
            }

            CancellationToken token = CancellationTokenSource.CreateLinkedTokenSource(cancellation, new CancellationTokenSource(3000).Token).Token;
            var sb = new StringBuilder();
            sb.Append(AppConfig.GetStartArgument(gameId)?.Trim());
            if (HypergryphGameConstants.IsEndfield(gameId.GameBiz) && AppConfig.GetUsePopupWindow(gameId))
            {
                sb.Append(" -popupwindow");
            }
            if (HypergryphGameConstants.IsEndfield(gameId.GameBiz) && AppConfig.GetEnableDX11(gameId.GameBiz))
            {
                sb.Append(" -force-d3d11");
            }
            if (!HypergryphGameConstants.IsHypergryphGame(gameId.GameBiz))
            {
                await AppendDX12Async(sb, gameId, token);
                await AppendLoginAuthTicketAsync(sb, gameId, token);
            }
            SetGenshinEnableHDR(gameId);
            string arg = sb.ToString();

            if (!thirdPartyTool && HypergryphGameConstants.IsEndfield(gameId.GameBiz))
            {
                await TryShareEndfieldVoiceFilesAsync(gameId.GameBiz, Path.GetDirectoryName(exe)!);
                _logger.LogInformation("Start Endfield with a signed auxiliary parent: {Path}", exe);
                Process game = await EndfieldLauncher.StartAsync(exe, arg);
                await _playTimeRecorderService.StartProcessToLogAsync(gameId, game.Id);
                return game;
            }
            if (!thirdPartyTool && AppConfig.StartGameWithCMD)
            {
                arg = $"""/c start "" /d "{Path.GetDirectoryName(exe)}" "{exe}" {arg}""";
                exe = "cmd.exe";
            }
            var info = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arg,
                UseShellExecute = true,
                Verb = verb,
                WorkingDirectory = Path.GetDirectoryName(exe),
            };
            Process? process = Process.Start(info);
            if (process != null)
            {
                if (thirdPartyTool || AppConfig.StartGameWithCMD)
                {
                    return await _playTimeRecorderService.StartProcessToLogAsync(gameId);
                }
                else
                {
                    await _playTimeRecorderService.StartProcessToLogAsync(gameId, process.Id);
                    return process;
                }
            }
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_CANCELLED)
        {
            // Operation canceled
            _logger.LogInformation("Start game operation canceled.");
        }
        return null;
    }


    private async Task AppendDX12Async(StringBuilder sb, GameId gameId, CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested)
        {
            return;
        }
        try
        {
            var gameConfig = await _hoYoPlayService.GetGameConfigAsync(gameId, cancellation);
            if (gameConfig?.EnableDxSwitch is true)
            {
                var dxConfig = await _hoYoPlayService.GetDXConfigAsync(gameId, GPUInfoHelper.GetGPUInfos(), cancellation);
                if (dxConfig?.EnableDXSwitch is true)
                {
                    sb.Append(' ');
                    bool? enable = AppConfig.GetEnableDX12(gameId);
                    if (dxConfig.AutoMode && enable.HasValue)
                    {
                        sb.Append(enable.Value ? dxConfig.CmdArgs : dxConfig.DX11CmdArgs);
                    }
                    else if (enable.HasValue)
                    {
                        sb.Append(enable.Value ? dxConfig.CmdArgs : dxConfig.DX11CmdArgs);
                    }
                    else
                    {
                        sb.Append(dxConfig.UseDX12ByDefault ? dxConfig.CmdArgs : dxConfig.DX11CmdArgs);
                    }
                }
            }
        }
        catch { }
    }


    private async Task AppendLoginAuthTicketAsync(StringBuilder sb, GameId gameId, CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested)
        {
            return;
        }
        try
        {
            if (AppConfig.AuthLoginGameByPassportAccount)
            {
                string? ticket = await _passportService.CreateAuthTicketAsync(gameId, cancellation);
                if (!string.IsNullOrWhiteSpace(ticket))
                {
                    sb.Append($" login_auth_ticket={ticket}");
                    if (AppConfig.IsAppInRemovableStorage)
                    {
                        sb.Append(" login_storage_type=session");
                    }
                    else
                    {
                        sb.Append(" login_storage_type=local");
                    }
                }
            }
        }
        catch { }
    }


    private void SetGenshinEnableHDR(GameId gameId)
    {
        if (gameId.GameBiz.Game is GameBiz.hk4e)
        {
            GameSettingService.SetGenshinEnableHDR(gameId.GameBiz, AppConfig.EnableGenshinHDR);
        }
    }


    #endregion



    #region Third Party Tool


    /// <summary>
    /// 获取第三方工具路径
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public static string? GetThirdPartyToolPath(GameId gameId)
    {
        string? path = AppConfig.GetThirdPartyToolPath(gameId);
        if (!string.IsNullOrWhiteSpace(path))
        {
            path = GetFullPathIfRelativePath(path);
        }
        if (File.Exists(path))
        {
            return path;
        }
        else
        {
            AppConfig.SetThirdPartyToolPath(gameId, null);
            return null;
        }
    }


    /// <summary>
    /// 设置第三方工具路径
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string? SetThirdPartyToolPath(GameId gameId, string? path)
    {
        if (File.Exists(path))
        {
            path = Path.GetFullPath(path);
            string relativePath = GetRelativePathIfInRemovableStorage(path, out bool removable);
            AppConfig.SetThirdPartyToolPath(gameId, relativePath);
        }
        else
        {
            path = null;
            AppConfig.SetThirdPartyToolPath(gameId, null);
        }
        return path;
    }


    #endregion


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



}
