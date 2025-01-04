using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Starward.Features.Background;
using Starward.Features.ViewHost;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;


namespace Starward.Features.GameLauncher;

public sealed partial class GameLauncherPage : PageBase
{


    private readonly ILogger<GameLauncherPage> _logger = AppService.GetLogger<GameLauncherPage>();

    private readonly GameLauncherService _gameLauncherService = AppService.GetService<GameLauncherService>();

    private readonly GamePackageService _gamePackageService = AppService.GetService<GamePackageService>();

    private readonly BackgroundService _backgroundService = AppService.GetService<BackgroundService>();

    public GameLauncherPage()
    {
        this.InitializeComponent();
    }



    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InstalledLocateGameEnabled))]
    public partial GameState GameState { get; set; }


    public bool StartGameButtonCanExecute { get; set => SetProperty(ref field, value); } = true;



    protected override void OnLoaded()
    {
        CheckGameVersion();
        WeakReferenceMessenger.Default.Register<GameInstallPathChangedMessage>(this, OnGameInstallPathChanged);
        WeakReferenceMessenger.Default.Register<MainWindowStateChangedMessage>(this, OnMainWindowStateChanged);
        WeakReferenceMessenger.Default.Register<RemovableStorageDeviceChangedMessage>(this, OnRemovableStorageDeviceChanged);
    }



    protected override void OnUnloaded()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
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





    /// <summary>
    /// 定位游戏路径
    /// </summary>
    /// <returns></returns>
    private async Task LocateGameAsync()
    {
        try
        {
            string? folder = await FileDialogHelper.PickFolderAsync(this.XamlRoot);
            if (string.IsNullOrWhiteSpace(folder))
            {
                return;
            }
            AppSetting.SetGameInstallPath(CurrentGameBiz, folder);
            AppSetting.SetGameInstallPathRemovable(CurrentGameBiz, DriveHelper.IsDeviceRemovableOrOnUSB(folder));
            CheckGameVersion();
        }
        catch (Exception ex)
        {

        }
    }



    /// <summary>
    /// 定位游戏路径
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private async void Hyperlink_LocateGame_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
    {
        await LocateGameAsync();
    }




    private void OnGameInstallPathChanged(object _, GameInstallPathChangedMessage message)
    {
        CheckGameVersion();
    }




    private void OnMainWindowStateChanged(object _, MainWindowStateChangedMessage message)
    {
        try
        {
            if (message.Activate && (message.ElapsedOver(TimeSpan.FromMinutes(10)) || message.IsCrossingHour))
            {
                CheckGameVersion();
            }
        }
        catch { }
    }




    private void OnRemovableStorageDeviceChanged(object _, RemovableStorageDeviceChangedMessage message)
    {
        try
        {
            CheckGameVersion();
        }
        catch { }
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






    #region Drop Background File




    private void RootGrid_DragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            Border_BackgroundDragIn.Opacity = 1;
        }
    }




    private async void RootGrid_Drop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        Border_BackgroundDragIn.Opacity = 0;
        var defer = e.GetDeferral();
        try
        {
            if ((await e.DataView.GetStorageItemsAsync()).FirstOrDefault() is StorageFile file)
            {
                string? name = await _backgroundService.ChangeCustomBackgroundFileAsync(file);
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }
                AppSetting.SetCustomBg(CurrentGameBiz, name);
                AppSetting.SetEnableCustomBg(CurrentGameBiz, true);
                WeakReferenceMessenger.Default.Send(new BackgroundChangedMessage());
            }
        }
        catch (COMException ex)
        {
            InAppToast.MainWindow?.Error(Lang.GameLauncherSettingDialog_CannotDecodeFile);
            _logger.LogError(ex, "Change custom background failed");
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(Lang.GameLauncherSettingDialog_AnUnknownErrorOccurredPleaseCheckTheLogs);
            _logger.LogError(ex, "Change custom background failed");
        }
        defer.Complete();
    }



    private void RootGrid_DragLeave(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        Border_BackgroundDragIn.Opacity = 0;
    }



    #endregion





    [RelayCommand]
    private async Task OpenGameLauncherSettingDialogAsync()
    {
        await new GameLauncherSettingDialog { CurrentGameId = this.CurrentGameId, XamlRoot = this.XamlRoot }.ShowAsync();
    }


}
