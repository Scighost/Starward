using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.SelfQuery;
using Starward.Features.Database;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Starward.Features.SelfQuery;

public sealed partial class SelfQueryPage : PageBase
{


    private readonly ILogger<SelfQueryPage> _logger = AppConfig.GetLogger<SelfQueryPage>();

    private readonly SelfQueryService _selfQueryService = AppConfig.GetService<SelfQueryService>();



    public SelfQueryPage()
    {
        this.InitializeComponent();
    }




    public string GameIcon => CurrentGameBiz.Game switch
    {
        GameBiz.hk4e => "ms-appx:///Assets/Image/icon_ys.jpg",
        GameBiz.hkrpg => "ms-appx:///Assets/Image/icon_sr.jpg",
        GameBiz.bh3 => "ms-appx:///Assets/Image/icon_bh3.jpg",
        GameBiz.nap => "ms-appx:///Assets/Image/icon_zzz.jpg",
        _ => "",
    };



    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (CurrentGameBiz.Game == GameBiz.hk4e)
        {
            ListView_QueryItems_Genshin.Visibility = Visibility.Visible;
        }
        if (CurrentGameBiz.Game == GameBiz.hkrpg)
        {
            ListView_QueryItems_StarRail.Visibility = Visibility.Visible;
        }
        if (CurrentGameBiz.Game == GameBiz.nap)
        {
            ListView_QueryItems_ZZZ.Visibility = Visibility.Visible;
        }
    }



    public ObservableCollection<long> UidList { get; set => SetProperty(ref field, value); }

    public long? SelectUid
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                LoadQueryTypeStats();
            }
        }
    }


    public List<TypeStats> TypeStatsList { get; set => SetProperty(ref field, value); }


    public TypeStats SelectTypeStats
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                if (value is not null)
                {
                    TypeStatsChanged(value.Type);
                }
            }
        }
    }



    public List<string> TypeStatsMonthList { get; set => SetProperty(ref field, value); }


    public string? SelectTypeStatsMonth
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                if (value is null)
                {
                    GenshinQueryItemList = null;
                    StarRailQueryItemList = null;
                    ZZZQueryItemList = null;
                }
                else
                {
                    LoadMonthQueryItems(SelectTypeStats?.Type ?? 0, value);
                }
            }
        }
    }



    private long MonthAddNum { get; set => SetProperty(ref field, value); }


    private long MonthSubNum { get; set => SetProperty(ref field, value); }



    private List<GenshinQueryItem>? GenshinQueryItemList { get; set => SetProperty(ref field, value); }

    private List<StarRailQueryItem>? StarRailQueryItemList { get; set => SetProperty(ref field, value); }

    private List<ZZZQueryItem>? ZZZQueryItemList { get; set => SetProperty(ref field, value); }

    private SelfQueryUserInfo? QueryUserInfo { get; set => SetProperty(ref field, value); }



    private CancellationTokenSource tokenSource;



    protected override async void OnLoaded()
    {
        tokenSource = new CancellationTokenSource();
        await Task.Delay(16);
        LoadUidList();
    }



    protected override void OnUnloaded()
    {
        tokenSource?.Cancel();
        GenshinQueryItemList = null;
        StarRailQueryItemList = null;
        ZZZQueryItemList = null;
        QueryUserInfo = null;
        SelectTypeStats = null!;
        TypeStatsList = null!;
    }




    private void LoadUidList()
    {
        try
        {
            if (CurrentGameBiz.ToGame() == GameBiz.hk4e)
            {
                UidList = new(_selfQueryService.GetGenshinUids());
            }
            if (CurrentGameBiz.ToGame() == GameBiz.hkrpg)
            {
                UidList = new(_selfQueryService.GetStarRailUids());
            }
            if (CurrentGameBiz.ToGame() == GameBiz.nap)
            {
                UidList = new(_selfQueryService.GetZZZUids());
            }
            var info = _selfQueryService.UserInfo;
            if (info != null && info.GameBiz.ToGame() == CurrentGameBiz.ToGame())
            {
                QueryUserInfo = info;
                if (!UidList.Contains(QueryUserInfo.Uid))
                {
                    UidList.Add(QueryUserInfo.Uid);
                }
                SelectUid = QueryUserInfo.Uid;
            }
            else
            {
                _selfQueryService.Reset();
                if (UidList != null && UidList.Count > 0)
                {
                    SelectUid = UidList[0];
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load uid list");
        }
    }





    private void LoadQueryTypeStats()
    {
        try
        {
            if (SelectUid is null or 0)
            {
                return;
            }
            long uid = SelectUid.Value;
            if (CurrentGameBiz.ToGame() == GameBiz.hk4e)
            {
                var list = new List<TypeStats>();
                using var dapper = DatabaseService.CreateConnection();
                foreach (var i in Enumerable.Range(1, 5))
                {
                    var type = (GenshinQueryType)i;
                    (long add, long sub) = _selfQueryService.GetGenshinQueryItemsNumSum(uid, type);
                    list.Add(new TypeStats
                    {
                        Type = (int)type,
                        Icon = TypeToIcon(type),
                        Add = add,
                        Sub = sub,
                        Name = type.ToLocalization(),
                    });
                }
                TypeStatsList = list;
            }
            if (CurrentGameBiz.ToGame() == GameBiz.hkrpg)
            {
                var list = new List<TypeStats>();
                using var dapper = DatabaseService.CreateConnection();
                foreach (var i in Enumerable.Range(1, 5))
                {
                    var type = (StarRailQueryType)i;
                    (long add, long sub) = _selfQueryService.GetStarRailQueryItemsNumSum(uid, type);
                    list.Add(new TypeStats
                    {
                        Type = (int)type,
                        Icon = TypeToIcon(type),
                        Add = add,
                        Sub = sub,
                        Name = type.ToLocalization(),
                    });
                }
                TypeStatsList = list;
            }
            if (CurrentGameBiz.ToGame() == GameBiz.nap)
            {
                var list = new List<TypeStats>();
                using var dapper = DatabaseService.CreateConnection();
                foreach (var i in Enumerable.Range(1, 6))
                {
                    var type = (ZZZQueryType)i;
                    (long add, long sub) = _selfQueryService.GetZZZQueryItemsNumSum(uid, type);
                    list.Add(new TypeStats
                    {
                        Type = (int)type,
                        Icon = TypeToIcon(type),
                        Add = add,
                        Sub = sub,
                        Name = type.ToLocalization(),
                    });
                }
                TypeStatsList = list;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load query type stats");
        }
    }






    [RelayCommand]
    private async Task<bool> InputURLAsync()
    {
        try
        {
            var stackPanel = new StackPanel { Spacing = 4 };
            stackPanel.Children.Add(new TextBlock
            {
                Text = Lang.SelfQueryPage_StepsToGetURL,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush,
            });
            stackPanel.Children.Add(new TextBlock
            {
                Text = Lang.SelfQueryPage_InputURLSteps,
                TextWrapping = TextWrapping.Wrap,
            });
            stackPanel.Children.Add(new TextBlock
            {
                Text = Lang.SelfQueryPage_InputURLNote,

                TextWrapping = TextWrapping.Wrap,
                Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as Brush,
            });
            var textBox = new TextBox { Margin = new Thickness(0, 8, 0, 0) };
            stackPanel.Children.Add(textBox);
            var dialog = new ContentDialog
            {
                Title = Lang.GachaLogPage_InputURL,
                Content = stackPanel,
                PrimaryButtonText = Lang.Common_Confirm,
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };
            if (await dialog.ShowAsync() is ContentDialogResult.Primary)
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    QueryUserInfo = await _selfQueryService.InitializeAsync(textBox.Text, CurrentGameBiz);
                    if (QueryUserInfo != null)
                    {
                        if (!UidList.Contains(QueryUserInfo.Uid))
                        {
                            UidList.Add(QueryUserInfo.Uid);
                        }
                        SelectUid = QueryUserInfo.Uid;
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Input url");
            InAppToast.MainWindow?.Error(ex, "Input URL");
        }
        return false;
    }







    private async void Button_UpdateSelfQueryItems_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button && button.DataContext is TypeStats stats)
            {
                if (stats.IsUpdating)
                {
                    return;
                }
                stats.IsUpdating = true;
                if (CurrentGameBiz.ToGame() == GameBiz.hk4e)
                {
                    await UpdateGenshinQueryItemsAsync(stats, (GenshinQueryType)stats.Type);
                }
                if (CurrentGameBiz.ToGame() == GameBiz.hkrpg)
                {
                    await UpdateStarRailQueryItemsAsync(stats, (StarRailQueryType)stats.Type);
                }
                if (CurrentGameBiz.ToGame() == GameBiz.nap)
                {
                    await UpdateZZZQueryItemsAsync(stats, (ZZZQueryType)stats.Type);
                }
                stats.IsUpdating = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Click update button");
        }
    }



    private async Task UpdateGenshinQueryItemsAsync(TypeStats stats, GenshinQueryType type, bool all = false)
    {
        try
        {
            try
            {
                _selfQueryService.EnsureInitialized();
            }
            catch (Exception ex) when (ex.Message is "Not initialized.")
            {
                if (!await InputURLAsync())
                {
                    return;
                }
            }
            var progress = new Progress<int>(i => stats.Page = i);
            (stats.Add, stats.Sub) = await _selfQueryService.UpdateGenshinQueryItemsAsync(type, progress, all, tokenSource.Token);
            TypeStatsChanged((int)type);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Update Genshin query items");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update genshin query items");
            InAppToast.MainWindow?.Error(ex, "Update Genshin account history.");
        }
    }



    private async Task UpdateStarRailQueryItemsAsync(TypeStats stats, StarRailQueryType type, bool all = false)
    {
        try
        {
            try
            {
                _selfQueryService.EnsureInitialized();
            }
            catch (Exception ex) when (ex.Message is "Not initialized.")
            {
                if (!await InputURLAsync())
                {
                    return;
                }
            }
            var progress = new Progress<int>(i => stats.Page = i);
            (stats.Add, stats.Sub) = await _selfQueryService.UpdateStarRailQueryItemsAsync(type, progress, all, tokenSource.Token);
            TypeStatsChanged((int)type);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Update Star Rail query items");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update star rail query items");
            InAppToast.MainWindow?.Error(ex, "Update Star Rail account history.");
        }
    }



    private async Task UpdateZZZQueryItemsAsync(TypeStats stats, ZZZQueryType type, bool all = false)
    {
        try
        {
            try
            {
                _selfQueryService.EnsureInitialized();
            }
            catch (Exception ex) when (ex.Message is "Not initialized.")
            {
                if (!await InputURLAsync())
                {
                    return;
                }
            }
            var progress = new Progress<int>(i => stats.Page = i);
            (stats.Add, stats.Sub) = await _selfQueryService.UpdateZZZQueryItemsAsync(type, progress, all, tokenSource.Token);
            TypeStatsChanged((int)type);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Update ZZZ query items");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update zzz query items");
            InAppToast.MainWindow?.Error(ex, "Update ZZZ account history.");
        }
    }




    private void TypeStatsChanged(int type)
    {
        try
        {
            MonthAddNum = 0;
            MonthSubNum = 0;
            if (SelectUid is null)
            {
                return;
            }
            long uid = SelectUid.Value;
            using var dapper = DatabaseService.CreateConnection();
            if (CurrentGameBiz.ToGame() == GameBiz.hk4e)
            {
                TypeStatsMonthList = dapper.Query<string>("""
                    SELECT DISTINCT STRFTIME('%Y-%m', DateTime) FROM GenshinQueryItem WHERE Type=@type ORDER BY DateTime DESC;
                    """, new { uid, type }).ToList();
            }
            if (CurrentGameBiz.ToGame() == GameBiz.hkrpg)
            {
                TypeStatsMonthList = dapper.Query<string>("""
                    SELECT DISTINCT STRFTIME('%Y-%m', Time) FROM StarRailQueryItem WHERE Type=@type ORDER BY Time DESC;
                    """, new { uid, type }).ToList();
            }
            if (CurrentGameBiz.ToGame() == GameBiz.nap)
            {
                TypeStatsMonthList = dapper.Query<string>("""
                    SELECT DISTINCT STRFTIME('%Y-%m', DateTime) FROM ZZZQueryItem WHERE Type=@type ORDER BY DateTime DESC;
                    """, new { uid, type }).ToList();
            }
            SelectTypeStatsMonth = TypeStatsMonthList?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TypeStatsChanged");
        }
    }



    private void LoadMonthQueryItems(int type, string month)
    {
        try
        {
            if (SelectUid is null)
            {
                return;
            }
            long uid = SelectUid.Value;
            using var dapper = DatabaseService.CreateConnection();
            if (CurrentGameBiz.ToGame() == GameBiz.hk4e)
            {
                GenshinQueryItemList = dapper.Query<GenshinQueryItem>("""
                    SELECT * FROM GenshinQueryItem WHERE Uid=@uid AND Type=@type AND DateTime LIKE @month ORDER BY DateTime DESC;
                    """, new { uid, type, month = month + "%" }).ToList();
                MonthAddNum = GenshinQueryItemList.Where(x => x.AddNum > 0).Sum(x => x.AddNum);
                MonthSubNum = GenshinQueryItemList.Where(x => x.AddNum < 0).Sum(x => x.AddNum);
            }
            if (CurrentGameBiz.ToGame() == GameBiz.hkrpg)
            {
                StarRailQueryItemList = dapper.Query<StarRailQueryItem>("""
                    SELECT * FROM StarRailQueryItem WHERE Uid=@uid AND Type=@type AND Time LIKE @month ORDER BY Time DESC;
                    """, new { uid, type, month = month + "%" }).ToList();
                MonthAddNum = StarRailQueryItemList.Where(x => x.AddNum > 0).Sum(x => x.AddNum);
                MonthSubNum = StarRailQueryItemList.Where(x => x.AddNum < 0).Sum(x => x.AddNum);
            }
            if (CurrentGameBiz.ToGame() == GameBiz.nap)
            {
                var list = ZZZQueryItemList = dapper.Query<ZZZQueryItem>("""
                    SELECT * FROM ZZZQueryItem WHERE Uid=@uid AND Type=@type AND DateTime LIKE @month ORDER BY DateTime DESC;
                    """, new { uid, type, month = month + "%" }).ToList();
                if (type is (int)ZZZQueryType.PurchaseGift)
                {
                    foreach (var item in list)
                    {
                        item.Reason = item.ItemName;
                    }
                }
                ZZZQueryItemList = list;
                MonthAddNum = ZZZQueryItemList.Where(x => x.AddNum > 0).Sum(x => x.AddNum);
                MonthSubNum = ZZZQueryItemList.Where(x => x.AddNum < 0).Sum(x => x.AddNum);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoadMonthQueryItems");
        }
    }



    [RelayCommand]
    private async Task DeleteCurrentMonthData()
    {
        try
        {
            if (SelectUid is null || SelectTypeStats is null || SelectTypeStatsMonth is null)
            {
                return;
            }
            long uid = SelectUid.Value;
            string type = "";
            string month = SelectTypeStatsMonth;
            if (CurrentGameBiz.ToGame() == GameBiz.hk4e)
            {
                type = ((GenshinQueryType)SelectTypeStats.Type).ToLocalization();
            }
            if (CurrentGameBiz.ToGame() == GameBiz.hkrpg)
            {
                type = ((StarRailQueryType)SelectTypeStats.Type).ToLocalization();
            }
            if (CurrentGameBiz.ToGame() == GameBiz.nap)
            {
                type = ((ZZZQueryType)SelectTypeStats.Type).ToLocalization();
            }
            var dialog = new ContentDialog
            {
                Title = Lang.SelfQueryPage_DeleteThisMonthSData,
                Content = string.Format(Lang.SelfQueryPage_DeleteThisMonthSData_DialogContent, type, month),
                PrimaryButtonText = Lang.Common_Delete,
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot,
            };
            if (await dialog.ShowAsync() is ContentDialogResult.Primary)
            {
                _logger.LogInformation($"Ready to delete records of {type} in {month}.");
                using var dapper = DatabaseService.CreateConnection();
                int count = 0;
                if (CurrentGameBiz.ToGame() == GameBiz.hk4e)
                {
                    count = dapper.Execute("""
                        DELETE FROM GenshinQueryItem WHERE Uid=@uid AND Type=@Type AND DateTime LIKE @time;
                        """, new { uid, SelectTypeStats.Type, time = month + "%" });
                }
                if (CurrentGameBiz.ToGame() == GameBiz.hkrpg)
                {
                    count = dapper.Execute("""
                        DELETE FROM StarRailQueryItem WHERE Uid=@uid AND Type=@Type AND Time LIKE @time;
                        """, new { uid, SelectTypeStats.Type, time = month + "%" });
                }
                if (CurrentGameBiz.ToGame() == GameBiz.nap)
                {
                    count = dapper.Execute("""
                        DELETE FROM ZZZQueryItem WHERE Uid=@uid AND Type=@Type AND DateTime LIKE @time;
                        """, new { uid, SelectTypeStats.Type, time = month + "%" });
                }
                InAppToast.MainWindow?.Success(string.Format(Lang.SelfQueryPage_DeleteThisMonthSData_DeleteSuccessful, count, type, month));
                TypeStatsChanged(SelectTypeStats.Type);
            }
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex);
            _logger.LogError(ex, "Delete records");
        }
    }


    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task UpdateAllQueryItemsAsync()
    {

        try
        {
            if (SelectUid is null)
            {
                return;
            }
            if (SelectTypeStats is null)
            {
                return;
            }
            var stats = SelectTypeStats;
            if (stats.IsUpdating)
            {
                return;
            }
            stats.IsUpdating = true;
            if (CurrentGameBiz.ToGame() == GameBiz.hk4e)
            {
                await UpdateGenshinQueryItemsAsync(stats, (GenshinQueryType)stats.Type, true);
            }
            if (CurrentGameBiz.ToGame() == GameBiz.hkrpg)
            {
                await UpdateStarRailQueryItemsAsync(stats, (StarRailQueryType)stats.Type, true);
            }
            if (CurrentGameBiz.ToGame() == GameBiz.nap)
            {
                await UpdateZZZQueryItemsAsync(stats, (ZZZQueryType)stats.Type, true);
            }
            stats.IsUpdating = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update all query items");
        }
    }



    private static string TypeToIcon(GenshinQueryType type)
    {
        return type switch
        {
            GenshinQueryType.Crystal => "ms-appx:///Assets/Image/UI_ItemIcon_203.png",
            GenshinQueryType.Primogem => "ms-appx:///Assets/Image/UI_ItemIcon_201.png",
            GenshinQueryType.Resin => "ms-appx:///Assets/Image/UI_ItemIcon_210.png",
            GenshinQueryType.Artifact => "ms-appx:///Assets/Image/SpriteUI_BagTabIcon_Equip.png",
            GenshinQueryType.Weapon => "ms-appx:///Assets/Image/SpriteUI_BagTabIcon_Weapon.png",
            _ => "",
        };
    }



    private static string TypeToIcon(StarRailQueryType type)
    {
        return type switch
        {
            StarRailQueryType.Stellar => "ms-appx:///Assets/Image/900001.png",
            StarRailQueryType.Dreams => "ms-appx:///Assets/Image/3.png",
            StarRailQueryType.Relic => "ms-appx:///Assets/Image/IconAvatarRelic.png",
            StarRailQueryType.Cone => "ms-appx:///Assets/Image/IconAvatarLightCone.png",
            StarRailQueryType.Power => "ms-appx:///Assets/Image/11.png",
            _ => "",
        };
    }



    private static string TypeToIcon(ZZZQueryType type)
    {
        return type switch
        {
            ZZZQueryType.Monochrome => "ms-appx:///Assets/Image/IconCurrency02.png",
            ZZZQueryType.Ploychrome => "ms-appx:///Assets/Image/IconCurrency.png",
            ZZZQueryType.PurchaseGift => "ms-appx:///Assets/Image/GiftpackWegineBig.png",
            ZZZQueryType.Battery => "ms-appx:///Assets/Image/IconStamina.png",
            ZZZQueryType.Engine => "ms-appx:///Assets/Image/3593482e8866f0529e8a247772e02cf4_5418014644502214835.png",
            ZZZQueryType.Disk => "ms-appx:///Assets/Image/222103265483a5389ab8e589a81b8f29_6239774837764585524.png",
            _ => "",
        };
    }



    public class TypeStats : ObservableObject
    {

        public int Type { get; set; }

        public string Name { get; set; }

        public string Icon { get; set; }


        private long _Add;
        public long Add
        {
            get => _Add;
            set => SetProperty(ref _Add, value);
        }


        private long _Sub;
        public long Sub
        {
            get => _Sub;
            set => SetProperty(ref _Sub, value);
        }


        private bool _IsUpdating;
        public bool IsUpdating
        {
            get => _IsUpdating;
            set => SetProperty(ref _IsUpdating, value);
        }


        private int _Page;
        public int Page
        {
            get => _Page;
            set => SetProperty(ref _Page, value);
        }


    }


}
