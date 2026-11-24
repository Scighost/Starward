using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.GameInstall.Service;
using Starward.GameInstall.Step;
using System.Security.Cryptography;
using System.Text.Json;
using Vanara.PInvoke;

namespace Starward.GameInstall.HotUpdate;

internal class GenshinHotUpdateStep : StepBase
{

    private readonly ILogger<GenshinHotUpdateStep> _logger;

    private readonly GenshinHotUpdateService _genshinHotUpdateService;

    private readonly HttpClient _httpClient;


    public GenshinHotUpdateStep(GameInstallContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<GenshinHotUpdateStep>>();
        _genshinHotUpdateService = serviceProvider.GetRequiredService<GenshinHotUpdateService>();
        _httpClient = serviceProvider.GetRequiredService<HttpClient>();
    }



    private ServerRegion _serverRegion;

    private GameBranch _gameBranch;

    private RegionInfo _regionInfo;

    private string _dataFolder;

    private string _persistentFolder;

    private string _streamAssetsFolder;

    private string _resUrl;

    private string _dataUrl;

    private string _silenceUrl;

    private string _audioUrl;

    private string _audioRevision;

    private Dictionary<string, HotPatchItem> _streamAssetItems;

    private List<DownloadItem> _downloadItems;



    public override async Task PrepareAsync(CancellationToken cancellation = default)
    {
        if (_context.GameId.GameBiz.Game is not GameBiz.hk4e)
        {
            SkipStep = true;
            return;
        }
        if (!await _genshinHotUpdateService.CheckUpdateAsync(_context.GameId, _context.InstallPath, cancellation))
        {
            SkipStep = true;
            return;
        }
        _gameBranch = (await _hoYoPlayService.GetGameBranchAsync(_context.GameId, cancellation)).EnsureNotNull(_context.GameId);
        _regionInfo = await _genshinHotUpdateService.GetDispatchRegionInfoAsync(_context.GameId, cancellation);
        _serverRegion = _context.GameId.GameBiz.Value switch
        {
            GameBiz.hk4e_cn => ServerRegion.miHoYo,
            GameBiz.hk4e_global => ServerRegion.HoYoverse,
            _ => throw new NotSupportedException($"GameId ({_context.GameId}) is not supported."),
        };
        _dataFolder = Path.Combine(_context.InstallPath, _serverRegion is ServerRegion.miHoYo ? "YuanShen_Data" : "GenshinImpact_Data");
        _persistentFolder = Path.Combine(_dataFolder, "Persistent");
        _streamAssetsFolder = Path.Combine(_dataFolder, "StreamingAssets");
        Directory.CreateDirectory(_persistentFolder);

        _resUrl = $"{_regionInfo.ResourceUrl}/output_{_regionInfo.ResVersionConfig.Version}_{_regionInfo.ResVersionConfig.VersionSuffix}/client/StandaloneWindows64";
        _dataUrl = $"{_regionInfo.DataUrl}/output_{_regionInfo.ClientDataVersion}_{_regionInfo.ClientVersionSuffix}/client/General";
        _silenceUrl = $"{_regionInfo.DataUrl}/output_{_regionInfo.ClientSilenceDataVersion}_{_regionInfo.ClientSilenceVersionSuffix}/client_silence/General";

        string base_version = await _httpClient.GetStringAsync($"{_resUrl}/base_revision", cancellation);
        _audioRevision = base_version.Trim().Split(' ')[0];
        _audioUrl = $"{_regionInfo.ResourceUrl}/output_{base_version.Trim().Replace(' ', '_')}/client/StandaloneWindows64";
    }




    protected override async Task ExecuteInternalAsync(CancellationToken cancellation = default)
    {
        _context.Step = RunnerStep.Pending;
        await WritePersistentFilesAsync(cancellation);
        await PrepareDownloadFilesAsync(cancellation);
        await DownloadFilesAsync(cancellation);
    }



    private async Task WritePersistentFilesAsync(CancellationToken cancellation)
    {
        if (_streamAssetItems is not null)
        {
            return;
        }
        string res_version_streaming = Path.Combine(_streamAssetsFolder, "res_versions_streaming");
        if (!File.Exists(res_version_streaming))
        {
            throw new FileNotFoundException("Required file res_version_streaming is not found.", res_version_streaming);
        }
        using var fs = File.OpenRead(res_version_streaming);
        string md5 = Convert.ToHexStringLower(await MD5.HashDataAsync(fs, cancellation));
        string base_res_version_hash = Path.Combine(_persistentFolder, "base_res_version_hash");
        bool deletePatchFile = false;
        if (!File.Exists(base_res_version_hash) || await File.ReadAllTextAsync(base_res_version_hash) != md5)
        {
            deletePatchFile = true;
            DeleteFileIfExists(Path.Combine(_persistentFolder, "res_revision"));
            DeleteFileIfExists(Path.Combine(_persistentFolder, "data_revision"));
            DeleteFileIfExists(Path.Combine(_persistentFolder, "silence_revision"));
            DeleteFileIfExists(Path.Combine(_persistentFolder, "res_versions_remote"));
            DeleteFileIfExists(Path.Combine(_persistentFolder, "data_versions_remote"));
        }
        fs.Position = 0;
        Dictionary<string, HotPatchItem> itemsDict = new();
        IAsyncEnumerable<HotPatchItem?> patchItems = JsonSerializer.DeserializeAsyncEnumerable(fs, GenshinHotUpdateJsonContext.Default.HotPatchItem, true, cancellation);
        DownloadPref pref = new() { ScriptVersion = _gameBranch.Main.Tag };
        pref.ResCategoryStatusDict.Add(0, 1);
        await foreach (HotPatchItem? item in patchItems)
        {
            if (item is not null)
            {
                if (item.Category > 0)
                {
                    pref.ResCategoryStatusDict.TryAdd(item.Category, 1);
                    pref.ResCategoryStatusDict.TryAdd(item.Category + 1_000_000, 1);
                }
                if (deletePatchFile && item.IsPatch)
                {
                    DeleteFileIfExists(Path.Combine(_persistentFolder, GetLocalPath(item.LocalName ?? item.RemoteName)));
                }
                item.Source = HotPatchItem.Res;
                itemsDict[item.RemoteName] = item;
            }
        }

        await File.WriteAllTextAsync(base_res_version_hash, md5, cancellation);
        await File.WriteAllTextAsync(Path.Combine(_persistentFolder, "ChannelName"), _serverRegion is ServerRegion.miHoYo ? "CNRELWin" : "OSRELWin", cancellation);
        await File.WriteAllTextAsync(Path.Combine(_persistentFolder, "ScriptVersion"), _gameBranch.Main.Tag, cancellation);
        await File.WriteAllBytesAsync(Path.Combine(_persistentFolder, "DownloadPref"), pref.Encode(), cancellation);

        string url = $"{_resUrl}/patch_node_versions";
        using var hs = await _httpClient.GetStreamAsync(url, cancellation);
        IAsyncEnumerable<PatchNodeVersionItem?> patchNodes = JsonSerializer.DeserializeAsyncEnumerable(hs, GenshinHotUpdateJsonContext.Default.PatchNodeVersionItem, true, cancellation);
        PatchNodeVersionItem? patchNode = await patchNodes.FirstOrDefaultAsync(x => x?.Branch == _regionInfo.ResVersionConfig.Branch, cancellationToken: cancellation);
        if (patchNode is not null)
        {
            await File.WriteAllTextAsync(Path.Combine(_persistentFolder, "PatchDone"), patchNode.Revision.ToString(), cancellation);
        }

        string ctable_streaming_data = Path.Combine(_streamAssetsFolder, "ctable_streaming.dat");
        if (File.Exists(ctable_streaming_data))
        {
            string ctable_dat = Path.Combine(_persistentFolder, "ctable.dat");
            DeleteFileIfExists(ctable_dat);
            if (!Kernel32.CreateHardLink(ctable_dat, ctable_streaming_data))
            {
                File.Copy(ctable_streaming_data, ctable_dat, true);
            }
        }

        string data_versions_streaming = Path.Combine(_streamAssetsFolder, "data_versions_streaming");
        if (File.Exists(data_versions_streaming))
        {
            using var fs_data = File.OpenRead(data_versions_streaming);
            IAsyncEnumerable<HotPatchItem?> items = JsonSerializer.DeserializeAsyncEnumerable(fs_data, GenshinHotUpdateJsonContext.Default.HotPatchItem, true, cancellation);
            await foreach (HotPatchItem? item in items)
            {
                if (item is not null)
                {
                    item.Source = HotPatchItem.Data;
                    itemsDict[item.RemoteName] = item;
                }
            }
        }
        string silence_versions_streaming = Path.Combine(_streamAssetsFolder, "silence_versions_streaming");
        if (File.Exists(silence_versions_streaming))
        {
            using var fs_silence = File.OpenRead(silence_versions_streaming);
            IAsyncEnumerable<HotPatchItem?> items = JsonSerializer.DeserializeAsyncEnumerable(fs_silence, GenshinHotUpdateJsonContext.Default.HotPatchItem, true, cancellation);
            await foreach (HotPatchItem? item in items)
            {
                if (item is not null)
                {
                    item.Source = HotPatchItem.Silence;
                    itemsDict[item.RemoteName] = item;
                }
            }
        }
        _streamAssetItems = itemsDict;
    }



    private async Task PrepareDownloadFilesAsync(CancellationToken cancellation)
    {
        if (_downloadItems is not null)
        {
            return;
        }
        List<HotPatchItem> patchItems = new();

        {
            string res_revision = Path.Combine(_persistentFolder, "res_revision");
            string audio_revision = Path.Combine(_persistentFolder, "audio_revision");
            if (!File.Exists(res_revision)
                || await File.ReadAllTextAsync(res_revision, cancellation) != _regionInfo.ResVersionConfig.Version.ToString()
                || !File.Exists(audio_revision)
                || await File.ReadAllTextAsync(audio_revision, cancellation) != _audioRevision)
            {
                string res_versions_external_url = $"{_resUrl}/res_versions_external";
                byte[] res_versions_external_bytes = await _httpClient.GetByteArrayAsync(res_versions_external_url, cancellation);
                await File.WriteAllBytesAsync(Path.Combine(_persistentFolder, "res_versions_persist"), res_versions_external_bytes, cancellation);
                ReadOnlyMemoryStream ms = new ReadOnlyMemoryStream(res_versions_external_bytes);
                IAsyncEnumerable<HotPatchItem?> items = JsonSerializer.DeserializeAsyncEnumerable(ms, GenshinHotUpdateJsonContext.Default.HotPatchItem, true, cancellation);
                await foreach (HotPatchItem? item in items)
                {
                    if (item is not null)
                    {
                        if (_streamAssetItems.TryGetValue(item.RemoteName, out var oldItem) && string.Equals(item.MD5, oldItem.MD5, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        item.Source = HotPatchItem.Res;
                        patchItems.Add(item);
                    }
                }
            }
        }
        {
            string data_revision = Path.Combine(_persistentFolder, "data_revision");
            if (!File.Exists(data_revision) || await File.ReadAllTextAsync(data_revision, cancellation) != _regionInfo.ClientDataVersion.ToString())
            {
                string data_versions_url = $"{_dataUrl}/AssetBundles/data_versions";
                byte[] data_versions_bytes = await _httpClient.GetByteArrayAsync(data_versions_url, cancellation);
                await File.WriteAllBytesAsync(Path.Combine(_persistentFolder, "data_versions_persist"), data_versions_bytes, cancellation);
                ReadOnlyMemoryStream ms = new ReadOnlyMemoryStream(data_versions_bytes);
                IAsyncEnumerable<HotPatchItem?> items = JsonSerializer.DeserializeAsyncEnumerable(ms, GenshinHotUpdateJsonContext.Default.HotPatchItem, true, cancellation);
                await foreach (HotPatchItem? item in items)
                {
                    if (item is not null)
                    {
                        if (_streamAssetItems.TryGetValue(item.RemoteName, out var oldItem) && string.Equals(item.MD5, oldItem.MD5, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        item.Source = HotPatchItem.Data;
                        patchItems.Add(item);
                    }
                }
            }
        }
        {
            string silence_revision = Path.Combine(_persistentFolder, "silence_revision");
            if (!File.Exists(silence_revision) || await File.ReadAllTextAsync(silence_revision, cancellation) != _regionInfo.ClientSilenceDataVersion.ToString())
            {
                string data_versions_url = $"{_silenceUrl}/AssetBundles/data_versions";
                byte[] data_versions_bytes = await _httpClient.GetByteArrayAsync(data_versions_url, cancellation);
                await File.WriteAllBytesAsync(Path.Combine(_persistentFolder, "silence_data_versions_persist"), data_versions_bytes, cancellation);
                ReadOnlyMemoryStream ms = new ReadOnlyMemoryStream(data_versions_bytes);
                IAsyncEnumerable<HotPatchItem?> items = JsonSerializer.DeserializeAsyncEnumerable(ms, GenshinHotUpdateJsonContext.Default.HotPatchItem, true, cancellation);
                await foreach (HotPatchItem? item in items)
                {
                    if (item is not null)
                    {
                        if (_streamAssetItems.TryGetValue(item.RemoteName, out var oldItem) && string.Equals(item.MD5, oldItem.MD5, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        item.Source = HotPatchItem.Silence;
                        patchItems.Add(item);
                    }
                }
            }
        }


        List<string> audio = new();
        string audio_lang_14 = Path.Combine(_persistentFolder, "audio_lang_14");
        if (File.Exists(audio_lang_14))
        {
            audio.AddRange(await File.ReadAllLinesAsync(audio_lang_14, cancellation));
        }

        List<DownloadItem> downloadItems = new();
        await Parallel.ForEachAsync(patchItems, cancellation, async (item, token) =>
        {
            if (item.RemoteName.Contains("Chinese/") || item.RemoteName.Contains("English(US)/") || item.RemoteName.Contains("Japanese/") || item.RemoteName.Contains("Korean/"))
            {
                var lang = item.RemoteName[..item.RemoteName.IndexOf('/')];
                if (!audio.Contains(lang))
                {
                    return;
                }
            }

            string relativePath = GetLocalPath(item.LocalName ?? item.RemoteName);
            string persist = Path.Combine(_persistentFolder, relativePath);
            if (await HashService.CheckFileMD5Async(persist, item.FileSize, item.MD5, token))
            {
                return;
            }
            string assets = Path.Combine(_streamAssetsFolder, relativePath);
            if (await HashService.CheckFileMD5Async(assets, item.FileSize, item.MD5, token))
            {
                DeleteFileIfExists(persist);
                return;
            }
            string remoteName = GetLocalPath(item.RemoteName);
            string? url = item.Source switch
            {
                HotPatchItem.Res => $"{(item.IsPatch ? _resUrl : _audioUrl)}/{remoteName}",
                HotPatchItem.Data => $"{_dataUrl}/{remoteName}",
                HotPatchItem.Silence => $"{_silenceUrl}/{remoteName}",
                _ => null,
            };
            if (url != null)
            {
                lock (downloadItems)
                {
                    downloadItems.Add(new DownloadItem
                    {
                        Path = persist,
                        Hash = item.Hash,
                        MD5 = item.MD5,
                        Size = item.FileSize,
                        Url = url,
                    });
                }
            }
        });
        _downloadItems = downloadItems;
    }



    private async Task DownloadFilesAsync(CancellationToken cancellation)
    {
        if (_downloadItems is null)
        {
            return;
        }

        _context.ClearProgress();
        _context.NetworkTotalBytes = _downloadItems.Sum(x => x.Size);
        _context.ProgressDisplay = ProgressDisplay.NetworkBytes | ProgressDisplay.NetworkSpeed;
        _context.Step = RunnerStep.Downloading;

        await _polly.ForEachAsync(_downloadItems, cancellation, async (item, token) =>
        {
            if (item.IsFinished)
            {
                _context.AddNetworkBytes(item.Size);
                return;
            }
            string tmp = item.Path + ".tmp";
            await _downloadService.DownloadToFileAsync(tmp, item.Url, item.Size, _context, token);
            if (await HashService.CheckFileMD5Async(tmp, item.Size, item.MD5, cancellation))
            {
                File.Move(tmp, item.Path, true);
                item.IsFinished = true;
            }
            else
            {
                DeleteFileIfExists(tmp);
                throw new FileMD5FailedException(tmp, item.MD5);
            }
        });
    }



    protected override async Task PostAsync(CancellationToken cancellation = default)
    {
        string res_revision = Path.Combine(_persistentFolder, "res_revision");
        string res_revision_eternal = Path.Combine(_persistentFolder, "res_revision_eternal");
        string audio_revision = Path.Combine(_persistentFolder, "audio_revision");
        string data_revision = Path.Combine(_persistentFolder, "data_revision");
        string silence_revision = Path.Combine(_persistentFolder, "silence_revision");
        await File.WriteAllTextAsync(res_revision, _regionInfo.ResVersionConfig.Version.ToString(), cancellation);
        await File.WriteAllTextAsync(res_revision_eternal, _regionInfo.ResVersionConfig.Version.ToString(), cancellation);
        await File.WriteAllTextAsync(audio_revision, _audioRevision, cancellation);
        await File.WriteAllTextAsync(data_revision, _regionInfo.ClientDataVersion.ToString(), cancellation);
        await File.WriteAllTextAsync(silence_revision, _regionInfo.ClientSilenceDataVersion.ToString(), cancellation);

        foreach (var item in Directory.GetFiles(_persistentFolder, "*.tmp", SearchOption.AllDirectories))
        {
            DeleteFileIfExists(item);
        }
    }



    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }



    private static string GetLocalPath(string name)
    {
        if (name.EndsWith(".pck"))
        {
            return $"AudioAssets/{name}";
        }
        if (name.EndsWith(".blk") || name.EndsWith("svc_catalog"))
        {
            return $"AssetBundles/{name}";
        }
        if (name.EndsWith(".usm") || name.EndsWith(".cuepoint"))
        {
            return $"VideoAssets/{name}";
        }
        return name;
    }



    private class DownloadItem
    {
        public string Path { get; set; }

        public string Url { get; set; }

        public string MD5 { get; set; }

        public string Hash { get; set; }

        public long Size { get; set; }

        public bool IsFinished { get; set; }
    }


}
