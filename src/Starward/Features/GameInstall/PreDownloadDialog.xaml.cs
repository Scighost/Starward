using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.HoYoPlay;
using Starward.Frameworks;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Starward.Features.GameInstall;

[INotifyPropertyChanged]
public sealed partial class PreDownloadDialog : ContentDialog
{

    private const double GB = 1 << 30;


    private readonly ILogger<PreDownloadDialog> _logger = AppService.GetLogger<PreDownloadDialog>();

    private readonly HoYoPlayService _hoYoPlayService = AppService.GetService<HoYoPlayService>();

    private readonly GamePackageService _gamePackageService = AppService.GetService<GamePackageService>();


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
            string? installPath = _gamePackageService.GetGameInstallPath(CurrentGameId);
            if (installPath is null)
            {
                _logger.LogWarning("InstallPath of ({GameBiz}) is null.", CurrentGameId.GameBiz);
                this.Hide();
                return;
            }
            _installationPath = installPath;
            // 本地版本
            Version? version = await _gamePackageService.GetLocalGameVersionAsync(CurrentGameId, _installationPath);
            if (version is null)
            {
                _logger.LogWarning("LocalGameVersion of ({GameBiz}) is null.", CurrentGameId.GameBiz);
                this.Hide();
                return;
            }
            _localGameVersion = version.ToString();
            // 游戏配置
            GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(CurrentGameId);
            if (config is null)
            {
                _logger.LogWarning("GameConfig of ({GameBiz}) is null.", CurrentGameId.GameBiz);
                this.Hide();
                return;
            }
            GamePackage package = await _hoYoPlayService.GetGamePackageAsync(CurrentGameId);
            if (package.PreDownload.Major is null)
            {
                _logger.LogWarning("PreDownloadMajor of ({GameBiz}) is null.", CurrentGameId.GameBiz);
                this.Hide();
                return;
            }

            _hasPatch = package.PreDownload.Patches.Count > 0;
            _canPatch = package.PreDownload.Patches.Any(x => x.Version == _localGameVersion);
            if (_hasPatch && !_canPatch)
            {
                TextBlock_NoPatches.Visibility = Visibility.Visible;
            }

            if (config.DefaultDownloadMode is DownloadMode.DOWNLOAD_MODE_CHUNK)
            {
                var branch = await _hoYoPlayService.GetGameBranchAsync(CurrentGameId);
                if (branch?.PreDownload is not null)
                {
                    if (_canPatch)
                    {
                        _gameSophonPatchBuild = await _hoYoPlayService.GetGameSophonPatchBuildAsync(branch, branch.PreDownload);
                    }
                    if (_gameSophonPatchBuild is null)
                    {
                        _gameSophonChunkBuild = await _hoYoPlayService.GetGameSophonChunkBuildAsync(branch, branch.PreDownload);
                    }
                }
            }
            if (_gameSophonPatchBuild is null || _gameSophonChunkBuild is null)
            {
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

    public string PackageSizeText => PackageSizeBytes == 0 ? "..." : $"{UnzipSpaceBytes / GB:F2} GB";



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

    }





    [RelayCommand]
    private void Close()
    {
        this.Hide();
    }




}
