// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Core.Gacha.StarRail;
using Starward.Helper;
using Starward.Model;
using Starward.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Display.Core;
using Windows.Storage;
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
public sealed partial class WarpRecordPage : Page
{


    private readonly WarpRecordService _warpRecordService;


    public WarpRecordPage()
    {
        this.InitializeComponent();
        _warpRecordService = ServiceProvider.GetService<WarpRecordService>();
        if (ShowDepatureWarp)
        {
            Grid_Star5List.ColumnDefinitions.Add(new ColumnDefinition());
        }
    }



    [ObservableProperty]
    private ObservableCollection<int> uidList;


    [ObservableProperty]
    private int selectUid;
    partial void OnSelectUidChanged(int value)
    {
        AppConfig.SelectUidInWarpRecordPage = value;
        UpdateWarpTypeStats(value);
    }


    [ObservableProperty]
    private int selectServerInPage;


    [ObservableProperty]
    private bool showDepatureWarp = AppConfig.ShowDepatureWarp;
    partial void OnShowDepatureWarpChanged(bool value)
    {
        AppConfig.ShowDepatureWarp = value;
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
    private string? warpLanguage = AppConfig.WarpLanguage;
    partial void OnWarpLanguageChanged(string? value)
    {
        AppConfig.WarpLanguage = value;
    }


    [ObservableProperty]
    private WarpTypeStats? stellarWarp;

    [ObservableProperty]
    private WarpTypeStats? departureWarp;

    [ObservableProperty]
    private WarpTypeStats? characterEventWarp;

    [ObservableProperty]
    private WarpTypeStats? lightConeEventWarp;


    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(16);
        Initialize();
    }



    private void Initialize()
    {
        try
        {
            UidList = new(_warpRecordService.GetUids());
            var lastUid = AppConfig.SelectUidInWarpRecordPage;
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

        }
    }



    private void UpdateWarpTypeStats(int uid)
    {
        try
        {
            if (uid == 0)
            {
                StellarWarp = null;
                DepartureWarp = null;
                CharacterEventWarp = null;
                LightConeEventWarp = null;
            }
            else
            {
                var stats = _warpRecordService.GetWarpTypeStats(uid);
                StellarWarp = stats.FirstOrDefault(x => x.WarpType == WarpType.Stellar);
                DepartureWarp = stats.FirstOrDefault(x => x.WarpType == WarpType.Departure);
                CharacterEventWarp = stats.FirstOrDefault(x => x.WarpType == WarpType.CharacterEvent);
                LightConeEventWarp = stats.FirstOrDefault(x => x.WarpType == WarpType.LightConeEvent);
            }
        }
        catch (Exception ex)
        {

        }
    }






    [RelayCommand]
    private async Task UpdateWarpRecordAsync(string? param = null)
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
                url = _warpRecordService.GetUrlByUid(SelectUid);
                if (string.IsNullOrWhiteSpace(url))
                {
                    NotificationBehavior.Instance.Warning(null, $"Cannot find cached URL of uid {SelectUid}.");
                    return;
                }
            }
            else
            {
                var serverIndex = SelectServerInPage - 1;
                if (serverIndex < 0)
                {
                    serverIndex = AppConfig.GameServerIndex;
                }
                var path = GameService.GetGameInstallPath((RegionType)serverIndex);
                if (!Directory.Exists(path))
                {
                    NotificationBehavior.Instance.Warning("", $"Cannot find game install path (server {(serverIndex == 1 ? "OS" : "CN")})");
                    return;
                }
                url = _warpRecordService.GetWarpRecordUrlFromWebCache(path);
                if (string.IsNullOrWhiteSpace(url))
                {
                    NotificationBehavior.Instance.Warning("", $"Cannot find URL (server {(serverIndex == 1 ? "OS" : "CN")})");
                    return;
                }
            }
            await UpdateWarpRecordInternalAsync(url, param is "all");
        }
        catch (Exception ex)
        {
            NotificationBehavior.Instance.Error(ex);
        }
    }




    private async Task UpdateWarpRecordInternalAsync(string url, bool all = false)
    {
        try
        {
            var uid = await _warpRecordService.GetUidFromWarpRecordUrl(url);
            var infoBar = new InfoBar
            {
                Title = $"Uid {uid}",
                Severity = InfoBarSeverity.Informational,
                Background = Application.Current.Resources["CustomAcrylicBrush"] as Brush
            };
            NotificationBehavior.Instance.Show(infoBar);
            var progress = new Progress<string>((str) => infoBar.Message = str);
            await _warpRecordService.GetWarpRecordAsync(url, all, WarpLanguage, progress);
            infoBar.Severity = InfoBarSeverity.Success;
            if (SelectUid == uid)
            {
                UpdateWarpTypeStats(uid);
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
            if (ex.ReturnCode == -101)
            {
                // authkey timeout
                NotificationBehavior.Instance.Warning("Authkey Timeout", "Please open warp records page in game.");
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
                Title = "Input URL",
                Content = textbox,
                PrimaryButtonText = "OK",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = MainWindow.Current.Content.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var url = textbox.Text;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    await UpdateWarpRecordInternalAsync(url);
                }
            }
        }
        catch (Exception ex)
        {
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
                Title = "Warning",
                Content = $"All warp records of the uid {uid} will be deleted soon, and these records will not be recovered.",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = MainWindow.Current.Content.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var count = _warpRecordService.DeleteUid(uid);
                NotificationBehavior.Instance.Success(null, $"{count} warp records of uid {uid} have been deleted.");
                SelectUid = UidList.FirstOrDefault(x => x != uid);
                UidList.Remove(uid);
            }
        }
        catch (Exception ex)
        {
            NotificationBehavior.Instance.Error(ex);
        }
    }




    [RelayCommand]
    private async Task ExportWarpRecordAsync(string format)
    {
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
                _warpRecordService.ExportWarpRecord(uid, file.Path, format);
                var options = new FolderLauncherOptions();
                options.ItemsToSelect.Add(file);
                NotificationBehavior.Instance.ShowWithButton(InfoBarSeverity.Success, "Export Successfully", file.Name, "Open Folder", async () => await Launcher.LaunchFolderAsync(await file.GetParentAsync(), options));
            }
        }
        catch (Exception ex)
        {
            NotificationBehavior.Instance.Error(ex);
        }
    }




}
