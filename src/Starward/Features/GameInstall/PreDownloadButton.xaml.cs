using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Starward.RPC.GameInstall;
using System;
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

    public GameInstallState State { get; set { if (SetProperty(ref field, value)) { UpdateButtonState(); } } }

    public bool IsPredownloadFinished { get; set { if (SetProperty(ref field, value)) { UpdateButtonState(); } } }

    public bool PointerOver { get; set => SetProperty(ref field, value); }

    public string ProgressPercentText { get; set => SetProperty(ref field, value); }

    public string? DownloadBytesText { get; set => SetProperty(ref field, value); }

    public string? DownloadSpeedText { get; set => SetProperty(ref field, value); }

    public string? RemainTimeText { get; set => SetProperty(ref field, value); }

    public string? ErrorMessage { get; set => SetProperty(ref field, value); }


    public bool IsButtonEnabled => !(IsPredownloadFinished || State is GameInstallState.Finish);


    public string ButtonIcon => GetButtonIcon();

    public string GetButtonIcon()
    {
        if (PointerOver && State is GameInstallState.Paused or GameInstallState.Error or GameInstallState.Queueing)
        {
            // Play Icon
            return "\uF5B0";
        }
        else if (PointerOver && State is GameInstallState.Downloading)
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
        if (!IsPredownloadFinished && State is GameInstallState.Stop)
        {
            // 预下载
            return Lang.LauncherPage_PreInstall;
        }
        else if (State is GameInstallState.Waiting or GameInstallState.Queueing)
        {
            // 等待中
            return Lang.StartGameButton_Waiting;
        }
        else if (State is GameInstallState.Downloading)
        {
            // 下载中
            return ProgressPercentText;
        }
        else if (State is GameInstallState.Paused)
        {
            // 已暂停
            return Lang.DownloadGamePage_Paused;
        }
        else if (IsPredownloadFinished || State is GameInstallState.Finish)
        {
            // 已完成
            return Lang.DownloadGamePage_Finished;
        }
        else if (State is GameInstallState.Error)
        {
            // 出错了
            return Lang.Common_Error;
        }
        else
        {
            return "State Error";
        }
    }


    public string InstallStateText => GetInstallStateText();

    public string GetInstallStateText()
    {
        if (State is GameInstallState.Waiting or GameInstallState.Queueing)
        {
            // 等待中
            return Lang.StartGameButton_Waiting;
        }
        else if (State is GameInstallState.Downloading)
        {
            // 下载中
            return Lang.DownloadGamePage_Downloading;
        }
        else if (State is GameInstallState.Paused)
        {
            // 已暂停
            return Lang.DownloadGamePage_Paused;
        }
        else if (IsPredownloadFinished || State is GameInstallState.Finish)
        {
            // 已完成
            return Lang.PreDownloadButton_PreInstallFinished;
        }
        else if (State is GameInstallState.Error)
        {
            // 出错了
            return Lang.DownloadGamePage_SomethingError;
        }
        else
        {
            return "State Error";
        }
    }



    private void Grid_Root_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        PointerOver = true;
        UpdateButtonState();
        if (IsPredownloadFinished || State is not GameInstallState.Stop)
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



    public void UpdateGameInstallTaskState(GameInstallContext task)
    {
        State = task.State;
        long total = task.Progress_DownloadTotalBytes;
        long finish = task.Progress_DownloadFinishBytes;
        ProgressPercentText = $"{(double)finish / total:P1}";
        DownloadBytesText = ToBytesText(finish, total);
        if (State is GameInstallState.Paused or GameInstallState.Queueing or GameInstallState.Error)
        {
            DownloadSpeedText = "- KB/s";
            RemainTimeText = "--:--:--";
        }
        else if (State is not GameInstallState.Finish)
        {
            DownloadSpeedText = ToSpeedText(task.NetworkDownloadSpeed);
            RemainTimeText = ToRemainTimeText(task.RemainTimeSeconds);
            ErrorMessage = task.ErrorMessage;
        }
        else
        {
            DownloadSpeedText = null;
            RemainTimeText = null;
            ErrorMessage = null;
        }
        UpdateButtonState();
    }




    private static string ToBytesText(long finish, long total)
    {
        const double MB = 1 << 20;
        const double GB = 1 << 30;
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





}
