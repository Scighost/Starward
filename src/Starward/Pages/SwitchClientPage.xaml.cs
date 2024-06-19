using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Starward.Core;
using Starward.Core.Launcher;
using Starward.Messages;
using Starward.Services;
using Starward.Services.InstallGame;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class SwitchClientPage : PageBase
{


    private readonly ILogger<SwitchClientPage> _logger = AppConfig.GetLogger<SwitchClientPage>();


    private readonly GameResourceService _gameResourceService = AppConfig.GetService<GameResourceService>();


    private readonly GameService _gameService = AppConfig.GetService<GameService>();


    private readonly HttpClient _httpClient = AppConfig.GetService<HttpClient>();


    private readonly DispatcherQueueTimer _timer;


    public SwitchClientPage()
    {
        this.InitializeComponent();
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(50);
        _timer.Tick += _timer_Tick;
    }



    private string gameFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Starward\game");


    private string installPath;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FromGameBizName))]
    private GameBiz fromGameBiz;


    public string FromGameBizName => $"{FromGameBiz.ToGameName()} - {FromGameBiz.ToGameServer()}";


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToGameBizName))]
    private GameBiz toGameBiz;

    public string ToGameBizName => $"{ToGameBiz.ToGameName()} - {ToGameBiz.ToGameServer()}";


    [ObservableProperty]
    private Version? gameVersion;

    /// <summary>
    /// ������Ϸ�汾�������µ�
    /// </summary>
    [ObservableProperty]
    private bool isLocalGameVersionNotLatest;


    private GamePackagesWrapper fromGameResource;

    private GamePackagesWrapper toGameResource;

    private GameSDK? toGameSdk;

    private string toGameResourcePrefix;

    [ObservableProperty]
    private List<TargetGameBiz> targetGameBizs;

    [ObservableProperty]
    private TargetGameBiz? selectedTargetGameBiz;

    /// <summary>
    /// ������Ϸ�汾��Ŀ����Ϸ�汾��ͬ
    /// </summary>
    [ObservableProperty]
    private bool isTowGameBizVersionDifferent;


    private List<DownloadFileTask> fromPkgVersions;

    private List<DownloadFileTask> toPkgVersions;

    private List<DownloadFileTask> removeFiles;

    private List<DownloadFileTask> addFiles;


    [ObservableProperty]
    private bool canCancel = true;

    [ObservableProperty]
    private string? stateText;

    [ObservableProperty]
    private string? errorText;

    [ObservableProperty]
    private string? progressBytesText;

    [ObservableProperty]
    private string? speedText;

    [ObservableProperty]
    private string? remainTimeText;


    private CancellationTokenSource tokenSource;

    private long totalBytes;

    private long downloadedBytes;

    private long lastDownloadedBytes;

    private long lastTimeTicks;

    [ObservableProperty]
    private bool isPrepared;

    private bool isCompleted;



    protected override async void OnLoaded()
    {
        await InitializeAsync();
    }


    protected override void OnUnloaded()
    {
        _timer.Stop();
        _timer.Tick -= _timer_Tick;
    }


    private void _timer_Tick(DispatcherQueueTimer sender, object args)
    {
        const double MB = 1 << 20;
        long thisDownloadBytes = downloadedBytes;
        long thisTimeTicks = Stopwatch.GetTimestamp();
        ProgressBytesText = $"{thisDownloadBytes / MB:F2}/{totalBytes / MB:F2} MB";
        if (thisTimeTicks - lastTimeTicks >= Stopwatch.Frequency)
        {
            double speed = (thisDownloadBytes - lastDownloadedBytes) / Stopwatch.GetElapsedTime(lastTimeTicks, thisTimeTicks).TotalSeconds;
            if (speed >= 0)
            {
                SpeedText = $"{speed / MB:F2} MB/s";
                if (speed == 0)
                {
                    RemainTimeText = "-";
                }
                else
                {
                    var remainTime = TimeSpan.FromSeconds((totalBytes - thisDownloadBytes) / speed);
                    RemainTimeText = $"{remainTime.Days * 24 + remainTime.Hours}h {remainTime.Minutes}m {remainTime.Seconds}s";
                }
            }
            lastDownloadedBytes = thisDownloadBytes;
            lastTimeTicks = thisTimeTicks;
        }
    }



    private void StarTimer()
    {
        lastDownloadedBytes = downloadedBytes;
        lastTimeTicks = Stopwatch.GetTimestamp();
        _timer.Start();
    }



    private void StopTimer()
    {
        _timer.Stop();
        ProgressBytesText = null;
        SpeedText = null;
        RemainTimeText = null;
    }




    private async Task InitializeAsync()
    {
        try
        {
            StateText = null;
            ErrorText = null;
            installPath = _gameResourceService.GetGameInstallPath(CurrentGameBiz)!;
            (GameVersion, var biz) = await _gameResourceService.GetLocalGameVersionAndBizAsync(CurrentGameBiz);
            if (biz.ToGame() == CurrentGameBiz.ToGame())
            {
                FromGameBiz = biz;
            }
            else
            {
                FromGameBiz = CurrentGameBiz;
            }
            fromGameResource = await _gameResourceService.GetGameResourceAsync(FromGameBiz);
            if (GameVersion?.ToString() != fromGameResource.Main.Major.Version)
            {
                IsLocalGameVersionNotLatest = true;
                Button_Prepair.IsEnabled = false;
                ErrorText = Lang.SwitchClientPage_TheLocalGameVersionIsNotTheLatest;
                return;
            }
            if (CurrentGameBiz.ToGame() is GameBiz.GenshinImpact)
            {
                string cn_data = Path.Join(installPath, "YuanShen_Data");
                string os_data = Path.Join(installPath, "GenshinImpact_Data");
                if (Directory.Exists(cn_data) && Directory.Exists(os_data))
                {
                    // �����͹��ʷ������ļ���ͬʱ����
                    IsLocalGameVersionNotLatest = true;
                    Button_Prepair.IsEnabled = false;
                    ErrorText = Lang.SwitchClientPage_TheTowFoldersExistAtTheSameTime;
                    return;
                }
            }
            UpdateTargetGameBizs();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize switch client");
            StateText = Lang.DownloadGamePage_SomethingError;
        }
    }




    private void UpdateTargetGameBizs()
    {
        SelectedTargetGameBiz = null;
        List<TargetGameBiz> list = FromGameBiz.ToGame() switch
        {
            GameBiz.Honkai3rd =>
            [
                new TargetGameBiz { GameBiz = GameBiz.bh3_cn },
                new TargetGameBiz { GameBiz = GameBiz.bh3_global },
                new TargetGameBiz { GameBiz = GameBiz.bh3_jp },
                new TargetGameBiz { GameBiz = GameBiz.bh3_kr },
                new TargetGameBiz { GameBiz = GameBiz.bh3_overseas },
                new TargetGameBiz { GameBiz = GameBiz.bh3_tw },
            ],
            GameBiz.GenshinImpact =>
            [
                new TargetGameBiz { GameBiz = GameBiz.hk4e_cn },
                new TargetGameBiz { GameBiz = GameBiz.hk4e_global },
                new TargetGameBiz { GameBiz = GameBiz.hk4e_bilibili },
            ],
            GameBiz.StarRail =>
            [
                new TargetGameBiz { GameBiz = GameBiz.hkrpg_cn },
                new TargetGameBiz { GameBiz = GameBiz.hkrpg_global },
                new TargetGameBiz { GameBiz = GameBiz.hkrpg_bilibili },
            ],
            _ => [],
        };
        if (list.FirstOrDefault(x => x.GameBiz == FromGameBiz) is TargetGameBiz from)
        {
            list.Remove(from);
        }
        TargetGameBizs = list;
    }




    [RelayCommand]
    private async Task PrepareForSwitchClientAsync()
    {
        try
        {
            IsPrepared = false;
            if (fromGameResource is null)
            {
                await InitializeAsync();
            }
            if (IsLocalGameVersionNotLatest || SelectedTargetGameBiz is null)
            {
                return;
            }

            StateText = Lang.DownloadGamePage_Preparing;
            ErrorText = null;
            ToGameBiz = SelectedTargetGameBiz.GameBiz;
            toGameResource = await _gameResourceService.GetGameResourceAsync(SelectedTargetGameBiz.GameBiz);
            toGameSdk = await _gameResourceService.GetGameSdkAsync(SelectedTargetGameBiz.GameBiz);
            toGameResourcePrefix = toGameResource.Main.Major.ResListUrl.TrimEnd('/');
            if (fromGameResource?.Main.Major.Version != toGameResource.Main.Major.Version)
            {
                StateText = Lang.DownloadGamePage_SomethingError;
                ErrorText = Lang.SwitchClientPage_TheTargetServerVersionIsDifferentFromTheLocalVersion;
                IsTowGameBizVersionDifferent = true;
                return;
            }

            tokenSource?.Cancel();
            tokenSource = new CancellationTokenSource();
            await GetDownloadFilesAsync(tokenSource.Token);
            await MoveExistFilesAsync(tokenSource.Token);
            await DownloadFilesAsync(tokenSource.Token);
            CanCancel = false;
            await VerifyDownloadFilesAsync();
            await WriteConfigFileAsync();

            StateText = Lang.SwitchClientPage_ReadyToSwitchClient;
            IsPrepared = true;
        }
        catch (TaskCanceledException)
        {
            StateText = Lang.GachaLogPage_OperationCanceled;
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Prepare for switch client");
            StateText = Lang.DownloadGamePage_SomethingError;
            ErrorText = ex.Message;
        }
        finally
        {
            CanCancel = true;
            StopTimer();
        }
    }



    private async Task GetDownloadFilesAsync(CancellationToken cancellationToken)
    {
        fromPkgVersions = await GetPkgVersionsAsync($"{fromGameResource.Main.Major.ResListUrl.TrimEnd('/')}/pkg_version", cancellationToken);
        toPkgVersions = await GetPkgVersionsAsync($"{toGameResource.Main.Major.ResListUrl.TrimEnd('/')}/pkg_version", cancellationToken);
        removeFiles = fromPkgVersions.ExceptBy(toPkgVersions.Select(GetUniqueIdentify), GetUniqueIdentify).ToList();
        addFiles = toPkgVersions.ExceptBy(fromPkgVersions.Select(GetUniqueIdentify), GetUniqueIdentify).ToList();

        if (ToGameBiz.ToGame() is GameBiz.Honkai3rd)
        {
            var bh3base_from = await GetContentLengthAndMd5Async($"{fromGameResource.Main.Major.ResListUrl.TrimEnd('/')}/BH3Base.dll", cancellationToken);
            var bh3base_to = await GetContentLengthAndMd5Async($"{toGameResource.Main.Major.ResListUrl.TrimEnd('/')}/BH3Base.dll", cancellationToken);
            removeFiles.Add(new DownloadFileTask
            {
                FileName = "BH3Base.dll",
                MD5 = bh3base_from.Md5,
                Size = bh3base_from.Length ?? 0,
            });
            addFiles.Add(new DownloadFileTask
            {
                FileName = "BH3Base.dll",
                MD5 = bh3base_to.Md5,
                Size = bh3base_to.Length ?? 0,
            });
        }

        if (ToGameBiz.IsBilibiliServer() && toGameSdk is GameSDK sdk)
        {
            addFiles.Add(new DownloadFileTask
            {
                FileName = Path.GetFileName(sdk.Pkg.Url),
                MD5 = sdk.Pkg.Md5,
                Size = sdk.Pkg.Size,
                Url = sdk.Pkg.Url,
                IsSegment = true,
            });
        }
    }



    private static string GetUniqueIdentify(DownloadFileTask task)
    {
        return $"{task.FileName[(task.FileName.IndexOf('/') + 1)..]}{task.MD5}";
    }




    private async Task MoveExistFilesAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(gameFolder);
        await Parallel.ForEachAsync(removeFiles, cancellationToken, async (task, token) =>
        {
            var fromPath = Path.Combine(installPath, task.FileName);
            string toPath = Path.Combine(gameFolder, task.MD5);
            if (File.Exists(toPath) && new FileInfo(toPath).Length == task.Size)
            {
                return;
            }
            if (File.Exists(fromPath))
            {
                using var fs = File.Open(fromPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var md5 = Convert.ToHexString(await MD5.HashDataAsync(fs, token));
                if (string.Equals(md5, task.MD5, StringComparison.OrdinalIgnoreCase))
                {
                    using var fs2 = File.Open(toPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                    if (fs2.Length == fs.Length)
                    {
                        return;
                    }
                    fs.Position = 0;
                    await fs.CopyToAsync(fs2, token);
                }
            }
        });
    }




    private async Task DownloadFilesAsync(CancellationToken cancellationToken)
    {
        const int BUFFER_SIZE = 1 << 16;

        foreach (var item in addFiles)
        {
            item.Url ??= $"{toGameResourcePrefix}/{item.FileName.TrimStart('/')}";
            string path = Path.Combine(gameFolder, item.MD5);
            if (File.Exists(path))
            {
                item.DownloadSize = new FileInfo(path).Length;
            }
        }
        totalBytes = addFiles.Sum(x => x.Size);
        downloadedBytes = addFiles.Sum(x => x.DownloadSize);
        StarTimer();

        await Parallel.ForEachAsync(addFiles, new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
        }, async (task, token) =>
        {
            string path = Path.Combine(gameFolder, task.MD5);
            using var fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            if (fs.Length < task.Size)
            {
                _logger.LogInformation("Download: FileName {name}, Url {url}", task.FileName, task.Url);
                fs.Position = fs.Length;
                var request = new HttpRequestMessage(HttpMethod.Get, task.Url) { Version = HttpVersion.Version11 };
                request.Headers.Range = new RangeHeaderValue(fs.Length, null);
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                using var hs = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                var buffer = new byte[BUFFER_SIZE];
                int length;
                while ((length = await hs.ReadAsync(buffer, token).ConfigureAwait(false)) != 0)
                {
                    await fs.WriteAsync(buffer.AsMemory(0, length), token).ConfigureAwait(false);
                    Interlocked.Add(ref downloadedBytes, length);
                }
                _logger.LogInformation("Download Successfully: FileName {name}", task.FileName);
            }
        });
        StopTimer();
    }



    private async Task VerifyDownloadFilesAsync()
    {
        bool failed = false;
        await Parallel.ForEachAsync(addFiles, async (task, _) =>
        {
            string path = Path.Combine(gameFolder, task.MD5);
            if (File.Exists(path))
            {
                using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var md5 = Convert.ToHexString(await MD5.HashDataAsync(fs));
                if (string.Equals(md5, task.MD5, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                fs.Dispose();
                File.Delete(path);
                _logger.LogWarning("Verify Failed, deleted: FileName {name}", path);
            }
            failed = true;
        });
        if (failed)
        {
            throw new Exception(Lang.SwitchClientPage_FileVerificationFailedPleaseTryAgain);
        }
    }



    private async Task VerifyMovedFilesAsync()
    {
        bool failed = false;
        await Parallel.ForEachAsync(addFiles, async (task, _) =>
        {
            if (task.IsSegment)
            {
                return;
            }
            string path = Path.Combine(installPath, task.FileName);
            if (File.Exists(path))
            {
                using var fs = File.OpenRead(path);
                var md5 = Convert.ToHexString(await MD5.HashDataAsync(fs));
                if (string.Equals(md5, task.MD5, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                _logger.LogWarning("Verify Failed: FileName {name}", path);
            }
            failed = true;
        });
        if (failed)
        {
            throw new Exception(Lang.SwitchClientPage_FileVerificationFailedPleaseTryToRepairGameResources);
        }
    }



    private async Task WriteConfigFileAsync()
    {
        string version = toGameResource.Main.Major.Version;
        string sdk_version = toGameSdk?.Version ?? "";
        string cps = "", channel = "1", sub_channel = "1";
        if (ToGameBiz.IsBilibiliServer())
        {
            cps = "bilibili";
            channel = "14";
            sub_channel = "0";
        }
        else if (ToGameBiz.IsChinaServer())
        {
            cps = "mihoyo";
        }
        else if (ToGameBiz.IsGlobalServer())
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
            game_biz={ToGameBiz}
            """;
        byte[] configBytes = Encoding.UTF8.GetBytes(config);
        string md5 = Convert.ToHexString(MD5.HashData(configBytes)).ToLower();
        await File.WriteAllTextAsync(Path.Combine(gameFolder, md5), config);
        addFiles.Add(new DownloadFileTask
        {
            FileName = "config.ini",
            MD5 = md5,
            Size = configBytes.Length,
            DownloadSize = configBytes.Length,
        });
    }


    [RelayCommand]
    private async Task StartSwitchClientAsync()
    {
        if (isCompleted)
        {
            Close();
            return;
        }
        try
        {
            if (_gameService.GetGameProcess(FromGameBiz) is not null)
            {
                ErrorText = Lang.LauncherPage_GameIsRunning;
                return;
            }
            Button_Prepair.IsEnabled = false;
            CanCancel = false;
            var sb = new StringBuilder();
            sb.AppendLine("$ProgressPreference = 'SilentlyContinue';");
            sb.AppendLine("$ErrorActionPreference = 'Continue';");
            foreach (var item in removeFiles)
            {
                sb.AppendLine($"$null = Remove-Item -Path '{Path.GetFullPath(Path.Combine(installPath, item.FileName))}' -Force;");
            }
            if (ToGameBiz.ToGame() is GameBiz.GenshinImpact)
            {
                string path_cn = Path.Combine(installPath, "YuanShen_Data");
                string path_os = Path.Combine(installPath, "GenshinImpact_Data");
                if (ToGameBiz is GameBiz.hk4e_cn or GameBiz.hk4e_bilibili)
                {
                    if (Directory.Exists(path_os))
                    {
                        sb.AppendLine($"$null = Rename-Item -Path '{path_os}' -NewName 'YuanShen_Data' -Force;");
                    }
                }
                if (ToGameBiz is GameBiz.hk4e_global)
                {
                    if (Directory.Exists(path_cn))
                    {
                        sb.AppendLine($"$null = Rename-Item -Path '{path_cn}' -NewName 'GenshinImpact_Data' -Force;");
                    }
                }
            }
            foreach (var item in addFiles)
            {
                sb.AppendLine($"$null = New-Item -ItemType File -Path '{Path.GetFullPath(Path.Combine(installPath, item.FileName))}' -Force;");
                sb.AppendLine($"$null = Copy-Item -Path '{Path.GetFullPath(Path.Combine(gameFolder, item.MD5))}' -Destination '{Path.GetFullPath(Path.Combine(installPath, item.FileName))}' -Force;");
            }
            if (ToGameBiz.IsBilibiliServer() && toGameSdk is GameSDK sdk)
            {
                string package = Path.Combine(installPath, Path.GetFileName(sdk.Pkg.Url));
                sb.AppendLine($"$null = Expand-Archive -Path '{package}' -DestinationPath '{installPath}' -Force;");
                sb.AppendLine($"$null = Remove-Item -Path '{package}' -Force;");
            }
            else if (!ToGameBiz.IsBilibiliServer())
            {
                string? dll = null;
                if (ToGameBiz is GameBiz.hk4e_cn)
                {
                    dll = Path.Join(installPath, @"YuanShen_Data\Plugins\PCGameSDK.dll");
                }
                else if (ToGameBiz is GameBiz.hk4e_global)
                {
                    dll = Path.Join(installPath, @"GenshinImpact_Data\Plugins\PCGameSDK.dll");
                }
                else if (ToGameBiz.ToGame() is GameBiz.StarRail)
                {
                    dll = Path.Join(installPath, @"StarRail_Data\Plugins\PCGameSDK.dll");
                }
                if (!string.IsNullOrWhiteSpace(dll))
                {
                    sb.AppendLine($$"""
                        if (Test-Path '{{dll}}') {
                            $null = Remove-Item -Path '{{dll}}' -Force;
                        }
                        """);
                }
            }
            _logger.LogInformation("Start switching client.");
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = "PowerShell",
                Arguments = sb.ToString(),
                UseShellExecute = true,
                CreateNoWindow = true,
                Verb = "runas",
            });
            if (p != null)
            {
                await p.WaitForExitAsync();
            }

            await VerifyMovedFilesAsync();

            isCompleted = true;
            Button_StartSwitch.Content = Lang.SettingPage_Completed;
            AppConfig.SetGameInstallPath(ToGameBiz, installPath);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            _logger.LogInformation("User canceled switch client.");
            Button_Prepair.IsEnabled = true;
            CanCancel = true;
            return;
        }
        catch (Exception ex)
        {
            IsPrepared = false;
            _logger.LogError(ex, "Start switching client");
            ErrorText = ex.Message;
            Button_Prepair.IsEnabled = true;
            CanCancel = true;
        }
    }








    [RelayCommand]
    private void Close()
    {
        tokenSource?.Cancel();
        MainWindow.Current.CloseOverlayPage();
        if (isCompleted)
        {
            if (_gameResourceService.GetGameInstallPath(ToGameBiz) is null)
            {
                AppConfig.SetGameInstallPath(ToGameBiz, installPath);
            }
            WeakReferenceMessenger.Default.Send(new ChangeGameBizMessage(ToGameBiz));
        }
    }




    public async Task<List<DownloadFileTask>> GetPkgVersionsAsync(string url, CancellationToken cancellationToken)
    {
        var list = new List<DownloadFileTask>();
        var str = await _httpClient.GetStringAsync(url, cancellationToken);
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




    public async Task<(long? Length, string Md5)> GetContentLengthAndMd5Async(string url, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Version = HttpVersion.Version11,
        };
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        long? contentLength = response.Content.Headers.ContentLength;
        string md5 = "";
        if (response.Headers.TryGetValues("ETag", out var etags))
        {
            md5 = etags.FirstOrDefault() ?? "";
        }
        string contentMD5 = md5.Trim('"');
        return (contentLength, contentMD5);
    }




    public class TargetGameBiz
    {

        public GameBiz GameBiz { get; set; }

        public string Name => $"{GameBiz.ToGameName()} - {GameBiz.ToGameServer()}";

    }




}
