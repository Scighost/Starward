using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.HoYoPlay;
using Starward.Helpers;
using Starward.RPC.GameInstall;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ZstdSharp;


namespace Starward.Features.GameInstall;

[INotifyPropertyChanged]
public sealed partial class PreDownloadDialog : ContentDialog
{

    private const double GB = 1 << 30;


    private readonly ILogger<PreDownloadDialog> _logger = AppConfig.GetLogger<PreDownloadDialog>();

    private readonly HoYoPlayService _hoYoPlayService = AppConfig.GetService<HoYoPlayService>();

    private readonly GamePackageService _gamePackageService = AppConfig.GetService<GamePackageService>();

    private readonly GameInstallService _gameInstallService = AppConfig.GetService<GameInstallService>();

    private readonly HttpClient _httpClient = AppConfig.GetService<HttpClient>();

    public PreDownloadDialog()
    {
        this.InitializeComponent();
        this.Loaded += PreDownloadDialog_Loaded;
    }


    public GameId CurrentGameId { get; set; }



    private void PreDownloadDialog_Loaded(object sender, RoutedEventArgs e)
    {
        if (CurrentGameId is null)
        {
            _logger.LogWarning("CurrentGameId is null.");
            this.Hide();
            return;
        }
        _ = GetGamePackageAsync();
    }




    private string _installationPath;

    private string _localGameVersion;

    /// <summary>
    /// 有补丁包
    /// </summary>
    private bool _hasPatch;

    /// <summary>
    /// 可以下载补丁包
    /// </summary>
    private bool _canPatch;

    private GamePackage? _gamePackage;

    private GameSophonChunkBuild? _gameSophonChunkBuild;

    private GameSophonPatchBuild? _gameSophonPatchBuild;

    private AudioLanguage _audioLanguage;

    private List<string> _ignoreMatchingFields;

    private async Task GetGamePackageAsync()
    {
        try
        {
            // 安装路径
            string? installPath = GamePackageService.GetGameInstallPath(CurrentGameId);
            if (installPath is null)
            {
                _logger.LogWarning("InstallPath of ({GameBiz}) is null.", CurrentGameId.GameBiz);
                TextBlock_PredownloadUnavailable.Visibility = Visibility.Visible;
                return;
            }
            _installationPath = installPath;
            // 本地版本
            Version? version = await _gamePackageService.GetLocalGameVersionAsync(CurrentGameId, _installationPath);
            if (version is null)
            {
                _logger.LogWarning("LocalGameVersion of ({GameBiz}) is null.", CurrentGameId.GameBiz);
                TextBlock_PredownloadUnavailable.Visibility = Visibility.Visible;
                return;
            }
            _localGameVersion = version.ToString();
            // 游戏配置
            GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(CurrentGameId);
            if (config is null)
            {
                _logger.LogWarning("GameConfig of ({GameBiz}) is null.", CurrentGameId.GameBiz);
                TextBlock_PredownloadUnavailable.Visibility = Visibility.Visible;
                return;
            }
            _ignoreMatchingFields = GetIgnoreMatchingFields(_installationPath, config);
            if (config.DefaultDownloadMode is DownloadMode.DOWNLOAD_MODE_CHUNK or DownloadMode.DOWNLOAD_MODE_LDIFF)
            {
                GameBranch? gameBranch = await _hoYoPlayService.GetGameBranchAsync(CurrentGameId);
                if (gameBranch?.PreDownload is null)
                {
                    _logger.LogWarning("GameBranch.PreDownload of ({GameBiz}) is null.", CurrentGameId.GameBiz);
                    TextBlock_PredownloadUnavailable.Visibility = Visibility.Visible;
                    return;
                }
                _hasPatch = gameBranch.PreDownload.DiffTags.Count > 0;
                _canPatch = gameBranch.PreDownload.DiffTags.Any(x => x == _localGameVersion);
                if (_hasPatch && !_canPatch)
                {
                    TextBlock_NoPatches.Visibility = Visibility.Visible;
                }
                if (_canPatch)
                {
                    _gameSophonPatchBuild = await _hoYoPlayService.GetGameSophonPatchBuildAsync(gameBranch, gameBranch.PreDownload);
                }
                if (_gameSophonPatchBuild is null)
                {
                    _gameSophonChunkBuild = await _hoYoPlayService.GetGameSophonChunkBuildAsync(gameBranch, gameBranch.PreDownload);
                }
            }
            if (_gameSophonPatchBuild is null && _gameSophonChunkBuild is null)
            {
                GamePackage package = await _hoYoPlayService.GetGamePackageAsync(CurrentGameId);
                if (package.PreDownload.Major is null)
                {
                    _logger.LogWarning("PreDownloadMajor of ({GameBiz}) is null.", CurrentGameId.GameBiz);
                    TextBlock_PredownloadUnavailable.Visibility = Visibility.Visible;
                    return;
                }
                _gamePackage = package;
            }
            _audioLanguage = await _gamePackageService.GetAudioLanguageAsync(CurrentGameId);
            await ComputePackageSizeAsync();
            CheckCanPreDownload();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get game package.");
        }
    }




    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PackageSizeText))]
    public partial long PackageSizeBytes { get; set; }

    public string PackageSizeText => PackageSizeBytes == 0 ? "..." : $"{PackageSizeBytes / GB:F2} GB";



    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnzipSpaceText))]
    public partial long UnzipSpaceBytes { get; set; }

    public string UnzipSpaceText => UnzipSpaceBytes == 0 ? "..." : $"{UnzipSpaceBytes / GB:F2} GB";


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvailableSpaceText))]
    public partial long AvailableSpaceBytes { get; set; }

    public string AvailableSpaceText => AvailableSpaceBytes == 0 ? "..." : $"{AvailableSpaceBytes / GB:F2} GB";



    private async Task ComputePackageSizeAsync()
    {
        try
        {
            AvailableSpaceBytes = DriveHelper.GetDriveAvailableSpace(_installationPath);
            long size = 0, unzipSize = 0;
            if (_gamePackage is not null)
            {
                if (_gamePackage.PreDownload.Patches.FirstOrDefault(x => x.Version == _localGameVersion) is GamePackageResource patch)
                {
                    size += patch.GamePackages.Sum(x => x.Size);
                    unzipSize += patch.GamePackages.Sum(x => x.DecompressedSize);

                    foreach (var lang in Enum.GetValues<AudioLanguage>())
                    {
                        if (_audioLanguage.HasFlag(lang))
                        {
                            if (patch.AudioPackages.FirstOrDefault(x => x.Language == lang.ToDescription()) is GamePackageFile packageFile)
                            {
                                size += packageFile.Size;
                                unzipSize += packageFile.DecompressedSize;
                            }
                        }
                    }
                }
                else if (_gamePackage.PreDownload.Major is not null)
                {
                    size += _gamePackage.PreDownload.Major.GamePackages.Sum(x => x.Size);
                    unzipSize += _gamePackage.PreDownload.Major.GamePackages.Sum(x => x.DecompressedSize);

                    foreach (var lang in Enum.GetValues<AudioLanguage>())
                    {
                        if (_audioLanguage.HasFlag(lang))
                        {
                            if (_gamePackage.PreDownload.Major.AudioPackages.FirstOrDefault(x => x.Language == lang.ToDescription()) is GamePackageFile packageFile)
                            {
                                size += packageFile.Size;
                                unzipSize += packageFile.DecompressedSize;
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("PreDownloadMajor of ({GameBiz}) is null.", CurrentGameId.GameBiz);
                    TextBlock_PredownloadUnavailable.Visibility = Visibility.Visible;
                }
            }
            else if (_gameSophonChunkBuild is not null)
            {
                List<GameSophonChunkManifest> manifests = GetAvailableGameSophonChunkManifests(_gameSophonChunkBuild, _audioLanguage, _ignoreMatchingFields);
                foreach (GameSophonChunkManifest manifest in manifests)
                {
                    size += manifest.Stats.CompressedSize;
                    unzipSize += manifest.Stats.UncompressedSize;
                }
            }
            else if (_gameSophonPatchBuild is not null)
            {
                List<GameSophonPatchManifest> manifests = GetAvaliableGameSophonPatchManifests(_gameSophonPatchBuild, _audioLanguage, _ignoreMatchingFields);
                foreach (GameSophonPatchManifest manifest in manifests)
                {
                    bool isGameOrAudio = manifest.MatchingField is "game" or "zh-cn" or "en-us" or "ja-jp" or "ko-kr";
                    if (isGameOrAudio)
                    {
                        if (manifest.Stats.TryGetValue(_localGameVersion, out var stats))
                        {
                            size += stats.CompressedSize;
                            unzipSize += stats.UncompressedSize;
                        }
                    }
                    else
                    {
                        SophonPatchManifest patchManifest = await GetSophonPatchManifestAsync(manifest);
                        List<SophonPatch> patches = new();
                        foreach (SophonPatchFile item in patchManifest.Patches)
                        {
                            if (item.Patches.FirstOrDefault(x => x.Tag == _localGameVersion) is SophonPatchInfo info)
                            {
                                if (string.IsNullOrWhiteSpace(info.Patch?.OriginalFileName))
                                {
                                    string path = Path.Join(_installationPath, item.File);
                                    if (File.Exists(path) && new FileInfo(path).Length == item.Size)
                                    {
                                        // 排除已存在的文件
                                        continue;
                                    }
                                }
                                if (info.Patch is not null)
                                {
                                    patches.Add(info.Patch);
                                }
                            }
                        }
                        long patchSize = patches.DistinctBy(x => x.Id).Sum(x => x.PatchFileSize);
                        size += patchSize;
                        unzipSize += patchSize;
                    }
                }
            }
            PackageSizeBytes = size;
            UnzipSpaceBytes = unzipSize;
            if (AvailableSpaceBytes > 0 && UnzipSpaceBytes > AvailableSpaceBytes)
            {
                TextBlock_AvailableSpace.Foreground = App.Current.Resources["SystemFillColorCautionBrush"] as Brush;
            }
            else
            {
                TextBlock_AvailableSpace.Foreground = App.Current.Resources["TextFillColorSecondaryBrush"] as Brush;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Compute package size.");
        }
    }




    private void CheckCanPreDownload()
    {
        try
        {
            if (_gamePackage is not null || _gameSophonChunkBuild is not null || _gameSophonPatchBuild is not null)
            {
                if (Path.IsPathFullyQualified(_installationPath) && !string.IsNullOrWhiteSpace(_localGameVersion))
                {
                    if (PackageSizeBytes > 0 && UnzipSpaceBytes > 0)
                    {
                        Button_StartPredownload.IsEnabled = true;
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check can start predownload.");
        }
        Button_StartPredownload.IsEnabled = false;
    }




    [RelayCommand]
    private async Task StartPredownloadAsync()
    {
        try
        {
            GameInstallContext? task = await _gameInstallService.StartPredownloadAsync(CurrentGameId, _installationPath, _audioLanguage);
            if (task is not null && task.State is not GameInstallState.Stop and not GameInstallState.Error)
            {
                WeakReferenceMessenger.Default.Send(new GameInstallTaskStartedMessage(task));
                Close();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start predownload.");
        }
    }





    [RelayCommand]
    private void Close()
    {
        this.Hide();
    }





    public static List<string> GetIgnoreMatchingFields(string installPath, GameConfig gameConfig)
    {
        List<string> ignoreMatchingFields = new List<string>();
        if (gameConfig is not null)
        {
            string file = Path.Join(installPath, gameConfig.ResCategoryDir);
            if (File.Exists(file))
            {
                string[] lines = File.ReadAllLines(file);
                // eg. {"category":"10302","is_delete":true}
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var obj = JsonSerializer.Deserialize<IgnoreMatchingField>(line);
                        if (obj?.IsDelete is true && !string.IsNullOrWhiteSpace(obj.Category))
                        {
                            ignoreMatchingFields.Add(obj.Category);
                        }
                    }
                }
            }
        }
        return ignoreMatchingFields;
    }



    private class IgnoreMatchingField
    {
        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("is_delete")]
        public bool IsDelete { get; set; }
    }



    public static List<GameSophonChunkManifest> GetAvailableGameSophonChunkManifests(GameSophonChunkBuild build, AudioLanguage audioLanguage, IEnumerable<string> ignoreMatchingFields)
    {
        List<GameSophonChunkManifest> manifests = new();
        foreach (GameSophonChunkManifest manifest in build.Manifests)
        {
            if (ignoreMatchingFields.Contains(manifest.MatchingField))
            {
                continue;
            }
            if (manifest.MatchingField.Length == 5 && manifest.MatchingField[2] == '-')
            {
                // 跳过语音包
                continue;
            }
            manifests.Add(manifest);
        }
        foreach (AudioLanguage lang in Enum.GetValues<AudioLanguage>())
        {
            if (audioLanguage.HasFlag(lang))
            {
                if (build.Manifests.FirstOrDefault(x => x.MatchingField == lang.ToDescription()) is GameSophonChunkManifest audioManifest)
                {
                    manifests.Add(audioManifest);
                }
            }
        }
        return manifests;
    }



    public static List<GameSophonPatchManifest> GetAvaliableGameSophonPatchManifests(GameSophonPatchBuild build, AudioLanguage audioLanguage, IEnumerable<string> ignoreMatchingFields)
    {
        List<GameSophonPatchManifest> manifests = new();
        foreach (GameSophonPatchManifest manifest in build.Manifests)
        {
            if (ignoreMatchingFields.Contains(manifest.MatchingField))
            {
                continue;
            }
            if (manifest.MatchingField.Length == 5 && manifest.MatchingField[2] == '-')
            {
                // 跳过语音包
                continue;
            }
            manifests.Add(manifest);
        }
        foreach (AudioLanguage lang in Enum.GetValues<AudioLanguage>())
        {
            if (audioLanguage.HasFlag(lang))
            {
                if (build.Manifests.FirstOrDefault(x => x.MatchingField == lang.ToDescription()) is GameSophonPatchManifest audioManifest)
                {
                    manifests.Add(audioManifest);
                }
            }
        }
        return manifests;
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
            _logger.LogWarning("Manifest file ({Id}) checksum mismatch, re-downloading.", manifestFile.Id);
            fs.Dispose();
            File.Delete(file);
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



}
