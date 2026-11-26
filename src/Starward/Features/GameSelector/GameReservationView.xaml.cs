using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Starward.Core.HoYoPlay;
using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Networking.Connectivity;
using Windows.System;


namespace Starward.Features.GameSelector;

[INotifyPropertyChanged]
public sealed partial class GameReservationView : UserControl
{
    public GameReservationView()
    {
        InitializeComponent();
        this.Unloaded += GameReservationView_Unloaded;
        _mediaPlayer = new MediaPlayer();
    }



    public GameReservationContent? ReservationContent { get; set => SetProperty(ref field, value); }


    [ObservableProperty]
    public partial GameReservationMedia? SelectedMedia { get; set; }


    public string? HomePageUrl { get; set => SetProperty(ref field, value); }


    private MediaPlayer _mediaPlayer;



    private void GameReservationView_Unloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            this.Unloaded -= GameReservationView_Unloaded;
            SelectedMedia = null;
            ReservationContent = null;
            _mediaPlayer?.Dispose();
            _mediaPlayer = null!;
        }
        catch { }
    }


    /// <summary>
    /// 隐藏 FilpView 中自动出现的翻页按键
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FlipView_Media_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FlipView flipView)
            {
                var grid = VisualTreeHelper.GetChild(flipView, 0);
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
                                button.IsHitTestVisible = false;
                                button.Opacity = 0;
                            }
                        }
                    }
                }
            }
        }
        catch { }
    }




    partial void OnSelectedMediaChanged(GameReservationMedia? oldValue, GameReservationMedia? newValue)
    {
        try
        {
            bool isPlaying = _mediaPlayer.PlaybackSession.PlaybackState is MediaPlaybackState.Playing or MediaPlaybackState.Buffering;
            _mediaPlayer.Pause();
            if (oldValue is not null && oldValue.Type is GameReservationMediaType.RESERVATION_MEDIA_TYPE_VIDEO)
            {
                MediaPlayerElement? element = ((FlipView_Media.ContainerFromItem(oldValue) as FlipViewItem)
                                             ?.ContentTemplateRoot as Grid)
                                             ?.FindName("MediaPlayer_Video") as MediaPlayerElement;
                if (element is not null)
                {
                    element.Visibility = Visibility.Collapsed;
                    element.SetMediaPlayer(null);
                }
            }
            if (newValue is not null && newValue.Type is GameReservationMediaType.RESERVATION_MEDIA_TYPE_VIDEO)
            {
                MediaPlayerElement? element = ((FlipView_Media.ContainerFromItem(newValue) as FlipViewItem)
                                             ?.ContentTemplateRoot as Grid)
                                             ?.FindName("MediaPlayer_Video") as MediaPlayerElement;
                if (element is not null)
                {
                    element.Visibility = Visibility.Visible;
                    element.SetMediaPlayer(_mediaPlayer);
                    _mediaPlayer.Source = MediaSource.CreateFromUri(new(newValue.ResourceUrl));
                    if (NetworkInformation.GetInternetConnectionProfile().GetConnectionCost().NetworkCostType is NetworkCostType.Unrestricted)
                    {
                        _mediaPlayer.Play();
                    }
                }
            }
        }
        catch { }
    }


    private void Button_MediaIcon_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Button { DataContext: GameReservationIcon { QrImage: not null } } button)
        {
            ToolTip? tooltip = ToolTipService.GetToolTip(button) as ToolTip;
            tooltip?.Visibility = Visibility.Visible;
            tooltip?.IsOpen = true;
        }
    }


    private void Button_MediaIcon_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Button { DataContext: GameReservationIcon { QrImage: not null } } button)
        {
            ToolTip? tooltip = ToolTipService.GetToolTip(button) as ToolTip;
            tooltip?.IsOpen = false;
        }
    }

    private async void Button_MediaIcon_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button { DataContext: GameReservationIcon { Link.Length: > 0 } icon })
            {
                await Launcher.LaunchUriAsync(new Uri(icon.Link));
            }
        }
        catch { }
    }




    private void PlayPauseVideo()
    {
        try
        {
            if (_mediaPlayer.PlaybackSession.PlaybackState is MediaPlaybackState.Buffering or MediaPlaybackState.Playing)
            {
                _mediaPlayer.Pause();
            }
            else
            {
                _mediaPlayer.Play();
            }
        }
        catch { }
    }


    /// <summary>
    /// 点击视频画面切换播放/暂停
    /// </summary>
    private void MediaPlayer_Video_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (SelectedMedia?.Type is GameReservationMediaType.RESERVATION_MEDIA_TYPE_VIDEO)
        {
            PlayPauseVideo();
        }
    }


    [RelayCommand]
    private void ScrollLeft()
    {
        try
        {
            int i = GridView_ImageLines.SelectedIndex;
            int count = GridView_ImageLines.Items.Count;
            GridView_ImageLines.Focus(FocusState.Programmatic);
            GridView_ImageLines.SelectedIndex = (i - 1 + count) % count;
        }
        catch { }
    }



    [RelayCommand]
    private void ScrollRight()
    {
        try
        {
            int i = GridView_ImageLines.SelectedIndex;
            int count = GridView_ImageLines.Items.Count;
            GridView_ImageLines.Focus(FocusState.Programmatic);
            GridView_ImageLines.SelectedIndex = (i + 1 + count) % count;
        }
        catch { }
    }



    [RelayCommand]
    private async Task OpenHomePageAsync()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(HomePageUrl))
            {
                await Launcher.LaunchUriAsync(new(HomePageUrl));
            }
        }
        catch { }
    }


    public static Visibility IsVideoMedia(string type)
    {
        return type is GameReservationMediaType.RESERVATION_MEDIA_TYPE_VIDEO ? Visibility.Visible : Visibility.Collapsed;
    }


}
