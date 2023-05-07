// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Helpers;
using Starward.Models;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class WarpRecordPage : Page
{



    public WarpRecordPage()
    {
        this.InitializeComponent();
        if (ShowDepatureWarp)
        {
            Grid_Star5List.ColumnDefinitions.Add(new ColumnDefinition());
        }
    }



    [ObservableProperty]
    private List<int> uidList;


    [ObservableProperty]
    private int selectUid;
    partial void OnSelectUidChanged(int value)
    {
        AppConfig.SelectUidInWarpRecordPage = value;
        GetWarpTypeStats();
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
            UidList = WarpRecordService.Instance.GetUids();
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


    [RelayCommand]
    private async Task GetWarpRecordAsync()
    {
        try
        {
            var serverIndex = SelectServerInPage - 1;
            if (serverIndex < 0)
            {
                serverIndex = AppConfig.GameServerIndex;
            }
            var path = GameService.GetGameInstallPath(serverIndex);
            if (!Directory.Exists(path))
            {
                NotificationBehavior.Instance.Error("", $"Cannot find game install path (server: {(serverIndex == 1 ? "OS" : "CN")})");
                return;
            }
            var url = WarpRecordService.Instance.GetWarpRecordUrlFromWebCache(path);
            if (string.IsNullOrWhiteSpace(url))
            {
                NotificationBehavior.Instance.Error("", $"Cannot find URL (server: {(serverIndex == 1 ? "OS" : "CN")})");
                return;
            }
            var infoBar = new InfoBar
            {
                Title = "Processing",
                Message = "Getting uid of URL...",
                Severity = InfoBarSeverity.Informational,
            };
            NotificationBehavior.Instance.Show(infoBar);
            var uid = await WarpRecordService.Instance.GetUidFromWarpRecordUrl(url);
            var progress = new Progress<string>((str) => infoBar.Message = str);
            await WarpRecordService.Instance.GetWarpRecordAsync(url, false, WarpLanguage, progress);
            infoBar.Title = "Finish";
            infoBar.Severity = InfoBarSeverity.Success;
        }
        catch (MihoyoApiException ex)
        {
            if (ex.ReturnCode == -101)
            {
                // authkey timeout

            }
            else
            {

            }
        }
        catch (Exception ex)
        {
            NotificationBehavior.Instance.Error(ex);
        }
    }




    private void GetWarpTypeStats()
    {
        try
        {
            int uid = SelectUid;
            if (uid > 0)
            {
                var stats = WarpRecordService.Instance.GetWarpTypeStats(uid);
                StellarWarp = stats.FirstOrDefault(x => x.WarpType == Core.Warp.WarpType.StellarWarp);
                DepartureWarp = stats.FirstOrDefault(x => x.WarpType == Core.Warp.WarpType.DepartureWarp);
                CharacterEventWarp = stats.FirstOrDefault(x => x.WarpType == Core.Warp.WarpType.CharacterEventWarp);
                LightConeEventWarp = stats.FirstOrDefault(x => x.WarpType == Core.Warp.WarpType.LightConeEventWarp);
            }
        }
        catch (Exception ex)
        {

        }
    }








}
