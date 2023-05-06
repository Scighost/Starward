// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Starward.Core;
using Starward.Core.Launcher;
using Starward.Models;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class LauncherPage : Page
{

    private readonly LauncherClient _launcherClient = new();

    private Timer _timer = new(5000) { AutoReset = true };


    public LauncherPage()
    {
        this.InitializeComponent();
        _timer.Elapsed += _timer_Elapsed;
    }



    [ObservableProperty]
    private List<LauncherBanner> bannerList;


    [ObservableProperty]
    private List<LauncherPostGroup> launcherPostGroupList;


    [ObservableProperty]
    private int gameServerIndex = AppConfig.GameServerIndex;
    partial void OnGameServerIndexChanged(int value)
    {
        AppConfig.GameServerIndex = value;
        _ = GetLauncherContentAsync();
    }

    [ObservableProperty]
    private int accountServerIndex = AppConfig.GameServerIndex;
    partial void OnAccountServerIndexChanged(int value)
    {
        try
        {
            GameAccountList = allGameAccounts.Where(x => x.Server == value).ToList();
            SelectGameAccount = GameAccountList.FirstOrDefault(x => x.IsLogin);
        }
        catch { }
    }


    [ObservableProperty]
    private string? startGameArgument = AppConfig.StartGameArgument;
    partial void OnStartGameArgumentChanged(string? value)
    {
        AppConfig.StartGameArgument = value;
    }


    [ObservableProperty]
    private int targetFPS = AppConfig.TargetFPS;
    partial void OnTargetFPSChanged(int value)
    {
        try
        {
            value = Math.Clamp(value, 60, 120);
            AppConfig.TargetFPS = value;
            GameService.ChangeGameFPS(value);
        }
        catch (Exception ex)
        {

        }
    }


    [ObservableProperty]
    private bool enableBannerAndPost = AppConfig.EnableBannerAndPost;
    partial void OnEnableBannerAndPostChanged(bool value)
    {
        Grid_BannerAndPost.Opacity = value ? 1 : 0;
        Grid_BannerAndPost.IsHitTestVisible = value;
        AppConfig.EnableBannerAndPost = value;
    }


    [ObservableProperty]
    private GameAccount? selectGameAccount;
    partial void OnSelectGameAccountChanged(GameAccount? value)
    {
        CanChangeGameAccount = value is null;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ChangeGameAccountCommand))]
    private bool canChangeGameAccount;

    [ObservableProperty]
    private List<GameAccount> gameAccountList;

    private List<GameAccount> allGameAccounts;


    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            GetGameAccount();
            await GetLauncherContentAsync();
            _timer.Start();
        }
        catch (Exception ex)
        {

        }
    }


    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
    }


    private void FlipView_Banner_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _timer.Stop();
    }

    private void FlipView_Banner_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _timer.Start();
    }



    private void _timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            if (BannerList?.Any() ?? false)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    PipsPager_Banner.SelectedPageIndex = (PipsPager_Banner.SelectedPageIndex + 1) % PipsPager_Banner.NumberOfPages;
                });
            }
        }
        catch { }

    }



    private async Task GetLauncherContentAsync()
    {
        try
        {
            var content = await _launcherClient.GetLauncherContentAsync(GameServerIndex);
            BannerList = content.Banner;
            LauncherPostGroupList = content.Post.GroupBy(x => x.Type).OrderBy(x => x.Key).Select(x => new LauncherPostGroup(x.Key.ToDescription(), x)).ToList();
            if (EnableBannerAndPost)
            {
                Grid_BannerAndPost.Opacity = 1;
                Grid_BannerAndPost.IsHitTestVisible = true;
            }

            var url = content.BackgroundImage?.Background;
            if (!string.IsNullOrWhiteSpace(url))
            {
                var name = Path.GetFileName(url);
                var folder = Path.Join(AppConfig.ConfigDirectory, "bg");
                Directory.CreateDirectory(folder);
                var file = Path.Join(folder, name);
                if (!File.Exists(file))
                {
                    var bytes = await new HttpClient().GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(file, bytes);
                }
                MainPage.Current.BackgroundImageUri = new Uri(file);
                AppConfig.BackgroundImage = name;
            }
        }
        catch (Exception ex)
        {

        }
    }


    private void GetGameAccount()
    {
        try
        {
            allGameAccounts = GameService.GetGameAccounts();
            GameAccountList = allGameAccounts.Where(x => x.Server == AccountServerIndex).ToList();
            SelectGameAccount = GameAccountList.FirstOrDefault(x => x.IsLogin);
            CanChangeGameAccount = false;
        }
        catch (Exception ex)
        {

        }
    }




    private async void Image_Banner_Tapped(object sender, TappedRoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement fe && fe.DataContext is LauncherBanner banner)
            {
                await Launcher.LaunchUriAsync(new Uri(banner.Url));
            }
        }
        catch (Exception ex)
        {

        }
    }



    [RelayCommand]
    private void StartGame()
    {
        try
        {
            GameService.StartGame(GameServerIndex);
        }
        catch (Exception ex)
        {

        }
    }



    [RelayCommand(CanExecute = nameof(CanChangeGameAccount))]
    private void ChangeGameAccount()
    {
        try
        {
            if (SelectGameAccount is not null)
            {
                GameService.ChangeGameAccount(SelectGameAccount);
                foreach (var item in GameAccountList)
                {
                    item.IsLogin = false;
                }
                CanChangeGameAccount = false;
                SelectGameAccount.IsLogin = true;
            }
        }
        catch (Exception ex)
        {

        }
    }


    [RelayCommand]
    private async Task SaveGameAccountAsync()
    {
        try
        {
            if (SelectGameAccount is not null)
            {
                SelectGameAccount.Time = DateTime.Now;
                GameService.SaveGameAccount(SelectGameAccount);
                FontIcon_SaveGameAccount.Glyph = "\uE10B";
                await Task.Delay(3000);
                FontIcon_SaveGameAccount.Glyph = "\uE105";
            }
        }
        catch (Exception ex)
        {

        }
    }


    [RelayCommand]
    private void DeleteGameAccount()
    {
        try
        {
            if (SelectGameAccount is not null)
            {
                GameService.DeleteGameAccount(SelectGameAccount);
                GetGameAccount();
            }
        }
        catch (Exception ex)
        {

        }
    }


}
