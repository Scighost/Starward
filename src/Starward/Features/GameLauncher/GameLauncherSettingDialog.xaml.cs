using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.Background;
using Starward.Features.GameInstall;
using Starward.Features.GameSelector;
using Starward.Features.HoYoPlay;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
#pragma warning disable MVVMTK0045 // Using [ObservableProperty] on fields is not AOT compatible for WinRT


namespace Starward.Features.GameLauncher;

[INotifyPropertyChanged]
public sealed partial class GameLauncherSettingDialog : ContentDialog
{


    private readonly ILogger<GameLauncherSettingDialog> _logger = AppService.GetLogger<GameLauncherSettingDialog>();


    private readonly HoYoPlayService _hoyoPlayService = AppService.GetService<HoYoPlayService>();


    private readonly GameLauncherService _gameLauncherService = AppService.GetService<GameLauncherService>();


    private readonly GamePackageService _gamePackageService = AppService.GetService<GamePackageService>();


    private readonly BackgroundService _backgroundService = AppService.GetService<BackgroundService>();


    public GameLauncherSettingDialog()
    {
        this.InitializeComponent();
        this.Loaded += GameLauncherSettingDialog_Loaded;
        this.Unloaded += GameLauncherSettingDialog_Unloaded;
    }



    public GameId CurrentGameId { get; set; }


    public GameBiz CurrentGameBiz { get; set; }




    private void FlipView_Settings_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var grid = VisualTreeHelper.GetChild(FlipView_Settings, 0);
            if (grid != null)
            {
                var count = VisualTreeHelper.GetChildrenCount(grid);
                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var child = VisualTreeHelper.GetChild(grid, i);
                        if (child is Button button)
                        {
                            button.IsHitTestVisible = false;
                            button.Opacity = 0;
                        }
                        else if (child is ScrollViewer scrollViewer)
                        {
                            scrollViewer.PointerWheelChanged += (_, e) => e.Handled = true;
                        }
                    }
                }
            }
        }
        catch { }
    }



    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            if (args.InvokedItemContainer?.Tag is string index && int.TryParse(index, out int target))
            {
                int steps = target - FlipView_Settings.SelectedIndex;
                if (steps > 0)
                {
                    for (int i = 0; i < steps; i++)
                    {
                        FlipView_Settings.SelectedIndex++;
                    }
                }
                else
                {
                    for (int i = 0; i < -steps; i++)
                    {
                        FlipView_Settings.SelectedIndex--;
                    }
                }
            }
        }
        catch { }
    }




    private async void GameLauncherSettingDialog_Loaded(object sender, RoutedEventArgs e)
    {
        CurrentGameBiz = CurrentGameId?.GameBiz ?? GameBiz.None;
        await InitializeBasicInfoAsync();
        InitializeStartArgument();
        InitializeCustomBg();
        await InitializeGamePackagesAsync();
    }


    private void GameLauncherSettingDialog_Unloaded(object sender, RoutedEventArgs e)
    {
        LatestPackageGroups = null!;
        PreInstallPackageGroups = null!;
        FlipView_Settings.Items.Clear();
    }




    [RelayCommand]
    private void Close()
    {
        this.Hide();
    }





    #region 基本信息



    private bool _hasAudioPackages;


    public bool CanRepairGame { get; set => SetProperty(ref field, value); } = true;


    public GameBizIcon CurrentGameBizIcon { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// 安装路径
    /// </summary>
    public string? InstallPath { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// 文件夹大小
    /// </summary>
    public string? GameSize { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// 是否可以卸载和修复
    /// </summary>
    public bool UninstallAndRepairEnabled { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// 是否启用公告
    /// </summary>
    public bool EnableBannerAndPost
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                AppSetting.EnableBannerAndPost = value;
                WeakReferenceMessenger.Default.Send(new GameAnnouncementSettingChangedMessage());
            }
        }
    } = AppSetting.EnableBannerAndPost;


    /// <summary>
    /// 是否启用游戏公告红点
    /// </summary>
    public bool DisableGameNoticeRedHot
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                AppSetting.DisableGameNoticeRedHot = value;
                WeakReferenceMessenger.Default.Send(new GameAnnouncementSettingChangedMessage());
            }
        }
    } = AppSetting.DisableGameNoticeRedHot;




    private async Task InitializeBasicInfoAsync()
    {
        try
        {
            if (CurrentGameId.GameBiz.IsKnown())
            {
                CurrentGameBizIcon = new GameBizIcon(CurrentGameId.GameBiz);
            }
            else
            {
                var info = await _hoyoPlayService.GetGameInfoAsync(CurrentGameId);
                CurrentGameBizIcon = new GameBizIcon(info);
            }
            InstallPath = _gameLauncherService.GetGameInstallPath(CurrentGameId, out bool storageRemoved);
            UninstallAndRepairEnabled = InstallPath != null && !storageRemoved;
            GameSize = GetSize(InstallPath);
            await InitializeAudioLanguageAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InitializeBasicInfoAsync ({biz})", CurrentGameBiz);
        }
    }



    private static string? GetSize(string? path)
    {
        if (!Directory.Exists(path))
        {
            return null;
        }
        var size = new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        var gb = (double)size / (1 << 30);
        return $"{gb:F2}GB";
    }



    private async Task InitializeAudioLanguageAsync()
    {
        try
        {
            GameConfig? config = await _hoyoPlayService.GetGameConfigAsync(CurrentGameId);
            if (!string.IsNullOrWhiteSpace(config?.AudioPackageScanDir))
            {
                _hasAudioPackages = true;
                Segmented_SelectLanguage.SelectedItems.Clear();
                AudioLanguage audioLanguage = await _gamePackageService.GetAudioLanguageAsync(CurrentGameId, InstallPath);
                if (audioLanguage.HasFlag(AudioLanguage.Chinese))
                {
                    Segmented_SelectLanguage.SelectedItems.Add(SegmentedItem_Chinese);
                }
                if (audioLanguage.HasFlag(AudioLanguage.English))
                {
                    Segmented_SelectLanguage.SelectedItems.Add(SegmentedItem_English);
                }
                if (audioLanguage.HasFlag(AudioLanguage.Japanese))
                {
                    Segmented_SelectLanguage.SelectedItems.Add(SegmentedItem_Japanese);
                }
                if (audioLanguage.HasFlag(AudioLanguage.Korean))
                {
                    Segmented_SelectLanguage.SelectedItems.Add(SegmentedItem_Korean);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InitializeAudioLanguageAsync ({biz})", CurrentGameBiz);
        }
    }



    /// <summary>
    /// 打开游戏安装文件夹
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OpenInstalGameFolderAsync()
    {
        try
        {
            if (Directory.Exists(InstallPath))
            {
                await Launcher.LaunchUriAsync(new Uri(InstallPath));
            }
        }
        catch { }
    }


    /// <summary>
    /// 删除游戏安装路径
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task DeleteGameInstllPathAsync()
    {
        try
        {
            _gameLauncherService.ChangeGameInstallPath(CurrentGameId, null);
            WeakReferenceMessenger.Default.Send(new GameInstallPathChangedMessage());
            await InitializeBasicInfoAsync();
        }
        catch { }
    }



    /// <summary>
    /// 定位游戏路径
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task LocateGameAsync()
    {
        try
        {
            string? folder = await _gameLauncherService.ChangeGameInstallPathAsync(CurrentGameId, this.XamlRoot);
            if (!string.IsNullOrWhiteSpace(folder))
            {
                await InitializeBasicInfoAsync();
                WeakReferenceMessenger.Default.Send(new GameInstallPathChangedMessage());
            }
        }
        catch (Exception ex)
        {

        }
    }



    /// <summary>
    /// 修复游戏
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task RepairGameAsync()
    {
        if (_hasAudioPackages)
        {
            Segmented_SelectLanguage.Visibility = Visibility.Visible;
            Button_StartRepairing.Visibility = Visibility.Visible;
        }
        else
        {
            await RepairGameInternalAsync();
        }
    }



    [RelayCommand]
    private async Task RepairGameInternalAsync()
    {
        try
        {
            // todo

        }
        catch (Exception ex)
        {

        }
    }



    private void Segmented_SelectLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is Segmented segmented)
        {
            CanRepairGame = segmented.SelectedItems.Count > 0;
        }
    }




    #endregion




    #region 启动参数


    /// <summary>
    /// 命令行启动参数
    /// </summary>
    [ObservableProperty]
    public string? _StartGameArgument;
    partial void OnStartGameArgumentChanged(string? value)
    {
        AppSetting.SetStartArgument(CurrentGameBiz, value);
    }


    /// <summary>
    /// 启动游戏后的操作
    /// </summary>
    public int StartGameAction
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                AppSetting.StartGameAction = (StartGameAction)value;
            }
        }
    } = Math.Clamp((int)AppSetting.StartGameAction, 0, 2);


    /// <summary>
    /// 是否启用第三方工具
    /// </summary>
    [ObservableProperty]
    public bool _EnableThirdPartyTool;
    partial void OnEnableThirdPartyToolChanged(bool value)
    {
        AppSetting.SetEnableThirdPartyTool(CurrentGameBiz, value);
    }


    /// <summary>
    /// 第三方工具路径
    /// </summary>
    [ObservableProperty]
    public string? _ThirdPartyToolPath;
    partial void OnThirdPartyToolPathChanged(string? value)
    {
        try
        {
            GameLauncherService.SetThirdPartyToolPath(CurrentGameId, value);
        }
        catch { }
    }



    private void InitializeStartArgument()
    {
        _StartGameArgument = AppSetting.GetStartArgument(CurrentGameBiz);
        _EnableThirdPartyTool = AppSetting.GetEnableThirdPartyTool(CurrentGameBiz);
        _ThirdPartyToolPath = GameLauncherService.GetThirdPartyToolPath(CurrentGameId);
        OnPropertyChanged(nameof(StartGameArgument));
        OnPropertyChanged(nameof(EnableThirdPartyTool));
        OnPropertyChanged(nameof(ThirdPartyToolPath));
    }



    /// <summary>
    /// 修改第三方启动工具路径
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task ChangeThirdPartyPathAsync()
    {
        try
        {
            var file = await FileDialogHelper.PickSingleFileAsync(this.XamlRoot);
            if (File.Exists(file))
            {
                ThirdPartyToolPath = file;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change third party tool path ({biz})", CurrentGameBiz);
        }
    }


    /// <summary>
    /// 打开第三方工具文件夹
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OpenThirdPartyToolFolderAsync()
    {
        try
        {
            if (File.Exists(ThirdPartyToolPath))
            {
                var folder = Path.GetDirectoryName(ThirdPartyToolPath);
                var file = await StorageFile.GetFileFromPathAsync(ThirdPartyToolPath);
                var option = new FolderLauncherOptions();
                option.ItemsToSelect.Add(file);
                await Launcher.LaunchFolderPathAsync(folder, option);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open third party tool folder {folder}", ThirdPartyToolPath);
        }
    }


    /// <summary>
    /// 删除第三方工具路径
    /// </summary>
    [RelayCommand]
    private void DeleteThirdPartyToolPath()
    {
        ThirdPartyToolPath = null;
    }





    #endregion




    #region 自定义背景



    /// <summary>
    /// 是否使用版本海报
    /// </summary>
    [ObservableProperty]
    public bool _UseVersionPoster;
    partial void OnUseVersionPosterChanged(bool value)
    {
        AppSetting.SetUseVersionPoster(CurrentGameBiz, value);
        if (value && EnableCustomBg)
        {
            EnableCustomBg = false;
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new BackgroundChangedMessage());
        }
    }


    /// <summary>
    /// 版本海报，文件名，存储在 UserDataFolder/bg
    /// </summary>
    public string? VersionPoster { get; set => SetProperty(ref field, value); }


    /// <summary>
    /// 是否启用自定义背景
    /// </summary>
    [ObservableProperty]
    public bool _EnableCustomBg;
    partial void OnEnableCustomBgChanged(bool value)
    {
        AppSetting.SetEnableCustomBg(CurrentGameBiz, value);
        WeakReferenceMessenger.Default.Send(new BackgroundChangedMessage());
    }


    /// <summary>
    /// 自定义背景，文件名，存储在 UserDataFolder/bg
    /// </summary>
    public string? CustomBg { get; set => SetProperty(ref field, value); }


    /// <summary>
    /// 修改背景错误信息
    /// </summary>
    public string? ChangeBgError { get; set => SetProperty(ref field, value); }


    private void InitializeCustomBg()
    {
        _UseVersionPoster = AppSetting.GetUseVersionPoster(CurrentGameBiz);
        _EnableCustomBg = AppSetting.GetEnableCustomBg(CurrentGameBiz);
        CustomBg = AppSetting.GetCustomBg(CurrentGameBiz);
        VersionPoster = AppSetting.GetVersionPoster(CurrentGameBiz);
        OnPropertyChanged(nameof(UseVersionPoster));
        OnPropertyChanged(nameof(EnableCustomBg));
    }



    /// <summary>
    /// 打开版本海报
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OpenVersionPosterAsync()
    {
        try
        {
            string path = Path.Join(AppSetting.UserDataFolder, "bg", VersionPoster);
            if (File.Exists(path))
            {
                await Launcher.LaunchUriAsync(new Uri(path));
            }
        }
        catch { }
    }



    /// <summary>
    /// 修改自定义背景
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task ChangeCustomBgAsync()
    {
        try
        {
            ChangeBgError = null;
            string? name = await _backgroundService.ChangeCustomBackgroundFileAsync(this.XamlRoot);
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }
            CustomBg = name;
            AppSetting.SetCustomBg(CurrentGameBiz, name);
            WeakReferenceMessenger.Default.Send(new BackgroundChangedMessage());
        }
        catch (COMException ex)
        {
            ChangeBgError = Lang.GameLauncherSettingDialog_CannotDecodeFile;
            _logger.LogError(ex, "Change custom background failed");
        }
        catch (Exception ex)
        {
            ChangeBgError = Lang.GameLauncherSettingDialog_AnUnknownErrorOccurredPleaseCheckTheLogs;
            _logger.LogError(ex, "Change custom background failed");
        }
    }



    /// <summary>
    /// 打开自定义背景文件
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OpenCustomBgAsync()
    {
        try
        {
            string path = Path.Join(AppSetting.UserDataFolder, "bg", CustomBg);
            if (File.Exists(path))
            {
                await Launcher.LaunchUriAsync(new Uri(path));
            }
        }
        catch { }
    }



    /// <summary>
    /// 删除自定义背景
    /// </summary>
    [RelayCommand]
    private void DeleteCustomBg()
    {
        CustomBg = null;
        AppSetting.SetCustomBg(CurrentGameBiz, null);
        WeakReferenceMessenger.Default.Send(new BackgroundChangedMessage());
    }


    /// <summary>
    /// 接受拖放文件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Grid_BackgroundDragIn_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }



    /// <summary>
    /// 拖放文件，修改自定义背景
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void Grid_BackgroundDragIn_Drop(object sender, DragEventArgs e)
    {
        ChangeBgError = null;
        var defer = e.GetDeferral();
        try
        {
            if ((await e.DataView.GetStorageItemsAsync()).FirstOrDefault() is StorageFile file)
            {
                string? name = await _backgroundService.ChangeCustomBackgroundFileAsync(file);
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }
                CustomBg = name;
                AppSetting.SetCustomBg(CurrentGameBiz, name);
                if (EnableCustomBg)
                {
                    WeakReferenceMessenger.Default.Send(new BackgroundChangedMessage());
                }
                else
                {
                    EnableCustomBg = true;
                }
            }
        }
        catch (COMException ex)
        {
            ChangeBgError = Lang.GameLauncherSettingDialog_CannotDecodeFile;
            _logger.LogError(ex, "Change custom background failed");
        }
        catch (Exception ex)
        {
            ChangeBgError = Lang.GameLauncherSettingDialog_AnUnknownErrorOccurredPleaseCheckTheLogs;
            _logger.LogError(ex, "Change custom background failed");
        }
        defer.Complete();
    }



    #endregion




    #region 游戏包体



    /// <summary>
    /// 最新版本
    /// </summary>
    public string LatestVersion { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// 最新版本包体
    /// </summary>
    public List<PackageGroup> LatestPackageGroups { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// 预下载版本
    /// </summary>
    public string PreInstallVersion { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// 预下载版本包体
    /// </summary>
    public List<PackageGroup> PreInstallPackageGroups { get; set => SetProperty(ref field, value); }




    private async Task InitializeGamePackagesAsync()
    {
        try
        {
            var gamePackage = await _hoyoPlayService.GetGamePackageAsync(CurrentGameId);
            LatestVersion = gamePackage.Main.Major!.Version;
            var list = GetGameResourcePackageGroups(gamePackage.Main);
            var sdk = await _hoyoPlayService.GetGameChannelSDKAsync(CurrentGameId);
            if (sdk is not null)
            {
                list.Add(new PackageGroup
                {
                    Name = "Channel SDK",
                    Items = [new PackageItem
                    {
                        FileName = Path.GetFileName(sdk.ChannelSDKPackage.Url),
                        Url = sdk.ChannelSDKPackage.Url,
                        Md5 = sdk.ChannelSDKPackage.MD5,
                        PackageSize = sdk.ChannelSDKPackage.Size,
                        DecompressSize = sdk.ChannelSDKPackage.DecompressedSize,
                    }],
                });
            }
            // todo plugin
            LatestPackageGroups = list;
            if (!string.IsNullOrWhiteSpace(gamePackage.PreDownload?.Major?.Version))
            {
                PreInstallVersion = gamePackage.PreDownload.Major.Version;
                PreInstallPackageGroups = GetGameResourcePackageGroups(gamePackage.PreDownload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get game resource failed, gameBiz: {gameBiz}", CurrentGameBiz);
        }
    }



    private List<PackageGroup> GetGameResourcePackageGroups(GamePackageVersion gameResource)
    {
        var list = new List<PackageGroup>();
        var fullPackageGroup = new PackageGroup
        {
            Name = Lang.GameResourcePage_FullPackages,
            Items = new List<PackageItem>()
        };
        foreach (var item in gameResource.Major?.GamePackages ?? [])
        {
            fullPackageGroup.Items.Add(new PackageItem
            {
                FileName = Path.GetFileName(item.Url),
                Url = item.Url,
                Md5 = item.MD5,
                PackageSize = item.Size,
                DecompressSize = item.DecompressedSize,
            });
        }
        foreach (var item in gameResource.Major?.AudioPackages ?? [])
        {
            fullPackageGroup.Items.Add(new PackageItem
            {
                FileName = Path.GetFileName(item.Url),
                Url = item.Url,
                Md5 = item.MD5,
                PackageSize = item.Size,
                DecompressSize = item.DecompressedSize,
            });
        }
        list.Add(fullPackageGroup);

        foreach (var patch in gameResource.Patches ?? [])
        {
            var diffPackageGroup = new PackageGroup
            {
                Name = $"{Lang.GameResourcePage_DiffPackages}  {patch.Version}",
                Items = new List<PackageItem>()
            };
            foreach (var item in patch.GamePackages ?? [])
            {
                diffPackageGroup.Items.Add(new PackageItem
                {
                    FileName = Path.GetFileName(item.Url),
                    Url = item.Url,
                    Md5 = item.MD5,
                    PackageSize = item.Size,
                    DecompressSize = item.DecompressedSize,
                });
            }
            foreach (var item in patch.AudioPackages ?? [])
            {
                diffPackageGroup.Items.Add(new PackageItem
                {
                    FileName = Path.GetFileName(item.Url),
                    Url = item.Url,
                    Md5 = item.MD5,
                    PackageSize = item.Size,
                    DecompressSize = item.DecompressedSize,
                });
            }
            list.Add(diffPackageGroup);
        }
        return list;
    }



    private async void Button_CopyUrl_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button)
            {
                if (button.DataContext is PackageGroup group)
                {
                    if (group.Items is not null)
                    {
                        var sb = new StringBuilder();
                        foreach (var item in group.Items)
                        {
                            if (!string.IsNullOrEmpty(item.Url))
                            {
                                sb.AppendLine(item.Url);
                            }
                        }
                        string url = sb.ToString().TrimEnd();
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            ClipboardHelper.SetText(url);
                            await CopySuccessAsync(button);
                        }
                    }
                }
                if (button.DataContext is PackageItem package)
                {
                    if (!string.IsNullOrEmpty(package.Url))
                    {
                        ClipboardHelper.SetText(package.Url);
                        await CopySuccessAsync(button);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy url failed");
        }
    }



    private async Task CopySuccessAsync(Button button)
    {
        try
        {
            button.IsEnabled = false;
            if (button.Content is FontIcon icon)
            {
                // Accpet
                icon.Glyph = "\uF78C";
                await Task.Delay(1000);
            }
        }
        finally
        {
            button.IsEnabled = true;
            if (button.Content is FontIcon icon)
            {
                // Link
                icon.Glyph = "\uE71B";
            }
        }
    }




    public class PackageGroup
    {
        public string Name { get; set; }

        public List<PackageItem> Items { get; set; }
    }



    public class PackageItem
    {
        public string FileName { get; set; }

        public string Url { get; set; }

        public string Md5 { get; set; }

        public long PackageSize { get; set; }

        public long DecompressSize { get; set; }

        public string PackageSizeString => GetSizeString(PackageSize);

        public string DecompressSizeString => GetSizeString(DecompressSize);

        private string GetSizeString(long size)
        {
            const double KB = 1 << 10;
            const double MB = 1 << 20;
            const double GB = 1 << 30;
            if (size >= GB)
            {
                return $"{size / GB:F2} GB";
            }
            else if (size >= MB)
            {
                return $"{size / MB:F2} MB";
            }
            else
            {
                return $"{size / KB:F2} KB";
            }
        }
    }



    #endregion





    private void TextBlock_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        if (sender.FontSize > 12)
        {
            sender.FontSize -= 1;
        }
    }


}
