using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using ZstdSharp;

namespace Starward.RPC.GameInstall;

internal partial class GamePackageService
{


    private readonly ILogger<GamePackageService> _logger;

    private readonly HoYoPlayClient _hoyoplayClient;

    private readonly HttpClient _httpClient;


    public GamePackageService(ILogger<GamePackageService> logger, HoYoPlayClient hoYoPlayClient, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _hoyoplayClient = hoYoPlayClient;
    }





    /// <summary>
    /// 本地游戏版本
    /// </summary>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public async Task<Version?> GetLocalGameVersionAsync(string installPath)
    {
        var config = Path.Join(installPath, "config.ini");
        if (File.Exists(config))
        {
            var str = await File.ReadAllTextAsync(config);
            _ = Version.TryParse(GameVersionRegex().Match(str).Groups[1].Value, out Version? version);
            return version;
        }
        else
        {
            _logger.LogWarning("config.ini not found: {path}", config);
            return null;
        }
    }


    [GeneratedRegex(@"game_version=(.+)")]
    private static partial Regex GameVersionRegex();





    /// <summary>
    /// 设置语音包语言
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="installPath"></param>
    /// <param name="lang"></param>
    /// <returns></returns>
    public async Task SetAudioLanguageAsync(GameId gameId, string installPath, AudioLanguage lang, CancellationToken cancellationToken = default)
    {
        GameConfig? config = await GetGameConfigAsync(gameId, cancellationToken);
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
        await File.WriteAllLinesAsync(file, lines, cancellationToken);
    }



    /// <summary>
    /// 准备游戏包
    /// </summary>
    /// <param name="task"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task PrepareGamePackageAsync(GameInstallTask task, CancellationToken cancellationToken = default)
    {
        try
        {
            task.GameConfig = await GetGameConfigAsync(task.GameId, cancellationToken);
            if (task.GameConfig is null)
            {
                _logger.LogWarning("GameConfig of ({GameBiz}) is null.", task.GameId.GameBiz);
                throw new ArgumentNullException($"GameConfig of ({task.GameId.GameBiz}) is null.");
            }
            if (task.Operation is GameInstallOperation.Predownload or GameInstallOperation.Update)
            {
                await PrepareForPredownloadOrUpdateAsync(task, cancellationToken);
            }
            else if (task.Operation is GameInstallOperation.Install or GameInstallOperation.Repair)
            {
                await PrepareForInstallOrRepairAsync(task, cancellationToken);
            }
            task.GameChannelSDK = await GetGameChannelSDKAsync(task.GameId, cancellationToken);
            task.DeprecatedFileConfig = await GetGameDeprecatedFileAsync(task.GameId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Prepare game package ({GameBiz})", task.GameId.GameBiz);
            throw;
        }
    }


    /// <summary>
    /// 准备安装或修复
    /// </summary>
    /// <param name="task"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task PrepareForInstallOrRepairAsync(GameInstallTask task, CancellationToken cancellationToken = default)
    {
        GameId gameId = task.GameId;
        if (task.GameConfig!.DefaultDownloadMode is DownloadMode.DOWNLOAD_MODE_CHUNK or DownloadMode.DOWNLOAD_MODE_LDIFF)
        {
            var branch = await _hoyoplayClient.GetGameBranchAsync(LauncherId.FromGameId(gameId)!, "en-us", gameId, cancellationToken);
            if (branch is not null)
            {
                task.LatestGameVersion = branch.Main.Tag;
                task.PredownloadVersion = branch.PreDownload?.Tag;
                task.GameSophonChunkBuild = await GetGameSophonChunkBuildAsync(branch, branch.Main, "", cancellationToken);
                Version? localVersion = await GetLocalGameVersionAsync(task.InstallPath);
                if (localVersion is not null)
                {
                    task.LocalVersionSophonChunkBuild = await GetGameSophonChunkBuildAsync(branch, branch.Main, localVersion.ToString(), cancellationToken);
                }
            }
        }
        if (task.GameSophonChunkBuild is null)
        {
            task.GamePackage = await GetGamePackageAsync(gameId, cancellationToken);
            task.LatestGameVersion = task.GamePackage?.Main.Major?.Version;
            task.PredownloadVersion = task.GamePackage?.PreDownload.Major?.Version;
        }
        await PrepareGameInstallTaskFilesAsync(task, null, cancellationToken);
    }


    /// <summary>
    /// 准备预下载或更新
    /// </summary>
    /// <param name="task"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    private async Task PrepareForPredownloadOrUpdateAsync(GameInstallTask task, CancellationToken cancellationToken = default)
    {
        GameId gameId = task.GameId;
        // 本地游戏版本
        Version? localVersion = await GetLocalGameVersionAsync(task.InstallPath);
        if (localVersion is null)
        {
            _logger.LogWarning("LocalGameVersion of ({GameBiz}) is null.", gameId.GameBiz);
            throw new ArgumentNullException($"LocalGameVersion of ({gameId.GameBiz}) is null.");
        }
        task.LocalGameVersion = localVersion.ToString();
        GamePackage? package = await GetGamePackageAsync(gameId, cancellationToken);
        if (package is null)
        {
            _logger.LogError("GamePackage of ({GameBiz}) is null.", gameId.GameBiz);
            throw new ArgumentNullException($"GamePackage of ({gameId.GameBiz}) is null.");
        }
        if (task.Operation is GameInstallOperation.Predownload && package.PreDownload.Major is null)
        {
            _logger.LogError("Predownload package of ({GameBiz}) is null.", gameId.GameBiz);
            throw new NotSupportedException($"Predownload package of ({gameId.GameBiz}) is null.");
        }
        task.LatestGameVersion = package.Main.Major?.Version;
        if (package.PreDownload.Major is null)
        {
            // 更新
            // 本地游戏版本是否有补丁
            bool canPatch = package.Main.Patches.Any(x => x.Version == localVersion.ToString());

            if (task.GameConfig!.DefaultDownloadMode is DownloadMode.DOWNLOAD_MODE_CHUNK or DownloadMode.DOWNLOAD_MODE_LDIFF)
            {
                var branch = await _hoyoplayClient.GetGameBranchAsync(LauncherId.FromGameId(gameId)!, "en-us", gameId, cancellationToken);
                if (branch?.Main is not null)
                {
                    if (canPatch)
                    {
                        task.GameSophonPatchBuild = await GetGameSophonPatchBuildAsync(branch, branch.Main, cancellationToken);
                    }
                    if (task.GameSophonPatchBuild is null)
                    {
                        task.GameSophonChunkBuild = await GetGameSophonChunkBuildAsync(branch, branch.Main, "", cancellationToken);
                        task.LocalVersionSophonChunkBuild = await GetGameSophonChunkBuildAsync(branch, branch.Main, localVersion.ToString(), cancellationToken);
                    }
                }
            }
            if (task.GameSophonPatchBuild is null && task.GameSophonChunkBuild is null)
            {
                task.GamePackage = package;
            }
        }
        else
        {
            // 预下载
            task.PredownloadVersion = package.PreDownload.Major.Version;

            //bool hasPatch = package.PreDownload.Patches.Count > 0;
            bool canPatch = package.PreDownload.Patches.Any(x => x.Version == localVersion.ToString());

            if (task.GameConfig!.DefaultDownloadMode is DownloadMode.DOWNLOAD_MODE_CHUNK)
            {
                var branch = await _hoyoplayClient.GetGameBranchAsync(LauncherId.FromGameId(gameId)!, "en-us", gameId, cancellationToken);
                if (branch?.PreDownload is not null)
                {
                    if (canPatch)
                    {
                        task.GameSophonPatchBuild = await GetGameSophonPatchBuildAsync(branch, branch.PreDownload, cancellationToken);
                    }
                    if (task.GameSophonPatchBuild is null)
                    {
                        task.GameSophonChunkBuild = await GetGameSophonChunkBuildAsync(branch, branch.PreDownload, "", cancellationToken);
                        task.LocalVersionSophonChunkBuild = await GetGameSophonChunkBuildAsync(branch, branch.PreDownload, localVersion.ToString(), cancellationToken);
                    }
                }
            }
            if (task.GameSophonPatchBuild is null && task.GameSophonChunkBuild is null)
            {
                task.GamePackage = package;
            }
        }
        await PrepareGameInstallTaskFilesAsync(task, localVersion.ToString(), cancellationToken);
    }




    /// <summary>
    /// 准备游戏任务的所有文件信息
    /// </summary>
    /// <param name="task"></param>
    /// <param name="localVersion"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task PrepareGameInstallTaskFilesAsync(GameInstallTask task, string? localVersion = null, CancellationToken cancellationToken = default)
    {
        List<GameInstallFile> taskFiles = new();
        if (task.Operation is GameInstallOperation.Predownload or GameInstallOperation.Update && !string.IsNullOrWhiteSpace(localVersion))
        {
            // 预下载或更新
            if (task.GameSophonPatchBuild is not null)
            {
                task.DownloadMode = GameInstallDownloadMode.Patch;
                List<SophonPatchFile> patches = new();
                List<SophonPatchDeleteFile> deletes = new();
                if (task.GameSophonPatchBuild.Manifests.FirstOrDefault(x => x.MatchingField is "game") is GameSophonPatchManifest manifest)
                {
                    SophonPatchManifest patchManifest = await GetSophonPatchManifestAsync(manifest, cancellationToken);
                    patches.AddRange(patchManifest.Patches);
                    if (patchManifest.Deletes.FirstOrDefault(x => x.Tag == localVersion) is SophonPatchDeleteInfo info)
                    {
                        deletes.AddRange(info.Deletes);
                    }
                    foreach (SophonPatchFile item in patchManifest.Patches)
                    {
                        taskFiles.Add(GameInstallFile.FromSophonPatchFile(item, task.InstallPath, localVersion, manifest.DiffDownload.UrlPrefix));
                    }
                }
                foreach (var lang in Enum.GetValues<AudioLanguage>())
                {
                    if (task.AudioLanguage.HasFlag(lang))
                    {
                        if (task.GameSophonPatchBuild.Manifests.FirstOrDefault(x => x.MatchingField == lang.ToDescription()) is GameSophonPatchManifest audioManifest)
                        {
                            SophonPatchManifest patchManifest = await GetSophonPatchManifestAsync(audioManifest, cancellationToken);
                            patches.AddRange(patchManifest.Patches);
                            if (patchManifest.Deletes.FirstOrDefault(x => x.Tag == localVersion) is SophonPatchDeleteInfo info)
                            {
                                deletes.AddRange(info.Deletes);
                            }
                            foreach (SophonPatchFile item in patchManifest.Patches)
                            {
                                taskFiles.Add(GameInstallFile.FromSophonPatchFile(item, task.InstallPath, localVersion, audioManifest.DiffDownload.UrlPrefix));
                            }
                        }
                    }
                }
                task.SophonPatchFiles = patches;
                task.SophonPatchDeleteFiles = deletes;
            }
            else if (task.GameSophonChunkBuild is not null)
            {
                task.DownloadMode = GameInstallDownloadMode.Chunk;
                List<SophonChunkFile> chunks = new();
                List<SophonChunkFile> localChunks = new();

                if (task.GameSophonChunkBuild.Manifests.FirstOrDefault(x => x.MatchingField is "game") is GameSophonChunkManifest manifest)
                {
                    List<SophonChunkFile> items = await GetSophonChunkFilesAsync(manifest, cancellationToken);
                    chunks.AddRange(items);
                    Dictionary<string, SophonChunkFile> dict;
                    if (task.LocalVersionSophonChunkBuild?.Manifests.FirstOrDefault(x => x.MatchingField is "game") is GameSophonChunkManifest localManifest)
                    {
                        List<SophonChunkFile> localItems = await GetSophonChunkFilesAsync(localManifest);
                        localChunks.AddRange(localItems);
                        dict = localItems.ToDictionary(x => x.File);
                    }
                    else
                    {
                        dict = new();
                    }
                    foreach (SophonChunkFile item in items)
                    {
                        if (!item.IsFolder)
                        {
                            dict.TryGetValue(item.File, out SophonChunkFile? localFile);
                            taskFiles.Add(GameInstallFile.FromSophonChunkFile(item, localFile, task.InstallPath, manifest.ChunkDownload.UrlPrefix));
                        }
                    }
                }
                foreach (var lang in Enum.GetValues<AudioLanguage>())
                {
                    if (task.AudioLanguage.HasFlag(lang))
                    {
                        if (task.GameSophonChunkBuild.Manifests.FirstOrDefault(x => x.MatchingField == lang.ToDescription()) is GameSophonChunkManifest audioManifest)
                        {
                            List<SophonChunkFile> items = await GetSophonChunkFilesAsync(audioManifest, cancellationToken);
                            chunks.AddRange(items);
                            Dictionary<string, SophonChunkFile> dict;
                            if (task.LocalVersionSophonChunkBuild?.Manifests.FirstOrDefault(x => x.MatchingField is "game") is GameSophonChunkManifest localAudioManifest)
                            {
                                List<SophonChunkFile> localItems = await GetSophonChunkFilesAsync(localAudioManifest);
                                localChunks.AddRange(localItems);
                                dict = localItems.ToDictionary(x => x.File);
                            }
                            else
                            {
                                dict = new();
                            }
                            foreach (SophonChunkFile item in items)
                            {
                                if (!item.IsFolder)
                                {
                                    dict.TryGetValue(item.File, out SophonChunkFile? localFile);
                                    taskFiles.Add(GameInstallFile.FromSophonChunkFile(item, localFile, task.InstallPath, audioManifest.ChunkDownload.UrlPrefix));
                                }
                            }
                        }
                    }
                }

                task.SophonChunkFiles = chunks;
                if (localChunks.Count > 0)
                {
                    task.LocalVersionSophonChunkFiles = localChunks;
                }
            }
            else if (task.GamePackage is not null)
            {
                task.DownloadMode = GameInstallDownloadMode.CompressedPackage;
                if (task.GamePackage.PreDownload.Major is null)
                {
                    if (task.GamePackage.Main.Patches.FirstOrDefault(x => x.Version == localVersion) is GamePackageResource patch)
                    {
                        taskFiles.Add(GameInstallFile.FromGamePackageResource(patch, task.InstallPath));
                        foreach (var lang in Enum.GetValues<AudioLanguage>())
                        {
                            if (task.AudioLanguage.HasFlag(lang))
                            {
                                if (patch.AudioPackages.FirstOrDefault(x => x.Language == lang.ToDescription()) is GamePackageFile packageFile)
                                {
                                    taskFiles.Add(GameInstallFile.FromGamePackageFile(packageFile, task.InstallPath));
                                }
                            }
                        }
                    }
                    else
                    {
                        taskFiles.Add(GameInstallFile.FromGamePackageResource(task.GamePackage.Main.Major!, task.InstallPath));
                        foreach (var lang in Enum.GetValues<AudioLanguage>())
                        {
                            if (task.AudioLanguage.HasFlag(lang))
                            {
                                if (task.GamePackage.Main.Major!.AudioPackages.FirstOrDefault(x => x.Language == lang.ToDescription()) is GamePackageFile packageFile)
                                {
                                    taskFiles.Add(GameInstallFile.FromGamePackageFile(packageFile, task.InstallPath));
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (task.GamePackage.PreDownload.Patches.FirstOrDefault(x => x.Version == localVersion) is GamePackageResource patch)
                    {
                        taskFiles.Add(GameInstallFile.FromGamePackageResource(patch, task.InstallPath));
                        foreach (var lang in Enum.GetValues<AudioLanguage>())
                        {
                            if (task.AudioLanguage.HasFlag(lang))
                            {
                                if (patch.AudioPackages.FirstOrDefault(x => x.Language == lang.ToDescription()) is GamePackageFile packageFile)
                                {
                                    taskFiles.Add(GameInstallFile.FromGamePackageFile(packageFile, task.InstallPath));
                                }
                            }
                        }
                    }
                    else
                    {
                        taskFiles.Add(GameInstallFile.FromGamePackageResource(task.GamePackage.PreDownload.Major!, task.InstallPath));
                        foreach (var lang in Enum.GetValues<AudioLanguage>())
                        {
                            if (task.AudioLanguage.HasFlag(lang))
                            {
                                if (task.GamePackage.PreDownload.Major!.AudioPackages.FirstOrDefault(x => x.Language == lang.ToDescription()) is GamePackageFile packageFile)
                                {
                                    taskFiles.Add(GameInstallFile.FromGamePackageFile(packageFile, task.InstallPath));
                                }
                            }
                        }
                    }
                }
            }
        }
        else if (task.Operation is GameInstallOperation.Install)
        {
            // 安装
            if (task.GameSophonChunkBuild is not null)
            {
                task.DownloadMode = GameInstallDownloadMode.Chunk;
                List<SophonChunkFile> chunks = new();
                if (task.GameSophonChunkBuild.Manifests.FirstOrDefault(x => x.MatchingField is "game") is GameSophonChunkManifest manifest)
                {
                    List<SophonChunkFile> items = await GetSophonChunkFilesAsync(manifest, cancellationToken);
                    chunks.AddRange(items);
                    foreach (SophonChunkFile item in items)
                    {
                        if (!item.IsFolder)
                        {
                            taskFiles.Add(GameInstallFile.FromSophonChunkFile(item, null, task.InstallPath, manifest.ChunkDownload.UrlPrefix));
                        }
                    }
                }
                foreach (AudioLanguage lang in Enum.GetValues<AudioLanguage>())
                {
                    if (task.AudioLanguage.HasFlag(lang))
                    {
                        if (task.GameSophonChunkBuild.Manifests.FirstOrDefault(x => x.MatchingField == lang.ToDescription()) is GameSophonChunkManifest audioManifest)
                        {
                            List<SophonChunkFile> items = await GetSophonChunkFilesAsync(audioManifest, cancellationToken);
                            chunks.AddRange(items);
                            foreach (SophonChunkFile item in items)
                            {
                                if (!item.IsFolder)
                                {
                                    taskFiles.Add(GameInstallFile.FromSophonChunkFile(item, null, task.InstallPath, audioManifest.ChunkDownload.UrlPrefix));
                                }
                            }
                        }
                    }
                }
                task.SophonChunkFiles = chunks;
            }
            else if (task.GamePackage is not null)
            {
                task.DownloadMode = GameInstallDownloadMode.CompressedPackage;
                taskFiles.Add(GameInstallFile.FromGamePackageResource(task.GamePackage.Main.Major!, task.InstallPath));
                foreach (AudioLanguage lang in Enum.GetValues<AudioLanguage>())
                {
                    if (task.GamePackage.Main.Major?.AudioPackages.FirstOrDefault(x => x.Language == lang.ToDescription()) is GamePackageFile gamePackageFile)
                    {
                        taskFiles.Add(GameInstallFile.FromGamePackageFile(gamePackageFile, task.InstallPath));
                    }
                }
            }
        }
        else if (task.Operation is GameInstallOperation.Repair)
        {
            // 修复
            if (task.GameSophonChunkBuild is not null)
            {
                task.DownloadMode = GameInstallDownloadMode.Chunk;
                List<SophonChunkFile> chunks = new();
                List<SophonChunkFile> localChunks = new();

                if (task.GameSophonChunkBuild.Manifests.FirstOrDefault(x => x.MatchingField is "game") is GameSophonChunkManifest manifest)
                {
                    List<SophonChunkFile> items = await GetSophonChunkFilesAsync(manifest, cancellationToken);
                    chunks.AddRange(items);
                    Dictionary<string, SophonChunkFile> dict;
                    if (task.LocalVersionSophonChunkBuild?.Manifests.FirstOrDefault(x => x.MatchingField is "game") is GameSophonChunkManifest localManifest)
                    {
                        List<SophonChunkFile> localItems = await GetSophonChunkFilesAsync(localManifest);
                        localChunks.AddRange(localItems);
                        dict = localItems.ToDictionary(x => x.File);
                    }
                    else
                    {
                        dict = new();
                    }
                    foreach (SophonChunkFile item in items)
                    {
                        if (!item.IsFolder)
                        {
                            dict.TryGetValue(item.File, out SophonChunkFile? localFile);
                            taskFiles.Add(GameInstallFile.FromSophonChunkFile(item, localFile, task.InstallPath, manifest.ChunkDownload.UrlPrefix));
                        }
                    }
                }
                foreach (var lang in Enum.GetValues<AudioLanguage>())
                {
                    if (task.AudioLanguage.HasFlag(lang))
                    {
                        if (task.GameSophonChunkBuild.Manifests.FirstOrDefault(x => x.MatchingField == lang.ToDescription()) is GameSophonChunkManifest audioManifest)
                        {
                            List<SophonChunkFile> items = await GetSophonChunkFilesAsync(audioManifest, cancellationToken);
                            chunks.AddRange(items);
                            Dictionary<string, SophonChunkFile> dict;
                            if (task.LocalVersionSophonChunkBuild?.Manifests.FirstOrDefault(x => x.MatchingField is "game") is GameSophonChunkManifest localAudioManifest)
                            {
                                List<SophonChunkFile> localItems = await GetSophonChunkFilesAsync(localAudioManifest);
                                localChunks.AddRange(localItems);
                                dict = localItems.ToDictionary(x => x.File);
                            }
                            else
                            {
                                dict = new();
                            }
                            foreach (SophonChunkFile item in items)
                            {
                                if (!item.IsFolder)
                                {
                                    dict.TryGetValue(item.File, out SophonChunkFile? localFile);
                                    taskFiles.Add(GameInstallFile.FromSophonChunkFile(item, localFile, task.InstallPath, audioManifest.ChunkDownload.UrlPrefix));
                                }
                            }
                        }
                    }
                }

                task.SophonChunkFiles = chunks;
                if (localChunks.Count > 0)
                {
                    task.LocalVersionSophonChunkFiles = localChunks;
                }
            }
            else if (task.GamePackage is not null)
            {
                if (!string.IsNullOrWhiteSpace(task.GamePackage.Main.Major?.ResListUrl))
                {
                    string prefix = task.GamePackage.Main.Major.ResListUrl.TrimEnd('/');
                    task.DownloadMode = GameInstallDownloadMode.SingleFile;
                    var pkg_versions = await GetPkgVersionItemsAsync(task, cancellationToken);
                    foreach (PkgVersionItem item in pkg_versions)
                    {
                        taskFiles.Add(GameInstallFile.FromPkgVersionItem(item, task.InstallPath, prefix));
                    }
                }
            }
        }

        if (Directory.Exists(task.HardLinkPath)
            && Path.GetFullPath(task.InstallPath) != Path.GetFullPath(task.HardLinkPath)
            && Path.GetPathRoot(task.HardLinkPath) == Path.GetPathRoot(task.InstallPath)
            && new DriveInfo(task.InstallPath).DriveFormat is "NTFS")
        {
            if (task.Operation is GameInstallOperation.Update && task.DownloadMode is GameInstallDownloadMode.Patch)
            {
                Version? hardLinkVersion = await GetLocalGameVersionAsync(task.HardLinkPath);
                if (hardLinkVersion is not null && task.LatestGameVersion == hardLinkVersion.ToString())
                {
                    task.Operation = GameInstallOperation.Repair;
                    await PrepareGamePackageAsync(task, cancellationToken);
                    return;
                }
            }
            GameBiz gameBiz = task.GameId.GameBiz;
            if (gameBiz.Game is GameBiz.hk4e)
            {
                const string YuanShen_Data = nameof(YuanShen_Data);
                const string GenshinImpact_Data = nameof(GenshinImpact_Data);
                string source = gameBiz.Server switch
                {
                    "cn" => "YuanShen_Data",
                    "global" => "GenshinImpact_Data",
                    "bilibili" => "YuanShen_Data",
                    _ => "",
                };
                string target = "";
                foreach (var dir in Directory.GetDirectories(task.HardLinkPath, "*_Data"))
                {
                    string name = Path.GetFileName(dir);
                    if (name is "YuanShen_Data" or "GenshinImpact_Data")
                    {
                        target = name;
                        break;
                    }
                }
                if (!string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(target))
                {
                    if (source == target)
                    {
                        foreach (var item in taskFiles)
                        {
                            if (!string.IsNullOrWhiteSpace(item.File))
                            {
                                item.HardLinkTarget = Path.GetFullPath(Path.Join(task.HardLinkPath, item.File));
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in taskFiles)
                        {
                            if (!string.IsNullOrWhiteSpace(item.File))
                            {
                                item.HardLinkTarget = Path.GetFullPath(Path.Join(task.HardLinkPath, item.File.Replace(source, target)));
                            }
                        }
                    }
                }
            }
            else if (gameBiz.Game is GameBiz.hkrpg)
            {
                Version? hardLinkVersion = await GetLocalGameVersionAsync(task.HardLinkPath);
                if (hardLinkVersion is not null && task.LatestGameVersion == hardLinkVersion.ToString())
                {
                    if (task.Operation is GameInstallOperation.Install or GameInstallOperation.Update)
                    {
                        task.Operation = GameInstallOperation.Repair;
                        await PrepareGameInstallTaskFilesAsync(task, localVersion, cancellationToken);
                        return;
                    }
                    else if (task.Operation is GameInstallOperation.Repair)
                    {
                        string folder = Path.GetFullPath(Path.Combine(task.HardLinkPath, task.GameConfig!.AudioPackageResDir));
                        if (Directory.Exists(folder))
                        {
                            string targetFolder = Path.GetFullPath(Path.Combine(task.InstallPath, task.GameConfig.AudioPackageResDir));
                            string[] files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
                            foreach (string item in files)
                            {
                                string target = item.Replace(folder, targetFolder);
                                if (File.Exists(target))
                                {
                                    File.Delete(target);
                                }
                                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                                Kernel32.CreateHardLink(target, item);
                            }
                        }
                        foreach (var item in taskFiles)
                        {
                            if (!string.IsNullOrWhiteSpace(item.File))
                            {
                                item.HardLinkTarget = Path.GetFullPath(Path.Join(task.HardLinkPath, item.File));
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var item in taskFiles)
                {
                    if (!string.IsNullOrWhiteSpace(item.File))
                    {
                        item.HardLinkTarget = Path.GetFullPath(Path.Join(task.HardLinkPath, item.File));
                    }
                }
            }
        }

        task.TaskFiles = taskFiles;
    }






    private async Task<List<SophonChunkFile>> GetSophonChunkFilesAsync(GameSophonChunkManifest manifest, CancellationToken cancellationToken = default)
    {
        byte[] bytes = await EnsureSophonManifestFileAsync(manifest.ManifestDownload, manifest.Manifest, cancellationToken);
        SophonChunkManifest manifest1 = SophonChunkManifest.Parser.ParseFrom(bytes);
        return manifest1.Chuncks.ToList();
    }



    private async Task<SophonPatchManifest> GetSophonPatchManifestAsync(GameSophonPatchManifest manifest, CancellationToken cancellationToken = default)
    {
        byte[] bytes = await EnsureSophonManifestFileAsync(manifest.ManifestDownload, manifest.Manifest, cancellationToken);
        return SophonPatchManifest.Parser.ParseFrom(bytes);
    }



    private async Task<byte[]> EnsureSophonManifestFileAsync(GameSophonManifestUrl manifestUrl, GameSophonManifestFile manifestFile, CancellationToken cancellationToken = default)
    {
        string cache = Path.Combine(AppConfig.CacheFolder, "game");
        Directory.CreateDirectory(cache);
        string file = Path.Combine(cache, manifestFile.Id);
        bool needDownload = true;
        if (File.Exists(file) && new FileInfo(file).Length == manifestFile.CompressedSize)
        {
            using FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using DecompressionStream ds = new DecompressionStream(fs);
            using MemoryStream ms = new MemoryStream();
            await ds.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;
            byte[] md5 = await MD5.HashDataAsync(ms, cancellationToken);
            if (string.Equals(Convert.ToHexString(md5), manifestFile.Checksum, StringComparison.OrdinalIgnoreCase))
            {
                return ms.ToArray();
            }
        }
        if (needDownload)
        {
            using FileStream fs = File.Open(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            string url = $"{manifestUrl.UrlPrefix.TrimEnd('/')}/{manifestFile.Id}";
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            using Stream hs = await response.Content.ReadAsStreamAsync(cancellationToken);
            await hs.CopyToAsync(fs, cancellationToken);
        }
        {
            using FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using DecompressionStream ds = new DecompressionStream(fs);
            using MemoryStream ms = new MemoryStream();
            await ds.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;
            byte[] md5 = await MD5.HashDataAsync(ms, cancellationToken);
            if (string.Equals(Convert.ToHexString(md5), manifestFile.Checksum, StringComparison.OrdinalIgnoreCase))
            {
                return ms.ToArray();
            }
        }
        throw new Exception($"Download manifest file ({manifestFile.Id}) failed.");
    }



    private async Task<List<PkgVersionItem>> GetPkgVersionItemsAsync(GameInstallTask task, CancellationToken cancellationToken = default)
    {
        List<PkgVersionItem> list = new();
        if (task.GamePackage is not null)
        {
            string? prefix = task.GamePackage.Main.Major!.ResListUrl;
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                list.AddRange(await DownloadAndParsePkgVersionAsync(task.InstallPath, prefix, "pkg_version", cancellationToken));
                if (task.AudioLanguage.HasFlag(AudioLanguage.Chinese))
                {
                    list.AddRange(await DownloadAndParsePkgVersionAsync(task.InstallPath, prefix, "Audio_Chinese_pkg_version", cancellationToken));
                }
                if (task.AudioLanguage.HasFlag(AudioLanguage.English))
                {
                    list.AddRange(await DownloadAndParsePkgVersionAsync(task.InstallPath, prefix, "Audio_English(US)_pkg_version", cancellationToken));
                }
                if (task.AudioLanguage.HasFlag(AudioLanguage.Japanese))
                {
                    list.AddRange(await DownloadAndParsePkgVersionAsync(task.InstallPath, prefix, "Audio_Japanese_pkg_version", cancellationToken));
                }
                if (task.AudioLanguage.HasFlag(AudioLanguage.Korean))
                {
                    list.AddRange(await DownloadAndParsePkgVersionAsync(task.InstallPath, prefix, "Audio_Korean_pkg_version", cancellationToken));
                }
            }
        }
        return list;
    }



    private async Task<List<PkgVersionItem>> DownloadAndParsePkgVersionAsync(string installPath, string prefix, string name, CancellationToken cancellationToken = default)
    {
        string path = Path.Combine(installPath, name);
        string url = prefix.TrimEnd('/') + '/' + name;
        using HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            return [];
        }
        response.EnsureSuccessStatusCode();
        using Stream hs = await response.Content.ReadAsStreamAsync(cancellationToken);
        using MemoryStream ms = new();
        await hs.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;
        List<PkgVersionItem> list = await GameInstallHelper.DeserilizerLinesAsync<PkgVersionItem>(ms, cancellationToken);
        using FileStream fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        ms.Position = 0;
        await ms.CopyToAsync(fs, cancellationToken);
        return list;
    }




    #region HoYoPlay API




    private async Task<GameConfig?> GetGameConfigAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        return await _hoyoplayClient.GetGameConfigAsync(LauncherId.FromGameId(gameId)!, "en-us", gameId, cancellationToken);
    }



    private async Task<GamePackage?> GetGamePackageAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        return await _hoyoplayClient.GetGamePackageAsync(LauncherId.FromGameId(gameId)!, "en-us", gameId, cancellationToken);
    }



    private async Task<GameSophonChunkBuild?> GetGameSophonChunkBuildAsync(GameBranch gameBranch, GameBranchPackage gameBranchPackage, string tag = "", CancellationToken cancellationToken = default)
    {
        try
        {
            return await _hoyoplayClient.GetGameSophonChunkBuildAsync(gameBranch, gameBranchPackage, tag, cancellationToken);
        }
        catch (miHoYoApiException ex)
        {
            // not found (-202)
            return null;
        }
    }



    private async Task<GameSophonPatchBuild?> GetGameSophonPatchBuildAsync(GameBranch gameBranch, GameBranchPackage gameBranchPackage, CancellationToken cancellationToken = default)
    {
        try
        {
            GameSophonPatchBuild build = await _hoyoplayClient.GetGameSophonPatchBuildAsync(gameBranch, gameBranchPackage, cancellationToken);
            if (string.IsNullOrWhiteSpace(build.BuildId))
            {
                return null;
            }
            else
            {
                return build;
            }
        }
        catch (miHoYoApiException ex)
        {
            return null;
        }
    }



    private async Task<GameChannelSDK?> GetGameChannelSDKAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        return await _hoyoplayClient.GetGameChannelSDKAsync(LauncherId.FromGameId(gameId)!, "en-us", gameId, cancellationToken);
    }



    private async Task<GameDeprecatedFileConfig?> GetGameDeprecatedFileAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        return await _hoyoplayClient.GetGameDeprecatedFileConfigAsync(LauncherId.FromGameId(gameId)!, "en-us", gameId, cancellationToken);
    }



    #endregion




}
