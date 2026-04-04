using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Starward.Features.GameInstall;
using Starward.RPC.GameInstall;
using System;
using System.Windows.Input;


namespace Starward.Features.GameLauncher;

[INotifyPropertyChanged]
public sealed partial class StartGameButton : UserControl
{


    private static Brush AccentFillColorDefaultBrush => (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
    private static Brush TextOnAccentFillColorDisabled => (Brush)Application.Current.Resources["TextOnAccentFillColorDisabledBrush"];
    private static Brush TextOnAccentFillColorPrimaryBrush => (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"];


    public StartGameButton()
    {
        this.InitializeComponent();
        this.ActualThemeChanged += StartGameButton_ActualThemeChanged;
    }


    public ICommand GameCommand { get; set => SetProperty(ref field, value); }


    public ICommand SettingCommand { get; set => SetProperty(ref field, value); }


    public string? RunningGameInfo { get; set => SetProperty(ref field, value); }



    public GameState GameState { get; set { if (SetProperty(ref field, value)) UpdateActionButtonState(); } }


    public bool ActionButtonPointerOver { get; set { if (SetProperty(ref field, value)) UpdateActionButtonState(); } }


    public bool SettingButtonPointerOver { get; set { if (SetProperty(ref field, value)) UpdateButtonForeground(); } }


    public bool GameStateIsInstalling => GameState is GameState.Installing;


    public bool IsAccentColorBackgroundVisible => Button_GameAction.IsEnabled && GameState is not GameState.Installing;


    public bool IsGameActionCommandRunning => !Button_GameAction.IsEnabled && GameState is not GameState.GameIsRunning;


    public string StartGameButtonText => GameState switch
    {
        GameState.StartGame => Lang.LauncherPage_StartGame,
        GameState.GameIsRunning => Lang.LauncherPage_GameIsRunning,
        GameState.InstallGame => Lang.LauncherPage_InstallGame,
        GameState.UpdateGame => Lang.LauncherPage_UpdateGame,
        GameState.UpdatePlugin => "Update Plugins",
        GameState.Installing => "",
        GameState.ResumeDownload => Lang.StartGameButton_ResumeDownload,
        GameState.ComingSoon => "Coming Soon",
        _ => "",
    };


    public Brush ActionButtonForeground => (Button_GameAction.IsEnabled, IsAccentColorBackgroundVisible, ActionButtonPointerOver) switch
    {
        (false, _, _) => TextOnAccentFillColorDisabled,
        (true, false, true) => AccentFillColorDefaultBrush,
        (true, false, false) => TextOnAccentFillColorDisabled,
        _ => TextOnAccentFillColorPrimaryBrush
    };


    public Brush SettingButtonForeground => (IsAccentColorBackgroundVisible, SettingButtonPointerOver) switch
    {
        (false, true) => AccentFillColorDefaultBrush,
        (false, false) => TextOnAccentFillColorDisabled,
        _ => TextOnAccentFillColorPrimaryBrush
    };



    private void UpdateActionButtonState()
    {
        Button_GameAction.IsEnabled = GameState is not GameState.GameIsRunning and not GameState.ComingSoon;
        OnPropertyChanged(nameof(GameStateIsInstalling));
        if (GameStateIsInstalling)
        {
            UpdateActionButtonStateWhenInstalling();
        }
        OnPropertyChanged(nameof(IsAccentColorBackgroundVisible));
        OnPropertyChanged(nameof(IsGameActionCommandRunning));
        OnPropertyChanged(nameof(StartGameButtonText));
        UpdateButtonForeground();
    }



    private void UpdateButtonForeground()
    {
        OnPropertyChanged(nameof(ActionButtonForeground));
        OnPropertyChanged(nameof(SettingButtonForeground));
    }



    private void Button_GameAction_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsAccentColorBackgroundVisible));
        OnPropertyChanged(nameof(IsGameActionCommandRunning));
        OnPropertyChanged(nameof(ActionButtonForeground));
        OnPropertyChanged(nameof(SettingButtonForeground));
    }



    private void Control_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender as Grid == Grid_Root)
        {
            if (GameState is GameState.GameIsRunning or GameState.Installing)
            {
                Popup_GameInfoOrDownloadProgress.IsOpen = true;
            }
        }
        else if (sender as Button == Button_GameAction)
        {
            ActionButtonPointerOver = true;
        }
        else if (sender as Button == Button_Setting)
        {
            SettingButtonPointerOver = true;
        }
    }


    private void Control_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender as Grid == Grid_Root)
        {
            Popup_GameInfoOrDownloadProgress.IsOpen = false;
        }
        else if (sender as Button == Button_GameAction)
        {
            ActionButtonPointerOver = false;
        }
        else if (sender as Button == Button_Setting)
        {
            SettingButtonPointerOver = false;
        }

    }





    public GameInstallState InstallState { get; set { if (SetProperty(ref field, value)) UpdateActionButtonStateWhenInstalling(); } }


    public bool InstallStateIsPending => InstallState is GameInstallState.Waiting;

    public bool InstallStateIsDownloading => InstallState is GameInstallState.Downloading;

    public double Button_GameAction_DownloadingRemainTime_Opacity => !ActionButtonPointerOver && InstallState is GameInstallState.Downloading ? 1 : 0;

    public double TextBlock_GameAction_InstallState_Opacity => !ActionButtonPointerOver && InstallState is not GameInstallState.Downloading ? 1 : 0;

    public double TextBlock_GameAction_PointerOver_Opacity => ActionButtonPointerOver ? 1 : 0;

    public string TextBlock_GameAction_InstallState_Text => (ActionButtonPointerOver, InstallState) switch
    {
        (false, GameInstallState.Waiting) => Lang.StartGameButton_Waiting,
        (false, GameInstallState.Decompressing) => Lang.DownloadGamePage_Decompressing,
        (false, GameInstallState.Merging) => Lang.DownloadGamePage_Merging,
        (false, GameInstallState.Verifying) => Lang.DownloadGamePage_Verifying,
        (false, GameInstallState.Paused) => Lang.DownloadGamePage_Paused,
        (false, GameInstallState.Error) => Lang.Common_Error,
        (false, GameInstallState.Queueing) => Lang.StartGameButton_InQueue,
        _ => ""
    };

    public string TextBlock_GameAction_PointerOver_Text => (ActionButtonPointerOver, InstallState) switch
    {
        (true, GameInstallState.Waiting or GameInstallState.Downloading) => Lang.DownloadGamePage_Pause,
        (true, GameInstallState.Paused or GameInstallState.Error or GameInstallState.Queueing) => Lang.Common_Continue,
        (true, GameInstallState.Decompressing or GameInstallState.Merging or GameInstallState.Verifying) => Lang.Common_Cancel,
        _ => ""

    };

    public string InstallStateText => InstallState switch
    {
        _ => GameInstallProgressFormatter.GetInstallStateText(InstallState),
    };


    public void UpdateActionButtonStateWhenInstalling()
    {
        OnPropertyChanged(nameof(InstallState));
        OnPropertyChanged(nameof(InstallStateIsPending));
        OnPropertyChanged(nameof(InstallStateIsDownloading));
        OnPropertyChanged(nameof(Button_GameAction_DownloadingRemainTime_Opacity));
        OnPropertyChanged(nameof(TextBlock_GameAction_InstallState_Opacity));
        OnPropertyChanged(nameof(TextBlock_GameAction_PointerOver_Opacity));
        OnPropertyChanged(nameof(TextBlock_GameAction_InstallState_Text));
        OnPropertyChanged(nameof(TextBlock_GameAction_PointerOver_Text));
        OnPropertyChanged(nameof(InstallStateText));
    }




    public int ProgressRingValue { get; set => SetProperty(ref field, value); }

    public string ProgressPercentText { get; set => SetProperty(ref field, value); }

    public string? DownloadBytesText { get; set => SetProperty(ref field, value); }

    public string? DownloadSpeedText { get; set => SetProperty(ref field, value); }

    public string? InstallBytesText { get; set => SetProperty(ref field, value); }

    public string? InstallSpeedText { get; set => SetProperty(ref field, value); }

    public string? VerifySpeedText { get; set => SetProperty(ref field, value); }

    public string? RemainTimeText { get; set => SetProperty(ref field, value); }

    public string? ErrorMessage { get; set => SetProperty(ref field, value); }



    public void UpdateGameInstallTaskState(GameInstallContext task)
    {
        InstallState = task.State;
        DownloadBytesText = GameInstallProgressFormatter.ToBytesText(task.Progress_DownloadFinishBytes, task.Progress_DownloadTotalBytes);
        InstallBytesText = GameInstallProgressFormatter.ToBytesText(task.Progress_WriteFinishBytes, task.Progress_WriteTotalBytes);
        ErrorMessage = task.ErrorMessage;
        if (InstallState is GameInstallState.Downloading)
        {
            double progress = GameInstallProgressFormatter.GetProgressPercent(task);
            DownloadSpeedText = GameInstallProgressFormatter.ToSpeedText(task.NetworkDownloadSpeed);
            InstallSpeedText = GameInstallProgressFormatter.ToSpeedText(task.StorageWriteSpeed);
            VerifySpeedText = GameInstallProgressFormatter.ToSpeedText(task.StorageReadSpeed);
            RemainTimeText = GameInstallProgressFormatter.ToRemainTimeText(task.RemainTimeSeconds);
            ProgressRingValue = (int)(progress * 100);
            ProgressPercentText = $"{progress:P1}";
        }
        else if (InstallState is GameInstallState.Decompressing or GameInstallState.Merging)
        {
            DownloadSpeedText = null;
            InstallSpeedText = null;
            VerifySpeedText = null;
            RemainTimeText = "--:--:--";
            double progress = GameInstallProgressFormatter.GetProgressPercent(task);
            ProgressRingValue = (int)(progress * 100);
            ProgressPercentText = $"{progress:P1}";
        }
        else if (InstallState is GameInstallState.Finish)
        {
            DownloadSpeedText = null;
            InstallBytesText = null;
            DownloadSpeedText = null;
            InstallSpeedText = null;
            VerifySpeedText = null;
            RemainTimeText = null;
            ErrorMessage = null;
            ProgressRingValue = 100;
            ProgressPercentText = "100%";
            Popup_GameInfoOrDownloadProgress.IsOpen = false;
        }
        else
        {
            DownloadSpeedText = null;
            InstallSpeedText = null;
            VerifySpeedText = null;
            RemainTimeText = "--:--:--";
        }
    }



    private void StartGameButton_ActualThemeChanged(FrameworkElement sender, object args)
    {
        OnPropertyChanged(nameof(ActionButtonForeground));
        OnPropertyChanged(nameof(SettingButtonForeground));
    }


}
