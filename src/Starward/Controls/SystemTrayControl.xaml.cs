using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Messages;
using Starward.Models;
using Starward.Services;
using Starward.Services.Launcher;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls;

[INotifyPropertyChanged]
public sealed partial class SystemTrayControl : UserControl
{


    private readonly ILogger<SystemTrayControl> _logger = AppConfig.GetLogger<SystemTrayControl>();

    private readonly GameService _gameService = AppConfig.GetService<GameService>();

    private readonly GameLauncherService _gameLauncherService = AppConfig.GetService<GameLauncherService>();

    private readonly PlayTimeService _playTimeService = AppConfig.GetService<PlayTimeService>();

    private string lang;


    public SystemTrayControl()
    {
        this.InitializeComponent();
        this.Loaded += SystemTrayControl_Loaded;
    }


    [ObservableProperty]
    private List<GameBizIcon>? installedGames;


    private void SystemTrayControl_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateContent();
    }


    public void UpdateContent()
    {
        this.Bindings.Update();
        try
        {
            var list = new List<GameBizIcon>();
            foreach (GameBiz biz in GameBiz.AllGameBizs)
            {
                if (biz.IsKnown())
                {
                    string? folder = _gameLauncherService.GetGameInstallPath(biz);
                    if (string.IsNullOrWhiteSpace(folder))
                    {
                        continue;
                    }
                    string name = GameResourceService.GetGameExeName(biz);
                    string file = Path.Join(folder, name);
                    if (File.Exists(file))
                    {
                        list.Add(new GameBizIcon { GameBiz = biz });
                    }
                }
            }
            if (list.Count > 0)
            {
                if ((!InstalledGames?.SequenceEqual(list) ?? true) || lang != CultureInfo.CurrentUICulture.Name)
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
            if (sender is Button button && button.Tag is GameBizIcon icon)
            {
                var process1 = _gameService.StartGame(icon.GameBiz, AppConfig.IgnoreRunningGame);
                if (process1 != null)
                {
                    WeakReferenceMessenger.Default.Send(new GameStartMessage(icon.GameBiz));
                    _logger.LogInformation("Game started ({name}, {pid})", process1.ProcessName, process1.Id);
                    _ = _playTimeService.StartProcessToLogAsync(icon.GameBiz);
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
        App.Current.EnsureMainWindow();
    }



    [RelayCommand]
    private void Exit()
    {
        App.Current.Exit();
    }



    public class GameServerModel : IEquatable<GameServerModel>
    {

        public ImageSource Icon { get; set; }

        public GameBiz GameBiz { get; set; }

        public string GameName => GameBiz.ToGameName();

        public string GameServer => GameBiz.ToGameServerName();

        public bool Equals(GameServerModel? other)
        {
            return ReferenceEquals(this, other) || GameBiz == other?.GameBiz;
        }
    }







}
