using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Starward.RPC.GameInstall;
using System;
using System.Collections.Generic;
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


    public bool LaunchOptionsButtonPointerOver { get; set { if (SetProperty(ref field, value)) UpdateButtonForeground(); } }


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


    public Brush LaunchOptionsButtonForeground => (IsLaunchOptionsButtonEnabled, IsAccentColorBackgroundVisible, LaunchOptionsButtonPointerOver) switch
    {
        (false, _, _) => TextOnAccentFillColorDisabled,
        (true, false, true) => AccentFillColorDefaultBrush,
        (true, false, false) => TextOnAccentFillColorDisabled,
        _ => TextOnAccentFillColorPrimaryBrush
    };


    /// <summary>
    /// 启动预设下拉按钮可见性。仅当存在自定义启动预设时显示。
    /// </summary>
    public bool IsLaunchOptionsButtonVisible { get; set { if (SetProperty(ref field, value)) OnPropertyChanged(nameof(StartGameTextMargin)); } }


    /// <summary>
    /// 启动预设下拉按钮启用条件，与 <see cref="GameState"/> 是否为 <see cref="GameState.StartGame"/> 相关
    /// </summary>
    public bool IsLaunchOptionsButtonEnabled => GameState is GameState.StartGame;


    /// <summary>
    /// 启动按钮文本左右边距。当启动预设下拉按钮可见时，右侧需要留出额外空间。
    /// </summary>
    public Thickness StartGameTextMargin => IsLaunchOptionsButtonVisible
        ? new Thickness(41, 5, 81, 6)
        : new Thickness(41, 5, 53, 6);


    /// <summary>
    /// 启动预设选择回调。参数为预设 Id（内置默认预设为 <see cref="GameLaunchScheme.BuiltInDefaultId"/>）。
    /// </summary>
    public event Action<string>? LaunchOptionSelected;


    /// <summary>
    /// 更新启动预设菜单。
    /// </summary>
    public void UpdateLaunchOptions(IReadOnlyList<GameLaunchScheme> schemes, string? selectedId)
    {
        Flyout_LaunchOptions.Items.Clear();
        if (schemes is null || schemes.Count <= 1)
        {
            IsLaunchOptionsButtonVisible = false;
            return;
        }
        foreach (GameLaunchScheme scheme in schemes)
        {
            var item = new MenuFlyoutItem
            {
                Text = string.IsNullOrWhiteSpace(scheme.Name) ? scheme.Id : scheme.Name,
                Tag = scheme.Id,
            };
            if (scheme.Id == selectedId)
            {
                item.Icon = new FontIcon { Glyph = "\uE73E", FontSize = 14 };
            }
            item.Click += LaunchOptionMenuItem_Click;
            Flyout_LaunchOptions.Items.Add(item);
        }
        IsLaunchOptionsButtonVisible = true;
        OnPropertyChanged(nameof(LaunchOptionsButtonForeground));
    }


    private void LaunchOptionMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: string id })
        {
            LaunchOptionSelected?.Invoke(id);
        }
    }


    private void Button_LaunchOptions_Click(object sender, RoutedEventArgs e)
    {
        // MenuFlyout 会自动展开；这里保留 Click 事件以维持 Style 一致的视觉反馈。
    }



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
        OnPropertyChanged(nameof(IsLaunchOptionsButtonEnabled));
        OnPropertyChanged(nameof(LaunchOptionsButtonForeground));
        UpdateButtonForeground();
    }



    private void UpdateButtonForeground()
    {
        OnPropertyChanged(nameof(ActionButtonForeground));
        OnPropertyChanged(nameof(SettingButtonForeground));
        OnPropertyChanged(nameof(LaunchOptionsButtonForeground));
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
        else if (sender as Button == Button_LaunchOptions)
        {
            LaunchOptionsButtonPointerOver = true;
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
        else if (sender as Button == Button_LaunchOptions)
        {
            LaunchOptionsButtonPointerOver = false;
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
        GameInstallState.Waiting => Lang.StartGameButton_Waiting,
        GameInstallState.Downloading => Lang.DownloadGamePage_Downloading,
        GameInstallState.Decompressing => Lang.DownloadGamePage_Decompressing,
        GameInstallState.Merging => Lang.DownloadGamePage_Merging,
        GameInstallState.Verifying => Lang.DownloadGamePage_Verifying,
        GameInstallState.Paused => Lang.DownloadGamePage_Paused,
        GameInstallState.Finish => Lang.DownloadGamePage_Finished,
        GameInstallState.Error => Lang.DownloadGamePage_SomethingError,
        GameInstallState.Queueing => Lang.StartGameButton_InQueue,
        _ => "State Error"
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
        DownloadBytesText = ToBytesText(task.Progress_DownloadFinishBytes, task.Progress_DownloadTotalBytes);
        InstallBytesText = ToBytesText(task.Progress_WriteFinishBytes, task.Progress_WriteTotalBytes);
        ErrorMessage = task.ErrorMessage;
        if (InstallState is GameInstallState.Downloading)
        {
            long total = task.Progress_DownloadTotalBytes;
            long finish = task.Progress_DownloadFinishBytes;
            double progress = (double)finish / total;
            DownloadSpeedText = ToSpeedText(task.NetworkDownloadSpeed);
            InstallSpeedText = ToSpeedText(task.StorageWriteSpeed);
            VerifySpeedText = ToSpeedText(task.StorageReadSpeed);
            RemainTimeText = ToRemainTimeText(task.RemainTimeSeconds);
            if (task.Operation is GameInstallOperation.Update && task.DownloadMode is GameInstallDownloadMode.Chunk)
            {
                progress = (double)task.Progress_WriteFinishBytes / task.Progress_WriteTotalBytes;
                ProgressRingValue = (int)(progress * 100);
                ProgressPercentText = $"{progress:P1}";
            }
            else
            {
                ProgressRingValue = (int)(progress * 100);
                ProgressPercentText = $"{progress:P1}";
            }
        }
        else if (InstallState is GameInstallState.Decompressing or GameInstallState.Merging)
        {
            DownloadSpeedText = null;
            InstallSpeedText = null;
            VerifySpeedText = null;
            RemainTimeText = "--:--:--";
            ProgressRingValue = (int)(task.Progress_Percent * 100);
            ProgressPercentText = $"{task.Progress_Percent:P1}";
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


    private static string? ToBytesText(long finish, long total)
    {
        const double MB = 1 << 20;
        const double GB = 1 << 30;
        if (total == 0)
        {
            return null;
        }
        if (total >= GB)
        {
            return $"{finish / GB:F2}/{total / GB:F2} GB";
        }
        else
        {
            return $"{finish / MB:F2}/{total / MB:F2} MB";
        }
    }


    private static string ToSpeedText(long bytes)
    {
        const double KB = 1 << 10;
        const double MB = 1 << 20;
        if (bytes >= MB)
        {
            return $"{bytes / MB:F2} MB/s";
        }
        else
        {
            return $"{bytes / KB:F2} KB/s";
        }
    }


    private static string? ToRemainTimeText(long seconds)
    {
        if (seconds == 0)
        {
            return "--:--:--";
        }
        return TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
    }



    private void StartGameButton_ActualThemeChanged(FrameworkElement sender, object args)
    {
        OnPropertyChanged(nameof(ActionButtonForeground));
        OnPropertyChanged(nameof(SettingButtonForeground));
        OnPropertyChanged(nameof(LaunchOptionsButtonForeground));
    }


}
