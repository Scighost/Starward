using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Starward.Core.HoYoPlay;
using Starward.Features.HoYoPlay;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Starward.Features.GameInstall;

[INotifyPropertyChanged]
public sealed partial class InstallGameDialog : ContentDialog
{

    private const double GB = 1 << 30;


    private readonly ILogger<InstallGameDialog> _logger = AppService.GetLogger<InstallGameDialog>();

    private readonly HoYoPlayService _hoYoPlayService = AppService.GetService<HoYoPlayService>();



    public InstallGameDialog()
    {
        this.InitializeComponent();
        this.Loaded += InstallGameDialog_Loaded;
        this.Unloaded += InstallGameDialog_Unloaded;
    }



    public GameId CurrentGameId { get; set; }



    private void InstallGameDialog_Loaded(object sender, RoutedEventArgs e)
    {
        if (CurrentGameId is null)
        {
            _logger.LogWarning("CurrentGameId is null.");
            this.Hide();
            return;
        }
        SetDefaultInstallationPath();
        _ = GetGamePackageAsync();
    }



    private void InstallGameDialog_Unloaded(object sender, RoutedEventArgs e)
    {
        Segmented_SelectLanguage.SelectionChanged -= Segmented_SelectLanguage_SelectionChanged;
        Segmented_SelectLanguage.Items.Clear();
    }



    private void SetDefaultInstallationPath()
    {
        try
        {
            string baseFolder = "";
            if (AppSetting.IsAppInRemovableStorage)
            {
                if (File.Exists(AppSetting.StarwardLauncherExecutePath))
                {
                    baseFolder = Path.Combine(Path.GetDirectoryName(AppSetting.StarwardLauncherExecutePath)!, "Games");
                }
                else
                {
                    baseFolder = Path.Combine(Path.GetDirectoryName(AppSetting.StarwardExecutePath)!, "Games");
                }
            }
            else
            {
                string? defaultPath = AppSetting.DefaultGameInstallationPath;
                if (Directory.Exists(defaultPath))
                {
                    baseFolder = defaultPath;
                }
                else
                {
                    baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Starward/Games");
                }
            }
            string target = Path.Combine(baseFolder, CurrentGameId.GameBiz);
            if (Path.IsPathFullyQualified(target))
            {
                SetInstallationPath(Path.GetFullPath(target));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set default install path.");
        }
    }



    private async Task GetGamePackageAsync()
    {
        try
        {
            GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(CurrentGameId);
            if (config is null)
            {
                _logger.LogWarning("GameConfig of ({GameBiz}) is null.", CurrentGameId.GameBiz);
                this.Hide();
                return;
            }
            if (!string.IsNullOrWhiteSpace(config.AudioPackageScanDir))
            {
                _needAudioPackage = true;
                Segmented_SelectLanguage.Visibility = Visibility.Visible;
                SetDefaultAudioPackage();
            }
            if (GameFeatureConfig.FromGameId(CurrentGameId).SupportHardLink)
            {
                StackPanel_HardLink.Visibility = Visibility.Visible;
            }
            if (config.DefaultDownloadMode is DownloadMode.DOWNLOAD_MODE_CHUNK)
            {
                var branch = await _hoYoPlayService.GetGameBranchAsync(CurrentGameId);
                if (branch is not null)
                {
                    _gameSophonChunkBuild = await _hoYoPlayService.GetGameSophonChunkBuildAsync(branch, branch.Main);
                }
            }
            if (_gameSophonChunkBuild is null)
            {
                _gamePackage = await _hoYoPlayService.GetGamePackageAsync(CurrentGameId);
            }
            ComputePackageSize();
            CheckCanStartInstallation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get game package.");
        }
    }



    private void SetDefaultAudioPackage()
    {
        if (_needAudioPackage)
        {
            Segmented_SelectLanguage.SelectedIndex = CultureInfo.CurrentUICulture.Name[..2] switch
            {
                "zh" => 0,
                "en" => 1,
                "ja" => 2,
                "ko" => 3,
                _ => 2,
            };
        }
    }



    private bool _needAudioPackage;


    private GamePackage? _gamePackage;


    private GameSophonChunkBuild? _gameSophonChunkBuild;


    public string InstallationPath { get; set => SetProperty(ref field, value); }


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
            long size = 0;
            List<string?> langs = Segmented_SelectLanguage.SelectedItems.Cast<SegmentedItem>().Select(x => x.Tag as string).ToList();
            if (_gamePackage is not null)
            {
                size += _gamePackage.Main.Major!.GamePackages.Sum(x => x.DecompressedSize);
                foreach (string? lang in langs)
                {
                    if (_gamePackage.Main.Major.AudioPackages.FirstOrDefault(x => x.Language == lang) is GamePackageFile gamePackageFile)
                    {
                        size += gamePackageFile.DecompressedSize;
                    }
                }
            }
            else if (_gameSophonChunkBuild is not null)
            {
                if (_gameSophonChunkBuild.Manifests.FirstOrDefault(x => x.MatchingField is "game") is GameSophonChunkManifest manifest)
                {
                    size += manifest.Stats.UncompressedSize;
                }
                foreach (string? lang in langs)
                {
                    if (_gameSophonChunkBuild.Manifests.FirstOrDefault(x => x.MatchingField == lang) is GameSophonChunkManifest audioManifest)
                    {
                        size += audioManifest.Stats.UncompressedSize;
                    }
                }
            }
            UnzipSpaceBytes = size;
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



    private void CheckCanStartInstallation()
    {
        try
        {
            if (_gamePackage is not null || _gameSophonChunkBuild is not null)
            {
                if (Path.IsPathFullyQualified(InstallationPath))
                {
                    if (!(_needAudioPackage ^ Segmented_SelectLanguage.SelectedItems.Count > 0))
                    {
                        Button_StartInstallation.IsEnabled = true;
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check can start installation.");
        }
        Button_StartInstallation.IsEnabled = false;
    }



    [RelayCommand]
    private async Task ChangeInstallationPathAsync()
    {
        try
        {
            string? path = await FileDialogHelper.PickFolderAsync(this.XamlRoot);
            if (Directory.Exists(path))
            {
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
            StackPanel_FreeSpace.Visibility = Visibility.Visible;
            AvailableSpaceBytes = new DriveInfo(path).AvailableFreeSpace;
            CheckCanStartInstallation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set installation path.");
        }
    }



    private void Segmented_SelectLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ComputePackageSize();
        CheckCanStartInstallation();
    }



    [RelayCommand]
    private async Task StartInstallationAsync()
    {
        // todo start installation
    }




    [RelayCommand]
    private void Close()
    {
        this.Hide();
    }



    private void TextBlock_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        if (sender.FontSize > 12)
        {
            sender.FontSize--;
        }
    }



}
