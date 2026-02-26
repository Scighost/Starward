using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.GameRecord;
using Starward.Features.Gacha.UIGF;
using Starward.Features.GameLauncher;
using Starward.Features.GameRecord;
using Starward.Features.ViewHost;
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

    private readonly GameRecordService _gameRecordService = AppConfig.GetService<GameRecordService>();


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
            MenuFlyoutItem_SyncFromMiyoushe.Visibility = Visibility.Visible;
            MenuFlyoutItem_SyncFromMiyousheAll.Visibility = Visibility.Visible;
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
    public partial long? SelectUid { get; set; }
    partial void OnSelectUidChanged(long? value)
    {
        AppConfig.SetLastUidInGachaLogPage(CurrentGameBiz.Game, value ?? 0);
        UpdateGachaTypeStats(value);
    }




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
        Grid_GachaStats.PointerWheelChanged += Grid_GachaStats_PointerWheelChanged;
        Initialize();
        await UpdateWikiDataAsync();
    }



    protected override void OnUnloaded()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        Grid_GachaStats.PointerWheelChanged -= Grid_GachaStats_PointerWheelChanged;
        if (DisplayGachaTypeStatsCollection is not null)
        {
            DisplayGachaTypeStatsCollection.Clear();
            DisplayGachaTypeStatsCollection = null!;
        }
        if (GachaItemStats is not null)
        {
            GachaItemStats.Clear();
            GachaItemStats = null;
        }
        ListView_GachaBanners.SelectionChanged -= ListView_GachaBanners_SelectionChanged;
        if (GachaBanners is not null)
        {
            GachaBanners.Clear();
            GachaBanners = null!;
        }
    }



    private void Initialize()
    {
        try
        {
            InitializeGachaBanners();
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



    private void Grid_GachaStats_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(Grid_GachaStats).Properties;
        if (properties.IsHorizontalMouseWheel || InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            var delta = properties.MouseWheelDelta;
            ScrollViewer_GachaStats.ChangeView(ScrollViewer_GachaStats.HorizontalOffset - delta, null, null);
            e.Handled = true;
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




    #region Gacha Stats



    public bool EnableGenshinGachaItemStats { get; set => SetProperty(ref field, value); }

    public bool EnableStarRailGachaItemStats { get; set => SetProperty(ref field, value); }

    public bool EnableZZZGachaItemStats { get; set => SetProperty(ref field, value); }

    public List<GachaBanner> GachaBanners { get; set => SetProperty(ref field, value); }


    public ObservableCollection<GachaTypeStats> DisplayGachaTypeStatsCollection { get; set => SetProperty(ref field, value); }


    public List<GachaLogItemEx>? GachaItemStats { get; set => SetProperty(ref field, value); }


    private List<GachaTypeStats>? gachaTypeStats;


    private int errorCount = 0;


    private void InitializeGachaBanners()
    {
        GachaBanners = _gachaLogService.QueryGachaTypes.Select(x => new GachaBanner(x)).ToList();
        string? banner = AppConfig.GetDisplayGachaBanners(CurrentGameBiz.Game);
        if (!string.IsNullOrWhiteSpace(banner))
        {
            foreach (var item in banner.Split(','))
            {
                if (int.TryParse(item, out int type))
                {
                    if (GachaBanners.FirstOrDefault(x => x.Value == type) is GachaBanner gachaType)
                    {
                        ListView_GachaBanners.SelectedItems.Add(gachaType);
                    }
                }
            }
        }
        if (ListView_GachaBanners.SelectedItems.Count == 0)
        {
            foreach (var item in GachaBanners)
            {
                ListView_GachaBanners.SelectedItems.Add(item);
            }
        }
        if (CurrentGameBiz.Game is GameBiz.hkrpg && !AppConfig.GetValue(false, "SavedStarRailBannersAfterCollaborationStarting"))
        {
            if (ListView_GachaBanners.SelectedItems.Cast<GachaBanner>().FirstOrDefault(x => x.Value == 21) is null)
            {
                ListView_GachaBanners.SelectedItems.Add(GachaBanners.FirstOrDefault(x => x.Value == 21));
            }
            if (ListView_GachaBanners.SelectedItems.Cast<GachaBanner>().FirstOrDefault(x => x.Value == 22) is null)
            {
                ListView_GachaBanners.SelectedItems.Add(GachaBanners.FirstOrDefault(x => x.Value == 22));
            }
        }
        if (CurrentGameBiz.Game is GameBiz.nap && !AppConfig.GetValue(false, "SavedZZZBannersSinceVersion2"))
        {
            if (ListView_GachaBanners.SelectedItems.Cast<GachaBanner>().FirstOrDefault(x => x.Value == 102) is null)
            {
                ListView_GachaBanners.SelectedItems.Add(GachaBanners.FirstOrDefault(x => x.Value == 102));
            }
            if (ListView_GachaBanners.SelectedItems.Cast<GachaBanner>().FirstOrDefault(x => x.Value == 103) is null)
            {
                ListView_GachaBanners.SelectedItems.Add(GachaBanners.FirstOrDefault(x => x.Value == 103));
            }
        }
        ListView_GachaBanners.SelectionChanged -= ListView_GachaBanners_SelectionChanged;
        ListView_GachaBanners.SelectionChanged += ListView_GachaBanners_SelectionChanged;
    }


    private void ListView_GachaBanners_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            string value = string.Join(',', ListView_GachaBanners.SelectedItems.Cast<GachaBanner>().Select(x => x.Value));
            AppConfig.SetDisplayGachaBanners(CurrentGameBiz.Game, value);
            if (CurrentGameBiz.Game is GameBiz.hkrpg)
            {
                AppConfig.SetValue(true, "SavedStarRailBannersAfterCollaborationStarting");
            }
            if (CurrentGameBiz.Game is GameBiz.nap)
            {
                AppConfig.SetValue(true, "SavedZZZBannersSinceVersion2.5");
            }
            UpdateDisplayGachaTypeStats();
        }
        catch { }
    }



    private void UpdateGachaTypeStats(long? uid)
    {
        try
        {
            if (uid is null or 0)
            {
                gachaTypeStats = null;
                DisplayGachaTypeStatsCollection = [];
                GachaItemStats = null;
                StackPanel_Emoji.Visibility = Visibility.Visible;
            }
            else
            {
                (gachaTypeStats, GachaItemStats) = _gachaLogService.GetGachaTypeStats(uid.Value);
                UpdateDisplayGachaTypeStats();
                StackPanel_Emoji.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateGachaTypeStats");
        }
    }


    private void UpdateDisplayGachaTypeStats()
    {
        if (gachaTypeStats is null)
        {
            return;
        }
        DisplayGachaTypeStatsCollection ??= [];
        DisplayGachaTypeStatsCollection.Clear();
        var list = ListView_GachaBanners.SelectedItems.Cast<GachaBanner>().ToList();
        if (list.Count == 0)
        {
            list = GachaBanners;
        }
        foreach (var item in list)
        {
            if (gachaTypeStats.FirstOrDefault(x => x.GachaType == item.Value) is GachaTypeStats stats)
            {
                DisplayGachaTypeStatsCollection.Add(stats);
            }
        }
    }


    private void UpdateGachaStatsCardLayout()
    {
        try
        {
            if (ItemsControl_GachaStats != null)
            {
                int count = ItemsControl_GachaStats.Items.Count;
                if (count > 0)
                {
                    double width = (ScrollViewer_GachaStats.ActualWidth - 40 - (count - 1) * 12) / count;
                    width = Math.Clamp(width, 262, double.MaxValue);
                    for (int i = 0; i < count; i++)
                    {
                        var a = ItemsControl_GachaStats.ContainerFromIndex(i);
                        if (ItemsControl_GachaStats.ContainerFromIndex(i) is ContentPresenter presenter)
                        {
                            presenter.Width = width;
                        }
                    }
                }
            }
            if (ItemsControl_ZZZGachaStats != null)
            {
                int count = ItemsControl_ZZZGachaStats.Items.Count;
                if (count > 0)
                {
                    double width = (ScrollViewer_GachaStats.ActualWidth - 40 - (count - 1) * 12) / count;
                    width = Math.Clamp(width, 262, double.MaxValue);
                    for (int i = 0; i < count; i++)
                    {
                        if (ItemsControl_ZZZGachaStats.ContainerFromIndex(i) is ContentPresenter presenter)
                        {
                            presenter.Width = width;
                        }
                    }
                }
            }
        }
        catch { }
    }


    private void GachaStatsCard_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateGachaStatsCardLayout();
    }


    private void GachaStatsCard_Unloaded(object sender, RoutedEventArgs e)
    {
        UpdateGachaStatsCardLayout();
    }



    #endregion



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
            // 原铁 -101 绝 -1
            if (ex.ReturnCode is -101 or -1)
            {
                // authkey timeout
                // 请在游戏中打开抽卡记录页面后再重试
                errorCount++;
                if (errorCount > 1 && IsGachaCacheFileExists())
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
    private async Task SyncFromMiyousheAsync(string? param = null)
    {
        try
        {
            if (CurrentGameBiz.Game != GameBiz.nap)
            {
                InAppToast.MainWindow?.Warning(null, Lang.GachaLogPage_OnlySupportZZZCNServer);
                return;
            }
            if (_gachaLogService is not ZZZGachaService zzzGachaService)
            {
                throw new InvalidOperationException($"Current gacha service type is {_gachaLogService.GetType().Name}.");
            }
            GameBiz roleGameBiz = CurrentGameBiz == GameBiz.nap_bilibili ? GameBiz.nap_cn : CurrentGameBiz;
            var roles = _gameRecordService.GetGameRoles(roleGameBiz);
            if (roles.Count == 0)
            {
                InAppToast.MainWindow?.Warning(null, Lang.GachaLogPage_PleaseLoginMiyousheAndAddZZZRole);
                WeakReferenceMessenger.Default.Send(new MainViewNavigateMessage(typeof(GameRecordPage)));
                return;
            }
            GameRecordRole? role;
            if (roles.Count == 1)
            {
                role = roles[0];
            }
            else
            {
                role = await SelectMiyousheRoleAsync(roles, roleGameBiz);
                if (role is null)
                {
                    return;
                }
            }
            if (string.IsNullOrWhiteSpace(role.Cookie))
            {
                InAppToast.MainWindow?.Warning(null, Lang.GachaLogPage_CurrentAccountMissingCookiePleaseReloginMiyoushe);
                return;
            }
            _gameRecordService.SetLastSelectGachaSyncRole(roleGameBiz, role);
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
            var uid = await zzzGachaService.GetGachaLogByGameRecordAsync(role, param is "all", GachaLanguage, progress, cancelSource.Token);
            infoBar.Title = $"Uid {uid}";
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
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Sync zzz gacha record from miyoushe canceled");
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogWarning(ex, "Sync zzz gacha record from miyoushe error ({retcode})", ex.ReturnCode);
            InAppToast.MainWindow?.Warning(null, ex.Message);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "Sync zzz gacha record from miyoushe is not supported");
            InAppToast.MainWindow?.Warning(null, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync zzz gacha record from miyoushe");
            InAppToast.MainWindow?.Error(ex);
        }
    }

    private async Task<GameRecordRole?> SelectMiyousheRoleAsync(List<GameRecordRole> roles, GameBiz roleGameBiz)
    {
        var items = roles.Select(role => new MiyousheRoleItem(role)).ToList();
        MiyousheRoleItem? selectedItem = null;
        if (SelectUid is long currentUid && currentUid > 0)
        {
            selectedItem = items.FirstOrDefault(x => x.Role.Uid == currentUid);
        }
        var selectedRole = _gameRecordService.GetLastSelectGachaSyncRoleOrTheFirstOne(roleGameBiz);
        selectedItem ??= items.FirstOrDefault(x =>
            selectedRole is not null
            && x.Role.Uid == selectedRole.Uid);
        var comboBox = new ComboBox
        {
            MinWidth = 420,
            ItemsSource = items,
            DisplayMemberPath = nameof(MiyousheRoleItem.DisplayText),
            SelectedItem = selectedItem ?? items.FirstOrDefault(),
        };
        var panel = new StackPanel
        {
            Spacing = 8,
        };
        panel.Children.Add(new TextBlock
        {
            Text = Lang.GachaLogPage_SelectMiyousheRoleDescription,
            TextWrapping = TextWrapping.Wrap,
        });
        panel.Children.Add(comboBox);
        var dialog = new ContentDialog
        {
            Title = Lang.GachaLogPage_SelectMiyousheRole,
            Content = panel,
            PrimaryButtonText = Lang.Common_Confirm,
            SecondaryButtonText = Lang.Common_Cancel,
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = comboBox.SelectedItem is not null,
            XamlRoot = this.XamlRoot,
        };
        comboBox.SelectionChanged += (_, _) => dialog.IsPrimaryButtonEnabled = comboBox.SelectedItem is not null;
        if (await dialog.ShowAsync() is not ContentDialogResult.Primary)
        {
            return null;
        }
        if (comboBox.SelectedItem is not MiyousheRoleItem item)
        {
            InAppToast.MainWindow?.Warning(null, Lang.GachaLogPage_PleaseSelectMiyousheRole);
            return null;
        }
        return item.Role;
    }


    private sealed class MiyousheRoleItem
    {
        public GameRecordRole Role { get; }

        public string DisplayText { get; }

        public MiyousheRoleItem(GameRecordRole role)
        {
            Role = role;
            DisplayText = $"{role.Nickname} ({role.Uid}) | {role.RegionName}";
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


}
