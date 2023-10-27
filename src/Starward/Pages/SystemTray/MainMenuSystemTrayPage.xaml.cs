using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Starward.Core;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Vanara.PInvoke;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.SystemTray;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class MainMenuSystemTrayPage : Page
{

    private readonly ILogger<MainMenuSystemTrayPage> _logger = AppConfig.GetLogger<MainMenuSystemTrayPage>();

    private readonly GameService _gameService = AppConfig.GetService<GameService>();

    private readonly SystemTrayService _systemTrayService = AppConfig.GetService<SystemTrayService>();

    private readonly PlayTimeService _playTimeService = AppConfig.GetService<PlayTimeService>();

    private string lang;


    public MainMenuSystemTrayPage()
    {
        this.InitializeComponent();
    }



    [ObservableProperty]
    private List<GameServerModel>? installedGames;



    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateContent();
    }



    public void UpdateContent()
    {
        this.Bindings.Update();
        try
        {
            var list = new List<GameServerModel>();
            foreach (GameBiz biz in Enum.GetValues<GameBiz>())
            {
                if (biz.ToGame() is not GameBiz.None)
                {
                    string? folder = _gameService.GetGameInstallPath(biz);
                    if (string.IsNullOrWhiteSpace(folder))
                    {
                        continue;
                    }
                    string name = GameService.GetGameExeName(biz);
                    string file = Path.Join(folder, name);
                    if (File.Exists(file))
                    {
                        list.Add(new GameServerModel
                        {
                            GameBiz = biz,
                            Icon = biz.ToGame() switch
                            {
                                GameBiz.Honkai3rd => new BitmapImage(new("ms-appx:///Assets/Image/icon_bh3.jpg")),
                                GameBiz.GenshinImpact => new BitmapImage(new("ms-appx:///Assets/Image/icon_ys.jpg")),
                                GameBiz.StarRail => new BitmapImage(new("ms-appx:///Assets/Image/icon_sr.jpg")),
                                _ => null!,
                            },
                        });
                    }
                }
            }
            if (list.Count > 0)
            {
                if (list.Count != InstalledGames?.Count || lang != CultureInfo.CurrentUICulture.Name)
                {
                    InstalledGames = list;
                    lang = CultureInfo.CurrentUICulture.Name;
                }
            }
            else
            {
                InstalledGames = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get installed game list");
        }
    }




    private void Button_StartGame_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button && button.Tag is GameServerModel game)
            {
                _systemTrayService.HideTrayWindow();
                var process1 = _gameService.StartGame(game.GameBiz, AppConfig.IgnoreRunningGame);
                if (process1 != null)
                {
                    MainPage.Current.PauseVideo();
                    //User32.ShowWindow(MainWindow.Current.HWND, ShowWindowCommand.SW_SHOWMINIMIZED);
                    _logger.LogInformation("Game started ({name}, {pid})", process1.ProcessName, process1.Id);
                    _ = _playTimeService.StartProcessToLogAsync(game.GameBiz);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start game");
        }
    }





    [RelayCommand]
    private void OpenLauncher()
    {
        User32.ShowWindow(MainWindow.Current.HWND, ShowWindowCommand.SW_SHOWNORMAL);
        User32.SetForegroundWindow(MainWindow.Current.HWND);
    }



    [RelayCommand]
    private void Exit()
    {
        _systemTrayService.Dispose();
        MainWindow.Current.Close();
    }



    public class GameServerModel
    {

        public ImageSource Icon { get; set; }

        public GameBiz GameBiz { get; set; }

        public string GameName => GameBiz.ToGameName();

        public string GameServer => GameBiz.ToGameServer();

    }


}
