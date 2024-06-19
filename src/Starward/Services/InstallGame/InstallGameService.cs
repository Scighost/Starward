using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Launcher;
using Starward.SevenZip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services.InstallGame;

internal abstract class InstallGameService
{


    protected readonly ILogger<InstallGameService> _logger;


    protected readonly GameResourceService _gameResourceService;


    protected readonly LauncherClient _launcherClient;


    protected readonly HttpClient _httpClient;




    protected InstallGameService(ILogger<InstallGameService> logger, GameResourceService gameResourceService, LauncherClient launcherClient, HttpClient httpClient)
    {
        _logger = logger;
        _gameResourceService = gameResourceService;
        _launcherClient = launcherClient;
        _httpClient = httpClient;
    }





    public abstract GameBiz CurrentGame { get; }



    public GameBiz CurrentGameBiz { get; protected set; }

    public string InstallPath { get; protected set; }

    public VoiceLanguage VoiceLanguages { get; protected set; }

    public bool IsRepairMode { get; protected set; }

    public bool IsPreInstall { get; protected set; }

    public bool IsReInstall { get; protected set; }



    public long TotalBytes { get; protected set; }

    protected long progressBytes;
    public long ProgressBytes => progressBytes;


    public int TotalCount { get; protected set; }

    protected int progressCount;
    public int ProgressCount => progressCount;

    protected InstallGameState _inState;
    protected InstallGameState _state;
    public InstallGameState State
    {
        get => _state;
        set
        {
            if (value is not InstallGameState.None)
            {
                _inState = value;
            }
            _state = value;
            InvokeStateOrProgressChanged(true);
        }
    }

    public bool CanCancel { get; protected set; }


    public event EventHandler<StateEventArgs> StateChanged;



    protected bool initialized;


    protected string separateUrlPrefix;


    protected List<DownloadFileTask> separateResources;


    protected List<DownloadFileTask> downloadTasks;


    protected GamePackagesWrapper launcherGameResource;


    protected GameSDK? gameSDK;


    protected CancellationTokenSource cancellationTokenSource;





    public void Cancel()
    {
        if (CanCancel)
        {
            cancellationTokenSource?.Cancel();
            CanCancel = false;
        }
    }



    protected void InvokeStateOrProgressChanged(bool stateChanged = false, Exception? ex = null)
    {
        if (ex is not null)
        {
            _state = InstallGameState.Error;
        }
        StateChanged?.Invoke(this, new StateEventArgs
        {
            State = State,
            StateChanged = stateChanged,
            TotalBytes = TotalBytes,
            ProgressBytes = ProgressBytes,
            TotalCount = TotalCount,
            ProgressCount = ProgressCount,
            Exception = ex
        });
    }


    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <param name="installPath"></param>
    /// <param name="language"></param>
    /// <param name="repair"></param>
    /// <param name="reinstall"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public void Initialize(GameBiz gameBiz, string installPath, VoiceLanguage language, bool repair, bool reinstall)
    {
        if (gameBiz.ToGame() != CurrentGame)
        {
            throw new ArgumentException($"{gameBiz} is not a game region of {CurrentGame}.", nameof(gameBiz));
        }
        if (!Directory.Exists(installPath))
        {
            throw new DirectoryNotFoundException(installPath);
        }
        CurrentGameBiz = gameBiz;
        InstallPath = installPath;
        VoiceLanguages = language;
        IsRepairMode = repair;
        if (!repair)
        {
            IsReInstall = reinstall;
        }
        initialized = true;
        _logger.LogInformation("""
            Initialize install game service
            biz: {biz}
            path: {path}
            lang: {lang}
            repair: {repair}
            reinstall: {reinstall}
            """, CurrentGameBiz, InstallPath, VoiceLanguages, IsRepairMode, IsReInstall);
    }


    /// <summary>
    /// 开始下载
    /// </summary>
    /// <param name="skipVerify"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public virtual async Task StartAsync(bool skipVerify = false)
    {
        if (!initialized)
        {
            throw new Exception("Not initialized");
        }
        try
        {
            _logger.LogInformation("Start install game, skipVerify: {skip}", skipVerify);
            CanCancel = false;
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            SetAllFileWriteable();

            if (_inState != InstallGameState.Download || skipVerify)
            {
                if (IsRepairMode)
                {
                    State = InstallGameState.Prepare;
                    await PrepareForRepairAsync().ConfigureAwait(false);

                    State = InstallGameState.Verify;
                    await VerifySeparateFilesAsync(CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    State = InstallGameState.Prepare;
                    await PrepareForDownloadAsync().ConfigureAwait(false);
                }
                await PrepareBilibiliServerGameSDKAsync();
            }

            if (_inState is InstallGameState.Download)
            {
                await UpdateDownloadTaskAsync();
            }

            CanCancel = true;
            State = InstallGameState.Download;
            await DownloadAsync(cancellationTokenSource.Token, IsRepairMode).ConfigureAwait(false);
            CanCancel = false;

            if (skipVerify)
            {
                await SkipVerifyDownloadedFilesAsync().ConfigureAwait(false);
            }
            else
            {
                State = InstallGameState.Verify;
                await VerifyDownloadedFilesAsnyc(cancellationTokenSource.Token).ConfigureAwait(false);
            }

            if (!(IsPreInstall || IsRepairMode))
            {
                State = InstallGameState.Decompress;
                await DecompressAndApplyDiffPackagesAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            }

            DecompressBilibiliServerGameSDK();

            await ClearDeprecatedFilesAsync().ConfigureAwait(false);

            if (!IsPreInstall)
            {
                await WriteConfigFileAsync().ConfigureAwait(false);
            }

            State = InstallGameState.Finish;
            _logger.LogInformation("Install game finished.");
        }
        catch (TaskCanceledException)
        {
            State = InstallGameState.None;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Install game error.");
            CanCancel = false;
            InvokeStateOrProgressChanged(true, ex);
        }
    }


    /// <summary>
    /// 使所有文件可写
    /// </summary>
    protected void SetAllFileWriteable()
    {
        var files = Directory.GetFiles(InstallPath, "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
    }



    /// <summary>
    /// 准备下载
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    protected async Task PrepareForDownloadAsync()
    {
        _logger.LogInformation("Prepare for download.");

        (var localVersion, _) = await _gameResourceService.GetLocalGameVersionAndBizAsync(CurrentGameBiz, InstallPath).ConfigureAwait(false);
        launcherGameResource = await _gameResourceService.GetGameResourceAsync(CurrentGameBiz).ConfigureAwait(false);
        (Version? latestVersion, Version? preDownloadVersion) = await _gameResourceService.GetGameResourceVersionAsync(CurrentGameBiz).ConfigureAwait(false);
        GameBranch? gameResource = null;
        if (localVersion is null || IsReInstall)
        {
            _logger.LogInformation("Install full game.");
            gameResource = launcherGameResource.Main;
        }
        else if (preDownloadVersion != null)
        {
            _logger.LogInformation("Pre install game.");
            IsPreInstall = true;
            gameResource = launcherGameResource.PreDownload;
        }
        else if (latestVersion > localVersion)
        {
            _logger.LogInformation("Update game.");
            gameResource = launcherGameResource.Main;
        }
        if (gameResource is null)
        {
            _logger.LogWarning("Game resource is null.");
            throw new Exception(Lang.DownloadGameService_AlreadyTheLatestVersion);
        }

        var list_package = new List<DownloadFileTask>();

        if (gameResource.Patches?.FirstOrDefault(x => x.Version == localVersion?.ToString()) is GamePackages diff)
        {
            // 有差分包
            // TODO: 未考虑GamePkgs多包体
            list_package.Add(new DownloadFileTask
            {
                FileName = Path.GetFileName(diff.GamePkgs.First().Url),
                Url = diff.GamePkgs.First().Url,
                Size = diff.GamePkgs.First().Size,
                MD5 = diff.GamePkgs.First().Md5,
            });
            VoiceLanguages = await _gameResourceService.GetVoiceLanguageAsync(CurrentGameBiz, InstallPath).ConfigureAwait(false);
            foreach (var lang in Enum.GetValues<VoiceLanguage>())
            {
                if (VoiceLanguages.HasFlag(lang))
                {
                    if (diff.AudioPkgs.FirstOrDefault(x => x.Language == lang.ToDescription()) is AudioPkg pack)
                    {
                        list_package.Add(new DownloadFileTask
                        {
                            FileName = Path.GetFileName(pack.Url),
                            Url = pack.Url,
                            Size = pack.Size,
                            MD5 = pack.Md5
                        });
                    }
                }
            }
        }
        else
        {
            // 无差分包
            if (gameResource.Major.GamePkgs.Count > 1)
            {
                // 本体分卷下载
                list_package.AddRange(gameResource.Major.GamePkgs.Select(x => new DownloadFileTask
                {
                    FileName = Path.GetFileName(x.Url),
                    Url = x.Url,
                    MD5 = x.Md5,
                    IsSegment = true
                }));
            }
            else
            {
                list_package.AddRange(gameResource.Major.GamePkgs.Select(x => new DownloadFileTask
                {
                    FileName = Path.GetFileName(x.Url),
                    Url = x.Url,
                    Size = x.Size,
                    MD5 = x.Md5,
                }));
            }
            foreach (var lang in Enum.GetValues<VoiceLanguage>())
            {
                if (VoiceLanguages.HasFlag(lang))
                {
                    if (gameResource.Major.AudioPkgs.FirstOrDefault(x => x.Language == lang.ToDescription()) is AudioPkg pack)
                    {
                        list_package.Add(new DownloadFileTask
                        {
                            FileName = Path.GetFileName(pack.Url),
                            Url = pack.Url,
                            Size = pack.Size,
                            MD5 = pack.Md5
                        });
                    }
                }
            }
        }

        await _gameResourceService.SetVoiceLanguageAsync(CurrentGameBiz, InstallPath, VoiceLanguages).ConfigureAwait(false);

        // 包大小
        await Parallel.ForEachAsync(list_package, async (task, _) =>
        {
            var len = await GetContentLengthAsync(task.Url);
            if (len.HasValue)
            {
                task.Size = len.Value;
            }
        }).ConfigureAwait(false);

        // 已下载大小
        foreach (var task in list_package)
        {
            string file = Path.Combine(InstallPath, task.FileName);
            string file_tmp = file + "_tmp";
            if (File.Exists(file))
            {
                task.DownloadSize = new FileInfo(file).Length;
            }
            else if (File.Exists(file_tmp))
            {
                task.DownloadSize = new FileInfo(file_tmp).Length;
            }
        }

        TotalBytes = list_package.Sum(x => x.Size);
        progressBytes = list_package.Sum(x => x.DownloadSize);
        downloadTasks = list_package;
    }


    /// <summary>
    /// 准备修复模式
    /// </summary>
    /// <returns></returns>
    protected virtual async Task PrepareForRepairAsync()
    {
        _logger.LogInformation("Repair mode, prepare for repair.");
        launcherGameResource = await _gameResourceService.GetGameResourceAsync(CurrentGameBiz).ConfigureAwait(false);
        GameBranch gameResource = launcherGameResource.Main;
        separateUrlPrefix = gameResource.Major.ResListUrl.TrimEnd('/');
        separateResources = await GetPkgVersionsAsync($"{separateUrlPrefix}/pkg_version").ConfigureAwait(false);
    }


    /// <summary>
    /// 修复模式使用，校验资源文件，失败则删除文件，加入到下载列表
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected virtual async Task VerifySeparateFilesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Repair mode, verify files.");

        if (separateResources is null)
        {
            throw new InvalidOperationException("Please call PrepareForDownloadAsync() first.");
        }

        TotalCount = separateResources.Count;
        progressCount = 0;

        var list = new List<DownloadFileTask>();

        await Parallel.ForEachAsync(separateResources, cancellationToken, async (item, token) =>
        {
            byte[] buffer = new byte[1 << 18];
            var file = Path.Combine(InstallPath, item.FileName);
            bool success = await VerifyFileAsync(item, token).ConfigureAwait(false);
            Interlocked.Increment(ref progressCount);
            if (!success)
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
        }).ConfigureAwait(false);

        downloadTasks = list;
    }


    /// <summary>
    /// B服SDK
    /// </summary>
    protected async Task PrepareBilibiliServerGameSDKAsync()
    {
        if (!IsPreInstall && CurrentGameBiz.IsBilibiliServer())
        {
            gameSDK = await _gameResourceService.GetGameSdkAsync(CurrentGameBiz).ConfigureAwait(false);
            if (gameSDK is not null)
            {
                _logger.LogInformation("Bilibili sdk version: {version}", gameSDK.Version);
                downloadTasks.Add(new DownloadFileTask
                {
                    FileName = Path.GetFileName(gameSDK.Pkg.Url),
                    Url = gameSDK.Pkg.Url,
                    Size = gameSDK.Pkg.Size,
                    MD5 = gameSDK.Pkg.Url,
                });
            }
        }
    }


    /// <summary>
    /// 解压B服SDK
    /// </summary>
    protected void DecompressBilibiliServerGameSDK()
    {
        if (!IsPreInstall && CurrentGameBiz.IsBilibiliServer())
        {
            if (gameSDK is not null)
            {
                string file = Path.Combine(InstallPath, Path.GetFileName(gameSDK.Pkg.Url));
                if (File.Exists(file))
                {
                    _logger.LogInformation("Decompress Bilibili sdk: {file}", file);
                    ZipFile.ExtractToDirectory(file, InstallPath, true);
                    File.Delete(file);
                }
            }
        }
    }


    /// <summary>
    /// 更新已下载大小
    /// </summary>
    protected async Task UpdateDownloadTaskAsync()
    {
        await Task.Run(() =>
        {
            foreach (var task in downloadTasks)
            {
                string file = Path.Combine(InstallPath, task.FileName);
                string file_tmp = file + "_tmp";
                if (File.Exists(file))
                {
                    task.DownloadSize = new FileInfo(file).Length;
                }
                else if (File.Exists(file_tmp))
                {
                    task.DownloadSize = new FileInfo(file_tmp).Length;
                }
            }
        });
    }


    /// <summary>
    /// 下载文件到临时文件 *_tmp
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task DownloadAsync(CancellationToken cancellationToken, bool noTmp = false)
    {
        _logger.LogInformation("Start download files.");

        TotalCount = downloadTasks.Count;
        TotalBytes = downloadTasks.Sum(x => x.Size);
        progressCount = 0;
        progressBytes = downloadTasks.Sum(x => x.DownloadSize);
        _logger.LogInformation("{count} files need to download.", downloadTasks.Count);
        await Parallel.ForEachAsync(downloadTasks, new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
        }, async (task, token) =>
        {
            await DownloadFileAsync(task, token, noTmp).ConfigureAwait(false);
            Interlocked.Increment(ref progressCount);
        }).ConfigureAwait(false);
    }


    /// <summary>
    /// 删除过期文件
    /// </summary>
    /// <returns></returns>
    protected async Task ClearDeprecatedFilesAsync()
    {
        _logger.LogInformation("Clear deprecated files.");
        var launcherGameDeprecatedFiles = await _launcherClient.GetLauncherGameDeprecatedFilesAsync(CurrentGameBiz);

        await Task.Run(() =>
        {
            if (launcherGameDeprecatedFiles != null)
            {
                foreach (var item in launcherGameDeprecatedFiles.DeprecatedFiles)
                {
                    var file = Path.Combine(InstallPath, item.Name);
                    if (File.Exists(file))
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                }
            }
            /*foreach (var item in launcherGameResource.DeprecatedPackages)
            {
                var file = Path.Combine(InstallPath, item.Name);
                if (File.Exists(file))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
            }*/
            foreach (var file in Directory.GetFiles(InstallPath, "*_tmp", SearchOption.AllDirectories))
            {
                _logger.LogInformation("Delete temp file: {file}", file);
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            foreach (var file in Directory.GetFiles(InstallPath, "*.hidff", SearchOption.AllDirectories))
            {
                _logger.LogInformation("Delete hdiff file: {file}", file);
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
        }).ConfigureAwait(false);
    }


    /// <summary>
    /// 解压资源包，合并差分文件
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    protected async Task DecompressAndApplyDiffPackagesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Decompress downloaded files.");

        TotalCount = downloadTasks.Count;
        TotalBytes = downloadTasks.Sum(x => x.Size);
        progressCount = 0;
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

        foreach (var task in downloadTasks)
        {
            if (task.IsSegment)
            {
                var files = Directory.GetFiles(InstallPath, $"{Path.GetFileNameWithoutExtension(task.FileName)}.*");
                if (files.Length > 0)
                {
                    _logger.LogInformation("Decompress file: {files}", string.Join(Environment.NewLine, files.Select(Path.GetFileName)));
                    await DecompressFileAsync(files).ConfigureAwait(false);
                }
            }
            else
            {
                _logger.LogInformation("Decompress {file}", task.FileName);
                var file = Path.Combine(InstallPath, task.FileName);
                if (File.Exists(file))
                {
                    await DecompressFileAsync(file).ConfigureAwait(false);
                }
            }
            await ApplyDiffPackageAsync().ConfigureAwait(false);
        }
    }


    /// <summary>
    /// 校验已下载的临时文件 *_tmp，成功则删除临时文件后缀 _tmp
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="CheckSumFailedException"></exception>
    protected async Task VerifyDownloadedFilesAsnyc(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Verify downloaded files.");

        TotalCount = downloadTasks.Count;
        TotalBytes = downloadTasks.Sum(x => x.Size);
        progressCount = 0;
        progressBytes = 0;
        var list = new List<string>();
        await Parallel.ForEachAsync(downloadTasks, cancellationToken, async (task, token) =>
        {
            bool success = await VerifyFileAsync(task, token).ConfigureAwait(false);
            Interlocked.Increment(ref progressCount);
            if (!success)
            {
                _logger.LogInformation("Verify failed: {file}", task.FileName);
                lock (list)
                {
                    list.Add(task.FileName);
                }
            }
        }).ConfigureAwait(false);
        if (list.Count > 0)
        {
            throw new CheckSumFailedException("Verify files failed.", list);
        }
    }


    /// <summary>
    /// 跳过校验下载的文件，删除临时文件后缀 _tmp
    /// </summary>
    /// <returns></returns>
    protected async Task SkipVerifyDownloadedFilesAsync()
    {
        _logger.LogInformation("Skip verify downloaded files.");

        await Task.Run(() =>
        {
            foreach (var task in downloadTasks)
            {
                string file = Path.Combine(InstallPath, task.FileName);
                string file_tmp = file + "_tmp";
                _logger.LogInformation("Skip verify: {file}", task.FileName);
                if (File.Exists(file))
                {
                    return;
                }
                else if (File.Exists(file_tmp))
                {
                    File.Move(file_tmp, file, true);
                }
            }
        }).ConfigureAwait(false);
    }


    /// <summary>
    /// 删除下载的文件或临时文件 *_tmp
    /// </summary>
    /// <param name="files"></param>
    /// <returns></returns>
    public async Task DeleteDownloadedFilesAsync(IEnumerable<string> files)
    {
        await Task.Run(() =>
        {
            foreach (var item in files)
            {
                string file = Path.Combine(InstallPath, item);
                string file_tmp = file + "_tmp";
                if (File.Exists(file))
                {
                    _logger.LogInformation("Delete downloaded file: {file}", file);
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                if (File.Exists(file_tmp))
                {
                    _logger.LogInformation("Delete downloaded file: {file}", file_tmp);
                    File.SetAttributes(file_tmp, FileAttributes.Normal);
                    File.Delete(file_tmp);
                }
            }
        }).ConfigureAwait(false);
    }


    /// <summary>
    /// 写入 config.ini
    /// </summary>
    /// <returns></returns>
    protected async Task WriteConfigFileAsync()
    {
        string version = launcherGameResource.Main.Major.Version;
        string sdk_version = gameSDK?.Version ?? "";
        string cps = "", channel = "1", sub_channel = "1";
        if (CurrentGameBiz.IsBilibiliServer())
        {
            cps = "bilibili";
            channel = "14";
            sub_channel = "0";
        }
        else if (CurrentGameBiz.IsChinaServer())
        {
            cps = "mihoyo";
        }
        else if (CurrentGameBiz.IsGlobalServer())
        {
            cps = "hoyoverse";
        }
        string config = $"""
            [General]
            channel={channel}
            cps={cps}
            game_version={version}
            sub_channel={sub_channel}
            sdk_version={sdk_version}
            game_biz={CurrentGameBiz}
            """;
        _logger.LogInformation("Write config.ini (game_version={version})", version);
        await File.WriteAllTextAsync(Path.Combine(InstallPath, "config.ini"), config).ConfigureAwait(false);
    }



    #region Common Method


    /// <summary>
    /// 下载文件到临时文件 *_tmp
    /// </summary>
    /// <param name="task"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task DownloadFileAsync(DownloadFileTask task, CancellationToken cancellationToken, bool noTmp = false)
    {
        const int BUFFER_SIZE = 1 << 16;
        string file = Path.Combine(InstallPath, task.FileName);
        string file_tmp = file + "_tmp";
        string target_file;
        if (File.Exists(file) || noTmp)
        {
            target_file = file;
        }
        else
        {
            target_file = file_tmp;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(target_file)!);
        using var fs = File.Open(target_file, FileMode.OpenOrCreate);
        if (fs.Length < task.Size)
        {
            if (string.IsNullOrWhiteSpace(task.Url))
            {
                task.Url = $"{separateUrlPrefix}/{task.FileName.TrimStart('/')}";
            }
            _logger.LogInformation("Download: FileName {name}, Url {url}", task.FileName, task.Url);
            fs.Position = fs.Length;
            var request = new HttpRequestMessage(HttpMethod.Get, task.Url) { Version = HttpVersion.Version11 };
            request.Headers.Range = new RangeHeaderValue(fs.Length, null);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var hs = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var buffer = new byte[BUFFER_SIZE];
            int length;
            while ((length = await hs.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
                Interlocked.Add(ref progressBytes, length);
            }
            _logger.LogInformation("Download Successfully: FileName {name}", task.FileName);
        }
    }


    /// <summary>
    /// 校验文件，成功则删除临时文件后缀 _tmp
    /// </summary>
    /// <param name="task"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task<bool> VerifyFileAsync(DownloadFileTask task, CancellationToken cancellationToken)
    {
        const int BUFFER_SIZE = 1 << 20;
        string file = Path.Combine(InstallPath, task.FileName);
        string file_tmp = file + "_tmp";
        string target_file;
        bool is_tmp = false;
        if (File.Exists(file))
        {
            target_file = file;
        }
        else if (File.Exists(file_tmp))
        {
            target_file = file_tmp;
            is_tmp = true;
        }
        else
        {
            return false;
        }

        using var fs = File.OpenRead(target_file);
        if (fs.Length != task.Size)
        {
            return false;
        }
        int length = 0;
        var hashProvider = MD5.Create();
        var buffer = new byte[BUFFER_SIZE];
        while ((length = await fs.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
        {
            hashProvider.TransformBlock(buffer, 0, length, buffer, 0);
            Interlocked.Add(ref progressBytes, length);
        }
        hashProvider.TransformFinalBlock(buffer, 0, length);
        var hash = hashProvider.Hash;
        fs.Dispose();
        if (string.Equals(Convert.ToHexString(hash!), task.MD5, StringComparison.OrdinalIgnoreCase))
        {
            if (is_tmp)
            {
                File.Move(file_tmp, file, true);
            }
            return true;
        }
        else
        {
            return false;
        }
    }


    /// <summary>
    /// 解压压缩包或分卷压缩包
    /// </summary>
    /// <param name="files"></param>
    /// <returns></returns>
    protected async Task DecompressFileAsync(params string[] files)
    {
        using var fs = new FileSliceStream(files);
        if (files[0].Contains(".7z", StringComparison.CurrentCultureIgnoreCase))
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
                extra.Extract(InstallPath, true);
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
                        var target = Path.Combine(InstallPath, item.FullName);
                        Directory.CreateDirectory(target);
                    }
                    else
                    {
                        var target = Path.Combine(InstallPath, item.FullName);
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
        foreach (var file in files)
        {
            File.Delete(file);
        }
    }


    /// <summary>
    /// 根据 pkg_version 获取获取资源文件列表
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    protected async Task<List<DownloadFileTask>> GetPkgVersionsAsync(string url)
    {
        var list = new List<DownloadFileTask>();
        var str = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
        var lines = str.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            var node = JsonNode.Parse(line.Trim());
            list.Add(new DownloadFileTask
            {
                FileName = node?["remoteName"]?.ToString()!,
                MD5 = node?["md5"]?.ToString()!,
                Size = (long)(node?["fileSize"] ?? 0),
            });
        }
        return list;
    }


    /// <summary>
    /// 合并差分文件 deletefiles/hdifffiles
    /// </summary>
    /// <returns></returns>
    protected async Task ApplyDiffPackageAsync()
    {
        var delete = Path.Combine(InstallPath, "deletefiles.txt");
        if (File.Exists(delete))
        {
            var deleteFiles = await File.ReadAllLinesAsync(delete).ConfigureAwait(false);
            foreach (var file in deleteFiles)
            {
                var target = Path.Combine(InstallPath, file);
                if (File.Exists(target))
                {
                    File.Delete(target);
                }
            }
            File.Delete(delete);
        }

        var hdifffiles = Path.Combine(InstallPath, "hdifffiles.txt");
        if (File.Exists(hdifffiles))
        {
            int tmp_TotalCount = TotalCount;
            int tmp_ProgressCount = ProgressCount;

            var hpatch = Path.Combine(AppContext.BaseDirectory, "hpatchz.exe");
            var lines = await File.ReadAllLinesAsync(hdifffiles).ConfigureAwait(false);
            TotalCount = lines.Length;
            progressCount = 0;
            State = InstallGameState.Merge;
            foreach (var line in lines)
            {
                var json = JsonNode.Parse(line);
                var name = json?["remoteName"]?.ToString();
                var target = Path.Join(InstallPath, name);
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
                progressCount++;
            }
            File.Delete(hdifffiles);
            TotalCount = tmp_TotalCount;
            progressCount = tmp_ProgressCount;
            State = InstallGameState.Decompress;
        }
    }



    protected async Task<long?> GetContentLengthAsync(string url)
    {
        _logger.LogInformation("Request head: {url}", url);
        var request = new HttpRequestMessage(HttpMethod.Head, url);
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return response.Content.Headers.ContentLength;
    }




    #endregion



}


