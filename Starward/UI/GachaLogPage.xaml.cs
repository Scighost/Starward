// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Helper;
using Starward.Model;
using Starward.Service;
using Starward.Service.Gacha;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.UI;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class GachaLogPage : Page
{

    private readonly ILogger<GachaLogPage> _logger = AppConfig.GetLogger<GachaLogPage>();

    private readonly GameService _gameService = AppConfig.GetService<GameService>();

    private GachaLogService _gachaLogService;


    private GameBiz gameBiz;


    public GachaLogPage()
    {
        this.InitializeComponent();
        if (ShowNoviceGacha)
        {
            Grid_Star5List.ColumnDefinitions.Add(new ColumnDefinition());
        }
    }


    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is GameBiz biz)
        {
            gameBiz = biz;
            Title = GachaLogService.GetGachaLogText(biz);
            if (biz is GameBiz.hk4e_cn or GameBiz.hk4e_global or GameBiz.hk4e_cloud)
            {
                _gachaLogService = AppConfig.GetService<GenshinGachaService>();
            }
            if (biz is GameBiz.hkrpg_cn or GameBiz.hkrpg_global)
            {
                _gachaLogService = AppConfig.GetService<StarRailGachaService>();
            }
        }
    }


    [ObservableProperty]
    private string title;



    [ObservableProperty]
    private ObservableCollection<int> uidList;


    [ObservableProperty]
    private int selectUid;
    partial void OnSelectUidChanged(int value)
    {
        AppConfig.SelectUidInGachaLogPage = value;
        UpdateGachaTypeStats(value);
    }




    [ObservableProperty]
    private bool showNoviceGacha = AppConfig.ShowNoviceGacha;
    partial void OnShowNoviceGachaChanged(bool value)
    {
        AppConfig.ShowNoviceGacha = value;
        if (value && Grid_Star5List.ColumnDefinitions.Count == 3)
        {
            Grid_Star5List.ColumnDefinitions.Add(new ColumnDefinition());
        }
        if (!value && Grid_Star5List.ColumnDefinitions.Count == 4)
        {
            Grid_Star5List.ColumnDefinitions.RemoveAt(3);
        }
    }


    [ObservableProperty]
    private string? gachaLanguage = AppConfig.GachaLanguage;
    partial void OnGachaLanguageChanged(string? value)
    {
        AppConfig.GachaLanguage = value;
    }


    [ObservableProperty]
    private GachaTypeStats? gachaTypeStats1;

    [ObservableProperty]
    private GachaTypeStats? gachaTypeStats2;

    [ObservableProperty]
    private GachaTypeStats? gachaTypeStats3;

    [ObservableProperty]
    private GachaTypeStats? gachaTypeStats4;


    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(16);
        Initialize();
    }



    private void Initialize()
    {
        try
        {
            UidList = new(_gachaLogService.GetUids());
            var lastUid = AppConfig.SelectUidInGachaLogPage;
            if (UidList.Contains(lastUid))
            {
                SelectUid = lastUid;
            }
            else
            {
                SelectUid = UidList.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize");
        }
    }



    private void UpdateGachaTypeStats(int uid)
    {
        try
        {
            if (uid == 0)
            {
                GachaTypeStats1 = null;
                GachaTypeStats2 = null;
                GachaTypeStats3 = null;
                GachaTypeStats4 = null;
            }
            else
            {
                var stats = _gachaLogService.GetGachaTypeStats(uid);
                GachaTypeStats1 = stats.ElementAtOrDefault(0);
                GachaTypeStats2 = stats.ElementAtOrDefault(1);
                GachaTypeStats3 = stats.ElementAtOrDefault(2);
                GachaTypeStats4 = stats.ElementAtOrDefault(3);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateGachaTypeStats");
        }
    }






    [RelayCommand]
    private async Task UpdateGachaLogAsync(string? param = null)
    {
        try
        {
            string? url = null;
            if (param is "cache")
            {
                if (SelectUid == 0)
                {
                    return;
                }
                url = _gachaLogService.GetGachaLogUrlByUid(SelectUid);
                if (string.IsNullOrWhiteSpace(url))
                {
                    NotificationBehavior.Instance.Warning(null, $"Cannot find cached URL of uid {SelectUid}.");
                    return;
                }
            }
            else
            {
                var path = _gameService.GetGameInstallPath(gameBiz);
                if (!Directory.Exists(path))
                {
                    NotificationBehavior.Instance.Warning("", $"Cannot find game install path ");
                    return;
                }
                url = _gachaLogService.GetGachaLogUrlFromWebCache(gameBiz, path);
                if (string.IsNullOrWhiteSpace(url))
                {
                    NotificationBehavior.Instance.Warning("", $"Cannot find URL");
                    return;
                }
            }
            await UpdateGachaLogInternalAsync(url, param is "all");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update gacha log");
            NotificationBehavior.Instance.Error(ex);
        }
    }




    private async Task UpdateGachaLogInternalAsync(string url, bool all = false)
    {
        try
        {
            var uid = await _gachaLogService.GetUidFromGachaLogUrl(url);
            var infoBar = new InfoBar
            {
                Severity = InfoBarSeverity.Informational,
                Background = Application.Current.Resources["CustomAcrylicBrush"] as Brush
            };
            NotificationBehavior.Instance.Show(infoBar);
            var progress = new Progress<string>((str) => infoBar.Message = str);
            var newUid = await _gachaLogService.GetGachaLogAsync(url, all, GachaLanguage, progress);
            infoBar.Title = $"Uid {newUid}";
            infoBar.Severity = InfoBarSeverity.Success;
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
        catch (MihoyoApiException ex)
        {
            _logger.LogWarning("Request mihoyo api error: {error}", ex.Message);
            if (ex.ReturnCode == -101)
            {
                // authkey timeout
                NotificationBehavior.Instance.Warning("Authkey Timeout", $"请在游戏中打开{Title}页面后再重试");
            }
            else
            {
                NotificationBehavior.Instance.Warning(null, ex.Message);
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
                Title = "输入 URL",
                Content = textbox,
                PrimaryButtonText = "确认",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = MainWindow.Current.Content.XamlRoot,
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
            NotificationBehavior.Instance.Error(ex);
        }
    }



    [RelayCommand]
    private async Task DeleteUidAsync()
    {
        try
        {
            var uid = SelectUid;
            if (uid == 0)
            {
                return;
            }
            var dialog = new ContentDialog
            {
                Title = "警告",
                Content = $"即将删除 Uid {uid} 所有的{Title}，此操作不可恢复。",
                PrimaryButtonText = "删除",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = MainWindow.Current.Content.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var count = _gachaLogService.DeleteUid(uid);
                NotificationBehavior.Instance.Success(null, $"已删除 Uid {uid} 的{Title} {count} 条");
                SelectUid = UidList.FirstOrDefault(x => x != uid);
                UidList.Remove(uid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete uid");
            NotificationBehavior.Instance.Error(ex);
        }
    }




    [RelayCommand]
    private async Task ExportWarpRecordAsync(string format)
    {
        // todo
        try
        {
            if (SelectUid == 0)
            {
                return;
            }
            int uid = SelectUid;
            var ext = format switch
            {
                "excel" => ".xlsx",
                "json" => ".json",
                _ => ".json"
            };
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.Downloads,
                SuggestedFileName = $"Stardward_Export_WarpRecords_{uid}_{DateTime.Now:yyyyMMddHHmmss}",
            };
            picker.FileTypeChoices.Add(format, new string[] { ext });
            InitializeWithWindow.Initialize(picker, MainWindow.Current.HWND);
            var file = await picker.PickSaveFileAsync();
            if (file is not null)
            {
                _gachaLogService.ExportGachaLog(uid, file.Path, format);
                var options = new FolderLauncherOptions();
                options.ItemsToSelect.Add(file);
                NotificationBehavior.Instance.ShowWithButton(InfoBarSeverity.Success, "成功导出", file.Name, "打开文件夹", async () => await Launcher.LaunchFolderAsync(await file.GetParentAsync(), options));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export gacha log");
            NotificationBehavior.Instance.Error(ex);
        }
    }




}
