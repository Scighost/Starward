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
using Starward.RPC.GameInstall;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Starward.Features.GameInstall;

[INotifyPropertyChanged]
public sealed partial class PreDownloadDialog : ContentDialog
{

    private const double GB = 1 << 30;


    private readonly ILogger<PreDownloadDialog> _logger = AppConfig.GetLogger<PreDownloadDialog>();

    private readonly HoYoPlayService _hoYoPlayService = AppConfig.GetService<HoYoPlayService>();

    private readonly GamePackageService _gamePackageService = AppConfig.GetService<GamePackageService>();

    private readonly GameInstallService _gameInstallService = AppConfig.GetService<GameInstallService>();


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
            else
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
            ComputePackageSize();
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



    private void ComputePackageSize()
    {
        try
        {
            AvailableSpaceBytes = new DriveInfo(_installationPath).AvailableFreeSpace;
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
                if (_gameSophonChunkBuild.Manifests.FirstOrDefault(x => x.MatchingField is "game") is GameSophonChunkManifest manifest)
                {
                    size += manifest.Stats.CompressedSize;
                    unzipSize += manifest.Stats.UncompressedSize;
                }
                foreach (var lang in Enum.GetValues<AudioLanguage>())
                {
                    if (_audioLanguage.HasFlag(lang))
                    {
                        if (_gameSophonChunkBuild.Manifests.FirstOrDefault(x => x.MatchingField == lang.ToDescription()) is GameSophonChunkManifest audioManifest)
                        {
                            size += audioManifest.Stats.CompressedSize;
                            unzipSize += audioManifest.Stats.UncompressedSize;
                        }
                    }
                }
            }
            else if (_gameSophonPatchBuild is not null)
            {
                if (_gameSophonPatchBuild.Manifests.FirstOrDefault(x => x.MatchingField is "game") is GameSophonPatchManifest manifest)
                {
                    if (manifest.Stats.TryGetValue(_localGameVersion, out GameSophonManifestStats? stats))
                    {
                        size += stats.CompressedSize;
                        unzipSize += stats.UncompressedSize;
                    }
                }
                foreach (var lang in Enum.GetValues<AudioLanguage>())
                {
                    if (_audioLanguage.HasFlag(lang))
                    {
                        if (_gameSophonPatchBuild.Manifests.FirstOrDefault(x => x.MatchingField == lang.ToDescription()) is GameSophonPatchManifest audioManifest)
                        {
                            if (audioManifest.Stats.TryGetValue(_localGameVersion, out GameSophonManifestStats? stats))
                            {
                                size += stats.CompressedSize;
                                unzipSize += stats.UncompressedSize;
                            }
                        }
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
            GameInstallTask? task = await _gameInstallService.StartPredownloadAsync(CurrentGameId, _installationPath, _audioLanguage);
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




}
