using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Frameworks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.Gacha.Endfield;

public sealed partial class EndfieldGachaPage : PageBase
{
    private static readonly Dictionary<string, string> CharacterPoolNames = new()
    {
        ["E_CharacterGachaPoolType_Special"] = "特许寻访",
        ["E_CharacterGachaPoolType_Joint"] = "辉光庆典",
        ["E_CharacterGachaPoolType_Standard"] = "基础寻访",
        ["E_CharacterGachaPoolType_Beginner"] = "启程寻访",
    };

    private readonly ILogger<EndfieldGachaPage> _logger = AppConfig.GetLogger<EndfieldGachaPage>();
    private readonly EndfieldGachaService _service = AppConfig.GetService<EndfieldGachaService>();
    private List<EndfieldGachaItem> _allItems = [];
    private List<EndfieldAccountRecordItem> _allAccountRecords = [];
    private string _accountRecordsAccountKey = "";
    private CancellationTokenSource? _syncCancellationTokenSource;
    private CancellationTokenSource? _gachaInfoCancellationTokenSource;

    public EndfieldGachaPage()
    {
        InitializeComponent();
        InitializeAccountRecordFilters();
    }

    public ObservableCollection<EndfieldGachaAccount> Accounts { get; } = [];

    public ObservableCollection<EndfieldGachaPoolStats> GachaStats { get; } = [];

    public ObservableCollection<EndfieldAccountRecordItem> AccountRecords { get; } = [];

    public ObservableCollection<EndfieldAccountRecordSummary> AccountRecordSummaries { get; } = [];

    public ObservableCollection<EndfieldAccountRecordFilterOption> AccountRecordFilters { get; } = [];

    public EndfieldGachaAccount? SelectedAccount
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                LoadSelectedAccount();
                if (value is not null)
                {
                    LoadLocalAccountRecords(value);
                }
            }
        }
    }

    public int SelectedViewIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(IsGachaView));
                OnPropertyChanged(nameof(IsAccountRecordView));
                if (IsAccountRecordView && SelectedAccount is not null)
                {
                    if (SelectedAccount.AccountKey != _accountRecordsAccountKey)
                    {
                        LoadLocalAccountRecords(SelectedAccount);
                    }
                }
            }
        }
    }

    public int SelectedRecordTypeIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                LoadSelectedAccount();
            }
        }
    }

    public int SelectedAccountRecordCategoryIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                AccountRecordListTitle = value >= 0 && value < AccountRecordFilters.Count
                    ? AccountRecordFilters[value].Name
                    : "账号记录";
                FilterAccountRecords();
            }
        }
    }

    public bool IsSyncing { get; set => SetProperty(ref field, value); }

    public bool IsEmpty { get; set => SetProperty(ref field, value); } = true;

    public bool IsAccountRecordEmpty { get; set => SetProperty(ref field, value); } = true;

    public bool IsStatusOpen { get; set => SetProperty(ref field, value); }

    public string StatusText { get; set => SetProperty(ref field, value); } = "";

    public InfoBarSeverity StatusSeverity { get; set => SetProperty(ref field, value); } = InfoBarSeverity.Informational;

    public string AccountRecordUpdatedText { get; set => SetProperty(ref field, value); } = "尚未同步";

    public string AccountRecordListTitle { get; set => SetProperty(ref field, value); } = "理智";

    public bool IsGachaView => SelectedViewIndex == 0;

    public bool IsAccountRecordView => SelectedViewIndex == 1;

    private string CurrentRecordType => SelectedRecordTypeIndex == 1 ? "weapon" : "char";

    protected override void OnLoaded()
    {
        LoadAccounts();
        _gachaInfoCancellationTokenSource?.Cancel();
        _gachaInfoCancellationTokenSource?.Dispose();
        _gachaInfoCancellationTokenSource = new CancellationTokenSource();
        if (Accounts.Count > 0)
        {
            _ = UpdateGachaInfoAsync(_gachaInfoCancellationTokenSource.Token);
        }
    }

    protected override void OnUnloaded()
    {
        _syncCancellationTokenSource?.Cancel();
        _syncCancellationTokenSource?.Dispose();
        _syncCancellationTokenSource = null;
        _gachaInfoCancellationTokenSource?.Cancel();
        _gachaInfoCancellationTokenSource?.Dispose();
        _gachaInfoCancellationTokenSource = null;
    }

    private async Task UpdateGachaInfoAsync(CancellationToken cancellationToken)
    {
        try
        {
            bool updated = await _service.UpdateGachaInfoAsync(cancellationToken);
            if (updated && !cancellationToken.IsCancellationRequested)
            {
                LoadSelectedAccount();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to update Endfield gacha icons: {ErrorType}", ex.GetType().Name);
        }
    }

    private void LoadAccounts(string? selectedAccountKey = null)
    {
        selectedAccountKey ??= SelectedAccount?.AccountKey;
        List<EndfieldGachaAccount> accounts = _service.GetAccounts();
        Accounts.Clear();
        foreach (EndfieldGachaAccount account in accounts)
        {
            Accounts.Add(account);
        }
        SelectedAccount = Accounts.FirstOrDefault(x => x.AccountKey == selectedAccountKey) ?? Accounts.FirstOrDefault();
        if (SelectedAccount is null)
        {
            _allItems.Clear();
            GachaStats.Clear();
            IsEmpty = true;
            ClearAccountRecords();
        }
    }

    private void LoadSelectedAccount()
    {
        if (SelectedAccount is null)
        {
            return;
        }
        _allItems = _service.GetItems(SelectedAccount.AccountKey, CurrentRecordType);
        BuildGachaStats();
    }

    private void ClearAccountRecords()
    {
        _allAccountRecords.Clear();
        _accountRecordsAccountKey = "";
        AccountRecords.Clear();
        AccountRecordSummaries.Clear();
        AccountRecordUpdatedText = "尚未同步";
        IsAccountRecordEmpty = true;
        UpdateAccountRecordFilterCounts();
    }

    private void InitializeAccountRecordFilters()
    {
        AddFilter("stamina", "理智", "\uE946");
        AddFilter("currency:1", "源石", "\uE8C7");
        AddFilter("currency:2", "嵌晶玉", "\uE8C7");
        AddFilter("currency:3", "武库配额", "\uE8C7");
        AddFilter("battle_pass", "协议通行证", "\uE8D7");
        AddFilter("monthly_card", "月卡", "\uE8C7");
        AddFilter("mail", "邮件", "\uE715");
        AddFilter("login", "登录", "\uE77B");
        SelectedAccountRecordCategoryIndex = -1;
        SelectedAccountRecordCategoryIndex = 0;

        void AddFilter(string key, string name, string glyph)
        {
            AccountRecordFilters.Add(new EndfieldAccountRecordFilterOption
            {
                Key = key,
                Name = name,
                Glyph = glyph,
                Icon = EndfieldGachaService.GetAccountRecordIcon(key),
            });
        }
    }

    private void LoadLocalAccountRecords(EndfieldGachaAccount account)
    {
        try
        {
            ApplyAccountRecords(account, _service.GetAccountRecords(account.AccountKey));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to load local Endfield account records: {ErrorType}", ex.GetType().Name);
            ClearAccountRecords();
        }
    }

    private void ApplyAccountRecords(EndfieldGachaAccount account, EndfieldAccountRecordSyncResult result)
    {
        _allAccountRecords = result.Items.ToList();
        _accountRecordsAccountKey = account.AccountKey;
        AccountRecordSummaries.Clear();
        foreach (EndfieldAccountRecordSummary summary in result.Summaries)
        {
            AccountRecordSummaries.Add(summary);
        }
        AccountRecordUpdatedText = result.SyncTime == DateTimeOffset.MinValue
            ? "尚未同步"
            : $"更新于 {result.SyncTime.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
        UpdateAccountRecordFilterCounts();
        FilterAccountRecords();
    }

    private void UpdateAccountRecordFilterCounts()
    {
        foreach (EndfieldAccountRecordFilterOption option in AccountRecordFilters)
        {
            option.Count = _allAccountRecords.Count(x => x.RecordType == option.Key);
        }
    }

    private void FilterAccountRecords()
    {
        string recordType = SelectedAccountRecordCategoryIndex >= 0 &&
            SelectedAccountRecordCategoryIndex < AccountRecordFilters.Count
            ? AccountRecordFilters[SelectedAccountRecordCategoryIndex].Key
            : "stamina";
        AccountRecords.Clear();
        foreach (EndfieldAccountRecordItem item in _allAccountRecords.Where(x => x.RecordType == recordType))
        {
            AccountRecords.Add(item);
        }
        IsAccountRecordEmpty = AccountRecords.Count == 0;
    }

    private void BuildGachaStats()
    {
        GachaStats.Clear();
        List<EndfieldGachaPoolStats> stats = CurrentRecordType == "char"
            ? BuildCharacterStats()
            : BuildWeaponStats();
        foreach (EndfieldGachaPoolStats item in stats)
        {
            GachaStats.Add(item);
        }
        IsEmpty = stats.Count == 0;
        DispatcherQueue.TryEnqueue(UpdateGachaStatsCardLayout);
    }

    private List<EndfieldGachaPoolStats> BuildCharacterStats()
    {
        var result = new List<EndfieldGachaPoolStats>();

        List<EndfieldGachaItem> specialItems = SortAscending(_allItems
            .Where(x => x.PoolType == "E_CharacterGachaPoolType_Special"));
        if (specialItems.Count > 0)
        {
            var batches = new List<List<EndfieldGachaItem>>();
            foreach (EndfieldGachaItem item in specialItems)
            {
                if (batches.Count == 0 || batches[^1][0].PoolId != item.PoolId)
                {
                    batches.Add([]);
                }
                batches[^1].Add(item);
            }
            int pity6 = 0;
            int pity5 = 0;
            var cards = new List<EndfieldGachaPoolStats>();
            foreach (List<EndfieldGachaItem> batch in batches)
            {
                string name = GetPoolName(batch, CharacterPoolNames["E_CharacterGachaPoolType_Special"]);
                cards.Add(CreatePoolStats($"special:{batch[0].PoolId}", name, batch, ref pity6, ref pity5, true));
            }
            cards.Reverse();
            result.AddRange(cards);
        }

        IEnumerable<IGrouping<string, EndfieldGachaItem>> jointPools = _allItems
            .Where(x => x.PoolType == "E_CharacterGachaPoolType_Joint")
            .GroupBy(x => x.PoolId)
            .OrderByDescending(x => x.Max(item => ParseTimestamp(item.GachaTime)));
        foreach (IGrouping<string, EndfieldGachaItem> pool in jointPools)
        {
            int pity6 = 0;
            int pity5 = 0;
            List<EndfieldGachaItem> items = pool.ToList();
            result.Add(CreatePoolStats($"joint:{pool.Key}",
                GetPoolName(items, CharacterPoolNames["E_CharacterGachaPoolType_Joint"]),
                items, ref pity6, ref pity5, true));
        }

        foreach (string poolType in CharacterPoolNames.Keys
            .Where(x => x is not "E_CharacterGachaPoolType_Special" and not "E_CharacterGachaPoolType_Joint")
            .OrderBy(GetCharacterPoolOrder))
        {
            List<EndfieldGachaItem> items = _allItems.Where(x => x.PoolType == poolType).ToList();
            if (items.Count == 0)
            {
                continue;
            }
            int pity6 = 0;
            int pity5 = 0;
            result.Add(CreatePoolStats($"type:{poolType}", CharacterPoolNames[poolType], items,
                ref pity6, ref pity5, false));
        }
        return result;
    }

    private List<EndfieldGachaPoolStats> BuildWeaponStats()
    {
        var result = new List<EndfieldGachaPoolStats>();
        IEnumerable<IGrouping<string, EndfieldGachaItem>> pools = _allItems
            .GroupBy(x => string.IsNullOrWhiteSpace(x.PoolId) ? x.PoolName : x.PoolId)
            .OrderByDescending(x => x.Max(item => ParseTimestamp(item.GachaTime)));
        foreach (IGrouping<string, EndfieldGachaItem> pool in pools)
        {
            int pity6 = 0;
            int pity5 = 0;
            List<EndfieldGachaItem> items = pool.ToList();
            result.Add(CreatePoolStats($"weapon:{pool.Key}", GetPoolName(items, pool.Key), items,
                ref pity6, ref pity5, false));
        }
        return result;
    }

    private static EndfieldGachaPoolStats CreatePoolStats(string key, string name,
        IEnumerable<EndfieldGachaItem> source, ref int pity6, ref int pity5, bool excludeFreePulls)
    {
        List<EndfieldGachaItem> items = SortAscending(source);
        foreach (EndfieldGachaItem item in items)
        {
            bool countsForPity = !excludeFreePulls || !item.IsFree;
            if (countsForPity)
            {
                pity6++;
                pity5++;
            }
            if (item.Rarity == 6)
            {
                item.Pity = countsForPity ? pity6 : 0;
                if (countsForPity)
                {
                    pity6 = 0;
                }
            }
            else if (item.Rarity == 5)
            {
                item.Pity = countsForPity ? pity5 : 0;
                if (countsForPity)
                {
                    pity5 = 0;
                }
            }
        }

        List<EndfieldGachaItem> list6 = items.Where(x => x.Rarity == 6).Reverse().ToList();
        List<EndfieldGachaItem> list5 = items.Where(x => x.Rarity == 5).Reverse().ToList();
        var stats = new EndfieldGachaPoolStats
        {
            Key = key,
            Name = name,
            Count = items.Count,
            StartTimeText = items[0].TimeText,
            EndTimeText = items[^1].TimeText,
            Count6 = list6.Count,
            Count5 = list5.Count,
            Count4 = items.Count(x => x.Rarity == 4),
            Pity6 = pity6,
            Pity5 = pity5,
            Average6 = list6.Where(x => !x.IsFree).Select(x => x.Pity).DefaultIfEmpty().Average(),
            List6 = list6,
            List5 = list5,
        };
        stats.List6.Insert(0, CreatePityItem(6, pity6, items[^1].GachaTime));
        stats.List5.Insert(0, CreatePityItem(5, pity5, items[^1].GachaTime));
        return stats;
    }

    private static EndfieldGachaItem CreatePityItem(int rarity, int pity, string time)
    {
        return new EndfieldGachaItem
        {
            ItemName = "保底",
            Rarity = rarity,
            Pity = pity,
            GachaTime = time,
            IsPityPlaceholder = true,
        };
    }

    private static List<EndfieldGachaItem> SortAscending(IEnumerable<EndfieldGachaItem> items)
    {
        return items.OrderBy(x => ParseTimestamp(x.GachaTime))
            .ThenBy(x => x.SeqId.Length)
            .ThenBy(x => x.SeqId, StringComparer.Ordinal)
            .ToList();
    }

    private static string GetPoolName(List<EndfieldGachaItem> items, string fallback)
    {
        return items.LastOrDefault(x => !string.IsNullOrWhiteSpace(x.PoolName))?.PoolName ?? fallback;
    }

    private static long ParseTimestamp(string value)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result) ? result : 0;
    }

    private static int GetCharacterPoolOrder(string poolType) => poolType switch
    {
        "E_CharacterGachaPoolType_Special" => 0,
        "E_CharacterGachaPoolType_Joint" => 1,
        "E_CharacterGachaPoolType_Standard" => 2,
        "E_CharacterGachaPoolType_Beginner" => 3,
        _ => 4,
    };

    private void UpdateGachaStatsCardLayout()
    {
        try
        {
            int count = ItemsControl_GachaStats.Items.Count;
            if (count == 0)
            {
                return;
            }
            double width = (ScrollViewer_GachaStats.ActualWidth - 40 - (count - 1) * 12) / count;
            width = Math.Clamp(width, 262, double.MaxValue);
            for (int i = 0; i < count; i++)
            {
                if (ItemsControl_GachaStats.ContainerFromIndex(i) is ContentPresenter presenter)
                {
                    presenter.Width = width;
                }
            }
        }
        catch
        {
        }
    }

    private void GachaStatsCard_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateGachaStatsCardLayout();
    }

    private void GachaStatsCard_Unloaded(object sender, RoutedEventArgs e)
    {
        UpdateGachaStatsCardLayout();
    }

    private void ScrollViewer_GachaStats_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateGachaStatsCardLayout();
    }

    [RelayCommand]
    private async Task SyncGachaAsync()
    {
        if (IsSyncing)
        {
            return;
        }
        if (SelectedAccount is not null && _service.HasSavedLoginToken(SelectedAccount.AccountKey))
        {
            EndfieldGachaAccount account = SelectedAccount;
            await ExecuteSyncAsync((progress, token) => _service.SyncAccountAsync(account, progress, token));
            return;
        }
        await LoginAccountInternalAsync();
    }

    [RelayCommand]
    private async Task LoginAccountAsync()
    {
        if (!IsSyncing)
        {
            await LoginAccountInternalAsync();
        }
    }

    private async Task LoginAccountInternalAsync(bool syncAccountRecords = false)
    {
        try
        {
            var dialog = new EndfieldLoginDialog { XamlRoot = XamlRoot };
            await dialog.ShowAsync();
            if (!string.IsNullOrWhiteSpace(dialog.LoginToken))
            {
                await AddAccountAndSyncAsync(dialog.LoginToken, syncAccountRecords);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to login Hypergryph account: {ErrorType}", ex.GetType().Name);
            StatusSeverity = InfoBarSeverity.Error;
            StatusText = ex.Message;
            IsStatusOpen = true;
        }
    }

    [RelayCommand]
    private async Task InputLoginTokenAsync()
    {
        if (IsSyncing)
        {
            return;
        }
        var passwordBox = new PasswordBox
        {
            MinWidth = 360,
            PlaceholderText = "登录 Token",
            PasswordRevealMode = PasswordRevealMode.Peek,
        };
        var dialog = new ContentDialog
        {
            Title = "输入鹰角账号登录 Token",
            Content = passwordBox,
            PrimaryButtonText = "确定",
            SecondaryButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(passwordBox.Password))
        {
            await AddAccountAndSyncAsync(passwordBox.Password);
        }
    }

    private async Task AddAccountAndSyncAsync(string loginToken, bool syncAccountRecords = false)
    {
        PrepareSync();
        try
        {
            var progress = new Progress<string>(text => StatusText = text);
            List<EndfieldGachaAccount> accounts = await _service.AddAccountsFromLoginTokenAsync(
                loginToken, progress, _syncCancellationTokenSource!.Token);
            EndfieldGachaAccount account = accounts[0];
            LoadAccounts(account.AccountKey);
            EndfieldAccountRecordSyncResult? accountRecordResult = null;
            string? accountRecordError = null;
            if (syncAccountRecords)
            {
                try
                {
                    accountRecordResult = await _service.SyncAccountRecordsAsync(
                        account, progress, _syncCancellationTokenSource.Token);
                    ApplyAccountRecords(account, accountRecordResult);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    accountRecordError = ex.Message;
                    _logger.LogWarning("Failed to sync Endfield account records after login: {ErrorType}",
                        ex.GetType().Name);
                }
            }
            EndfieldGachaSyncResult result = await _service.SyncAccountAsync(
                account, progress, _syncCancellationTokenSource.Token);
            LoadAccounts(result.Account.AccountKey);
            if (!syncAccountRecords)
            {
                ShowSyncResult(result);
            }
            else if (accountRecordResult is not null)
            {
                ShowSyncResult(result, accountRecordResult);
            }
            else
            {
                StatusSeverity = InfoBarSeverity.Warning;
                StatusText = $"寻访记录已同步，账号记录读取失败：{accountRecordError}";
            }
        }
        catch (OperationCanceledException)
        {
            StatusSeverity = InfoBarSeverity.Informational;
            StatusText = "同步已取消。";
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to add Endfield account: {ErrorType}", ex.GetType().Name);
            StatusSeverity = InfoBarSeverity.Error;
            StatusText = ex.Message;
        }
        finally
        {
            IsSyncing = false;
        }
    }

    [RelayCommand]
    private async Task SyncAccountRecordsAsync()
    {
        if (IsSyncing)
        {
            return;
        }
        if (SelectedAccount is null || !_service.HasSavedLoginToken(SelectedAccount.AccountKey))
        {
            await LoginAccountInternalAsync(syncAccountRecords: true);
            return;
        }
        await ExecuteAccountRecordSyncAsync(SelectedAccount);
    }

    private async Task ExecuteAccountRecordSyncAsync(EndfieldGachaAccount account)
    {
        PrepareSync();
        try
        {
            var progress = new Progress<string>(text => StatusText = text);
            EndfieldAccountRecordSyncResult result = await _service.SyncAccountRecordsAsync(
                account, progress, _syncCancellationTokenSource!.Token);
            if (SelectedAccount?.AccountKey == account.AccountKey)
            {
                ApplyAccountRecords(account, result);
            }
            StatusSeverity = result.FailedCategories.Length == 0
                ? InfoBarSeverity.Success
                : InfoBarSeverity.Warning;
            StatusText = result.FailedCategories.Length == 0
                ? $"账号记录同步完成，共读取 {result.Items.Count} 条。"
                : $"账号记录已部分更新，以下分类读取失败：{string.Join("、", result.FailedCategories)}。";
            IsStatusOpen = true;
        }
        catch (OperationCanceledException)
        {
            StatusSeverity = InfoBarSeverity.Informational;
            StatusText = "同步已取消。";
            IsStatusOpen = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to sync Endfield account records: {ErrorType}", ex.GetType().Name);
            StatusSeverity = InfoBarSeverity.Error;
            StatusText = ex.Message;
            IsStatusOpen = true;
        }
        finally
        {
            IsSyncing = false;
        }
    }

    [RelayCommand]
    private async Task SyncFromGameLogAsync()
    {
        if (!IsSyncing)
        {
            await ExecuteSyncAsync((progress, token) => _service.SyncFromGameLogAsync(progress, token));
        }
    }

    private async Task ExecuteSyncAsync(
        Func<IProgress<string>, CancellationToken, Task<EndfieldGachaSyncResult>> syncOperation)
    {
        PrepareSync();
        try
        {
            var progress = new Progress<string>(text => StatusText = text);
            EndfieldGachaSyncResult result = await syncOperation(progress, _syncCancellationTokenSource!.Token);
            LoadAccounts(result.Account.AccountKey);
            ShowSyncResult(result);
        }
        catch (OperationCanceledException)
        {
            StatusSeverity = InfoBarSeverity.Informational;
            StatusText = "同步已取消。";
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to sync Endfield gacha records: {ErrorType}", ex.GetType().Name);
            StatusSeverity = InfoBarSeverity.Error;
            StatusText = ex.Message;
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private void PrepareSync()
    {
        _syncCancellationTokenSource?.Dispose();
        _syncCancellationTokenSource = new CancellationTokenSource();
        IsSyncing = true;
        IsStatusOpen = true;
        StatusSeverity = InfoBarSeverity.Informational;
        StatusText = "正在准备同步";
    }

    private void ShowSyncResult(EndfieldGachaSyncResult result)
    {
        if (result.FailedPools.Length == 0)
        {
            StatusSeverity = InfoBarSeverity.Success;
            StatusText = $"同步完成：新增 {result.NewCount} 条，共 {result.TotalCount} 条记录。";
        }
        else
        {
            StatusSeverity = InfoBarSeverity.Warning;
            StatusText = $"同步完成，但以下卡池获取失败：{string.Join("、", result.FailedPools)}。请稍后重试。";
        }
    }

    private void ShowSyncResult(EndfieldGachaSyncResult result, EndfieldAccountRecordSyncResult accountRecordResult)
    {
        if (result.FailedPools.Length == 0 && accountRecordResult.FailedCategories.Length == 0)
        {
            StatusSeverity = InfoBarSeverity.Success;
            StatusText = $"同步完成：寻访新增 {result.NewCount} 条，账号记录 {accountRecordResult.Items.Count} 条。";
            return;
        }

        var failures = new List<string>();
        failures.AddRange(result.FailedPools);
        failures.AddRange(accountRecordResult.FailedCategories);
        StatusSeverity = InfoBarSeverity.Warning;
        StatusText = $"同步已部分完成，以下内容读取失败：{string.Join("、", failures.Distinct())}。";
    }

    [RelayCommand]
    private void CancelSync()
    {
        _syncCancellationTokenSource?.Cancel();
    }
}
