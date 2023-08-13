using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Starward.Core;
using Starward.Core.Launcher;
using Starward.Models;
using Starward.Services.Cache;
using Starward.SevenZip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services;

internal partial class DownloadGameService
{


    private readonly ILogger<DownloadGameService> _logger;


    private readonly GameService _gameService;


    private readonly LauncherClient _launcherClient;


    private readonly HttpClient _httpClient;

    public DownloadGameService(ILogger<DownloadGameService> logger, GameService gameService, LauncherClient launcherClient, HttpClient httpClient)
    {
        _logger = logger;
        _gameService = gameService;
        _launcherClient = launcherClient;
        _httpClient = httpClient;
    }






    #region Prepare




    public async Task<Version?> GetLocalGameVersionAsync(GameBiz biz)
    {
        var installPath = _gameService.GetGameInstallPath(biz);
        return await GetLocalGameVersionAsync(installPath);
    }



    [GeneratedRegex("game_version=(.+)")]
    private static partial Regex GameVersionRegex();


    public async Task<Version?> GetLocalGameVersionAsync(string? installPath)
    {
        var config = Path.Join(installPath, "config.ini");
        if (File.Exists(config))
        {
            var str = await File.ReadAllTextAsync(config);
            var ver = GameVersionRegex().Match(str).Groups[1].Value;
            if (Version.TryParse(ver, out var version))
            {
                return version;
            }
        }
        return null;
    }




    public async Task<LauncherResource> GetLauncherResourceAsync(GameBiz biz)
    {
        var resource = MemoryCache.Instance.GetItem<LauncherResource>($"LauncherResource_{biz}", TimeSpan.FromSeconds(10));
        if (resource is null)
        {
            resource = await _launcherClient.GetLauncherResourceAsync(biz);
            MemoryCache.Instance.SetItem($"LauncherResource_{biz}", resource);
        }
        return resource;
    }




    public async Task<(Version? LatestVersion, Version? PreDownloadVersion)> GetGameVersionAsync(GameBiz biz)
    {
        var resource = await GetLauncherResourceAsync(biz);
        _ = Version.TryParse(resource.Game?.Latest?.Version, out Version? latest);
        _ = Version.TryParse(resource.PreDownloadGame?.Latest?.Version, out Version? preDownload);
        return (latest, preDownload);
    }




    public async Task<bool> CheckPreDownloadIsOKAsync(GameBiz biz, string? installPath)
    {
        if (!Directory.Exists(installPath))
        {
            return false;
        }
        var resource = await GetLauncherResourceAsync(biz);
        if (resource.PreDownloadGame != null)
        {
            var localVersion = await GetLocalGameVersionAsync(biz);
            if (resource.PreDownloadGame.Diffs?.FirstOrDefault(x => x.Version == localVersion?.ToString()) is DiffPackage diff)
            {
                var package = CheckDownloadPackage(diff, installPath);
                if (package.DownloadedSize != package.PackageSize)
                {
                    long? length = await GetContentLengthAsync(package.Url);
                    return package.DownloadedSize == length;
                }
                var flag = await GetVoiceLanguageAsync(biz, installPath);
                foreach (var lang in Enum.GetValues<VoiceLanguage>())
                {
                    if (flag.HasFlag(lang))
                    {
                        if (diff.VoicePacks.FirstOrDefault(x => x.Language == lang.ToDescription()) is VoicePack pack)
                        {
                            var voicePackage = CheckDownloadPackage(pack, installPath);
                            if (voicePackage.DownloadedSize != voicePackage.PackageSize)
                            {
                                long? length = await GetContentLengthAsync(voicePackage.Url);
                                return voicePackage.DownloadedSize == length;
                            }
                        }
                    }
                }
                return true;
            }
            else
            {
                var package = CheckDownloadPackage(resource.PreDownloadGame.Latest, installPath);
                if (package.DownloadedSize != package.PackageSize)
                {
                    long? length = await GetContentLengthAsync(package.Url);
                    return package.DownloadedSize == length;
                }
                var flag = await GetVoiceLanguageAsync(biz, installPath);
                foreach (var lang in Enum.GetValues<VoiceLanguage>())
                {
                    if (flag.HasFlag(lang))
                    {
                        if (resource.PreDownloadGame.Latest.VoicePacks.FirstOrDefault(x => x.Language == lang.ToDescription()) is VoicePack pack)
                        {
                            var voicePackage = CheckDownloadPackage(pack, installPath);
                            if (voicePackage.DownloadedSize != voicePackage.PackageSize)
                            {
                                long? length = await GetContentLengthAsync(voicePackage.Url);
                                return voicePackage.DownloadedSize == length;
                            }
                        }
                    }
                }
                return true;
            }
        }
        return false;
    }



    public async Task<VoiceLanguage> GetVoiceLanguageAsync(GameBiz biz, string? installPath)
    {
        var file = biz switch
        {
            GameBiz.hk4e_cn => Path.Join(installPath, @"YuanShen_Data\Persistent\audio_lang_14"),
            GameBiz.hk4e_global => Path.Join(installPath, @"GenshinImpact_Data\Persistent\audio_lang_14"),
            _ => ""
        };
        var flag = VoiceLanguage.None;
        if (File.Exists(file))
        {
            var lines = await File.ReadAllLinesAsync(file);
            if (lines.Any(x => x.Contains("Chinese"))) { flag |= VoiceLanguage.Chinese; }
            if (lines.Any(x => x.Contains("English(US)"))) { flag |= VoiceLanguage.English; }
            if (lines.Any(x => x.Contains("Japanese"))) { flag |= VoiceLanguage.Japanese; }
            if (lines.Any(x => x.Contains("Korean"))) { flag |= VoiceLanguage.Korean; }
        }
        return flag;
    }



    public async Task SetVoiceLanguageAsync(GameBiz biz, string installPath, VoiceLanguage lang)
    {
        if (biz is GameBiz.hk4e_cn or GameBiz.hk4e_global)
        {
            var file = biz switch
            {
                GameBiz.hk4e_cn => Path.Join(installPath, @"YuanShen_Data\Persistent\audio_lang_14"),
                GameBiz.hk4e_global => Path.Join(installPath, @"GenshinImpact_Data\Persistent\audio_lang_14"),
                _ => ""
            };
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var lines = new List<string>(4);
            if (lang.HasFlag(VoiceLanguage.Chinese)) { lines.Add("Chinese"); }
            if (lang.HasFlag(VoiceLanguage.English)) { lines.Add("English(US)"); }
            if (lang.HasFlag(VoiceLanguage.Japanese)) { lines.Add("Japanese"); }
            if (lang.HasFlag(VoiceLanguage.Korean)) { lines.Add("Korean"); }
            await File.WriteAllLinesAsync(file, lines);
            if (File.Exists(file))
            {
                var key = biz switch
                {
                    GameBiz.hk4e_cn => GameRegistry.GamePath_hk4e_cn,
                    GameBiz.hk4e_global => GameRegistry.GamePath_hk4e_global,
                    _ => "HKEY_CURRENT_USER",
                };
                var value = Registry.GetValue(key, GameRegistry.GENERAL_DATA_h2389025596, null) as byte[];
                if (value is null)
                {
                    int? id = null;
                    if (lang.HasFlag(VoiceLanguage.Chinese))
                    {
                        id = 0;
                    }
                    else if (lang.HasFlag(VoiceLanguage.English))
                    {
                        id = 1;
                    }
                    else if (lang.HasFlag(VoiceLanguage.Japanese))
                    {
                        id = 2;
                    }
                    else if (lang.HasFlag(VoiceLanguage.Korean))
                    {
                        id = 3;
                    }
                    if (id != null)
                    {
                        Registry.SetValue(key, GameRegistry.GENERAL_DATA_h2389025596, Encoding.UTF8.GetBytes($"{{\"deviceVoiceLanguageType\":{id}}}\0"));
                    }
                }
            }
        }
    }





    public async Task<DownloadGameResource?> CheckDownloadGameResourceAsync(GameBiz biz, string installPath)
    {
        var localVersion = await GetLocalGameVersionAsync(installPath);
        (Version? latestVersion, Version? preDownloadVersion) = await GetGameVersionAsync(biz);
        var resource = await GetLauncherResourceAsync(biz);
        GameResource? gameResource = null;

        if (localVersion is null)
        {
            gameResource = resource.Game;
        }
        else if (preDownloadVersion != null)
        {
            gameResource = resource.PreDownloadGame;
        }
        else if (latestVersion > localVersion)
        {
            gameResource = resource.Game;
        }


        if (gameResource != null)
        {
            var downloadGameResource = new DownloadGameResource();
            downloadGameResource.FreeSpace = new DriveInfo(Path.GetFullPath(installPath).Substring(0, 1)).AvailableFreeSpace;
            if (gameResource.Diffs?.FirstOrDefault(x => x.Version == localVersion?.ToString()) is DiffPackage diff)
            {
                downloadGameResource.Game = CheckDownloadPackage(diff, installPath);
                foreach (var pack in diff.VoicePacks)
                {
                    var state = CheckDownloadPackage(pack, installPath);
                    state.Name = pack.Language;
                    downloadGameResource.Voices.Add(state);
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(gameResource.Latest.Path))
                {
                    var state = new DownloadPackageState
                    {
                        PackageSize = gameResource.Latest.PackageSize,
                        DecompressedSize = gameResource.Latest.Size,
                    };
                    var size = gameResource.Latest.Segments.Sum(x => CheckDownloadPackage(Path.GetFileName(x.Path), installPath));
                    state.DownloadedSize = size;
                    downloadGameResource.Game = state;
                }
                else
                {
                    downloadGameResource.Game = CheckDownloadPackage(gameResource.Latest, installPath);
                }
                foreach (var pack in gameResource.Latest.VoicePacks)
                {
                    var state = CheckDownloadPackage(pack, installPath);
                    state.Name = pack.Language;
                    downloadGameResource.Voices.Add(state);
                }
            }
            return downloadGameResource;
        }

        return null;

    }





    private DownloadPackageState CheckDownloadPackage(IGamePackage package, string installPath)
    {
        var state = new DownloadPackageState
        {
            Name = Path.GetFileName(package.Path),
            Url = package.Path,
            PackageSize = package.PackageSize,
            DecompressedSize = package.Size,
        };
        var file = Path.Join(installPath, state.Name);
        if (File.Exists(file))
        {
            state.DownloadedSize = new FileInfo(file).Length;
        }
        else
        {
            var files = Directory.GetFiles(installPath, $"{state.Name}.slice.*");
            state.DownloadedSize = files.Select(x => new FileInfo(x).Length).Sum();
        }
        return state;
    }



    private long CheckDownloadPackage(string name, string installPath)
    {
        var file = Path.Join(installPath, name);
        if (File.Exists(file))
        {
            return new FileInfo(file).Length;
        }
        else
        {
            var files = Directory.GetFiles(installPath, $"{name}.slice.*");
            return files.Select(x => new FileInfo(x).Length).Sum();
        }
    }



    #endregion





    #region Download




    private GameBiz gameBiz;

    private string installPath;


    private List<DownloadTask> packageTasks;


    private List<DownloadTask> sliceTasks;

    private LauncherResource launcherResource;

    private string sperateUrl;


    public long TotalBytes { get; private set; }

    private long progressBytes;
    public long ProgressBytes => progressBytes;


    public int TotalCount { get; set; }

    private int progressCount;
    public int ProgressCount => progressCount;


    public DownloadGameState State { get; private set; }

    public string ActionType { get; private set; }

    public string ErrorType { get; private set; }

    public string ErrorMessage { get; private set; }




    private async Task<long?> GetContentLengthAsync(string url)
    {
        _logger.LogInformation("Request head: {url}", url);
        var request = new HttpRequestMessage(HttpMethod.Head, url);
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        return response.Content.Headers.ContentLength;
    }




    public async Task<bool> PrepareForDownloadAsync(GameBiz biz, string installPath, VoiceLanguage language)
    {
        bool decompress = false;
        try
        {
            State = DownloadGameState.Preparing;
            TotalBytes = 0;
            progressBytes = 0;

            var localVersion = await GetLocalGameVersionAsync(installPath).ConfigureAwait(false);
            (Version? latestVersion, Version? preDownloadVersion) = await GetGameVersionAsync(biz).ConfigureAwait(false);
            launcherResource = await GetLauncherResourceAsync(biz).ConfigureAwait(false);
            GameResource? gameResource = null;
            if (localVersion is null)
            {
                gameResource = launcherResource.Game;
                decompress = true;
                ActionType = Lang.LauncherPage_InstallGame;
            }
            else if (preDownloadVersion != null)
            {
                gameResource = launcherResource.PreDownloadGame;
                ActionType = Lang.LauncherPage_PreInstall;
            }
            else if (latestVersion > localVersion)
            {
                gameResource = launcherResource.Game;
                decompress = true;
                ActionType = Lang.LauncherPage_UpdateGame;
            }


            if (gameResource == null)
            {
                // 已是最新版本
                throw new Exception(Lang.DownloadGameService_AlreadyTheLatestVersion);
            }

            this.installPath = installPath;
            this.gameBiz = biz;

            var list_package = new List<DownloadTask>();

            if (gameResource.Diffs?.FirstOrDefault(x => x.Version == localVersion?.ToString()) is DiffPackage diff)
            {
                list_package.Add(new DownloadTask { FileName = Path.GetFileName(diff.Path), Url = diff.Path, Size = diff.PackageSize, MD5 = diff.Md5 });
                var flag = await GetVoiceLanguageAsync(biz, installPath).ConfigureAwait(false);
                foreach (var lang in Enum.GetValues<VoiceLanguage>())
                {
                    if (flag.HasFlag(lang))
                    {
                        if (diff.VoicePacks.FirstOrDefault(x => x.Language == lang.ToDescription()) is VoicePack pack)
                        {
                            list_package.Add(new DownloadTask { FileName = Path.GetFileName(pack.Path), Url = pack.Path, Size = pack.PackageSize, MD5 = pack.Md5 });
                        }
                    }
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(gameResource.Latest.Path))
                {
                    list_package.AddRange(gameResource.Latest.Segments.Select(x => new DownloadTask { FileName = Path.GetFileName(x.Path), Url = x.Path, MD5 = x.Md5, IsSegment = true }));
                }
                else
                {
                    list_package.Add(new DownloadTask { FileName = Path.GetFileName(gameResource.Latest.Path), Url = gameResource.Latest.Path, Size = gameResource.Latest.PackageSize, MD5 = gameResource.Latest.Md5 });
                }
                foreach (var lang in Enum.GetValues<VoiceLanguage>())
                {
                    if (language.HasFlag(lang))
                    {
                        if (gameResource.Latest.VoicePacks.FirstOrDefault(x => x.Language == lang.ToDescription()) is VoicePack pack)
                        {
                            list_package.Add(new DownloadTask { FileName = Path.GetFileName(pack.Path), Url = pack.Path, Size = pack.PackageSize, MD5 = pack.Md5 });
                        }
                    }
                }
            }

            await SetVoiceLanguageAsync(biz, installPath, language);

            await Parallel.ForEachAsync(list_package, async (x, _) =>
            {
                var len = await GetContentLengthAsync(x.Url).ConfigureAwait(false);
                if (len.HasValue)
                {
                    x.Size = len.Value;
                }
                x.Range = (0, x.Size);
            }).ConfigureAwait(false);

            packageTasks = list_package;

            const long GB = 1 << 30;

            var list_slice = new List<DownloadTask>();
            foreach (var item in list_package)
            {
                var file = Path.Combine(installPath, item.FileName);
                if (File.Exists(file) && new FileInfo(file).Length == item.Size)
                {
                    var t = new DownloadTask
                    {
                        FileName = item.FileName,
                        Url = item.Url,
                        Size = item.Size,
                        MD5 = item.MD5,
                        Range = (0, item.Size),
                        DownloadSize = item.Size,
                    };
                    list_slice.Add(t);
                }
                else
                {
                    int b = (int)(item.Size / GB);
                    int c = (int)(item.Size % GB);
                    var temp = Enumerable.Range(0, b).Select(x =>
                    {
                        var t = new DownloadTask
                        {
                            FileName = $"{item.FileName}.slice.{x:D3}",
                            Url = item.Url,
                            Size = GB,
                            MD5 = item.MD5,
                            Range = (GB * x, GB * (x + 1)),
                        };
                        var f = Path.Combine(installPath, t.FileName);
                        if (File.Exists(f))
                        {
                            t.DownloadSize = new FileInfo(f).Length;
                        }
                        return t;
                    }).ToList();
                    if (c > 0)
                    {
                        var t = new DownloadTask
                        {
                            FileName = $"{item.FileName}.slice.{b:D3}",
                            Url = item.Url,
                            Size = c,
                            MD5 = item.MD5,
                            Range = (GB * b, item.Size),
                        };
                        var f = Path.Combine(installPath, t.FileName);
                        if (File.Exists(f))
                        {
                            t.DownloadSize = new FileInfo(f).Length;
                        }
                        temp.Add(t);
                    }
                    list_slice.AddRange(temp);
                }
            }
            sliceTasks = list_slice;


            TotalBytes = sliceTasks.Sum(x => x.Size);
            progressBytes = sliceTasks.Sum(x => x.DownloadSize);

            State = DownloadGameState.Prepared;
        }
        catch (Exception ex)
        {
            ErrorType = ex.GetType().Name;
            ErrorMessage = ex.Message;
            State = DownloadGameState.Error;
            _logger.LogError(ex, "Prepare for download");
        }
        return decompress;
    }



    public async Task PrepareForRepairAsync(GameBiz biz, string installPath, VoiceLanguage language)
    {
        try
        {
            State = DownloadGameState.Preparing;
            TotalBytes = 0;
            progressBytes = 0;
            ActionType = Lang.DownloadGameService_RepairMode;

            launcherResource = await GetLauncherResourceAsync(biz).ConfigureAwait(false);
            GameResource gameResource = launcherResource.Game;

            this.installPath = installPath;
            this.gameBiz = biz;

            sperateUrl = gameResource.Latest.DecompressedPath.TrimEnd('/');

            var list = new List<DownloadTask>();

            list.AddRange(await GetPkgVersionsAsync($"{sperateUrl}/pkg_version"));

            if (gameBiz.ToGame() is GameBiz.GenshinImpact)
            {
                if (language.HasFlag(VoiceLanguage.Chinese))
                {
                    list.AddRange(await GetPkgVersionsAsync($"{sperateUrl}/Audio_Chinese_pkg_version"));
                }
                if (language.HasFlag(VoiceLanguage.English))
                {
                    list.AddRange(await GetPkgVersionsAsync($"{sperateUrl}/Audio_English(US)_pkg_version"));
                }
                if (language.HasFlag(VoiceLanguage.Japanese))
                {
                    list.AddRange(await GetPkgVersionsAsync($"{sperateUrl}/Audio_Japanese_pkg_version"));
                }
                if (language.HasFlag(VoiceLanguage.Korean))
                {
                    list.AddRange(await GetPkgVersionsAsync($"{sperateUrl}/Audio_Korean_pkg_version"));
                }
            }

            packageTasks = list;

            if (gameBiz.ToGame() is GameBiz.GenshinImpact)
            {
                string? exe_cn = Path.Join(installPath, "YuanShen.exe");
                string? exe_os = Path.Join(installPath, "GenshinImpact.exe");
                string? data_cn = Path.Join(installPath, "YuanShen_Data");
                string? data_os = Path.Join(installPath, "GenshinImpact_Data");
                if (gameBiz is GameBiz.hk4e_cn)
                {
                    if (File.Exists(exe_os))
                    {
                        File.Delete(exe_os);
                    }
                    if (!Directory.Exists(data_cn) && Directory.Exists(data_os))
                    {
                        Directory.Move(data_os, data_cn);
                    }
                }
                if (gameBiz is GameBiz.hk4e_global)
                {
                    if (File.Exists(exe_cn))
                    {
                        File.Delete(exe_cn);
                    }
                    if (!Directory.Exists(data_os) && Directory.Exists(data_cn))
                    {
                        Directory.Move(data_cn, data_os);
                    }
                }
                await SetVoiceLanguageAsync(biz, installPath, language);
                await CopyAudioAssetsFromPersistentToStreamAssetsAsync();
            }

            State = DownloadGameState.Prepared;
        }
        catch (Exception ex)
        {
            ErrorType = ex.GetType().Name;
            ErrorMessage = ex.Message;
            State = DownloadGameState.Error;
            _logger.LogError(ex, "Prepare for repair");
        }
    }



    private async Task<List<DownloadTask>> GetPkgVersionsAsync(string url)
    {
        var list = new List<DownloadTask>();
        var str = await _httpClient.GetStringAsync(url);
        var lines = str.Split('\n');
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                var node = JsonNode.Parse(line.Trim());
                list.Add(new DownloadTask
                {
                    FileName = node?["remoteName"]?.ToString()!,
                    MD5 = node?["md5"]?.ToString()!,
                    Size = (long)(node?["fileSize"] ?? 0),
                });
            }
        }
        return list;
    }



    public async Task DownloadAsync(CancellationToken cancellationToken)
    {
        try
        {
            State = DownloadGameState.Downloading;

            int count = 0;
            while (true)
            {
                if (++count > 3)
                {
                    // 重试次数过多
                    throw new HttpRequestException(Lang.DownloadGameService_TooManyRetries);
                }

                var FileStreamOptions = new FileStreamOptions
                {
                    Access = FileAccess.Write,
                    BufferSize = 1 << 16,
                    Mode = FileMode.Append,
                    Options = FileOptions.Asynchronous
                };

                await Parallel.ForEachAsync(sliceTasks, new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = cancellationToken
                }, async (slice, token) =>
                {
                    try
                    {
                        var file = Path.Combine(installPath, slice.FileName);
                        using var fs = File.Open(file, FileStreamOptions);
                        if (fs.Length == slice.Size)
                        {
                            return;
                        }
                        _logger.LogInformation("Download Slice: FileName {name}, Url {url}", slice.FileName, slice.Url);
                        long ts = Stopwatch.GetTimestamp();
                        var request = new HttpRequestMessage(HttpMethod.Get, slice.Url) { Version = HttpVersion.Version11 };
                        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(slice.Range.Start + fs.Position, slice.Range.End - 1);
                        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();
                        using var hs = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);

                        var buffer = new byte[1 << 16];
                        int length;
                        while ((length = await hs.ReadAsync(buffer, token).ConfigureAwait(false)) != 0)
                        {
                            await fs.WriteAsync(buffer.AsMemory(0, length), token).ConfigureAwait(false);
                            Interlocked.Add(ref progressBytes, length);
                        }
                        _logger.LogInformation("Download Successfully: FileName {name}, Elapsed {time}", slice.FileName, Stopwatch.GetElapsedTime(ts));
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex, "Download Slice: FileName {name}, Url {url}", slice.FileName, slice.Url);
                    }
                }).ConfigureAwait(false);

                bool failed = false;
                foreach (var item in sliceTasks)
                {
                    var file = Path.Combine(installPath, item.FileName);
                    if (!File.Exists(file))
                    {
                        failed = true;
                        break;
                    }
                    if (new FileInfo(file).Length != item.Size)
                    {
                        failed = true;
                        break;
                    }
                }
                if (failed)
                {
                    continue;
                }
                break;
            }

            State = DownloadGameState.Downloaded;
        }
        catch (TaskCanceledException)
        {
            State = DownloadGameState.Prepared;
        }
        catch (OperationCanceledException)
        {
            State = DownloadGameState.Prepared;
        }
        catch (Exception ex)
        {
            ErrorType = ex.GetType().Name;
            ErrorMessage = ex.Message;
            State = DownloadGameState.Error;
            _logger.LogError(ex, "Download Slice");
        }
    }



    public async Task DownloadSeparateFilesAsync(CancellationToken cancellationToken)
    {
        try
        {
            State = DownloadGameState.Downloading;

            int count = 0;
            while (true)
            {
                if (++count > 3)
                {
                    // 重试次数过多
                    throw new HttpRequestException(Lang.DownloadGameService_TooManyRetries);
                }

                var FileStreamOptions = new FileStreamOptions
                {
                    Access = FileAccess.Write,
                    BufferSize = 1 << 16,
                    Mode = FileMode.Append,
                    Options = FileOptions.Asynchronous
                };

                await Parallel.ForEachAsync(sliceTasks, new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
                    CancellationToken = cancellationToken
                }, async (item, token) =>
                {
                    try
                    {
                        var file = Path.Combine(installPath, item.FileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                        using var fs = File.Open(file, FileStreamOptions);
                        if (fs.Length == item.Size)
                        {
                            return;
                        }
                        var url = $"{sperateUrl}/{item.FileName.TrimStart('/')}";
                        _logger.LogInformation("Download file: FileName {name}, Url {url}", item.FileName, url);
                        var request = new HttpRequestMessage(HttpMethod.Get, url) { Version = HttpVersion.Version20, };
                        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(fs.Position, null);
                        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();
                        using var hs = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);

                        var buffer = new byte[1 << 16];
                        int length;
                        while ((length = await hs.ReadAsync(buffer, token).ConfigureAwait(false)) != 0)
                        {
                            await fs.WriteAsync(buffer.AsMemory(0, length), token).ConfigureAwait(false);
                            Interlocked.Add(ref progressBytes, length);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        var url = $"{sperateUrl}/{item.FileName.TrimStart('/')}";
                        _logger.LogError(ex, "Download Slice: FileName {name}, Url {url}", item.FileName, url);
                    }
                }).ConfigureAwait(false);

                foreach (var item in sliceTasks)
                {
                    var file = Path.Combine(installPath, item.FileName);
                    if (!File.Exists(file))
                    {
                        continue;
                    }
                    if (new FileInfo(file).Length != item.Size)
                    {
                        continue;
                    }
                }
                break;
            }

            await ClearDeprecatedFilesAsync();

            var config = $"""
                [General]
                channel=1
                cps=
                game_version={launcherResource.Game.Latest.Version}
                sub_channel=1
                sdk_version=
                """;
            _logger.LogInformation("Write config.ini (game_version={version})", launcherResource.Game.Latest.Version);
            await File.WriteAllTextAsync(Path.Combine(installPath, "config.ini"), config);

            State = DownloadGameState.Downloaded;
        }
        catch (TaskCanceledException)
        {
            State = DownloadGameState.Verified;
        }
        catch (OperationCanceledException)
        {
            State = DownloadGameState.Verified;
        }
        catch (Exception ex)
        {
            ErrorType = ex.GetType().Name;
            ErrorMessage = ex.Message;
            State = DownloadGameState.Error;
            _logger.LogError(ex, "Download Slice");
        }
    }



    public async Task<List<string>> VerifyPackageAsync(CancellationToken cancellationToken)
    {
        var list = new List<string>();
        try
        {
            State = DownloadGameState.Verifying;
            TotalBytes = packageTasks.Sum(x => x.Size);
            progressBytes = 0;

            await Task.Run(async () =>
            {
                byte[] buffer = new byte[1 << 20];
                foreach (var item in packageTasks)
                {
                    _logger.LogInformation("Verify file {file}", item.FileName);
                    IEnumerable<string> files;
                    var file = Path.Combine(installPath, item.FileName);
                    if (File.Exists(file))
                    {
                        files = new string[] { file };
                    }
                    else
                    {
                        files = Directory.GetFiles(installPath, $"{item.FileName}.slice.*");
                        if (!files.Any())
                        {
                            list.Add(item.FileName);
                            _logger.LogWarning("File not found: {name}", item.FileName);
                            continue;
                        }
                    }
                    using var fs = new SliceStream(files);
                    int length = 0;
                    var hashProvider = MD5.Create();
                    while ((length = await fs.ReadAsync(buffer, cancellationToken)) != 0)
                    {
                        hashProvider.TransformBlock(buffer, 0, length, buffer, 0);
                        progressBytes += length;
                    }
                    hashProvider.TransformFinalBlock(buffer, 0, length);
                    var hash = hashProvider.Hash;
                    if (!(hash?.SequenceEqual(Convert.FromHexString(item.MD5)) ?? false))
                    {
                        list.Add(item.FileName);
                        _logger.LogWarning("File checksum failure: {name}", item.FileName);
                    }
                }
            }, cancellationToken);

            if (list.Any())
            {
                ErrorMessage = "File checksum failure";
                State = DownloadGameState.None;
            }
            else
            {
                State = DownloadGameState.Verified;
            }
        }
        catch (TaskCanceledException)
        {
            State = DownloadGameState.Verified;
        }
        catch (OperationCanceledException)
        {
            State = DownloadGameState.Verified;
        }
        catch (Exception ex)
        {
            ErrorType = ex.GetType().Name;
            ErrorMessage = ex.Message;
            State = DownloadGameState.Error;
            _logger.LogError(ex, "Verify file");
        }

        return list;
    }



    public async Task VerifySeparateFilesAsync(CancellationToken cancellationToken)
    {
        try
        {
            State = DownloadGameState.Verifying;
            TotalCount = packageTasks.Count;
            progressCount = 0;
            progressBytes = 0;

            var list = new List<DownloadTask>();

            await Parallel.ForEachAsync(packageTasks, cancellationToken, async (item, token) =>
            {
                byte[] buffer = new byte[1 << 18];
                var file = Path.Combine(installPath, item.FileName);
                bool needDownload = false;
                if (File.Exists(file))
                {
                    using var fs = File.OpenRead(file);
                    if (fs.Length != item.Size)
                    {
                        needDownload = true;
                    }
                    else
                    {
                        var hashProvider = MD5.Create();
                        int read = 0;
                        while ((read = await fs.ReadAsync(buffer, cancellationToken)) != 0)
                        {
                            hashProvider.TransformBlock(buffer, 0, read, buffer, 0);
                            Interlocked.Add(ref progressBytes, read);
                        }
                        hashProvider.TransformFinalBlock(buffer, 0, read);
                        var hash = hashProvider.Hash;
                        if (!(hash?.SequenceEqual(Convert.FromHexString(item.MD5)) ?? false))
                        {
                            needDownload = true;
                        }
                    }
                }
                else
                {
                    needDownload = true;
                }
                Interlocked.Increment(ref progressCount);
                if (needDownload)
                {
                    _logger.LogInformation("Need to download: {file}", item.FileName);
                    if (File.Exists(file))
                    {
                        File.SetAttributes(file, FileAttributes.Archive);
                        File.Delete(file);
                    }
                    lock (list)
                    {
                        list.Add(item);
                    }
                }
            });

            if (gameBiz.ToGame() is GameBiz.Honkai3rd)
            {
                string BH3Base = Path.Combine(installPath, "BH3Base.dll");
                if (File.Exists(BH3Base))
                {
                    File.Delete(BH3Base);
                }
                string bugtrace = Path.Combine(installPath, "bugtrace.dll");
                if (File.Exists(bugtrace))
                {
                    File.Delete(bugtrace);
                }
                long? length = await GetContentLengthAsync($"{sperateUrl}/BH3Base.dll");
                list.Add(new DownloadTask { FileName = "BH3Base.dll", MD5 = "", Size = length ?? 0 });
                length = await GetContentLengthAsync($"{sperateUrl}/bugtrace.dll");
                list.Add(new DownloadTask { FileName = "bugtrace.dll", MD5 = "", Size = length ?? 0 });
            }

            sliceTasks = list;
            TotalBytes = sliceTasks.Sum(x => x.Size);
            progressBytes = 0;
            State = DownloadGameState.Verified;
        }
        catch (TaskCanceledException)
        {
            State = DownloadGameState.Verified;
        }
        catch (OperationCanceledException)
        {
            State = DownloadGameState.Verified;
        }
        catch (Exception ex)
        {
            ErrorType = ex.GetType().Name;
            ErrorMessage = ex.Message;
            State = DownloadGameState.Error;
            _logger.LogError(ex, "Verify separate files");
        }
    }



    public async Task DecompressAsync()
    {
        try
        {
            State = DownloadGameState.Decompressing;
            TotalBytes = packageTasks.Sum(x => x.Size);
            progressBytes = 0;

            var sevenZipDll = Path.Combine(AppContext.BaseDirectory, "7z.dll");
            if (!File.Exists(sevenZipDll))
            {
                throw new FileNotFoundException($"File not found: {sevenZipDll}");
            }
            var hpatch = Path.Combine(AppContext.BaseDirectory, "hpatchz.exe");
            if (!File.Exists(sevenZipDll))
            {
                throw new FileNotFoundException($"File not found: {hpatch}");
            }

            await CopyAudioAssetsFromPersistentToStreamAssetsAsync();


            foreach (var package in packageTasks)
            {
                _logger.LogInformation("Decompress {file}", package.FileName);
                var file = Path.Combine(installPath, package.FileName);
                if (File.Exists(file) && new FileInfo(file).Length == package.Size)
                {
                    await DecompressAsync(new string[] { file }, package.FileName.Contains(".7z", StringComparison.OrdinalIgnoreCase)).ConfigureAwait(false);
                }
                else
                {
                    if (package.IsSegment)
                    {
                        var files = Directory.GetFiles(installPath, $"{Path.GetFileNameWithoutExtension(package.FileName)}.*");
                        await DecompressAsync(files, package.FileName.Contains(".7z", StringComparison.OrdinalIgnoreCase)).ConfigureAwait(false);
                    }
                    else
                    {
                        var files = Directory.GetFiles(installPath, $"{package.FileName}.slice.*");
                        await DecompressAsync(files, package.FileName.Contains(".7z", StringComparison.OrdinalIgnoreCase)).ConfigureAwait(false);
                    }
                }
            }


            await ClearDeprecatedFilesAsync();


            var config = $"""
                [General]
                channel=1
                cps=
                game_version={launcherResource.Game.Latest.Version}
                sub_channel=1
                sdk_version=
                """;
            _logger.LogInformation("Write config.ini (game_version={version})", launcherResource.Game.Latest.Version);
            await File.WriteAllTextAsync(Path.Combine(installPath, "config.ini"), config);

            State = DownloadGameState.Decompressed;
        }
        catch (Exception ex)
        {
            ErrorType = ex.GetType().Name;
            ErrorMessage = ex.Message;
            State = DownloadGameState.Error;
            _logger.LogError(ex, "Decompress file");
        }
    }



    private async Task CopyAudioAssetsFromPersistentToStreamAssetsAsync()
    {
        if (gameBiz is GameBiz.hk4e_cn or GameBiz.hk4e_global)
        {
            await Task.Run(() =>
            {
                string dataName = gameBiz switch
                {
                    GameBiz.hk4e_cn => "YuanShen_Data",
                    GameBiz.hk4e_global => "GenshinImpact_Data",
                    _ => "",
                };
                if (!string.IsNullOrWhiteSpace(dataName))
                {
                    var source = Path.Combine(installPath, $@"{dataName}\Persistent\AudioAssets");
                    var target = Path.Combine(installPath, $@"{dataName}\StreamingAssets\AudioAssets");
                    if (Directory.Exists(source))
                    {
                        var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
                        _logger.LogInformation("Move audio assets: {count} files.", files.Length);
                        foreach (var file in files)
                        {
                            var relative = Path.GetRelativePath(source, file);
                            var dest = Path.Combine(target, relative);
                            if (File.Exists(dest))
                            {
                                File.SetAttributes(dest, FileAttributes.Archive);
                            }
                            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                            File.Move(file, dest, true);
                            if (File.Exists(dest))
                            {
                                File.SetAttributes(dest, FileAttributes.Archive);
                            }
                        }
                    }
                }
            });
        }
    }


    private async Task DecompressAsync(IEnumerable<string> files, bool use7zip)
    {
        if (files.Any())
        {
            using var fs = new SliceStream(files);
            if (use7zip)
            {
                await Task.Run(() =>
                {
                    using var extra = new ArchiveFile(fs);
                    double ratio = (double)fs.Length / extra.Entries.Sum(x => (long)x.Size);
                    long sum = 0;
                    extra.ExtractProgress += (_, e) =>
                    {
                        long size = (long)(e.Read * ratio);
                        progressBytes += size;
                        sum += size;
                    };
                    extra.Extract(installPath, true);
                    progressBytes += fs.Length - sum;
                }).ConfigureAwait(false);
            }
            else
            {
                await Task.Run(() =>
                {
                    long sum = 0;
                    using var zip = new ZipArchive(fs, ZipArchiveMode.Read, true);
                    foreach (var item in zip.Entries)
                    {
                        if ((item.ExternalAttributes & 0x10) > 0)
                        {
                            var target = Path.Combine(installPath, item.FullName);
                            Directory.CreateDirectory(target);
                        }
                        else
                        {
                            var target = Path.Combine(installPath, item.FullName);
                            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                            item.ExtractToFile(target, true);
                            progressBytes += item.CompressedLength;
                            sum += item.CompressedLength;
                        }
                    }
                    progressBytes += fs.Length - sum;
                }).ConfigureAwait(false);
            }
            fs.Dispose();

            foreach (var item in files)
            {
                File.Delete(item);
            }

            var delete = Path.Combine(installPath, "deletefiles.txt");
            if (File.Exists(delete))
            {
                var deleteFiles = await File.ReadAllLinesAsync(delete).ConfigureAwait(false);
                foreach (var file in deleteFiles)
                {
                    var target = Path.Combine(installPath, file);
                    if (File.Exists(target))
                    {
                        File.Delete(target);
                    }
                }
                File.Delete(delete);
            }

            var hdifffiles = Path.Combine(installPath, "hdifffiles.txt");
            if (File.Exists(hdifffiles))
            {
                var hpatch = Path.Combine(AppContext.BaseDirectory, "hpatchz.exe");
                var lines = await File.ReadAllLinesAsync(hdifffiles).ConfigureAwait(false);
                foreach (var line in lines)
                {
                    var json = JsonNode.Parse(line);
                    var name = json?["remoteName"]?.ToString();
                    var target = Path.Join(installPath, name);
                    var diff = $"{target}.hdiff";
                    if (File.Exists(target) && File.Exists(diff))
                    {
                        File.SetAttributes(target, FileAttributes.Archive);
                        using var process = Process.Start(new ProcessStartInfo
                        {
                            FileName = hpatch,
                            Arguments = $"""-f "{target}" "{diff}" "{target}"  """,
                            CreateNoWindow = true,
                        });
                        if (process != null)
                        {
                            await process.WaitForExitAsync().ConfigureAwait(false);
                        }
                        if (File.Exists(diff))
                        {
                            File.Delete(diff);
                        }
                    }
                }
                File.Delete(hdifffiles);
            }
        }
    }


    private async Task ClearDeprecatedFilesAsync()
    {
        _logger.LogInformation("Clear deprecated files.");
        await Task.Run(() =>
        {
            foreach (var item in launcherResource.DeprecatedFiles)
            {
                var file = Path.Combine(installPath, item.Name);
                if (File.Exists(file))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
            }
            foreach (var item in launcherResource.DeprecatedPackages)
            {
                var file = Path.Combine(installPath, item.Name);
                if (File.Exists(file))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
            }
            foreach (var file in Directory.GetFiles(installPath, "*.slice.*", SearchOption.AllDirectories))
            {
                _logger.LogInformation("Delete file: {file}", file);
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            foreach (var file in Directory.GetFiles(installPath, "*_tmp", SearchOption.AllDirectories))
            {
                _logger.LogInformation("Delete file: {file}", file);
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            foreach (var file in Directory.GetFiles(installPath, "*.hidff", SearchOption.AllDirectories))
            {
                _logger.LogInformation("Delete file: {file}", file);
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
        });
    }




    #endregion




    private class DownloadTask
    {

        public string FileName { get; set; }


        public string Url { get; set; }


        public long Size { get; set; }


        public long DownloadSize { get; set; }


        public required string MD5 { get; set; }

        /// <summary>
        /// 左闭右开
        /// </summary>
        public (long Start, long End) Range { get; set; }


        public bool IsSegment { get; set; }

    }





    public enum DownloadGameState
    {

        None,

        Preparing,

        Prepared,

        Downloading,

        Downloaded,

        Verifying,

        Verified,

        Decompressing,

        Decompressed,

        Finish,

        Error,

    }





    private class SliceStream : Stream
    {

        private const long GB = 1 << 30;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        private readonly long _length;
        public override long Length => _length;

        public override long Position
        {
            get
            {
                if (disposedValue)
                {
                    throw new ObjectDisposedException("Cannot access a closed file.");
                }
                return _streamIndex * GB + _currentStream.Position;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(Position));
                }
                Seek(value, SeekOrigin.Begin);
            }
        }



        private readonly IList<FileStream> _fileStreams;

        private FileStream _currentStream;
        private int _streamIndex;


        public SliceStream(IEnumerable<string> files)
        {
            _fileStreams = files.Select(File.OpenRead).ToList();
            _length = _fileStreams.Sum(x => x.Length);
            _currentStream = _fileStreams.First();
        }



        public override void Flush()
        {
            _currentStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int length = _currentStream.Read(buffer, offset, count);
            if (length == 0)
            {
                if (_streamIndex < _fileStreams.Count - 1)
                {
                    _streamIndex++;
                    _currentStream = _fileStreams[_streamIndex];
                    _currentStream.Position = 0;
                    length = _currentStream.Read(buffer, offset, count);
                }
            }
            return length;
        }


        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin is SeekOrigin.Current)
            {
                offset += Position;
            }
            if (origin is SeekOrigin.End)
            {
                offset += Length;
            }
            int index = (int)Math.Clamp(offset / GB, 0, _fileStreams.Count - 1);
            long position = offset - (index * GB);
            _streamIndex = index;
            _currentStream = _fileStreams[index];
            _currentStream.Position = position;
            return offset;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }



        private bool disposedValue;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }

                foreach (var fs in _fileStreams)
                {
                    fs.Dispose();
                }

                disposedValue = true;
            }
        }

        ~SliceStream()
        {
            Dispose(disposing: false);
        }


        public override async ValueTask DisposeAsync()
        {
            foreach (var fs in _fileStreams)
            {
                await fs.DisposeAsync();
            }
        }



    }




}
