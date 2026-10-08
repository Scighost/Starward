using Microsoft.Extensions.Logging;
using Polly;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.GameManagement.Model;

namespace Starward.GameManagement;

public class GameResourceService
{

    private readonly ILogger<GameResourceService> _logger;

    private readonly GameConfigService _gameConfigService;

    private readonly HoYoPlayService _hoYoPlayService;

    private readonly ResiliencePipeline _polly;

    public GameResourceService(ILogger<GameResourceService> logger, GameConfigService gameConfigService, HoYoPlayService hoYoPlayService, ResiliencePipeline pipeline)
    {
        _logger = logger;
        _gameConfigService = gameConfigService;
        _hoYoPlayService = hoYoPlayService;
        _polly = pipeline;
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


    public async Task<List<GameSophonChunkManifest>> GetGameSophonChunkManifestsAsync(GameId gameId, string scenarios, AudioLanguage audio, CancellationToken cancellation = default)
    {
        GameBranch branch = (await _hoYoPlayService.GetGameBranchAsync(gameId, cancellation)).EnsureNotNull(gameId);
        GameSophonChunkBuild chunkBuild = (await _hoYoPlayService.GetGameSophonChunkBuildAsync(gameId, branch.Main, null, cancellation)).EnsureNotNull(gameId);
        HashSet<string> matchedIds = GameConfigService.GetMatchedCategoryIds(branch.Main, scenarios, audio);
        return chunkBuild.Manifests.Where(x => matchedIds.Contains(x.CategoryId)).ToList();
    }


    public async Task<List<GameSophonChunkManifest>> GetGameSophonChunkManifestsAsync(GameId gameId, string scenarios, AudioLanguage audio, string version, CancellationToken cancellation = default)
    {
        try
        {
            GameBranch branch = (await _hoYoPlayService.GetGameBranchAsync(gameId, cancellation)).EnsureNotNull(gameId);
            GameSophonChunkBuild chunkBuild = (await _hoYoPlayService.GetGameSophonChunkBuildAsync(gameId, branch.Main, version, cancellation)).EnsureNotNull(gameId);
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
        GameSophonChunkBuild chunkBuild = (await _hoYoPlayService.GetGameSophonChunkBuildAsync(gameId, branchPackage, null, cancellation)).EnsureNotNull(gameId);
        HashSet<string> ignoreIds = await _gameConfigService.GetIgnoreMatchingFieldsAsync(gameId, installPath, cancellation);
        HashSet<string> matchedIds = GameConfigService.GetMatchedCategoryIds(branchPackage, scenarios, audio, ignoreIds);
        return chunkBuild.Manifests.Where(x => matchedIds.Contains(x.CategoryId)).ToList();
    }


    public async Task<List<GameSophonChunkManifest>> GetGameSophonChunkManifestsAsync(GameId gameId, GameBranchPackage branchPackage, string installPath, string scenarios, AudioLanguage audio, string version, CancellationToken cancellation = default)
    {
        try
        {
            GameSophonChunkBuild chunkBuild = (await _hoYoPlayService.GetGameSophonChunkBuildAsync(gameId, branchPackage, version, cancellation)).EnsureNotNull(gameId);
            HashSet<string> ignoreIds = await _gameConfigService.GetIgnoreMatchingFieldsAsync(gameId, installPath, cancellation);
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
        GameSophonPatchBuild patchBuild = (await _hoYoPlayService.GetGameSophonPatchBuildAsync(gameId, branchPackage, cancellation)).EnsureNotNull(gameId);
        HashSet<string> ignoreIds = await _gameConfigService.GetIgnoreMatchingFieldsAsync(gameId, installPath, cancellation);
        HashSet<string> matchedIds = GameConfigService.GetMatchedCategoryIds(branchPackage, scenarios, audio, ignoreIds);
        return patchBuild.Manifests.Where(x => matchedIds.Contains(x.CategoryId)).ToList();
    }



    internal async Task<List<SophonChunkItem>> GetSophonChunkItemsAsync(GameId gameId, GameBranchPackage branchPackage, string installPath, string scenarios, AudioLanguage audio, string? version = null, CancellationToken cancellation = default)
    {
        List<GameSophonChunkManifest> chunkManifests = string.IsNullOrWhiteSpace(version)
            ? await GetGameSophonChunkManifestsAsync(gameId, branchPackage, installPath, scenarios, audio, cancellation)
            : await GetGameSophonChunkManifestsAsync(gameId, branchPackage, installPath, scenarios, audio, version, cancellation);
        HashSet<string> blackListSet = await _gameConfigService.GetBlackListFileNamesAsync(gameId, installPath, cancellation);
        List<SophonChunkItem> chunkItems = new();
        await _polly.ForEachAsync(chunkManifests, cancellation, async (item, token) =>
        {
            SophonChunkManifest manifest = await _hoYoPlayService.DownloadAndParseChunkManifestAsync(item.ManifestDownload, item.Manifest, token);
            List<SophonChunkItem> list = manifest.ChunkFiles.Where(x => !blackListSet.Contains(x.File))
                                                            .Select(x => new SophonChunkItem(item.ChunkDownload, x))
                                                            .ToList();
            lock (chunkItems)
            {
                chunkItems.AddRange(list);
            }
        });
        return chunkItems;
    }


    internal async Task<(List<SophonPatchItem> PatchItems, List<string> DeleteFiles)> GetSophonPatchItemsAsync(GameId gameId, GameBranchPackage branchPackage, string installPath, string scenarios, AudioLanguage audio, string version, CancellationToken cancellation = default)
    {
        List<GameSophonPatchManifest> patchManifests = await GetGameSophonPatchManifestsAsync(gameId, branchPackage, installPath, scenarios, audio, cancellation);
        HashSet<string> blackListSet = await _gameConfigService.GetBlackListFileNamesAsync(gameId, installPath, cancellation);
        List<SophonPatchItem> patchItems = new();
        List<string> deleteFiles = new();
        await _polly.ForEachAsync(patchManifests, cancellation, async (item, token) =>
        {
            SophonPatchManifest manifest = await _hoYoPlayService.DownloadAndParsePatchManifestAsync(item.ManifestDownload, item.Manifest, token);
            lock (patchItems)
            {
                foreach (SophonPatchFile? patchFile in manifest.PatchFiles.Where(x => !blackListSet.Contains(x.File)))
                {
                    SophonDiffInfo? diffInfo = patchFile.Diffs.FirstOrDefault(x => x.Tag == version);
                    if (diffInfo is not null)
                    {
                        patchItems.Add(new SophonPatchItem(item.DiffDownload, patchFile, diffInfo.DiffFile));
                    }
                }
                SophonPatchDeleteTag? deleteTag = manifest.DeleteTags.FirstOrDefault(x => x.Tag == version);
                if (deleteTag is not null)
                {
                    deleteFiles.AddRange(deleteTag.DeleteCollection.DeleteFiles.Select(x => x.File));
                }
            }
        });
        return (patchItems, deleteFiles);
    }


}