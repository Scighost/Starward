using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.GameInstall.Model;
using Starward.GameInstall.Service;
using Starward.GameInstall.Sophon;
using System.Text.Json;

namespace Starward.GameInstall;

public class GameResourceService
{

    /// <summary>
    /// 预下载阶段不下载的资源分类。
    /// </summary>
    /// <remarks>
    /// <c>asb</c> 是崩坏3的过场动画（wmv，常驻 28 GiB 以上），走游戏内按需下载；
    /// 官方启动器也不会把它登记进预下载的 depot（bh3 只登记 10161/game）。
    /// 若计入，9.1.0 → 9.2.0 会多算 3.5 GiB 的新增 wmv。
    /// </remarks>
    private static readonly string[] PredownloadExcludedMatchingFields = ["asb"];

    private readonly ILogger<GameResourceService> _logger;

    private readonly GameConfigService _gameConfigService;

    private readonly HoYoPlayService _hoYoPlayService;

    private readonly ResiliencePipeline _polly;

    public GameResourceService(ILogger<GameResourceService> logger, GameConfigService gameConfigService, HoYoPlayService hoYoPlayService)
    {
        _logger = logger;
        _gameConfigService = gameConfigService;
        _hoYoPlayService = hoYoPlayService;
        _polly = new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            BackoffType = DelayBackoffType.Linear,
            OnRetry = args =>
            {
                _logger.LogWarning(args.Outcome.Exception, "Operation failed, retry attempt {attempt}.", args.AttemptNumber + 1);
                return default;
            }
        }).Build();
    }



    public static (List<GamePackageFile> GamePackages, List<GamePackageFile> AudioPackages) GetDownloadPackageFiles(GamePackageResource resource, AudioLanguage audio)
    {
        List<GamePackageFile> list = new();
        foreach (AudioLanguage lang in Enum.GetValues<AudioLanguage>())
        {
            if (audio.HasFlag(lang))
            {
                if (resource.AudioPackages?.FirstOrDefault(x => x.Language == lang.ToDescription()) is GamePackageFile audioPackage)
                {
                    list.Add(audioPackage);
                }
            }
        }
        return (resource.GamePackages, list);
    }


    public static (long CompressedGameBytes, long UncompressedGameBytes, long CompressedAudioBytes, long UncompressedAudioBytes) GetDownloadPackageSize(GamePackageResource resource, AudioLanguage audioLanguage)
    {
        (List<GamePackageFile>? game, List<GamePackageFile>? audio) = GameResourceService.GetDownloadPackageFiles(resource, audioLanguage);
        long gameBytes = game.Sum(x => x.Size);
        long audioBytes = audio.Sum(x => x.Size);
        long unGameBytes = game.Sum(x => x.DecompressedSize);
        long unAudioBytes = audio.Sum(x => x.DecompressedSize);
        return (gameBytes, unGameBytes, audioBytes, unAudioBytes);
    }


    public static (long CompressedGameBytes, long UncompressedGameBytes, long CompressedAudioBytes, long UncompressedAudioBytes) GetChunkDownloadPackageSize(GameBranchPackage package, GameSophonChunkBuild build, string categoryScenario, AudioLanguage audioLanguage)
    {
        HashSet<string> ids = GameConfigService.GetMatchedCategoryIds(package, categoryScenario, audioLanguage);
        HashSet<string> gameIds = package.Categories.Where(x => ids.Contains(x.CategoryId) && x.Type is CategoryType.CATEGORY_TYPE_RESOURCE).Select(x => x.CategoryId).ToHashSet();
        HashSet<string> audioIds = package.Categories.Where(x => ids.Contains(x.CategoryId) && x.Type is CategoryType.CATEGORY_TYPE_AUDIO).Select(x => x.CategoryId).ToHashSet();
        List<GameSophonChunkManifest> games = build.Manifests.Where(x => gameIds.Contains(x.CategoryId)).ToList();
        List<GameSophonChunkManifest> audios = build.Manifests.Where(x => audioIds.Contains(x.CategoryId)).ToList();

        long gameBytes = games.Sum(x => x.Stats.CompressedSize);
        long audioBytes = audios.Sum(x => x.Stats.CompressedSize);
        long unGameBytes = games.Sum(x => x.Stats.UncompressedSize);
        long unAudioBytes = audios.Sum(x => x.Stats.UncompressedSize);
        return (gameBytes, unGameBytes, audioBytes, unAudioBytes);
    }


    public static async Task<bool> VerifyPluginAsync(string installPath, GamePluginPackage pluginPackage, CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(pluginPackage.Validation))
        {
            return false;
        }
        var list = JsonSerializer.Deserialize<List<PluginValidationItem>>(pluginPackage.Validation);
        if (list is null)
        {
            return false;
        }
        foreach (PluginValidationItem item in list)
        {
            cancellation.ThrowIfCancellationRequested();
            string path = Path.Join(installPath, item.Path);
            if (item.Size.HasValue)
            {
                if (!await HashService.CheckFileMD5Async(path, item.Size.Value, item.MD5, cancellation).ConfigureAwait(false))
                {
                    return false;
                }
            }
            else if (!File.Exists(path))
            {
                return false;
            }
            else
            {
                string? md5 = await HashService.GetFileMD5Async(path, cancellation).ConfigureAwait(false);
                if (!string.Equals(md5, item.MD5, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
        }
        return true;
    }



    public async Task<List<GameSophonChunkManifest>> GetGameSophonChunkManifestsAsync(GameId gameId, string scenarios, AudioLanguage audio, CancellationToken cancellation = default)
    {
        GameBranch branch = (await _hoYoPlayService.GetGameBranchAsync(gameId, cancellation).ConfigureAwait(false)).EnsureNotNull(gameId);
        GameSophonChunkBuild chunkBuild = (await _hoYoPlayService.GetGameSophonChunkBuildAsync(gameId, branch.Main, null, cancellation).ConfigureAwait(false)).EnsureNotNull(gameId);
        HashSet<string> matchedIds = GameConfigService.GetMatchedCategoryIds(branch.Main, scenarios, audio);
        return chunkBuild.Manifests.Where(x => matchedIds.Contains(x.CategoryId)).ToList();
    }


    public async Task<List<GameSophonChunkManifest>> GetGameSophonChunkManifestsAsync(GameId gameId, string scenarios, AudioLanguage audio, string version, CancellationToken cancellation = default)
    {
        try
        {
            GameBranch branch = (await _hoYoPlayService.GetGameBranchAsync(gameId, cancellation).ConfigureAwait(false)).EnsureNotNull(gameId);
            GameSophonChunkBuild chunkBuild = (await _hoYoPlayService.GetGameSophonChunkBuildAsync(gameId, branch.Main, version, cancellation).ConfigureAwait(false)).EnsureNotNull(gameId);
            HashSet<string> matchedIds = GameConfigService.GetMatchedCategoryIds(branch.Main, scenarios, audio);
            return chunkBuild.Manifests.Where(x => matchedIds.Contains(x.CategoryId)).ToList();
        }
        catch (miHoYoApiException)
        {
            return [];
        }
    }


    public async Task<List<GameSophonChunkManifest>> GetGameSophonChunkManifestsAsync(GameId gameId, GameBranchPackage branchPackage, string installPath, string scenarios, AudioLanguage audio, CancellationToken cancellation = default)
    {
        GameSophonChunkBuild chunkBuild = (await _hoYoPlayService.GetGameSophonChunkBuildAsync(gameId, branchPackage, null, cancellation).ConfigureAwait(false)).EnsureNotNull(gameId);
        HashSet<string> ignoreIds = await _gameConfigService.GetIgnoreMatchingFieldsAsync(gameId, installPath, cancellation).ConfigureAwait(false);
        HashSet<string> matchedIds = GameConfigService.GetMatchedCategoryIds(branchPackage, scenarios, audio, ignoreIds);
        return chunkBuild.Manifests.Where(x => matchedIds.Contains(x.CategoryId)).ToList();
    }


    public async Task<List<GameSophonChunkManifest>> GetGameSophonChunkManifestsAsync(GameId gameId, GameBranchPackage branchPackage, string installPath, string scenarios, AudioLanguage audio, string version, CancellationToken cancellation = default)
    {
        try
        {
            GameSophonChunkBuild chunkBuild = (await _hoYoPlayService.GetGameSophonChunkBuildAsync(gameId, branchPackage, version, cancellation).ConfigureAwait(false)).EnsureNotNull(gameId);
            HashSet<string> ignoreIds = await _gameConfigService.GetIgnoreMatchingFieldsAsync(gameId, installPath, cancellation).ConfigureAwait(false);
            HashSet<string> matchedIds = GameConfigService.GetMatchedCategoryIds(branchPackage, scenarios, audio, ignoreIds);
            return chunkBuild.Manifests.Where(x => matchedIds.Contains(x.CategoryId)).ToList();
        }
        catch (miHoYoApiException)
        {
            return [];
        }
    }


    public async Task<List<GameSophonPatchManifest>> GetGameSophonPatchManifestsAsync(GameId gameId, GameBranchPackage branchPackage, string installPath, string scenarios, AudioLanguage audio, CancellationToken cancellation = default)
    {
        GameSophonPatchBuild patchBuild = (await _hoYoPlayService.GetGameSophonPatchBuildAsync(gameId, branchPackage, cancellation).ConfigureAwait(false)).EnsureNotNull(gameId);
        HashSet<string> ignoreIds = await _gameConfigService.GetIgnoreMatchingFieldsAsync(gameId, installPath, cancellation).ConfigureAwait(false);
        HashSet<string> matchedIds = GameConfigService.GetMatchedCategoryIds(branchPackage, scenarios, audio, ignoreIds);
        return patchBuild.Manifests.Where(x => matchedIds.Contains(x.CategoryId)).ToList();
    }



    /// <summary>
    /// 预下载需要下载的分片（按文件做局部差分）
    /// </summary>
    /// <remarks>
    /// 不能退化成「新旧分片 id 的全局差集」：全局差集会把「本文件确实缺、但恰好被别的旧文件用过」的分片跳过，
    /// 结果偏小（崩坏3 9.1.0 → 9.2.0 少 46 MiB）；反过来，若直接把 asb 一起算进来又会多算 3.5 GiB。
    /// </remarks>
    internal async Task<List<SophonChunkItem>> GetPredownloadChunkItemsAsync(GameId gameId, GameBranch branch, string installPath, string scenarios, AudioLanguage audioLanguage, string? localVersion, CancellationToken cancellation = default)
    {
        if (branch.PreDownload is null)
        {
            return [];
        }
        List<SophonChunkItem> oldChunkItems = await GetSophonChunkItemsAsync(gameId, branch.Main, installPath, scenarios, audioLanguage, localVersion, cancellation, forPredownload: true).ConfigureAwait(false);
        List<SophonChunkItem> newChunkItems = await GetSophonChunkItemsAsync(gameId, branch.PreDownload, installPath, scenarios, audioLanguage, null, cancellation, forPredownload: true).ConfigureAwait(false);
        return DiffChunkItemsByFile(oldChunkItems, newChunkItems);
    }



    /// <summary>
    /// 计算预下载的下载量与所需空间
    /// </summary>
    /// <remarks>
    /// 所需空间 = 预下载完成后的成品占用（解压后） + 下载的压缩分片，两者会在应用阶段短暂共存。
    /// 与官方启动器弹窗口径一致（崩坏3 9.1.0 → 9.2.0：365.83 MB 下载 / 1.80 GB 所需）。
    /// </remarks>
    public async Task<PredownloadResourceSize> GetPredownloadResourceSizeAsync(GameId gameId, GameBranch branch, string installPath, string scenarios, AudioLanguage audioLanguage, string? localVersion, CancellationToken cancellation = default)
    {
        if (branch.PreDownload is null)
        {
            return default;
        }
        List<SophonChunkItem> items = await GetPredownloadChunkItemsAsync(gameId, branch, installPath, scenarios, audioLanguage, localVersion, cancellation).ConfigureAwait(false);
        long downloadSize = items.Sum(x => x.Chunk!.CompressedSize);

        HashSet<string> excludedIds = GetPredownloadExcludedCategoryIds(branch.PreDownload);
        List<GameSophonChunkManifest> manifests = await GetGameSophonChunkManifestsAsync(gameId, branch.PreDownload, installPath, scenarios, audioLanguage, cancellation).ConfigureAwait(false);
        long uncompressedSize = manifests.Where(x => !excludedIds.Contains(x.CategoryId)).Sum(x => x.Stats.UncompressedSize);
        return new PredownloadResourceSize(downloadSize, uncompressedSize + downloadSize);
    }



    /// <summary>
    /// 预下载阶段需要跳过的资源分类 id
    /// </summary>
    private static HashSet<string> GetPredownloadExcludedCategoryIds(GameBranchPackage branchPackage)
    {
        return branchPackage.Categories
                            .Where(x => PredownloadExcludedMatchingFields.Contains(x.MatchingField, StringComparer.OrdinalIgnoreCase))
                            .Select(x => x.CategoryId)
                            .ToHashSet();
    }



    /// <summary>
    /// 按文件做局部差分
    ///
    /// 新增文件整包下载；内容变更的文件只补「旧版本该文件里没有」的分片；未变更文件跳过。
    /// 最后按分片 id 去重 —— 同一分片被多个文件引用时只下载一次。
    /// </summary>
    /// <param name="oldChunkItems">旧版本的文件级条目（<see cref="SophonChunkItem.Chunk"/> 为 null，分片在 <see cref="SophonChunkFile.Chunks"/> 里）</param>
    /// <param name="newChunkItems">新版本的文件级条目</param>
    internal static List<SophonChunkItem> DiffChunkItemsByFile(List<SophonChunkItem> oldChunkItems, List<SophonChunkItem> newChunkItems)
    {
        Dictionary<string, SophonChunkFile> oldFiles = new(StringComparer.OrdinalIgnoreCase);
        foreach (SophonChunkItem item in oldChunkItems)
        {
            oldFiles.TryAdd(item.ChunkFile.File, item.ChunkFile);
        }

        Dictionary<string, HashSet<string>> oldChunkIds = new(StringComparer.OrdinalIgnoreCase);
        List<SophonChunkItem> chunkItems = new();
        foreach (SophonChunkItem item in newChunkItems)
        {
            SophonChunkFile chunkFile = item.ChunkFile;
            if (!oldFiles.TryGetValue(chunkFile.File, out SophonChunkFile? oldFile))
            {
                foreach (SophonChunk? chunk in chunkFile.Chunks)
                {
                    if (chunk is not null)
                    {
                        chunkItems.Add(new SophonChunkItem(item.ManifestUrl, chunkFile, chunk));
                    }
                }
                continue;
            }
            if (string.Equals(oldFile.Md5, chunkFile.Md5, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (!oldChunkIds.TryGetValue(chunkFile.File, out HashSet<string>? ids))
            {
                ids = oldFile.Chunks.Where(x => x is not null).Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
                oldChunkIds[chunkFile.File] = ids;
            }
            foreach (SophonChunk? chunk in chunkFile.Chunks)
            {
                if (chunk is not null && !ids.Contains(chunk.Id))
                {
                    chunkItems.Add(new SophonChunkItem(item.ManifestUrl, chunkFile, chunk));
                }
            }
        }
        return chunkItems.DistinctBy(x => x.Chunk!.Id).ToList();
    }



    internal async Task<List<SophonChunkItem>> GetSophonChunkItemsAsync(GameId gameId, GameBranchPackage branchPackage, string installPath, string scenarios, AudioLanguage audio, string? version = null, CancellationToken cancellation = default, bool forPredownload = false)
    {
        List<GameSophonChunkManifest> chunkManifests = string.IsNullOrWhiteSpace(version)
            ? await GetGameSophonChunkManifestsAsync(gameId, branchPackage, installPath, scenarios, audio, cancellation).ConfigureAwait(false)
            : await GetGameSophonChunkManifestsAsync(gameId, branchPackage, installPath, scenarios, audio, version, cancellation).ConfigureAwait(false);
        if (forPredownload)
        {
            HashSet<string> excludedIds = GetPredownloadExcludedCategoryIds(branchPackage);
            if (excludedIds.Count > 0)
            {
                chunkManifests = chunkManifests.Where(x => !excludedIds.Contains(x.CategoryId)).ToList();
            }
        }
        HashSet<string> blackListSet = await _gameConfigService.GetIgnoreFileNamesAsync(gameId, installPath, cancellation).ConfigureAwait(false);
        List<SophonChunkItem> chunkItems = new();
        await _polly.ForEachAsync(chunkManifests, cancellation, async (item, token) =>
        {
            SophonChunkManifest manifest = await _hoYoPlayService.DownloadAndParseChunkManifestAsync(item.ManifestDownload, item.Manifest, token).ConfigureAwait(false);
            List<SophonChunkItem> list = manifest.ChunkFiles.Where(x => !blackListSet.Contains(x.File))
                                                            .Select(x => new SophonChunkItem(item.ChunkDownload, x))
                                                            .ToList();
            lock (chunkItems)
            {
                chunkItems.AddRange(list);
            }
        }).ConfigureAwait(false);
        return chunkItems;
    }


    internal async Task<(List<SophonPatchItem> PatchItems, List<string> DeleteFiles)> GetSophonPatchItemsAsync(GameId gameId, GameBranchPackage branchPackage, string installPath, string scenarios, AudioLanguage audio, string version, CancellationToken cancellation = default)
    {
        List<GameSophonPatchManifest> patchManifests = await GetGameSophonPatchManifestsAsync(gameId, branchPackage, installPath, scenarios, audio, cancellation).ConfigureAwait(false);
        HashSet<string> blackListSet = await _gameConfigService.GetIgnoreFileNamesAsync(gameId, installPath, cancellation).ConfigureAwait(false);
        List<SophonPatchItem> patchItems = new();
        List<string> deleteFiles = new();
        await _polly.ForEachAsync(patchManifests, cancellation, async (item, token) =>
        {
            SophonPatchManifest manifest = await _hoYoPlayService.DownloadAndParsePatchManifestAsync(item.ManifestDownload, item.Manifest, token).ConfigureAwait(false);
            lock (patchItems)
            {
                foreach (SophonPatchFile? patchFile in manifest.PatchFiles.Where(x => !blackListSet.Contains(x.File)))
                {
                    SophonDiffInfo? diffInfo = patchFile.Diffs.FirstOrDefault(x => x.Tag == version);
                    if (diffInfo is not null)
                    {
                        patchItems.Add(new SophonPatchItem(item.DiffDownload, patchFile, diffInfo.DiffFile, manifest.PatchMode));
                    }
                }
                SophonPatchDeleteTag? deleteTag = manifest.DeleteTags.FirstOrDefault(x => x.Tag == version);
                if (deleteTag is not null)
                {
                    deleteFiles.AddRange(deleteTag.DeleteCollection.DeleteFiles.Select(x => x.File));
                }
            }
        }).ConfigureAwait(false);
        return (patchItems, deleteFiles);
    }


}