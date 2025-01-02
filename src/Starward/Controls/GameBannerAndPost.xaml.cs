using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Helpers;
using Starward.Messages;
using Starward.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls;

public sealed partial class GameBannerAndPost : UserControl
{


    private readonly ILogger<GameBannerAndPost> _logger;

    private Microsoft.UI.Dispatching.DispatcherQueueTimer _bannerTimer;



    public GameBiz GameBiz { get; set; }


    public GameBannerAndPost()
    {
        this.InitializeComponent();
        _bannerTimer = DispatcherQueue.CreateTimer();
        _bannerTimer.Interval = TimeSpan.FromSeconds(5);
        _bannerTimer.IsRepeating = true;
        _bannerTimer.Tick += _bannerTimer_Tick;
        WeakReferenceMessenger.Default.Register<WindowStateChangedMessage>(this, (_, m) =>
        {
            if (m.IsHide)
            {
                _bannerTimer.Stop();
            }
            else
            {
                _bannerTimer.Start();
            }
        });
    }





    private GameContent _GameContent;
    public GameContent GameContent
    {
        get => GameContent;
        set
        {
            _GameContent = value;
            Banners = value.Banners;
            PostGroups = GamePostGroup.FromGameContent(value);
            ShowBannerAndPost = AppConfig.EnableBannerAndPost;
        }
    }




    public List<GameBanner>? Banners
    {
        get { return (List<GameBanner>)GetValue(BannersProperty); }
        set { SetValue(BannersProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Banners.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty BannersProperty =
        DependencyProperty.Register("Banners", typeof(List<GameBanner>), typeof(GameBannerAndPost), new PropertyMetadata(default));





    public List<GamePostGroup>? PostGroups
    {
        get { return (List<GamePostGroup>)GetValue(PostGroupsProperty); }
        set { SetValue(PostGroupsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for PostGroups.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PostGroupsProperty =
        DependencyProperty.Register("PostGroups", typeof(List<GamePostGroup>), typeof(GameBannerAndPost), new PropertyMetadata(default));






    public bool IsGameNoticesAlert
    {
        get { return (bool)GetValue(IsGameNoticesAlertProperty); }
        set { SetValue(IsGameNoticesAlertProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsGameNoticesAlert.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsGameNoticesAlertProperty =
        DependencyProperty.Register("IsGameNoticesAlert", typeof(bool), typeof(GameBannerAndPost), new PropertyMetadata(default));




    public bool ShowBannerAndPost
    {
        get => Grid_BannerAndPost.Opacity == 1;
        set
        {
            if (value && Banners?.Count > 0 && PostGroups?.Count > 0)
            {
                _bannerTimer.Start();
                Grid_BannerAndPost.Opacity = 1;
                Grid_BannerAndPost.IsHitTestVisible = true;
            }
            else
            {
                _bannerTimer.Stop();
                Grid_BannerAndPost.Opacity = 0;
                Grid_BannerAndPost.IsHitTestVisible = false;
            }
        }
    }





    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        _bannerTimer.Stop();
    }




    private void _bannerTimer_Tick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        try
        {
            if (Banners?.Count > 0)
            {
                FlipView_Banner.SelectedIndex = (FlipView_Banner.SelectedIndex + 1) % Banners.Count;
            }
        }
        catch { }
    }


    private async void Image_Banner_Tapped(object sender, TappedRoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement fe && fe.DataContext is GameBanner banner)
            {
                await Launcher.LaunchUriAsync(new Uri(banner.Image.Link));
                _logger.LogInformation("Open banner: {url}", banner.Image.Link);
            }
        }
        catch { }
    }


    private void FlipView_Banner_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var grid = VisualTreeHelper.GetChild(FlipView_Banner, 0);
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
                            // 隐藏banner中自动出现的翻页按键
                            button.IsHitTestVisible = false;
                            button.Opacity = 0;
                        }
                    }
                }
            }
        }
        catch { }
    }





    private void Grid_BannerContainer_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _bannerTimer.Stop();
        Border_PipsPager.Visibility = Visibility.Visible;
    }



    private void Grid_BannerContainer_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _bannerTimer.Start();
        Border_PipsPager.Visibility = Visibility.Collapsed;
    }






    [RelayCommand]
    private void NavigateToGameNoticesPage()
    {
        WindowManager.Active(new GameNoticesWindow { GameBiz = GameBiz });
    }



    [RelayCommand]
    private async Task OpenGameNoticesInBrowser()
    {
        try
        {
            // todo
            //long uid = SelectGameAccount?.Uid ?? 0;
            //string lang = CultureInfo.CurrentUICulture.Name;
            //string url = LauncherClient.GetGameNoticesUrl(CurrentGameBiz, uid, lang);
            //await Launcher.LaunchUriAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open game notices in browser");
        }
    }


}
