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
using Starward.Helpers;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class SelfQueryPage : PageBase
{


    private readonly ILogger<SelfQueryPage> _logger = AppConfig.GetLogger<SelfQueryPage>();

    private readonly SelfQueryService _selfQueryService = AppConfig.GetService<SelfQueryService>();

    private readonly DatabaseService _databaseService = AppConfig.GetService<DatabaseService>();



    public SelfQueryPage()
    {
        this.InitializeComponent();
    }




    private GameBiz gameBiz;


    public string GameIcon => gameBiz.ToGame() switch
    {
        GameBiz.GenshinImpact => "ms-appx:///Assets/Image/icon_ys.jpg",
        GameBiz.StarRail => "ms-appx:///Assets/Image/icon_sr.jpg",
        GameBiz.Honkai3rd => "ms-appx:///Assets/Image/icon_bh3.jpg",
        _ => "",
    };



    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is GameBiz biz)
        {
            if (biz is GameBiz.hk4e_cloud)
            {
                biz = GameBiz.hk4e_cn;
            }
            if (biz.ToGame() is GameBiz.GenshinImpact)
            {
                ListView_QueryItems_Genshin.Visibility = Visibility.Visible;
            }
            if (biz.ToGame() is GameBiz.StarRail)
            {
                ListView_QueryItems_StarRail.Visibility = Visibility.Visible;
            }
            gameBiz = biz;
        }
    }



    [ObservableProperty]
    private ObservableCollection<long> uidList;

    [ObservableProperty]
    private long? selectUid;
    partial void OnSelectUidChanged(long? value)
    {
        LoadQueryTypeStats();
    }

    [ObservableProperty]
    private List<TypeStats> typeStatsList;


    [ObservableProperty]
    private TypeStats selectTypeStats;
    partial void OnSelectTypeStatsChanged(TypeStats value)
    {
        if (value is null)
        {
            return;
        }
        TypeStatsChanged(value.Type);
    }


    [ObservableProperty]
    private List<string> typeStatsMonthList;


    [ObservableProperty]
    private string? selectTypeStatsMonth;
    partial void OnSelectTypeStatsMonthChanged(string? value)
    {
        if (value is null)
        {
            GenshinQueryItemList = null;
            StarRailQueryItemList = null;
        }
        else
        {
            LoadMonthQueryItems(SelectTypeStats?.Type ?? 0, value);
        }
    }


    [ObservableProperty]
    private long monthAddNum;


    [ObservableProperty]
    private long monthSubNum;



    [ObservableProperty]
    private List<GenshinQueryItem>? genshinQueryItemList;

    [ObservableProperty]
    private List<StarRailQueryItem>? starRailQueryItemList;


    [ObservableProperty]
    private SelfQueryUserInfo? queryUserInfo;


    private CancellationTokenSource tokenSource;


    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        tokenSource = new CancellationTokenSource();
        await Task.Delay(16);
        LoadUidList();
    }


    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        tokenSource?.Cancel();
    }



    private void LoadUidList()
    {
        try
        {
            if (gameBiz.ToGame() is GameBiz.GenshinImpact)
            {
                UidList = new(_selfQueryService.GetGenshinUids());
            }
            if (gameBiz.ToGame() is GameBiz.StarRail)
            {
                UidList = new(_selfQueryService.GetStarRailUids());
            }
            var info = _selfQueryService.UserInfo;
            if (info != null && info.GameBiz.ToGame() == gameBiz.ToGame())
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
            if (gameBiz.ToGame() is GameBiz.GenshinImpact)
            {
                var list = new List<TypeStats>();
                using var dapper = _databaseService.CreateConnection();
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
            if (gameBiz.ToGame() is GameBiz.StarRail)
            {
                var list = new List<TypeStats>();
                using var dapper = _databaseService.CreateConnection();
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load query type stats");
        }
    }






    [RelayCommand]
    private async Task InputURLAsync()
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
                    QueryUserInfo = await _selfQueryService.InitializeAsync(textBox.Text, gameBiz);
                    if (QueryUserInfo != null)
                    {
                        if (!UidList.Contains(QueryUserInfo.Uid))
                        {
                            UidList.Add(QueryUserInfo.Uid);
                        }
                        SelectUid = QueryUserInfo.Uid;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Input url");
            NotificationBehavior.Instance.Error(ex, "Input URL");
        }
    }







    private async void Button_UpdateSelfQueryItems_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button && button.DataContext is TypeStats stats)
            {
                stats.IsUpdating = true;
                if (gameBiz.ToGame() is GameBiz.GenshinImpact)
                {
                    await UpdateGenshinQueryItemsAsync(stats, (GenshinQueryType)stats.Type);
                }
                if (gameBiz.ToGame() is GameBiz.StarRail)
                {
                    await UpdateStarRailQueryItemsAsync(stats, (StarRailQueryType)stats.Type);
                }
                stats.IsUpdating = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Click update button");
        }
    }



    private async Task UpdateGenshinQueryItemsAsync(TypeStats stats, GenshinQueryType type)
    {
        try
        {
            try
            {
                _selfQueryService.EnsureInitialized();
            }
            catch (Exception ex) when (ex.Message is "Not initialized.")
            {
                await InputURLAsync();
            }
            var progress = new Progress<int>(i => stats.Page = i);
            (stats.Add, stats.Sub) = await _selfQueryService.UpdateGenshinQueryItemsAsync(type, progress, tokenSource.Token);
            TypeStatsChanged((int)type);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Update Genshin query items");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update genshin query items");
            NotificationBehavior.Instance.Error(ex, "Update Genshin account history.");
        }
    }



    private async Task UpdateStarRailQueryItemsAsync(TypeStats stats, StarRailQueryType type)
    {
        try
        {
            try
            {
                _selfQueryService.EnsureInitialized();
            }
            catch (Exception ex) when (ex.Message is "Not initialized.")
            {
                await InputURLAsync();
            }
            var progress = new Progress<int>(i => stats.Page = i);
            (stats.Add, stats.Sub) = await _selfQueryService.UpdateStarRailQueryItemsAsync(type, progress, tokenSource.Token);
            TypeStatsChanged((int)type);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Update Star Rail query items");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update star rail query items");
            NotificationBehavior.Instance.Error(ex, "Update Star Rail account history.");
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
            using var dapper = _databaseService.CreateConnection();
            if (gameBiz.ToGame() is GameBiz.GenshinImpact)
            {
                TypeStatsMonthList = dapper.Query<string>("""
                    SELECT DISTINCT STRFTIME('%Y-%m', DateTime) FROM GenshinQueryItem WHERE Type=@type ORDER BY DateTime DESC;
                    """, new { uid, type }).ToList();
            }
            if (gameBiz.ToGame() is GameBiz.StarRail)
            {
                TypeStatsMonthList = dapper.Query<string>("""
                    SELECT DISTINCT STRFTIME('%Y-%m', Time) FROM StarRailQueryItem WHERE Type=@type ORDER BY Time DESC;
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
            using var dapper = _databaseService.CreateConnection();
            if (gameBiz.ToGame() is GameBiz.GenshinImpact)
            {
                GenshinQueryItemList = dapper.Query<GenshinQueryItem>("""
                    SELECT * FROM GenshinQueryItem WHERE Uid=@uid AND Type=@type AND DateTime LIKE @month ORDER BY DateTime DESC;
                    """, new { uid, type, month = month + "%" }).ToList();
                MonthAddNum = GenshinQueryItemList.Where(x => x.AddNum > 0).Sum(x => x.AddNum);
                MonthSubNum = GenshinQueryItemList.Where(x => x.AddNum < 0).Sum(x => x.AddNum);
            }
            if (gameBiz.ToGame() is GameBiz.StarRail)
            {
                StarRailQueryItemList = dapper.Query<StarRailQueryItem>("""
                    SELECT * FROM StarRailQueryItem WHERE Uid=@uid AND Type=@type AND Time LIKE @month ORDER BY Time DESC;
                    """, new { uid, type, month = month + "%" }).ToList();
                MonthAddNum = StarRailQueryItemList.Where(x => x.AddNum > 0).Sum(x => x.AddNum);
                MonthSubNum = StarRailQueryItemList.Where(x => x.AddNum < 0).Sum(x => x.AddNum);
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
            if (gameBiz.ToGame() is GameBiz.GenshinImpact)
            {
                type = ((GenshinQueryType)SelectTypeStats.Type).ToLocalization();
            }
            if (gameBiz.ToGame() is GameBiz.StarRail)
            {
                type = ((StarRailQueryType)SelectTypeStats.Type).ToLocalization();
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
                using var dapper = _databaseService.CreateConnection();
                int count = 0;
                if (gameBiz.ToGame() is GameBiz.GenshinImpact)
                {
                    count = dapper.Execute("""
                        DELETE FROM GenshinQueryItem WHERE Uid=@uid AND Type=@Type AND DateTime LIKE @time;
                        """, new { uid, SelectTypeStats.Type, time = month + "%" });
                }
                if (gameBiz.ToGame() is GameBiz.StarRail)
                {
                    count = dapper.Execute("""
                        DELETE FROM StarRailQueryItem WHERE Uid=@uid AND Type=@Type AND Time LIKE @time;
                        """, new { uid, SelectTypeStats.Type, time = month + "%" });
                }
                NotificationBehavior.Instance.Success(string.Format(Lang.SelfQueryPage_DeleteThisMonthSData_DeleteSuccessful, count, type, month));
                TypeStatsChanged(SelectTypeStats.Type);
            }
        }
        catch (Exception ex)
        {
            NotificationBehavior.Instance.Error(ex);
            _logger.LogError(ex, "Delete records");
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
