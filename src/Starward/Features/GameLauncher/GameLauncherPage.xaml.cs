using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Starward.Frameworks;
using System;
using System.Threading.Tasks;


namespace Starward.Features.GameLauncher;

public sealed partial class GameLauncherPage : PageBase
{


    private readonly ILogger<GameLauncherPage> _logger = AppService.GetLogger<GameLauncherPage>();

    private readonly GameLauncherService _gameLauncherService = AppService.GetService<GameLauncherService>();

    private readonly GamePackageService _gamePackageService = AppService.GetService<GamePackageService>();


    public GameLauncherPage()
    {
        this.InitializeComponent();
    }



    [NotifyPropertyChangedFor(nameof(InstalledLocateGameEnabled))]
    [ObservableProperty]
    public partial GameState GameState { get; set; }


    public bool StartGameButtonCanExecute { get; set => SetProperty(ref field, value); } = true;



    protected override void OnLoaded()
    {
        CheckGameVersion();
    }





    [RelayCommand]
    private async Task ClickStartGameButtonAsync()
    {
        StartGameButtonCanExecute = false;
        switch (GameState)
        {
            case GameState.StartGame:
                await StartGameAsync();
                break;
            case GameState.GameIsRunning:
            case GameState.InstallGame:
            case GameState.UpdateGame:
            case GameState.Downloading:
            case GameState.Waiting:
            case GameState.Paused:
            case GameState.ResumeDownload:
            default:
                StartGameButtonCanExecute = true;
                break;
        }
    }






    #region Game Version


    public string? GameInstallPath { get; set => SetProperty(ref field, value); }


    public bool IsInstallPathRemovableTipEnabled { get; set => SetProperty(ref field, value); }


    public bool InstalledLocateGameEnabled => GameState is GameState.InstallGame && !IsInstallPathRemovableTipEnabled;


    private Version? localGameVersion;


    private Version? latestGameVersion;


    private Version? preInstallGameVersion;


    private bool isGameExeExists;


    private bool isPreDownloadOK;






    private async void CheckGameVersion()
    {
        try
        {
            GameState = GameState.Waiting;
            GameInstallPath = _gameLauncherService.GetGameInstallPath(CurrentGameId, out bool storageRemoved);
            IsInstallPathRemovableTipEnabled = storageRemoved;
            if (GameInstallPath is null || storageRemoved)
            {
                GameState = GameState.InstallGame;
                return;
            }
            isGameExeExists = await _gameLauncherService.IsGameExeExistsAsync(CurrentGameId);
            localGameVersion = await _gameLauncherService.GetLocalGameVersionAsync(CurrentGameId);
            if (isGameExeExists && localGameVersion != null)
            {
                GameState = GameState.StartGame;
            }
            else
            {
                GameState = GameState.ResumeDownload;
                return;
            }
            latestGameVersion = await _gamePackageService.GetLatestGameVersionAsync(CurrentGameId);
            if (latestGameVersion > localGameVersion)
            {
                GameState = GameState.UpdateGame;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check game version");
        }
    }




    #endregion







    #region Start Game








    private async void UpdateGameState()
    {
        try
        {

        }
        catch { }
    }





    [RelayCommand]
    private async Task StartGameAsync()
    {
        try
        {
            await Task.Delay(2000);
            var process1 = await _gameLauncherService.StartGameAsync(CurrentGameId);
            if (process1 == null)
            {
                StartGameButtonCanExecute = true;
            }
            else
            {
                GameState = GameState.GameIsRunning;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start game");
            StartGameButtonCanExecute = true;
        }
    }




    #endregion











}
