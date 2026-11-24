using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Starward.Core;
using Starward.Core.HoYoPlay;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Starward.GameInstall.HotUpdate;

public class GenshinHotUpdateService
{

    private readonly ILogger<GenshinHotUpdateService> _logger;

    private readonly GenshinDispatchClient _dispatchClient;

    private readonly HoYoPlayService _hoYoPlayService;

    private readonly HttpClient _httpClient;


    public GenshinHotUpdateService(ILogger<GenshinHotUpdateService> logger, GenshinDispatchClient dispatchClient, HoYoPlayService hoYoPlayService, HttpClient httpClient)
    {
        _logger = logger;
        _dispatchClient = dispatchClient;
        _hoYoPlayService = hoYoPlayService;
        _httpClient = httpClient;
    }



    public async Task<bool> CheckUpdateAsync(GameId gameId, string installPath, CancellationToken cancellation = default)
    {
        ServerRegion region = gameId.GameBiz.Value switch
        {
            GameBiz.hk4e_cn => ServerRegion.miHoYo,
            GameBiz.hk4e_global => ServerRegion.HoYoverse,
            _ => throw new NotSupportedException($"GameId ({gameId}) is not supported."),
        };
        GameBranch branch = (await _hoYoPlayService.GetGameBranchAsync(gameId, cancellation)).EnsureNotNull(gameId);
        var regionInfo = await GetDispatchRegionInfoAsync(gameId, cancellation);
        bool success = await CheckChannelNameAndScriptVersionAsync(region, installPath, branch.Main.Tag, cancellation)
                       && await CheckBaseResVersionHashAsync(region, installPath, cancellation)
                       && await CheckDownloadPrefAsync(region, installPath, branch.Main.Tag, cancellation)
                       && await CheckPatchDoneAsync(region, installPath, branch.Main.Tag, regionInfo, cancellation)
                       && await CheckRevisionAsync(region, installPath, branch.Main.Tag, regionInfo, cancellation);
        return !success;
    }



    internal async Task<RegionInfo> GetDispatchRegionInfoAsync(GameId gameId, CancellationToken cancellation = default)
    {
        ServerRegion region = gameId.GameBiz.Value switch
        {
            GameBiz.hk4e_cn => ServerRegion.miHoYo,
            GameBiz.hk4e_global => ServerRegion.HoYoverse,
            _ => throw new NotSupportedException($"GameId ({gameId}) is not supported."),
        };
        GameBranch branch = (await _hoYoPlayService.GetGameBranchAsync(gameId, cancellation)).EnsureNotNull(gameId);
        var cutsceneCollection = await _dispatchClient.GetCutsceneCollectionAsync(cancellation);
        string key = gameId.GameBiz == GameBiz.hk4e_cn ? $"CNRELWin{branch.Main.Tag}" : $"OSRELWin{branch.Main.Tag}";
        CutsceneDispatchInfo? dispatchInfo = cutsceneCollection.Dispatch.GetValueOrDefault(key);
        if (dispatchInfo is null)
        {
            throw new NotSupportedException($"CutsceneDispatchInfo of {key} is not supported.");
        }

        var regionList = await _dispatchClient.QueryRegionListAsync(region, branch.Main.Tag, cancellation);
        string? serverName = GetServerName(region);
        RegionSimpleInfo? regionSimple = regionList.RegionList.FirstOrDefault(x => x.Name == serverName) ?? regionList.RegionList.FirstOrDefault();
        if (regionSimple is null)
        {
            throw new NotSupportedException($"Could not find region {serverName}");
        }
        var curRegionHttpRsp = await _dispatchClient.QueryCurRegionAsync(regionSimple.DispatchUrl, region, branch.Main.Tag, dispatchInfo.DispatchSeed, dispatchInfo.DispatcherKey, cancellation);
        return curRegionHttpRsp.RegionInfo;
    }



    private static string? GetServerName(ServerRegion region)
    {
        try
        {
            if (region is ServerRegion.HoYoverse)
            {
                var bytes = Registry.GetValue(@"HKEY_CURRENT_USER\Software\miHoYo\Genshin Impact", "GENERAL_DATA_h2389025596", null) as byte[];
                if (bytes is not null)
                {
                    var node = JsonNode.Parse(bytes);
                    return node?["selectedServerName"]?.ToString();
                }
            }
        }
        catch { }
        return null;
    }


    private static async Task<bool> CheckChannelNameAndScriptVersionAsync(ServerRegion region, string installPath, string version, CancellationToken cancellation)
    {
        string folder = region is ServerRegion.miHoYo ? "YuanShen_Data" : "GenshinImpact_Data";
        string channelName = Path.Combine(installPath, folder, @"Persistent\ChannelName");
        if (!File.Exists(channelName))
        {
            return false;
        }
        if (await File.ReadAllTextAsync(channelName, cancellation) != (region is ServerRegion.miHoYo ? "CNRELWin" : "OSRELWin"))
        {
            return false;
        }
        string scriptVersion = Path.Combine(installPath, folder, @"Persistent\ScriptVersion");
        if (!File.Exists(scriptVersion))
        {
            return false;
        }
        return await File.ReadAllTextAsync(scriptVersion, cancellation) == version;
    }


    private static async Task<bool> CheckBaseResVersionHashAsync(ServerRegion region, string installPath, CancellationToken cancellation)
    {
        string folder = region is ServerRegion.miHoYo ? "YuanShen_Data" : "GenshinImpact_Data";
        string base_res_version_hash = Path.Combine(installPath, folder, @"Persistent\base_res_version_hash");
        if (!File.Exists(base_res_version_hash))
        {
            return false;
        }
        string res_version_streaming = Path.Combine(installPath, folder, @"StreamingAssets\res_versions_streaming");
        if (!File.Exists(res_version_streaming))
        {
            return false;
        }
        using var fs = File.OpenRead(res_version_streaming);
        string md5 = Convert.ToHexStringLower(await MD5.HashDataAsync(fs, cancellation));
        string content = await File.ReadAllTextAsync(base_res_version_hash, cancellation);
        return string.Equals(content, md5, StringComparison.OrdinalIgnoreCase);
    }


    private static async Task<bool> CheckDownloadPrefAsync(ServerRegion region, string installPath, string version, CancellationToken cancellation)
    {
        string folder = region is ServerRegion.miHoYo ? "YuanShen_Data" : "GenshinImpact_Data";
        string path_pref = Path.Combine(installPath, folder, @"Persistent\DownloadPref");
        if (!File.Exists(path_pref))
        {
            return false;
        }
        string res_version_streaming = Path.Combine(installPath, folder, @"StreamingAssets\res_versions_streaming");
        if (!File.Exists(res_version_streaming))
        {
            return false;
        }

        DownloadPref pref;
        try
        {
            pref = DownloadPref.Decode(await File.ReadAllBytesAsync(path_pref, cancellation));
        }
        catch
        {
            return false;
        }
        if (pref.ScriptVersion != version)
        {
            return false;
        }

        using var fs = File.OpenRead(res_version_streaming);
        IAsyncEnumerable<HotPatchItem?> items = JsonSerializer.DeserializeAsyncEnumerable(fs, GenshinHotUpdateJsonContext.Default.HotPatchItem, true, cancellation);
        HashSet<int> set = new();
        await foreach (HotPatchItem? item in items)
        {
            if (item?.Category > 0)
            {
                set.Add(item.Category);
                set.Add(item.Category + 1_000_000);
            }
        }

        return pref.ResCategoryStatusDict.Keys.ToHashSet().IsSupersetOf(set);
    }


    private async Task<bool> CheckPatchDoneAsync(ServerRegion region, string installPath, string version, RegionInfo regionInfo, CancellationToken cancellation)
    {
        string folder = region is ServerRegion.miHoYo ? "YuanShen_Data" : "GenshinImpact_Data";
        string path_patchdone = Path.Combine(installPath, folder, @"Persistent\PatchDone");
        if (!File.Exists(path_patchdone))
        {
            return false;
        }

        string url = $"{regionInfo.ResourceUrl}/output_{regionInfo.ResVersionConfig.Version}_{regionInfo.ResVersionConfig.VersionSuffix}/client/StandaloneWindows64/patch_node_versions";
        using var hs = await _httpClient.GetStreamAsync(url, cancellation);
        IAsyncEnumerable<PatchNodeVersionItem?> items = JsonSerializer.DeserializeAsyncEnumerable(hs, GenshinHotUpdateJsonContext.Default.PatchNodeVersionItem, true, cancellation);
        PatchNodeVersionItem? patchNode = await items.FirstOrDefaultAsync(x => x?.Branch == regionInfo.ResVersionConfig.Branch, cancellationToken: cancellation);
        if (patchNode is null)
        {
            return false;
        }

        string content = await File.ReadAllTextAsync(path_patchdone, cancellation);
        int.TryParse(content, out int revision);
        return revision >= patchNode.Revision;

    }


    private async Task<bool> CheckRevisionAsync(ServerRegion region, string installPath, string version, RegionInfo regionInfo, CancellationToken cancellation)
    {
        string folder = region is ServerRegion.miHoYo ? "YuanShen_Data" : "GenshinImpact_Data";
        string res_revision = Path.Combine(installPath, folder, @"Persistent\res_revision");
        if (!File.Exists(res_revision) || await File.ReadAllTextAsync(res_revision, cancellation) != regionInfo.ResVersionConfig.Version.ToString())
        {
            return false;
        }
        string data_revision = Path.Combine(installPath, folder, @"Persistent\data_revision");
        if (!File.Exists(data_revision) || await File.ReadAllTextAsync(data_revision, cancellation) != regionInfo.ClientDataVersion.ToString())
        {
            return false;
        }
        string silence_revision = Path.Combine(installPath, folder, @"Persistent\silence_revision");
        if (!File.Exists(silence_revision) || await File.ReadAllTextAsync(silence_revision, cancellation) != regionInfo.ClientSilenceDataVersion.ToString())
        {
            return false;
        }
        string audio_revision = Path.Combine(installPath, folder, @"Persistent\audio_revision");
        if (!File.Exists(audio_revision))
        {
            return false;
        }
        string url = $"{regionInfo.ResourceUrl}/output_{regionInfo.ResVersionConfig.Version}_{regionInfo.ResVersionConfig.VersionSuffix}/client/StandaloneWindows64/base_revision";
        string content = await _httpClient.GetStringAsync(url, cancellation);
        return await File.ReadAllTextAsync(audio_revision, cancellation) == content.Split(' ').First();
    }



}
