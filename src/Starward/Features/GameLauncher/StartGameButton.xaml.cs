using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Starward.Features.GameInstall;
using Starward.GameInstall;
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


    public string? RunningGameTime { get; set { field = value; OnPropertyChanged(nameof(StartGameButtonText)); OnPropertyChanged(nameof(GameActionAccessibilityText)); } }


    public string? RunningGameProcessInfo { get; set => SetProperty(ref field, value); }



    public GameState GameState { get; set { if (SetProperty(ref field, value)) UpdateActionButtonState(); } }


    public bool ActionButtonPointerOver { get; set { if (SetProperty(ref field, value)) UpdateActionButtonState(); } }


    public bool SettingButtonPointerOver { get; set { if (SetProperty(ref field, value)) UpdateButtonForeground(); } }


    public bool GameStateIsInstalling => GameState is GameState.Installing;


    public bool IsAccentColorBackgroundVisible => Button_GameAction.IsEnabled && GameState is not GameState.Installing;


    public bool IsGameActionCommandRunning => !Button_GameAction.IsEnabled && GameState is not GameState.GameIsRunning;


    public string StartGameButtonText => GameState switch
    {
        GameState.StartGame => Lang.LauncherPage_StartGame,
        GameState.GameIsRunning => RunningGameTime ?? Lang.LauncherPage_GameIsRunning,
        GameState.InstallGame => Lang.LauncherPage_InstallGame,
        GameState.UpdateGame => Lang.LauncherPage_UpdateGame,
        GameState.UpdatePlugin => Lang.StartGameButton_UpdatePlugin,
        GameState.Installing => "",
        GameState.ResumeDownload => Lang.StartGameButton_ResumeDownload,
        GameState.ComingSoon => Lang.StartGameButton_ComingSoon,
        GameState.Reservation => Lang.GameReservationView_PreRegister,
        GameState.HotUpdate => Lang.StartGameButton_HotUpdate,
        _ => "",
    };


    public string GameActionAccessibilityText
    {
        get
        {
            if (!GameStateIsInstalling)
            {
                return StartGameButtonText;
            }
            string text = InstallStateText;
            if (!string.IsNullOrWhiteSpace(ProgressPercentText))
            {
                text = $"{text} {ProgressPercentText}";
            }
            if (!string.IsNullOrWhiteSpace(InstallSecondaryText))
            {
                text = $"{text} {InstallSecondaryText}";
            }
            if (!string.IsNullOrWhiteSpace(InstallActionText))
            {
                text = $"{text} {InstallActionText}";
            }
            return text;
        }
    }


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
        OnPropertyChanged(nameof(GameActionAccessibilityText));
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





    public RunnerState InstallState { get; set { if (SetProperty(ref field, value)) UpdateActionButtonStateWhenInstalling(); } }

    public RunnerStep InstallStep { get; set { if (SetProperty(ref field, value)) UpdateActionButtonStateWhenInstalling(); } }

    public GameInstallOperation InstallOperation { get; set => SetProperty(ref field, value); }


    public bool InstallProgressIsIndeterminate { get; set => SetProperty(ref field, value); }

    public double InstallInfoOpacity { get; set => SetProperty(ref field, value); }

    public double InstallActionTextOpacity { get; set => SetProperty(ref field, value); }

    public string InstallPhaseText { get; set => SetProperty(ref field, value); } = "";

    public string InstallOperationText { get; set => SetProperty(ref field, value); } = "";

    public string InstallStepPhaseText { get; set => SetProperty(ref field, value); } = "";

    public string? InstallSecondaryText { get; set => SetProperty(ref field, value); }

    public string InstallActionText { get; set => SetProperty(ref field, value); } = "";

    public double InstallPhaseFontSize { get; set => SetProperty(ref field, value); } = 12;

    public TextAlignment InstallPhaseTextAlignment { get; set => SetProperty(ref field, value); } = TextAlignment.Left;

    public string InstallStateText { get; set => SetProperty(ref field, value); } = "";

    public string InstallBytesLabel { get; set => SetProperty(ref field, value); } = "";


    public void UpdateActionButtonStateWhenInstalling()
    {
        bool showInfo = !ActionButtonPointerOver && InstallState is not RunnerState.Finish;
        InstallInfoOpacity = showInfo ? 1 : 0;
        InstallActionTextOpacity = !showInfo && InstallState is not RunnerState.Finish ? 1 : 0;
        bool isLargeCentered = InstallState is RunnerState.Paused or RunnerState.Error
            || (InstallState is RunnerState.Running && (InstallStep is RunnerStep.Pending or RunnerStep.None));
        InstallPhaseFontSize = isLargeCentered ? 14 : 12;
        InstallPhaseTextAlignment = isLargeCentered ? TextAlignment.Center : TextAlignment.Left;
        OnPropertyChanged(nameof(InstallState));
        OnPropertyChanged(nameof(InstallPhaseText));
        OnPropertyChanged(nameof(InstallOperationText));
        OnPropertyChanged(nameof(InstallStepPhaseText));
        OnPropertyChanged(nameof(InstallSecondaryText));
        OnPropertyChanged(nameof(InstallActionText));
        OnPropertyChanged(nameof(InstallStateText));
        OnPropertyChanged(nameof(InstallBytesLabel));
        OnPropertyChanged(nameof(InstallInfoOpacity));
        OnPropertyChanged(nameof(InstallActionTextOpacity));
        OnPropertyChanged(nameof(GameActionAccessibilityText));
    }




    public int ProgressRingValue { get; set => SetProperty(ref field, value); }

    public string ProgressRingText { get; set => SetProperty(ref field, value); } = "";

    public string? ProgressPercentText { get; set => SetProperty(ref field, value); }

    public string? DownloadBytesText { get; set => SetProperty(ref field, value); }

    public string? DownloadSpeedText { get; set => SetProperty(ref field, value); }

    public string? InstallBytesText { get; set => SetProperty(ref field, value); }

    public string? RemainTimeText { get; set => SetProperty(ref field, value); }

    public string? ErrorMessage { get; set => SetProperty(ref field, value); }

    public string? HardLinkBytesText { get; set => SetProperty(ref field, value); }



    public void UpdateGameInstallTaskState(GameInstallProgress task)
    {
        InstallOperation = task.Operation;
        InstallState = task.State;
        InstallStep = task.Step;

        InstallPhaseText = GameInstallProgressDisplay.GetPhaseText(task.State, task.Step);
        InstallStateText = GameInstallProgressDisplay.GetOperationPhaseText(task);
        InstallActionText = GameInstallProgressDisplay.GetActionText(task.State);

        string operationText = GameInstallProgressDisplay.GetOperationText(task.Operation);
        InstallOperationText = string.IsNullOrWhiteSpace(operationText) ? InstallPhaseText : operationText;

        string? stepText = GameInstallProgressDisplay.GetStepText(task);
        InstallStepPhaseText = string.IsNullOrWhiteSpace(stepText) ? InstallPhaseText : $"{stepText} · {InstallPhaseText}";

        bool hasPercent = GameInstallProgressDisplay.TryGetPercent(task, out double percent);
        InstallProgressIsIndeterminate = task.State is RunnerState.Running && !hasPercent;
        int ringValue = hasPercent ? (int)Math.Floor(percent) : 0;
        if (hasPercent && ringValue == 0 && percent > 0)
        {
            ringValue = 1;
        }
        ProgressRingValue = ringValue;
        ProgressRingText = hasPercent ? ringValue.ToString() : "";
        ProgressPercentText = hasPercent ? GameInstallProgressDisplay.ToPercentText(percent) : null;

        DownloadBytesText = null;
        DownloadSpeedText = null;
        InstallBytesText = null;
        InstallBytesLabel = "";
        RemainTimeText = null;
        ErrorMessage = null;
        HardLinkBytesText = null;

        if (task.State is RunnerState.Running or RunnerState.Paused or RunnerState.Error)
        {
            if (task.ProgressDisplay.HasFlag(ProgressDisplay.NetworkBytes))
            {
                DownloadBytesText = GameInstallProgressDisplay.ToBytesText(task.NetworkFinishedBytes, task.NetworkTotalBytes);
            }
            if (task.State is RunnerState.Running
                && task.Step is RunnerStep.Downloading or RunnerStep.Patching
                && task.ProgressDisplay.HasFlag(ProgressDisplay.NetworkSpeed))
            {
                DownloadSpeedText = GameInstallProgressDisplay.ToSpeedText(task.NetworkDownloadSpeed);
            }
            if (task.ProgressDisplay.HasFlag(ProgressDisplay.StorageBytes))
            {
                InstallBytesText = GameInstallProgressDisplay.ToBytesText(task.StorageFinishedBytes, task.StorageTotalBytes);
                InstallBytesLabel = task.Step is RunnerStep.Verifying ? Lang.StartGameButton_Verify : Lang.StartGameButton_Install;
            }
            else
            {
                InstallBytesLabel = "";
            }
            if (task.HardLinkBytes > 0)
            {
                HardLinkBytesText = GameInstallProgressDisplay.FormatBytes(task.HardLinkBytes);
            }
            if (task.State is RunnerState.Running
                && task.Step is RunnerStep.Downloading
                && task.ProgressDisplay.HasFlag(ProgressDisplay.NetworkSpeed))
            {
                RemainTimeText = GameInstallProgressDisplay.ToRemainTimeText(task.RemainTimeSeconds);
            }
        }

        if (task.State is RunnerState.Error)
        {
            ErrorMessage = string.IsNullOrWhiteSpace(task.ErrorMessage) ? Lang.DownloadGamePage_UnknownError : task.ErrorMessage;
        }

        string? secondaryText = null;
        if (task.State is RunnerState.Running or RunnerState.Error)
        {
            // 第二行只显示剩余时间、网络字节或存储字节；暂停时不显示第二行。
            if (task.State is RunnerState.Running
                && task.Step is RunnerStep.Downloading
                && task.ProgressDisplay.HasFlag(ProgressDisplay.NetworkSpeed)
                && task.RemainTimeSeconds > 0)
            {
                secondaryText = GameInstallProgressDisplay.ToRemainTimeText(task.RemainTimeSeconds);
            }
            else if (task.ProgressDisplay.HasFlag(ProgressDisplay.StorageBytes)
                     && GameInstallProgressDisplay.ToBytesText(task.StorageFinishedBytes, task.StorageTotalBytes, 1) is { } storageText)
            {
                secondaryText = storageText;
            }
            else if (task.ProgressDisplay.HasFlag(ProgressDisplay.NetworkBytes)
                     && GameInstallProgressDisplay.ToBytesText(task.NetworkFinishedBytes, task.NetworkTotalBytes, 1) is { } networkText)
            {
                secondaryText = networkText;
            }
        }

        InstallSecondaryText = secondaryText;
        OnPropertyChanged(nameof(GameActionAccessibilityText));

        if (task.State is RunnerState.Finish)
        {
            Popup_GameInfoOrDownloadProgress.IsOpen = false;
        }
    }



    private void StartGameButton_ActualThemeChanged(FrameworkElement sender, object args)
    {
        OnPropertyChanged(nameof(ActionButtonForeground));
        OnPropertyChanged(nameof(SettingButtonForeground));
    }


}
