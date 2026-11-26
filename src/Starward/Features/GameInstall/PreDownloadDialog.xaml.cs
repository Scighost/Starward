using Starward.Core.Hypergryph;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.GameLauncher;
using Starward.GameInstall;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Starward.Features.GameInstall;


public sealed partial class PreDownloadDialog : ContentDialogBase
{

    private readonly ILogger<PreDownloadDialog> _logger = AppConfig.GetLogger<PreDownloadDialog>();

    private readonly HoYoPlayService _hoYoPlayService = AppConfig.GetService<HoYoPlayService>();

    private readonly GameConfigService _gameConfigService = AppConfig.GetService<GameConfigService>();

    private readonly GameInstallService _gameInstallService = AppConfig.GetService<GameInstallService>();

    private readonly GameResourceService _gameResourceService = AppConfig.GetService<GameResourceService>();


    public PreDownloadDialog()
    {
        this.InitializeComponent();
    }


    public GameId CurrentGameId { get; set; }

    public string InstallPath { get; set; }

    public GameConfig GameConfig { get; set; }

    public bool GameResourceLoaded { get; set => SetProperty(ref field, value); }

    public string? PredownloadVersion { get; set => SetProperty(ref field, value); }

    public string DownloadResourceSizeText { get; set => SetProperty(ref field, value); }

    public string SpaceRequiredSizeText { get; set => SetProperty(ref field, value); }

    public string SpaceAvailableSizeText { get; set => SetProperty(ref field, value); }

    public string? ErrorMessage { get; set => SetProperty(ref field, value); }


    private AudioLanguage _audioLanguage;

    private string? _localVersion;

    private string _categoryScenario;


    protected override async void OnLoaded()
    {
        await GetGamePackageAsync(UnloadCancellationToken);
    }




    private async Task GetGamePackageAsync(CancellationToken cancellation)
    {
        try
        {
            ErrorMessage = null;
            if (CurrentGameId is null)
            {
                ErrorMessage = Lang.InstallGameDialog_GameResourcesDoNotExist;
                return;
            }

            InstallPath = GameLauncherService.GetGameInstallPath(CurrentGameId)!;
            if (string.IsNullOrWhiteSpace(InstallPath))
            {
                _logger.LogWarning("InstallPath of ({GameBiz}) is null.", CurrentGameId.GameBiz);
                ErrorMessage = Lang.PreDownloadDialog_PreInstallIsNotAvailable;
                return;
            }

            GameConfig = (await _hoYoPlayService.GetGameConfigAsync(CurrentGameId, cancellation))!;
            if (GameConfig is null)
            {
                _logger.LogWarning("GameConfig of ({GameBiz}) is null.", CurrentGameId.GameBiz);
                ErrorMessage = Lang.InstallGameDialog_GameResourcesDoNotExist;
                return;
            }

            _audioLanguage = await _gameConfigService.GetAudioLanguageAsync(CurrentGameId, InstallPath, cancellation);
            _localVersion = (await GameConfigService.GetGameConfigIniAsync(InstallPath, cancellation))?.GameVersion?.ToString();
            _categoryScenario = await _gameConfigService.GetCategoryScenarioAsync(CurrentGameId, InstallPath, cancellation) ?? CategoryScenario.CATEGORY_SCENARIO_FULL;

            if (await GetPredownloadResourceAsync(cancellation))
            {
                GameResourceLoaded = true;
                Button_StartPredownload.IsEnabled = true;
            }
            else
            {
                ErrorMessage = Lang.InstallGameDialog_GameResourcesDoNotExist;
            }
        }
        catch (OperationCanceledException) { }
        catch (miHoYoApiException ex) when (ex.ReturnCode is -100 or -403)
        {
            if (ex.ReturnCode is -100)
            {
                ErrorMessage = Lang.InstallGameDialog_LoginExpiredPleaseLogInAgain;
            }
            if (ex.ReturnCode is -403)
            {
                ErrorMessage = Lang.InstallGameDialog_CurrentAccountDoesnTHaveBetaTestingAccess;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = Lang.InstallGameDialog_FailedToAcquireDownloadResources;
            _logger.LogError(ex, "Get game package.");
        }
        finally
        {
            StackPanel_GettingResource.Visibility = Visibility.Collapsed;
        }
    }




    /// <summary>
    /// 获取预下载资源并计算下载大小和所需空间。
    /// </summary>
    /// <returns>预下载资源是否可用。</returns>
    private async Task<bool> GetPredownloadResourceAsync(CancellationToken cancellation)
    {
        ErrorMessage = null;
        long downloadResourceSize = 0, spaceRequiredSize = 0;

        if (HypergryphGameConstants.IsHypergryphGame(CurrentGameId.GameBiz))
        {
            _localVersion = (await AppConfig.GetService<GameLauncherService>().GetLocalGameVersionAsync(CurrentGameId, InstallPath, cancellation))?.ToString();
            var latest = await AppConfig.GetService<HypergryphLauncherClient>().GetLatestGameAsync(CurrentGameId.GameBiz, _localVersion, cancellation);
            if (latest.PrePatch is not { DownloadParts.Count: > 0 } patch)
            {
                return false;
            }
            downloadResourceSize = patch.DownloadParts.Sum(x => x.PackageSize);
            spaceRequiredSize = downloadResourceSize;
            PredownloadVersion = $"{_localVersion}  →  {(string.IsNullOrWhiteSpace(patch.TargetVersion) ? patch.Version : patch.TargetVersion)}";
        }
        else if (GameConfig.DefaultDownloadMode is DownloadMode.DOWNLOAD_MODE_FILE)
        {
            GamePackage? package = await _hoYoPlayService.GetGamePackageAsync(CurrentGameId, cancellation);
            if (package?.PreDownload is null)
            {
                _logger.LogWarning("GamePackage.PreDownload of ({GameBiz}) is null.", CurrentGameId.GameBiz);
                return false;
            }

            GamePackageResource? resource = package.PreDownload.Patches?.FirstOrDefault(x => x.Version == _localVersion) ?? package.PreDownload.Major;
            if (resource is null)
            {
                _logger.LogWarning("PreDownload resource of ({GameBiz}) is null.", CurrentGameId.GameBiz);
                return false;
            }

            (List<GamePackageFile> gamePackages, List<GamePackageFile> audioPackages) = GameResourceService.GetDownloadPackageFiles(resource, _audioLanguage);
            downloadResourceSize = gamePackages.Sum(x => x.Size) + audioPackages.Sum(x => x.Size);
            spaceRequiredSize = gamePackages.Sum(x => x.DecompressedSize) + audioPackages.Sum(x => x.DecompressedSize);
            PredownloadVersion = $"{_localVersion}  →  {resource.Version}";
        }
        else
        {
            GameBranch? branch = await _hoYoPlayService.GetGameBranchAsync(CurrentGameId, cancellation);
            if (branch?.Main is null || branch?.PreDownload is null)
            {
                _logger.LogWarning("GameBranch.PreDownload of ({GameBiz}) is null.", CurrentGameId.GameBiz);
                return false;
            }
            if (!string.IsNullOrWhiteSpace(_localVersion) && branch.PreDownload.DiffTags.Contains(_localVersion))
            {
                var list = await _gameResourceService.GetGameSophonPatchManifestsAsync(CurrentGameId, branch.PreDownload, InstallPath, _categoryScenario, _audioLanguage, cancellation);
                downloadResourceSize = list.Sum(x => x.Stats.GetValueOrDefault(_localVersion)?.CompressedSize ?? 0);
                spaceRequiredSize = list.Sum(x => x.Stats.GetValueOrDefault(_localVersion)?.UncompressedSize ?? 0);
            }
            else
            {
                long size = await _gameResourceService.GetPredownloadResourceSizeAsync(CurrentGameId, branch, InstallPath, _categoryScenario, _audioLanguage, _localVersion, cancellation);
                downloadResourceSize = size;
                spaceRequiredSize = size;
            }
            PredownloadVersion = $"{_localVersion}  →  {branch.PreDownload.Tag}";
        }

        DownloadResourceSizeText = SizeTextFormat.ToGB(downloadResourceSize);
        SpaceRequiredSizeText = SizeTextFormat.ToGB(spaceRequiredSize);
        SpaceAvailableSizeText = SizeTextFormat.ToGB(DriveHelper.GetDriveAvailableSpace(InstallPath));
        return true;
    }




    [RelayCommand]
    private async Task StartPredownloadAsync()
    {
        try
        {
            GameInstallProgress? task = await _gameInstallService.StartPredownloadAsync(CurrentGameId, InstallPath, UnloadCancellationToken);
            if (task is not null && task.State is not RunnerState.Stop and not RunnerState.Error)
            {
                WeakReferenceMessenger.Default.Send(new GameInstallTaskStartedMessage(task));
                Close();
            }
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("miHoYoApiException") && ex.Message.Contains("-100"))
            {
                ErrorMessage = Lang.InstallGameDialog_LoginExpiredPleaseLogInAgain;
                return;
            }
            if (ex.Message.Contains("miHoYoApiException") && ex.Message.Contains("-403"))
            {
                ErrorMessage = Lang.InstallGameDialog_CurrentAccountDoesnTHaveBetaTestingAccess;
                return;
            }
            ErrorMessage = ex.Message;
            _logger.LogError(ex, "Start predownload.");
        }
    }


}
