using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.GameLauncher;
using Starward.Features.GameSelector;
using Starward.GameInstall;
using Starward.Helpers;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Starward.Features.GameInstall;


public sealed partial class InstallGameDialog : ContentDialogBase
{

    private readonly ILogger<InstallGameDialog> _logger = AppConfig.GetLogger<InstallGameDialog>();

    private readonly HoYoPlayService _hoYoPlayService = AppConfig.GetService<HoYoPlayService>();

    private readonly GameLauncherService _gameLauncherService = AppConfig.GetService<GameLauncherService>();

    private readonly GameInstallService _gameInstallService = AppConfig.GetService<GameInstallService>();


    public InstallGameDialog()
    {
        this.InitializeComponent();
    }



    public GameId CurrentGameId { get; set; }


    private LauncherConfigModel LauncherConfig;

    private GameConfig GameConfig;

    private string LauncherForInstallPath;

    private string SelectedCategoryScenario = CategoryScenario.CATEGORY_SCENARIO_FULL;


    public string InstallationPath { get; set => SetProperty(ref field, value); }

    public string? DriveTypeText { get; set => SetProperty(ref field, value); }

    public bool GameResourceLoaded { get; set => SetProperty(ref field, value); }

    public string LatestGameVersion { get; set => SetProperty(ref field, value); }

    public ScenarioPackageInfo? ScenarioPackageInfo { get; set => SetProperty(ref field, value); }

    public string FullGameResourceSizeText { get; set => SetProperty(ref field, value); }

    public string FullAudioResourceSizeText { get; set => SetProperty(ref field, value); }

    public string FullCompressedResourceSizeText { get; set => SetProperty(ref field, value); }

    public string FullUncompressedResourceSizeText { get; set => SetProperty(ref field, value); }

    public long FullUncompressedResourceSize { get; set => SetProperty(ref field, value); }

    public string BaseGameResourceSizeText { get; set => SetProperty(ref field, value); }

    public string BaseAudioResourceSizeText { get; set => SetProperty(ref field, value); }

    public string BaseCompressedResourceSizeText { get; set => SetProperty(ref field, value); }

    public string BaseUncompressedResourceSizeText { get; set => SetProperty(ref field, value); }

    public long BaseUncompressedResourceSize { get; set => SetProperty(ref field, value); }

    public string AvailableSpaceText { get; set => SetProperty(ref field, value); }

    public string? ErrorMessage { get; set => SetProperty(ref field, value); }

    public bool AutomaticallyCreateSubfolderForInstall { get; set => SetProperty(ref field, value); } = true;


    public AudioLanguage DefaultAudioLanguage => GetDefaultAudioLanguage();

    public string DefaultAudioLanguageName => GetDefaultAudioLanguageName();



    protected override async void OnLoaded()
    {
        await InitializeGameResourceAsync(UnloadCancellationToken);
    }


    private async Task InitializeGameResourceAsync(CancellationToken cancellation)
    {
        try
        {
            if (CurrentGameId is null)
            {
                ErrorMessage = Lang.InstallGameDialog_GameResourcesDoNotExist;
                return;
            }
            LauncherConfig = LauncherConfigModel.FromId(_hoYoPlayService.GetLauncherConfig(CurrentGameId)?.Id)!;
            if (LauncherConfig is null)
            {
                ErrorMessage = Lang.InstallGameDialog_GameResourcesDoNotExist;
                return;
            }

            var sb = new StringBuilder(LauncherConfig.Name);
            foreach (char item in Path.GetInvalidFileNameChars())
            {
                sb.Replace(item, ' ');
            }
            LauncherForInstallPath = sb.ToString();
            if (string.IsNullOrWhiteSpace(LauncherForInstallPath))
            {
                LauncherForInstallPath = LauncherConfig.Id;
            }

            GameConfig = (await _hoYoPlayService.GetGameConfigAsync(CurrentGameId, cancellation))!;
            if (GameConfig is null)
            {
                ErrorMessage = Lang.InstallGameDialog_GameResourcesDoNotExist;
                return;
            }
            if (!await GetGameResourceAsync(cancellation))
            {
                ErrorMessage = Lang.InstallGameDialog_GameResourcesDoNotExist;
                return;
            }
            SetDefaultInstallationPath();
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
            ErrorMessage = $"{Lang.InstallGameDialog_FailedToAcquireDownloadResources}{Environment.NewLine}{ex.Message}";
            _logger.LogError(ex, "Get game resource for '{GameId}'.", CurrentGameId);
        }
        finally
        {
            StackPanel_GettingResource.Visibility = Visibility.Collapsed;
        }
    }


    private async Task<bool> GetGameResourceAsync(CancellationToken cancellation)
    {
        AudioLanguage audioLanguage = GetDefaultAudioLanguage();
        if (GameConfig.DefaultDownloadMode is DownloadMode.DOWNLOAD_MODE_FILE)
        {
            GamePackage? package = await _hoYoPlayService.GetGamePackageAsync(CurrentGameId, cancellation);
            if (package?.Main.Major is null)
            {
                return false;
            }
            LatestGameVersion = package.Main.Major.Version;
            var size = GameResourceService.GetDownloadPackageSize(package.Main.Major, audioLanguage);
            FullGameResourceSizeText = SizeTextFormat.ToGB(size.CompressedGameBytes);
            FullAudioResourceSizeText = SizeTextFormat.ToGB(size.CompressedAudioBytes);
            FullCompressedResourceSizeText = SizeTextFormat.ToGB(size.CompressedGameBytes + size.CompressedAudioBytes);
            FullUncompressedResourceSize = size.UncompressedGameBytes + size.UncompressedAudioBytes;
            FullUncompressedResourceSizeText = SizeTextFormat.ToGB(FullUncompressedResourceSize);
            if (size.CompressedAudioBytes > 0)
            {
                StackPanel_AudioResourceSize.Visibility = Visibility.Visible;
            }
        }
        else
        {
            GameBranch? branch = await _hoYoPlayService.GetGameBranchAsync(CurrentGameId, cancellation);
            if (branch?.Main is null)
            {
                return false;
            }
            GameSophonChunkBuild? build = await _hoYoPlayService.GetGameSophonChunkBuildAsync(CurrentGameId, branch.Main, null, cancellation);
            if (build is null)
            {
                return false;
            }
            LatestGameVersion = branch.Main.Tag;
            var fullSize = GameResourceService.GetChunkDownloadPackageSize(branch.Main, build, CategoryScenario.CATEGORY_SCENARIO_FULL, audioLanguage);
            FullGameResourceSizeText = SizeTextFormat.ToGB(fullSize.CompressedGameBytes);
            FullAudioResourceSizeText = SizeTextFormat.ToGB(fullSize.CompressedAudioBytes);
            FullCompressedResourceSizeText = SizeTextFormat.ToGB(fullSize.CompressedGameBytes + fullSize.CompressedAudioBytes);
            FullUncompressedResourceSize = fullSize.UncompressedGameBytes + fullSize.UncompressedAudioBytes;
            FullUncompressedResourceSizeText = SizeTextFormat.ToGB(FullUncompressedResourceSize);
            if (fullSize.CompressedAudioBytes > 0)
            {
                StackPanel_AudioResourceSize.Visibility = Visibility.Visible;
            }

            if (GameConfig.EnableScenarioPackage)
            {
                ScenarioPackageInfo = GameConfig.ScenarioPackageInfo;
                var baseSize = GameResourceService.GetChunkDownloadPackageSize(branch.Main, build, CategoryScenario.CATEGORY_SCENARIO_BASE, audioLanguage);
                if (baseSize.CompressedAudioBytes is 0)
                {
                    baseSize.CompressedAudioBytes = fullSize.CompressedAudioBytes;
                }
                if (baseSize.UncompressedAudioBytes is 0)
                {
                    baseSize.UncompressedAudioBytes = fullSize.UncompressedAudioBytes;
                }
                BaseGameResourceSizeText = SizeTextFormat.ToGB(baseSize.CompressedGameBytes);
                BaseAudioResourceSizeText = SizeTextFormat.ToGB(baseSize.CompressedAudioBytes);
                BaseCompressedResourceSizeText = SizeTextFormat.ToGB(baseSize.CompressedGameBytes + baseSize.CompressedAudioBytes);
                BaseUncompressedResourceSize = baseSize.UncompressedGameBytes + baseSize.UncompressedAudioBytes;
                BaseUncompressedResourceSizeText = SizeTextFormat.ToGB(BaseUncompressedResourceSize);
            }
        }
        return true;
    }


    private void SetDefaultInstallationPath()
    {
        try
        {
            string baseFolder = "";
            if (AppConfig.IsAppInRemovableStorage)
            {
                if (File.Exists(AppConfig.StarwardPortableLauncherExecutePath))
                {
                    baseFolder = Path.Combine(Path.GetDirectoryName(AppConfig.StarwardPortableLauncherExecutePath)!, "HoYoGame");
                }
                else
                {
                    baseFolder = Path.Combine(Path.GetDirectoryName(AppConfig.StarwardExecutePath)!, "HoYoGame");
                }
            }
            else
            {
                baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "HoYoGame");
            }
            string target = Path.Combine(baseFolder, LauncherForInstallPath, GameConfig.InstallationDir);
            if (Path.IsPathFullyQualified(target))
            {
                SetInstallationPath(Path.GetFullPath(target));
            }
            GameResourceLoaded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set default install path.");
        }
        finally
        {
            StackPanel_GettingResource.Visibility = Visibility.Collapsed;
        }
    }


    private static AudioLanguage GetDefaultAudioLanguage()
    {
        return CultureInfo.InstalledUICulture.Name[..2] switch
        {
            "zh" => AudioLanguage.Chinese,
            "en" => AudioLanguage.English,
            "ja" => AudioLanguage.Japanese,
            "ko" => AudioLanguage.Korean,
            _ => AudioLanguage.English,
        };
    }


    private static string GetDefaultAudioLanguageName()
    {
        return CultureInfo.InstalledUICulture.Name[..2] switch
        {
            "zh" => Lang.Language_Chinese,
            "en" => Lang.Language_English,
            "ja" => Lang.Language_Japanese,
            "ko" => Lang.Language_Korean,
            _ => Lang.Language_English,
        };
    }


    [RelayCommand]
    private async Task ChangeInstallationPathAsync()
    {
        try
        {
            string? path = await FileDialogHelper.PickFolderAsync(this.XamlRoot);
            if (Directory.Exists(path))
            {
                if (AutomaticallyCreateSubfolderForInstall)
                {
                    path = Path.Combine(path, LauncherForInstallPath, GameConfig.InstallationDir);
                }
                SetInstallationPath(path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change installation path.");
        }
    }


    private void SetInstallationPath(string path)
    {
        try
        {
            TextBlock_InstallationPath.FontSize = 14;
            InstallationPath = path;
            AvailableSpaceText = SizeTextFormat.ToGB(DriveHelper.GetDriveAvailableSpace(path));
            CheckCanStartInstallation();
            DriveTypeText = DriveHelper.IsDeviceSeekPenalty(path) switch
            {
                true => "HDD",
                false => "SSD",
                _ => null,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set installation path.");
        }
    }


    private void CheckCanStartInstallation()
    {
        try
        {
            ErrorMessage = null;
            Button_StartInstallation.IsEnabled = false;
            if (DriveHelper.GetDriveType(InstallationPath) is DriveType.Network && !new Uri(InstallationPath).IsUnc)
            {
                ErrorMessage = Lang.InstallGameDialog_MappedNetworkDrivesAreNotSupportedPleaseUseANetworkSharePathStartingWithDoubleBackslashes;
            }
            else if (Path.GetPathRoot(InstallationPath) == InstallationPath)
            {
                ErrorMessage = Lang.LauncherPage_PleaseDoNotSelectTheRootDirectoryOfADrive;
            }
            else if (Path.IsPathFullyQualified(InstallationPath))
            {
                Button_StartInstallation.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check can start installation.");
        }
    }


    [RelayCommand]
    private async Task StartInstallationAsync()
    {
        try
        {
            string installPath = InstallationPath;
            GameInstallProgress? task = await _gameInstallService.StartInstallAsync(CurrentGameId, installPath, SelectedCategoryScenario, GetDefaultAudioLanguage(), UnloadCancellationToken);
            if (task is not null && task.State is not RunnerState.Stop and not RunnerState.Error)
            {
                _gameLauncherService.ChangeGameInstallPath(CurrentGameId, installPath);
                WeakReferenceMessenger.Default.Send(new GameInstallTaskStartedMessage(task));
                Close();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start installation.");
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
        }
    }


    private void GridView_CategoryScenario_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (ScenarioPackageInfo is null)
            {
                return;
            }
            if (sender is GridView gridView)
            {
                if (gridView.SelectedIndex is 0)
                {
                    TextBlock_ScenarioDesc.Text = ScenarioPackageInfo?.FullPackageDescription;
                    TextBlock_AudioResourceSize.Text = FullAudioResourceSizeText;
                    Run_DownloadResource.Text = FullCompressedResourceSizeText;
                    Run_SpaceRequired.Text = FullUncompressedResourceSizeText;
                    SelectedCategoryScenario = CategoryScenario.CATEGORY_SCENARIO_FULL;
                }
                else
                {
                    TextBlock_ScenarioDesc.Text = ScenarioPackageInfo?.BasePackageDescription;
                    TextBlock_AudioResourceSize.Text = BaseAudioResourceSizeText;
                    Run_DownloadResource.Text = BaseCompressedResourceSizeText;
                    Run_SpaceRequired.Text = BaseUncompressedResourceSizeText;
                    SelectedCategoryScenario = CategoryScenario.CATEGORY_SCENARIO_BASE;
                }
            }

        }
        catch { }
    }

}
