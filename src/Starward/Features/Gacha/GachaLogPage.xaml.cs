using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.Gacha.Genshin;
using Starward.Core.Gacha.StarRail;
using Starward.Features.Gacha.UIGF;
using Starward.Features.GameLauncher;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Storage;
using Windows.System;


namespace Starward.Features.Gacha;

public sealed partial class GachaLogPage : PageBase
{

    private readonly ILogger<GachaLogPage> _logger = AppConfig.GetLogger<GachaLogPage>();

    private readonly GameLauncherService _gameLauncherService = AppConfig.GetService<GameLauncherService>();


    private GachaLogService _gachaLogService;



    public GachaLogPage()
    {
        this.InitializeComponent();
    }



    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        GachaTypeText = GachaLogService.GetGachaLogText(CurrentGameBiz);
        if (CurrentGameBiz.Game == GameBiz.hk4e)
        {
            EnableGenshinGachaItemStats = true;
            ToggleSwitch_ShowChronicledWish.Visibility = Visibility.Visible;
            _gachaLogService = AppConfig.GetService<GenshinGachaService>();
            Image_Emoji.Source = new BitmapImage(AppConfig.EmojiPaimon);
        }
        if (CurrentGameBiz.Game == GameBiz.hkrpg)
        {
            EnableStarRailGachaItemStats = true;
            _gachaLogService = AppConfig.GetService<StarRailGachaService>();
            Image_Emoji.Source = new BitmapImage(AppConfig.EmojiPom);
        }
        if (CurrentGameBiz.Game == GameBiz.nap)
        {
            EnableZZZGachaItemStats = true;
            IsZZZGachaStatsCardVisible = true;
            _gachaLogService = AppConfig.GetService<ZZZGachaService>();
            Image_Emoji.Source = new BitmapImage(AppConfig.EmojiBangboo);
            MenuFlyoutItem_CloudGame.Visibility = Visibility.Collapsed;
        }
        if (CurrentGameBiz.IsGlobalServer())
        {
            MenuFlyoutItem_CloudGame.Visibility = Visibility.Collapsed;
        }
    }


    public bool IsZZZGachaStatsCardVisible { get; set => SetProperty(ref field, value); }



    public string GachaTypeText { get; set => SetProperty(ref field, value); }



    public ObservableCollection<long> UidList { get; set => SetProperty(ref field, value); }


    [ObservableProperty]
    private long? selectUid;
    partial void OnSelectUidChanged(long? value)
    {
        AppConfig.SetLastUidInGachaLogPage(CurrentGameBiz.Game, value ?? 0);
        UpdateGachaTypeStats(value);
    }




    [ObservableProperty]
    private bool showNoviceGacha = AppConfig.ShowNoviceGacha;
    partial void OnShowNoviceGachaChanged(bool value)
    {
        AppConfig.ShowNoviceGacha = value;
        if (noviceGachaTypeStats != null && GachaTypeStatsCollection != null)
        {
            if (value && !GachaTypeStatsCollection.Contains(noviceGachaTypeStats))
            {
                GachaTypeStatsCollection.Add(noviceGachaTypeStats);
            }
            if (!value && GachaTypeStatsCollection.Contains(noviceGachaTypeStats))
            {
                GachaTypeStatsCollection.Remove(noviceGachaTypeStats);
            }
        }
        UpdateGachaStatsCardLayout();
    }


    [ObservableProperty]
    private bool showChronicledWish = AppConfig.ShowChronicledWish;
    partial void OnShowChronicledWishChanged(bool value)
    {
        AppConfig.ShowChronicledWish = value;
        if (chronicledWishStats != null && GachaTypeStatsCollection != null)
        {
            if (value && !GachaTypeStatsCollection.Contains(chronicledWishStats))
            {
                GachaTypeStatsCollection.Add(chronicledWishStats);
            }
            if (!value && GachaTypeStatsCollection.Contains(chronicledWishStats))
            {
                GachaTypeStatsCollection.Remove(chronicledWishStats);
            }
        }
        UpdateGachaStatsCardLayout();
    }


    public string? GachaLanguage
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                AppConfig.GachaLanguage = value;
            }
        }
    } = AppConfig.GachaLanguage;




    public bool EnableGenshinGachaItemStats { get; set => SetProperty(ref field, value); }

    public bool EnableStarRailGachaItemStats { get; set => SetProperty(ref field, value); }

    public bool EnableZZZGachaItemStats { get; set => SetProperty(ref field, value); }


    public ObservableCollection<GachaTypeStats> GachaTypeStatsCollection { get; set => SetProperty(ref field, value); }

    public List<GachaLogItemEx>? GachaItemStats { get; set => SetProperty(ref field, value); }




    private GachaTypeStats? noviceGachaTypeStats;

    private GachaTypeStats? chronicledWishStats;

    private int errorCount = 0;



    protected override async void OnLoaded()
    {
        await Task.Delay(16);
        WeakReferenceMessenger.Default.Register<UpdateGachaLogMessage>(this, (s, m) =>
        {
            if (m.GameBiz == CurrentGameBiz)
            {
                User32.SetForegroundWindow((nint)this.XamlRoot.ContentIslandEnvironment.AppWindowId.Value);
                _ = UpdateGachaLogInternalAsync(m.Url);
            }
        });
        Initialize();
        await UpdateWikiDataAsync();
    }



    protected override void OnUnloaded()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        if (GachaTypeStatsCollection is not null)
        {
            GachaTypeStatsCollection.Clear();
            GachaTypeStatsCollection = null!;
        }
        if (GachaItemStats is not null)
        {
            GachaItemStats.Clear();
            GachaItemStats = null;
        }
        noviceGachaTypeStats = null;
        chronicledWishStats = null;
    }



    private void Initialize()
    {
        try
        {
            SelectUid = null;
            UidList = new(_gachaLogService.GetUids());
            var lastUid = AppConfig.GetLastUidInGachaLogPage(CurrentGameBiz.Game);
            if (UidList.Contains(lastUid))
            {
                SelectUid = lastUid;
            }
            else
            {
                SelectUid = UidList.FirstOrDefault();
            }
            if (UidList.Count == 0)
            {
                StackPanel_Emoji.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize");
        }
    }



    private void UpdateGachaTypeStats(long? uid)
    {
        try
        {
            if (uid is null or 0)
            {
                GachaTypeStatsCollection = [];
                noviceGachaTypeStats = null;
                chronicledWishStats = null;
                GachaItemStats = null;
                StackPanel_Emoji.Visibility = Visibility.Visible;
            }
            else
            {
                (var gachaStats, var itemStats) = _gachaLogService.GetGachaTypeStats(uid.Value);
                if (CurrentGameBiz.Game is GameBiz.hk4e or GameBiz.hkrpg)
                {
                    noviceGachaTypeStats = gachaStats.FirstOrDefault(x => x.GachaType == GenshinGachaType.NoviceWish || x.GachaType == StarRailGachaType.DepartureWarp);
                    chronicledWishStats = gachaStats.FirstOrDefault(x => x.GachaType == GenshinGachaType.ChronicledWish);
                }
                if (noviceGachaTypeStats != null && !ShowNoviceGacha)
                {
                    gachaStats.Remove(noviceGachaTypeStats);
                }
                if (chronicledWishStats != null && !ShowChronicledWish)
                {
                    gachaStats.Remove(chronicledWishStats);
                }
                GachaTypeStatsCollection = [.. gachaStats];
                GachaItemStats = itemStats;
                StackPanel_Emoji.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateGachaTypeStats");
        }
    }



    private async Task UpdateWikiDataAsync()
    {
        try
        {
            string lang = string.IsNullOrWhiteSpace(GachaLanguage) ? System.Globalization.CultureInfo.CurrentUICulture.Name : GachaLanguage;
            await _gachaLogService.UpdateGachaInfoAsync(CurrentGameBiz, lang);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update wiki data {gameBiz}", CurrentGameBiz);
        }
    }




    [RelayCommand]
    private void OpenItemStatsPane()
    {
        SplitView_Content.IsPaneOpen = true;
    }




    #region Get Gacha



    [RelayCommand]
    private async Task UpdateGachaLogAsync(string? param = null)
    {
        try
        {
            string? url = null;
            if (param is "cache")
            {
                if (SelectUid is null or 0)
                {
                    return;
                }
                url = _gachaLogService.GetGachaLogUrlByUid(SelectUid.Value);
                if (string.IsNullOrWhiteSpace(url))
                {
                    // 无法找到 uid {uid} 的已缓存 URL
                    InAppToast.MainWindow?.Warning(null, string.Format(Lang.GachaLogPage_CannotFindSavedURLOfUid, SelectUid));
                    return;
                }
            }
            else
            {
                var path = GameLauncherService.GetGameInstallPath(CurrentGameId);
                if (!Directory.Exists(path))
                {
                    // 游戏未安装
                    InAppToast.MainWindow?.Warning(null, Lang.GachaLogPage_GameNotInstalled);
                    return;
                }
                url = _gachaLogService.GetGachaLogUrlFromWebCache(CurrentGameBiz, path);
                if (string.IsNullOrWhiteSpace(url))
                {
                    // 无法找到 URL，请在游戏中打开抽卡记录页面
                    errorCount++;
                    if (errorCount > 2 && IsGachaCacheFileExists())
                    {
                        errorCount = 0;
                        InAppToast.MainWindow?.ShowWithButton(InfoBarSeverity.Warning,
                                                                     Lang.GachaLogPage_AlwaysFailedToGetGachaRecords,
                                                                     null,
                                                                     Lang.GachaLogPage_ClearURLCacheFiles,
                                                                     () => _ = DeleteGachaCacheFileAsync());
                    }
                    else
                    {
                        InAppToast.MainWindow?.Warning(null, Lang.GachaLogPage_CannotFindURL);
                    }
                    return;
                }
            }
            await UpdateGachaLogInternalAsync(url, param is "all");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update gacha log");
            InAppToast.MainWindow?.Error(ex);
        }
    }




    private async Task UpdateGachaLogInternalAsync(string url, bool all = false)
    {
        try
        {
            var uid = await _gachaLogService.GetUidFromGachaLogUrl(url);
            var cancelSource = new CancellationTokenSource();
            var button = new Button
            {
                // 取消
                Content = Lang.Common_Cancel,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            var infoBar = new InfoBar
            {
                Severity = InfoBarSeverity.Informational,
                Background = Application.Current.Resources["CustomAcrylicBrush"] as Brush,
                ActionButton = button,
            };
            button.Click += (_, _) =>
            {
                cancelSource.Cancel();
                // 操作已取消
                infoBar.Message = Lang.GachaLogPage_OperationCanceled;
                infoBar.ActionButton = null;
            };
            InAppToast.MainWindow?.Show(infoBar);
            var progress = new Progress<string>((str) => infoBar.Message = str);
            var newUid = await _gachaLogService.GetGachaLogAsync(url, all, GachaLanguage, progress, cancelSource.Token);
            infoBar.Title = $"Uid {newUid}";
            infoBar.Severity = InfoBarSeverity.Success;
            infoBar.ActionButton = null;
            if (SelectUid == uid)
            {
                UpdateGachaTypeStats(uid);
            }
            else
            {
                if (!UidList.Contains(uid))
                {
                    UidList.Add(uid);
                }
                SelectUid = uid;
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Get gacha log canceled");
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogWarning("Request mihoyo api error: {error}", ex.Message);
            if (ex.ReturnCode == -101)
            {
                // authkey timeout
                // 请在游戏中打开抽卡记录页面后再重试
                errorCount++;
                if (errorCount > 2 && IsGachaCacheFileExists())
                {
                    errorCount = 0;
                    InAppToast.MainWindow?.ShowWithButton(InfoBarSeverity.Warning,
                                                                 Lang.GachaLogPage_AlwaysFailedToGetGachaRecords,
                                                                 null,
                                                                 Lang.GachaLogPage_ClearURLCacheFiles,
                                                                 () => _ = DeleteGachaCacheFileAsync());
                }
                else
                {
                    InAppToast.MainWindow?.Warning("Authkey Timeout", Lang.GachaLogPage_PleaseOpenTheGachaRecordsPageInGameAndTryAgain);
                }
            }
            else
            {
                InAppToast.MainWindow?.Warning(null, ex.Message);
            }
        }
    }




    [RelayCommand]
    private async Task InputUrlAsync()
    {
        try
        {
            var textbox = new TextBox();
            var dialog = new ContentDialog
            {
                // 输入 URL
                Title = Lang.GachaLogPage_InputURL,
                Content = textbox,
                // 确认
                PrimaryButtonText = Lang.Common_Confirm,
                // 取消
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var url = textbox.Text;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    await UpdateGachaLogInternalAsync(url);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Input url");
            InAppToast.MainWindow?.Error(ex);
        }
    }




    [RelayCommand]
    private void OpenCloudGameWindow()
    {
        try
        {
            new CloudGameGachaWindow { GameBiz = CurrentGameBiz }.Activate();
        }
        catch { }
    }



    #endregion



    #region Gacha Setting Panel





    [RelayCommand]
    private async Task CopyUrlAsync()
    {
        try
        {
            if (SelectUid is null or 0)
            {
                return;
            }
            var url = _gachaLogService.GetGachaLogUrlByUid(SelectUid.Value);
            if (!string.IsNullOrWhiteSpace(url))
            {
                ClipboardHelper.SetText(url);
                FontIcon_CopyUrl.Glyph = "\uE8FB"; // accept
                await Task.Delay(1000);
                FontIcon_CopyUrl.Glyph = "\uE8C8";  // copy
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy url");
        }
    }



    [RelayCommand]
    private async Task ChangeGachaItemNameAsync()
    {
        try
        {
            string lang = string.IsNullOrWhiteSpace(GachaLanguage) ? System.Globalization.CultureInfo.CurrentUICulture.Name : GachaLanguage;
            (lang, int count) = await _gachaLogService.ChangeGachaItemNameAsync(CurrentGameBiz, lang);
            InAppToast.MainWindow?.Success(null, string.Format(Lang.GachaLogPage_0GachaItemsHaveBeenChangedToLanguage1, count, lang), 5000);
            UpdateGachaTypeStats(SelectUid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change gacha item name");
        }
    }



    [RelayCommand]
    private async Task DeleteUidAsync()
    {
        try
        {
            var uid = SelectUid;
            if (uid is null or 0)
            {
                return;
            }
            var dialog = new ContentDialog
            {
                // 警告
                Title = Lang.Common_Warning,
                // 即将删除 Uid {uid} 的所有抽卡记录，此操作不可恢复。
                Content = string.Format(Lang.GachaLogPage_DeleteGachaRecordsWarning, uid),
                // 删除
                PrimaryButtonText = Lang.Common_Delete,
                // 取消
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var count = _gachaLogService.DeleteUid(uid.Value);
                // 已删除 Uid {uid} 的抽卡记录 {count} 条
                InAppToast.MainWindow?.Success(null, string.Format(Lang.GachaLogPage_DeletedGachaRecordsOfUid, count, uid));
                Initialize();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete uid");
            InAppToast.MainWindow?.Error(ex);
        }
    }



    [RelayCommand]
    private async Task DeleteUidByTimeAsync()
    {
        try
        {
            var dialog = new DeleteGachaLogDialog
            {
                CurrentGameBiz = this.CurrentGameBiz,
                DefaultUid = this.SelectUid,
                XamlRoot = this.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (dialog.Deleted)
            {
                UpdateGachaTypeStats(dialog.SelectUid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete uid");
        }
    }



    [RelayCommand]
    private async Task DeleteGachaCacheFileAsync()
    {
        try
        {
            var installPath = GameLauncherService.GetGameInstallPath(CurrentGameId);
            if (Directory.Exists(installPath))
            {
                var path = GachaLogClient.GetGachaCacheFilePath(CurrentGameBiz, installPath);
                if (File.Exists(path))
                {
                    var file = await StorageFile.GetFileFromPathAsync(path);
                    if (file != null)
                    {
                        var option = new FolderLauncherOptions();
                        option.ItemsToSelect.Add(file);
                        await Launcher.LaunchFolderAsync(await file.GetParentAsync(), option);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete gacha cache file");
        }
    }



    private bool IsGachaCacheFileExists()
    {
        try
        {
            var installPath = GameLauncherService.GetGameInstallPath(CurrentGameId);
            if (Directory.Exists(installPath))
            {
                var path = GachaLogClient.GetGachaCacheFilePath(CurrentGameBiz, installPath);
                return File.Exists(path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check gacha cache file exists");
        }
        return false;
    }



    #endregion



    #region Import & Export



    [RelayCommand]
    private async Task ExportGachaLogAsync(string format)
    {
        try
        {
            if (SelectUid is null or 0)
            {
                return;
            }
            long uid = SelectUid.Value;
            var ext = format switch
            {
                "excel" => "xlsx",
                "json" => "json",
                _ => "json"
            };
            var suggestName = $"Starward_Export_{CurrentGameBiz.Game}_{uid}_{DateTime.Now:yyyyMMddHHmmss}.{ext}";
            var file = await FileDialogHelper.OpenSaveFileDialogAsync(this.XamlRoot, suggestName, (ext, $".{ext}"));
            if (file is not null)
            {
                await _gachaLogService.ExportGachaLogAsync(uid, file, format);
                var storageFile = await StorageFile.GetFileFromPathAsync(file);
                var options = new FolderLauncherOptions();
                options.ItemsToSelect.Add(storageFile);
                await Launcher.LaunchFolderAsync(await storageFile.GetParentAsync(), options);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export gacha log");
            InAppToast.MainWindow?.Error(ex);
        }
    }




    [RelayCommand]
    private async Task ImportGachaLogAsync()
    {
        try
        {
            var file = await FileDialogHelper.PickSingleFileAsync(this.XamlRoot, ("Json", ".json"));
            if (File.Exists(file))
            {
                var uid = _gachaLogService.ImportGachaLog(file);
                if (uid == SelectUid)
                {
                    UpdateGachaTypeStats(uid);
                }
                else if (UidList.Contains(uid))
                {
                    SelectUid = uid;
                }
                else
                {
                    UidList.Add(uid);
                    SelectUid = uid;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import gacha log");
            InAppToast.MainWindow?.Error(ex);
        }
    }



    [RelayCommand]
    private void OpenUIGF4Window()
    {
        new UIGF4GachaWindow().Activate();
    }




    #endregion



    #region Layout


    private void ScrollViewer_GachaStats_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateGachaStatsCardLayout();
    }


    private void GachaStatsCard_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateGachaStatsCardLayout();
    }


    private void GachaStatsCard_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            UpdateGachaStatsCardLayout(element);
        }
    }



    private void UpdateGachaStatsCardLayout(FrameworkElement? card = null)
    {
        ContentPresenter? container = null;
        if (card is not null)
        {
            container = VisualTreeHelper.GetParent(card) as ContentPresenter;
        }
        const int CardWidth = 320;
        try
        {
            if (ItemsControl_GachaStats != null)
            {
                int count = ItemsControl_GachaStats.Items.Count;
                if (count > 0)
                {
                    double width = (Grid_GachaStats.ActualWidth - (count - 1) * 12) / count;
                    Storyboard storyboard = new();
                    if (width >= CardWidth || container is null)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (ItemsControl_GachaStats.ContainerFromIndex(i) is ContentPresenter presenter)
                            {
                                presenter.Width = width;
                            }
                        }
                    }
                    else
                    {
                        width = (Grid_GachaStats.ActualWidth - (count - 1) * 12 - CardWidth) / (count - 1);
                        for (int i = 0; i < count; i++)
                        {
                            if (ItemsControl_GachaStats.ContainerFromIndex(i) is ContentPresenter presenter)
                            {
                                DoubleAnimation ani;
                                if (presenter == container)
                                {
                                    ani = CreateDoubleAnimation(CardWidth);
                                }
                                else
                                {
                                    ani = CreateDoubleAnimation(width);
                                }
                                Storyboard.SetTarget(ani, presenter);
                                storyboard.Children.Add(ani);
                            }
                        }
                    }
                    storyboard.Begin();
                }
            }
            if (ItemsControl_ZZZGachaStats != null)
            {
                int count = ItemsControl_ZZZGachaStats.Items.Count;
                if (count > 0)
                {
                    double width = (Grid_GachaStats.ActualWidth - (count - 1) * 12) / count;
                    Storyboard storyboard = new();
                    if (width >= CardWidth || container is null)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (ItemsControl_ZZZGachaStats.ContainerFromIndex(i) is ContentPresenter presenter)
                            {
                                presenter.Width = width;
                            }
                        }
                    }
                    else
                    {
                        width = (Grid_GachaStats.ActualWidth - (count - 1) * 12 - CardWidth) / (count - 1);
                        for (int i = 0; i < count; i++)
                        {
                            if (ItemsControl_ZZZGachaStats.ContainerFromIndex(i) is ContentPresenter presenter)
                            {
                                DoubleAnimation ani;
                                if (presenter == container)
                                {
                                    ani = CreateDoubleAnimation(CardWidth);
                                }
                                else
                                {
                                    ani = CreateDoubleAnimation(width);
                                }
                                Storyboard.SetTarget(ani, presenter);
                                storyboard.Children.Add(ani);
                            }
                        }
                    }
                    storyboard.Begin();
                }
            }
        }
        catch { }
    }


    private DoubleAnimation CreateDoubleAnimation(double value)
    {
        DoubleAnimation animation = new()
        {
            To = value,
            EnableDependentAnimation = true,
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            EasingFunction = new CircleEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTargetProperty(animation, nameof(Width));
        return animation;
    }



    #endregion


}
