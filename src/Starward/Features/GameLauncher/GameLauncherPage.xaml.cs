using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Core.Hypergryph;
using Starward.Features.Background;
using Starward.Features.CloudGame;
using Starward.Features.GameInstall;
using Starward.Features.Overlay;
using Starward.Features.ViewHost;
using Starward.GameInstall;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;


namespace Starward.Features.GameLauncher;

public sealed partial class GameLauncherPage : PageBase
{

    private readonly ILogger<GameLauncherPage> _logger = AppConfig.GetLogger<GameLauncherPage>();

    private readonly GameLauncherService _gameLauncherService = AppConfig.GetService<GameLauncherService>();

    private readonly GameInstallService _gameInstallService = AppConfig.GetService<GameInstallService>();

    private readonly HoYoPlayService _hoYoPlayService = AppConfig.GetService<HoYoPlayService>();


    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _dispatchTimer;


    public GameLauncherPage()
    {
        this.InitializeComponent();
        _dispatchTimer = DispatcherQueue.CreateTimer();
        _dispatchTimer.Interval = TimeSpan.FromMilliseconds(100);
        _dispatchTimer.Tick += UpdateGameInstallTaskProgress;
    }



    protected override void OnLoaded()
    {
        InitializeGameFeature();
        CheckGameVersion();
        UpdateGameInstallTask();
        CheckCloudGame();
        _ = InitializeGameServerAsync();
        WeakReferenceMessenger.Default.Register<GameInstallPathChangedMessage>(this, OnGameInstallPathChanged);
        WeakReferenceMessenger.Default.Register<MainWindowStateChangedMessage>(this, OnMainWindowStateChanged);
        WeakReferenceMessenger.Default.Register<RemovableStorageDeviceChangedMessage>(this, OnRemovableStorageDeviceChanged);
        WeakReferenceMessenger.Default.Register<GameInstallTaskStartedMessage>(this, OnGameInstallTaskStarted);
    }



    protected override void OnUnloaded()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        _dispatchTimer.Tick -= UpdateGameInstallTaskProgress;
        _dispatchTimer.Stop();
    }



    private void InitializeGameFeature()
    {
        GameFeatureConfig feature = GameFeatureConfig.FromGameId(CurrentGameId);
        EnableDX11 = HypergryphGameConstants.IsEndfield(CurrentGameBiz) && AppConfig.GetEnableDX11(CurrentGameBiz);
        if (feature.SupportCloudGame)
        {
            Button_CloudGame.Visibility = Visibility.Visible;
        }
    }



    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InstalledLocateGameEnabled))]
    [NotifyPropertyChangedFor(nameof(IsDX11OptionVisible), nameof(IsGraphicsOptionsVisible))]
    public partial GameState GameState { get; set; }



    [RelayCommand]
    private async Task ClickStartGameButtonAsync()
    {
        await Task.Delay(1);
        switch (GameState)
        {
            case GameState.None:
                break;
            case GameState.StartGame:
                await StartGameAsync();
                break;
            case GameState.GameIsRunning:
            case GameState.InstallGame:
                await InstallGameAsync();
                break;
            case GameState.Installing:
                await ChangeGameInstallTaskStateAsync();
                break;
            case GameState.UpdateGame:
                await UpdateGameAsync();
                break;
            case GameState.UpdatePlugin:
                await UpdatePluginAsync();
                break;
            case GameState.ResumeDownload:
                await ResumeDownloadAsync();
                break;
            case GameState.ComingSoon:
                break;
            case GameState.Reservation:
                await OpenReservationUrlAsync();
                break;
            case GameState.HotUpdate:
                await HotUpdateAsync();
                break;
            default:
                break;
        }
    }




    #region Game Server


    public List<GameServerConfig>? GameServers { get; set => SetProperty(ref field, value); }

    [ObservableProperty]
    public partial GameServerConfig? SelectedGameServer { get; set; }
    partial void OnSelectedGameServerChanged(GameServerConfig? oldValue, GameServerConfig? newValue)
    {
        if (oldValue is not null && newValue is not null)
        {
            AppConfig.LastGameIdOfBH3Global = newValue.GameId;
            WeakReferenceMessenger.Default.Send(new BH3GlobalGameServerChangedMessage(newValue.GameId));
        }
    }


    /// <summary>
    /// 初始化区服选项，仅崩坏三国际服使用
    /// </summary>
    /// <returns></returns>
    private async Task InitializeGameServerAsync()
    {
        try
        {
            if (CurrentGameBiz == GameBiz.bh3_global)
            {
                GameInfo? gameInfo = await _hoYoPlayService.GetGameInfoAsync(CurrentGameId);
                if (gameInfo?.GameServerConfigs?.Count > 0)
                {
                    GameServers = gameInfo.GameServerConfigs;
                    if (GameServers.FirstOrDefault(x => x.GameId == CurrentGameId.Id) is GameServerConfig config)
                    {
                        SelectedGameServer = config;
                    }
                    else
                    {
                        SelectedGameServer = GameServers.FirstOrDefault();
                        if (SelectedGameServer is not null)
                        {
                            CurrentGameId = new GameId(SelectedGameServer.GameId, GameBiz.bh3_global);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize game server");
        }
    }



    #endregion



    #region Game Version


    public string? GameInstallPath { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// 可移动存储设备提示
    /// </summary>
    public bool IsInstallPathRemovableTipEnabled { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// 已安装？定位游戏
    /// </summary>
    public bool InstalledLocateGameEnabled => GameState is GameState.InstallGame && !IsInstallPathRemovableTipEnabled;

    /// <summary>
    /// 预下载按钮是否可用
    /// </summary>
    public bool IsPredownloadButtonEnabled { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// 预下载是否完成
    /// </summary>
    public bool IsPredownloadFinished { get; set => SetProperty(ref field, value); }

    private Version? localGameVersion;

    private Version? latestGameVersion;

    private Version? predownloadGameVersion;

    private string? reservationUrl;


    /// <summary>
    /// 是否显示 DX12 选项
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGraphicsOptionsVisible))]
    public partial bool IsDX12OptionVisible { get; set; }

    /// <summary>
    /// 是否显示 DX11 选项
    /// </summary>
    public bool IsDX11OptionVisible => HypergryphGameConstants.IsEndfield(CurrentGameBiz)
        && GameState is GameState.StartGame or GameState.GameIsRunning or GameState.UpdateGame;

    public bool IsGraphicsOptionsVisible => IsDX11OptionVisible || IsDX12OptionVisible;

    /// <summary>
    /// 使用 DX11 启动
    /// </summary>
    public bool EnableDX11
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                AppConfig.SetEnableDX11(CurrentGameBiz, value);
                if (value && EnableDX12)
                {
                    EnableDX12 = false;
                }
            }
        }
    }


    /// <summary>
    /// DX12 配置
    /// </summary>
    public GameDXConfig? DXConfig { get; set => SetProperty(ref field, value); }


    /// <summary>
    /// 启用 DX12
    /// </summary>
    private bool _enableDX12;
    public bool EnableDX12
    {
        get => _enableDX12; set
        {
            if (SetProperty(ref _enableDX12, value))
            {
                AppConfig.SetEnableDX12(CurrentGameId, value);
                if (value && EnableDX11)
                {
                    EnableDX11 = false;
                }
            }
        }
    }


    private CancellationTokenSource? _checkGameVersionTokenSource;


    private async void CheckGameVersion()
    {
        try
        {
            _checkGameVersionTokenSource?.Cancel();
            _checkGameVersionTokenSource = CancellationTokenSource.CreateLinkedTokenSource(UnloadCancellationToken);
            CancellationToken cancellation = _checkGameVersionTokenSource.Token;

            GameInstallPath = GameLauncherService.GetGameInstallPath(CurrentGameId, out bool storageRemoved);
            IsInstallPathRemovableTipEnabled = storageRemoved;
            if (GameInstallPath is null || storageRemoved)
            {
                if (await CheckGameReservationAsync(cancellation))
                {
                    Button_PlayTime.Visibility = Visibility.Collapsed;
                    GameState = GameState.Reservation;
                }
                else
                {
                    GameState = GameState.InstallGame;
                }
                return;
            }
            bool isGameExeExists = await _gameLauncherService.IsGameExeExistsAsync(CurrentGameId, cancellation: cancellation);
            localGameVersion = await _gameLauncherService.GetLocalGameVersionAsync(CurrentGameId, cancellation: cancellation);
            if (isGameExeExists && localGameVersion != null)
            {
                GameState = GameState.StartGame;
            }
            else
            {
                GameState = GameState.ResumeDownload;
                return;
            }
            await CheckGameRunningAsync(cancellation);
            (latestGameVersion, predownloadGameVersion) = await _gameLauncherService.GetLatestGameVersionAsync(CurrentGameId, cancellation);
            if (latestGameVersion > localGameVersion)
            {
                GameState = GameState.UpdateGame;
                return;
            }
            if (predownloadGameVersion > localGameVersion)
            {
                IsPredownloadButtonEnabled = true;
                IsPredownloadFinished = await _gameLauncherService.CheckPreDownloadFinishedAsync(CurrentGameId, cancellation: cancellation);
            }
            await CheckDX12ConfigAsync(cancellation);
            if (GameState is not GameState.GameIsRunning)
            {
                if (await _gameLauncherService.CheckPluginUpdateAsync(CurrentGameId, cancellation: cancellation))
                {
                    GameState = GameState.UpdatePlugin;
                }
                else
                {
                    try
                    {
                        if (await _gameLauncherService.CheckGenshinHotUpdateAsync(CurrentGameId, cancellation: cancellation))
                        {
                            GameState = GameState.HotUpdate;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "CheckGenshinHotUpdateAsync");
                        if (ex.Message.Contains("CutsceneDispatchInfo"))
                        {
                            InAppToast.MainWindow?.Warning(Lang.StartGameButton_HotUpdate, Lang.GameLauncherSettingDialog_CutsceneResourceManifestIsUnavailable);
                        }
                        else
                        {
                            InAppToast.MainWindow?.Warning(Lang.GameLauncherPage_HotUpdateCheckFailed, ex.Message);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) when (UnloadCancellationToken.IsCancellationRequested) { }
        catch (miHoYoApiException ex) when (ex.ReturnCode is -100 or -403)
        {
            if (ex.ReturnCode is -100)
            {
                InAppToast.MainWindow?.Warning(null, Lang.InstallGameDialog_LoginExpiredPleaseLogInAgain);
            }
            if (ex.ReturnCode is -403)
            {
                InAppToast.MainWindow?.Warning(null, Lang.InstallGameDialog_CurrentAccountDoesnTHaveBetaTestingAccess);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check game version");
        }
    }


    private async Task<bool> CheckGameReservationAsync(CancellationToken cancellation)
    {
        try
        {
            var gameInfo = await _hoYoPlayService.GetGameInfoAsync(CurrentGameId, cancellation);
            if (gameInfo is null)
            {
                return false;
            }
            if (gameInfo.DisplayStatus is GameInfoDisplayStatus.LAUNCHER_GAME_DISPLAY_STATUS_RESERVATION_ENABLED)
            {
                reservationUrl = gameInfo.Reservation?.Link;
                return true;
            }
        }
        catch { }
        return false;
    }


    private async Task OpenReservationUrlAsync()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(reservationUrl))
            {
                await Launcher.LaunchUriAsync(new(reservationUrl));
            }
        }
        catch { }
    }


    /// <summary>
    /// 检查 DX12 配置
    /// </summary>
    /// <returns></returns>
    private async Task CheckDX12ConfigAsync(CancellationToken cancellation)
    {
        try
        {
            var gameConfig = await _hoYoPlayService.GetGameConfigAsync(CurrentGameId, cancellation);
            if (gameConfig?.EnableDxSwitch is true)
            {
                DXConfig = await _hoYoPlayService.GetDXConfigAsync(CurrentGameId, GPUInfoHelper.GetGPUInfos(), cancellation);
                if (DXConfig?.EnableDXSwitch is true && !DXConfig.AutoMode)
                {
                    IsDX12OptionVisible = true;
                    _enableDX12 = AppConfig.GetEnableDX12(CurrentGameId) ?? DXConfig.UseDX12ByDefault;
                    OnPropertyChanged(nameof(EnableDX12));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check DX12 config");
        }
    }


    /// <summary>
    /// 显示 DX12 说明对话框
    /// </summary>
    private async void Hyperlink_DX12Intro_Click(Hyperlink sender, HyperlinkClickEventArgs args)
    {
        if (DXConfig is not null)
        {
            await new DX12IntroDialog { GameDXConfig = DXConfig, XamlRoot = this.XamlRoot }.ShowAsync();
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
            if (!string.IsNullOrWhiteSpace(folder))
            {
                if (DriveHelper.GetDriveType(folder) is DriveType.Network && !new Uri(folder).IsUnc)
                {
                    InAppToast.MainWindow?.Warning(null, Lang.InstallGameDialog_MappedNetworkDrivesAreNotSupportedPleaseUseANetworkSharePathStartingWithDoubleBackslashes, 0);
                }
                else
                {
                    _gameLauncherService.ChangeGameInstallPath(CurrentGameId, folder);
                    CheckGameVersion();
                    WeakReferenceMessenger.Default.Send(new GameInstallPathChangedMessage());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Locate game");
        }
    }


    /// <summary>
    /// 定位游戏路径
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private async void Hyperlink_LocateGame_Click(Hyperlink sender, HyperlinkClickEventArgs args)
    {
        await LocateGameAsync();
    }


    private void OnGameInstallPathChanged(object _, GameInstallPathChangedMessage message)
    {
        CheckGameVersion();
        UpdateGameInstallTask();
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

    private System.Timers.Timer processTimer;


    [ObservableProperty]
    private partial Process? GameProcess { get; set; }


    partial void OnGameProcessChanged(Process? oldValue, Process? newValue)
    {
        processTimer?.Stop();
        if (processTimer is null)
        {
            processTimer = new(1000);
            processTimer.Elapsed += (_, _) => CheckGameExited();
        }
        if (newValue != null)
        {
            processTimer?.Start();
            RunningGameInfo = $"{newValue.ProcessName}.exe ({newValue.Id})";
            RunningGameService.AddRuninngGame(CurrentGameBiz, newValue);
        }
        else
        {
            RunningGameInfo = null;
            _logger.LogInformation("Game process exited");
        }
    }


    public string? RunningGameInfo { get; set => SetProperty(ref field, value); }


    public string? RunningGameTime { get; set => SetProperty(ref field, value); }


    private async Task<bool> CheckGameRunningAsync(CancellationToken cancellation)
    {
        try
        {
            GameProcess = await _gameLauncherService.GetGameProcessAsync(CurrentGameId, cancellation);
            if (GameProcess != null)
            {
                GameState = GameState.GameIsRunning;
                RunningGameTime = TimeSpanToString(DateTime.Now - GameProcess.StartTime);
                _logger.LogInformation("Game is running ({name}, {pid})", GameProcess.ProcessName, GameProcess.Id);
                return true;
            }
        }
        catch { }
        return false;
    }


    private void CheckGameExited()
    {
        try
        {
            if (GameProcess != null)
            {
                if (GameProcess.HasExited)
                {
                    DispatcherQueue.TryEnqueue(() => RunningGameTime = null);
                    DispatcherQueue.TryEnqueue(CheckGameVersion);
                    GameProcess = null;
                }
                else
                {
                    DispatcherQueue?.TryEnqueue(() => RunningGameTime = TimeSpanToString(DateTime.Now - GameProcess.StartTime));
                }
            }
        }
        catch { }
    }


    private static string TimeSpanToString(TimeSpan value)
    {
        return $"{value.Days * 24 + value.Hours:D2}:{value.Minutes:D2}:{value.Seconds:D2}";
    }


    [RelayCommand]
    private async Task StartGameAsync()
    {
        try
        {
            var process = await _gameLauncherService.StartGameAsync(CurrentGameId, cancellation: UnloadCancellationToken);
            if (process is not null)
            {
                GameState = GameState.GameIsRunning;
                GameProcess = process;
                WeakReferenceMessenger.Default.Send(new GameStartedMessage());
            }
        }
        catch (OperationCanceledException) when (UnloadCancellationToken.IsCancellationRequested) { }
        catch (FileNotFoundException)
        {
            CheckGameVersion();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start game");
            InAppToast.MainWindow?.Error(ex);
        }
    }


    #endregion



    #region Install Game


    private async Task InstallGameAsync()
    {
        try
        {
            if (_gameInstallTask is null)
            {
                await new InstallGameDialog { CurrentGameId = CurrentGameId, XamlRoot = this.XamlRoot, }.ShowAsync();
            }
            else
            {
                await ChangeGameInstallTaskStateAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Install game {GameBiz}", CurrentGameBiz);
            InAppToast.MainWindow?.Error(ex);
        }
    }


    private async Task ResumeDownloadAsync()
    {
        try
        {
            if (!Directory.Exists(GameInstallPath))
            {
                CheckGameVersion();
                return;
            }
            var task = await _gameInstallService.ResumeDownloadAsync(CurrentGameId, GameInstallPath, UnloadCancellationToken);
            if (task is not null)
            {
                _gameInstallTask = task;
                _dispatchTimer.Start();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resume download {GameBiz}", CurrentGameBiz);
            InAppToast.MainWindow?.Error(ex);
        }
    }


    [RelayCommand]
    private async Task PredownloadAsync()
    {
        try
        {
            if (_gameInstallTask is null)
            {
                await new PreDownloadDialog { CurrentGameId = this.CurrentGameId, XamlRoot = this.XamlRoot }.ShowAsync();
            }
            else if (_gameInstallTask.Operation is GameInstallOperation.Predownload)
            {
                if (_gameInstallTask.State is RunnerState.Stop or RunnerState.Paused or RunnerState.Error or RunnerState.Queue)
                {
                    await _gameInstallService.ContinueTaskAsync(_gameInstallTask);
                    _dispatchTimer.Start();
                }
                else if (_gameInstallTask.State is RunnerState.Running)
                {
                    await _gameInstallService.PauseTaskAsync(_gameInstallTask);
                    _dispatchTimer.Start();
                }
                else
                {
                    // GameInstallState.Stop
                    CheckGameVersion();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(PredownloadAsync));
            if (_gameInstallTask?.Operation is GameInstallOperation.Predownload)
            {
                _gameInstallTask.State = RunnerState.Error;
                _gameInstallTask.ErrorMessage = ex.Message;
            }
            else
            {
                InAppToast.MainWindow?.Error(ex);
            }
        }
    }


    private async Task UpdateGameAsync()
    {
        try
        {
            if (localGameVersion is not null && latestGameVersion > localGameVersion)
            {
                GameInstallProgress? task = await _gameInstallService.StartUpdateAsync(CurrentGameId, GameInstallPath!, UnloadCancellationToken);
                if (task is not null)
                {
                    _gameInstallTask = task;
                    _dispatchTimer.Start();
                }
            }
            else
            {
                CheckGameVersion();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update game {GameBiz}", CurrentGameBiz);
            InAppToast.MainWindow?.Error(ex);
        }
    }


    private async Task UpdatePluginAsync()
    {
        try
        {
            if (GameState is not GameState.GameIsRunning && Directory.Exists(GameInstallPath))
            {
                GameInstallProgress? task = await _gameInstallService.StarUpdatePluginAsync(CurrentGameId, GameInstallPath!, UnloadCancellationToken);
                if (task is not null)
                {
                    _gameInstallTask = task;
                    _dispatchTimer.Start();
                }
            }
            else
            {
                CheckGameVersion();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update plugin {GameBiz}", CurrentGameBiz);
            InAppToast.MainWindow?.Error(ex);
        }
    }


    private async Task HotUpdateAsync()
    {
        try
        {
            if (GameState is not GameState.GameIsRunning && Directory.Exists(GameInstallPath))
            {
                GameInstallProgress? task = await _gameInstallService.StarHotUpdateAsync(CurrentGameId, GameInstallPath!, UnloadCancellationToken);
                if (task is not null)
                {
                    _gameInstallTask = task;
                    _dispatchTimer.Start();
                }
            }
            else
            {
                CheckGameVersion();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update plugin {GameBiz}", CurrentGameBiz);
            InAppToast.MainWindow?.Error(ex);
        }
    }


    #endregion



    #region Game Install Task


    private GameInstallProgress? _gameInstallTask;


    private async Task ChangeGameInstallTaskStateAsync()
    {
        try
        {
            if (_gameInstallTask is null)
            {
                CheckGameVersion();
            }
            else if (_gameInstallTask.Operation is not GameInstallOperation.Predownload)
            {
                if (_gameInstallTask.State is RunnerState.Stop or RunnerState.Paused or RunnerState.Error or RunnerState.Queue)
                {
                    await _gameInstallService.ContinueTaskAsync(_gameInstallTask);
                    _dispatchTimer.Start();
                }
                else if (_gameInstallTask.State is RunnerState.Running)
                {
                    await _gameInstallService.PauseTaskAsync(_gameInstallTask);
                    _dispatchTimer.Start();
                }
                else
                {
                    // GameInstallState.Stop
                    CheckGameVersion();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change game install task state {GameBiz}", CurrentGameBiz);
        }
    }


    private void UpdateGameInstallTask()
    {
        try
        {
            _gameInstallTask = _gameInstallService.GetGameInstallTask(CurrentGameId);
            if (_gameInstallTask is not null)
            {
                if (_gameInstallTask.Operation is GameInstallOperation.Predownload)
                {
                    IsPredownloadButtonEnabled = true;
                }
                _dispatchTimer.Start();
            }
        }
        catch { }
    }


    private void OnGameInstallTaskStarted(object _, GameInstallTaskStartedMessage message)
    {
        if (message.InstallTask.GameId == CurrentGameId)
        {
            _gameInstallTask = message.InstallTask;
            _dispatchTimer.Start();
        }
    }


    private void UpdateGameInstallTaskProgress(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        if (_gameInstallTask is null)
        {
            _dispatchTimer.Stop();
            return;
        }
        try
        {
            if (_gameInstallTask.Operation is GameInstallOperation.Predownload)
            {
                Button_Predownload.UpdateGameInstallTaskState(_gameInstallTask);
            }
            else
            {
                GameState = GameState.Installing;
                Button_StartGame.UpdateGameInstallTaskState(_gameInstallTask);
            }
            if (_gameInstallTask.State is RunnerState.Error)
            {
                _dispatchTimer.Stop();
            }
            else if (_gameInstallTask.State is RunnerState.Stop or RunnerState.Finish)
            {
                _dispatchTimer.Stop();
                _gameInstallTask = null;
                CheckGameVersion();
            }
        }
        catch { }
    }


    #endregion



    #region Drop Background File


    private void RootGrid_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            Border_BackgroundDragIn.Opacity = 1;
        }
    }


    private async void RootGrid_Drop(object sender, DragEventArgs e)
    {
        Border_BackgroundDragIn.Opacity = 0;
        var defer = e.GetDeferral();
        try
        {
            if ((await e.DataView.GetStorageItemsAsync()).FirstOrDefault() is StorageFile file)
            {
                string? name = await BackgroundService.ChangeCustomBackgroundFileAsync(file);
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }
                AppConfig.SetCustomBg(CurrentGameId, name);
                await AppBackground.Current.UpdateBackgroundAsync();
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


    private void RootGrid_DragLeave(object sender, DragEventArgs e)
    {
        Border_BackgroundDragIn.Opacity = 0;
    }


    #endregion



    #region Game Setting


    [RelayCommand]
    private async Task OpenGameLauncherSettingDialogAsync()
    {
        await new GameLauncherSettingDialog { CurrentGameId = this.CurrentGameId, XamlRoot = this.XamlRoot }.ShowAsync();
    }


    #endregion



    #region Cloud Game


    private void CheckCloudGame()
    {
        try
        {
            Process? process = CloudGameService.GetCloudGameProcess(CurrentGameId);
            if (process is not null)
            {
                RunningGameService.AddRuninngGame(CurrentGameBiz, process);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check cloud game {GameBiz}", CurrentGameBiz);
        }
    }


    #endregion


}
