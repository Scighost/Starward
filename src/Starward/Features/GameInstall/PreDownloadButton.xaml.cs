using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Starward.GameInstall;
using System.Windows.Input;


namespace Starward.Features.GameInstall;

[INotifyPropertyChanged]
public sealed partial class PreDownloadButton : UserControl
{



    public PreDownloadButton()
    {
        this.InitializeComponent();
    }





    public ICommand PredownloadCommand { get; set => SetProperty(ref field, value); }

    public RunnerState State { get; set { if (SetProperty(ref field, value)) { UpdateButtonState(); } } }

    public RunnerStep Step { get; set { if (SetProperty(ref field, value)) { UpdateButtonState(); } } }

    public bool IsPredownloadFinished { get; set { if (SetProperty(ref field, value)) { UpdateButtonState(); } } }

    public bool PointerOver { get; set => SetProperty(ref field, value); }

    public string? ProgressPercentText { get; set => SetProperty(ref field, value); }

    public string? DownloadBytesText { get; set => SetProperty(ref field, value); }

    public string? DownloadSpeedText { get; set => SetProperty(ref field, value); }

    public string? RemainTimeText { get; set => SetProperty(ref field, value); }

    public string? ErrorMessage { get; set => SetProperty(ref field, value); }


    public bool IsButtonEnabled => !(IsPredownloadFinished || State is RunnerState.Finish);


    public string ButtonIcon => GetButtonIcon();

    public string GetButtonIcon()
    {
        if (IsPredownloadFinished || State is RunnerState.Finish)
        {
            // Download Icon
            return "\uEBD3";
        }
        if (PointerOver && State is RunnerState.Paused or RunnerState.Error or RunnerState.Queue)
        {
            // Play Icon
            return "\uF5B0";
        }
        else if (PointerOver && State is RunnerState.Running)
        {
            // Pause Icon
            return "\uE62E";
        }
        else
        {
            // Download Icon
            return "\uEBD3";
        }
    }


    public string ButtonText => GetButtonText();

    public string GetButtonText()
    {
        if (IsPredownloadFinished || State is RunnerState.Finish)
        {
            // 完成标志优先于任务状态，避免完成瞬间仍显示运行/清理状态。
            return Lang.DownloadGamePage_Finished;
        }
        if (State is RunnerState.Stop)
        {
            // 预下载
            return Lang.LauncherPage_PreInstall;
        }
        if (State is RunnerState.Queue)
        {
            // 排队中
            return Lang.StartGameButton_InQueue;
        }
        if (State is RunnerState.Running)
        {
            if (Step is RunnerStep.Cleaning)
            {
                // 预下载的清理阶段已无进度，按键固定显示 100%。
                return "100%";
            }
            if (Step is RunnerStep.Downloading)
            {
                // 下载阶段按键只显示百分比，完整阶段信息显示在浮层。
                return ProgressPercentText ?? "";
            }
            string phase = GameInstallProgressDisplay.GetPhaseText(State, Step);
            return string.IsNullOrEmpty(ProgressPercentText) ? phase : $"{phase} {ProgressPercentText}";
        }
        if (State is RunnerState.Paused)
        {
            // 已暂停
            return Lang.DownloadGamePage_Paused;
        }
        if (State is RunnerState.Error)
        {
            // 出错了
            return Lang.Common_Error;
        }
        return "";
    }


    public string InstallStateText
    {
        get => IsPredownloadFinished || State is RunnerState.Finish ? Lang.PreDownloadButton_PreInstallFinished : field;
        set => SetProperty(ref field, value);
    }



    private void Grid_Root_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        PointerOver = true;
        UpdateButtonState();
        if (IsPredownloadFinished || State is not RunnerState.Stop)
        {
            Popup_DownloadProgress.IsOpen = true;
        }
    }



    private void Grid_Root_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        PointerOver = false;
        UpdateButtonState();
        Popup_DownloadProgress.IsOpen = false;
    }



    private void UpdateButtonState()
    {
        OnPropertyChanged(nameof(IsButtonEnabled));
        OnPropertyChanged(nameof(ButtonIcon));
        OnPropertyChanged(nameof(ButtonText));
        OnPropertyChanged(nameof(InstallStateText));
    }



    public void UpdateGameInstallTaskState(GameInstallProgress task)
    {
        State = task.State;
        Step = task.Step;
        InstallStateText = GameInstallProgressDisplay.GetOperationPhaseText(task);

        bool hasPercent = GameInstallProgressDisplay.TryGetPercent(task, out double percent);
        ProgressPercentText = hasPercent ? GameInstallProgressDisplay.ToPercentText(percent) : null;

        DownloadBytesText = null;
        DownloadSpeedText = null;
        RemainTimeText = null;
        ErrorMessage = null;

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
        if (task.State is RunnerState.Running
            && task.Step is RunnerStep.Downloading
            && task.ProgressDisplay.HasFlag(ProgressDisplay.NetworkSpeed))
        {
            RemainTimeText = GameInstallProgressDisplay.ToRemainTimeText(task.RemainTimeSeconds);
        }
        if (task.State is RunnerState.Error)
        {
            ErrorMessage = string.IsNullOrWhiteSpace(task.ErrorMessage) ? Lang.DownloadGamePage_UnknownError : task.ErrorMessage;
        }

        UpdateButtonState();
    }





}
