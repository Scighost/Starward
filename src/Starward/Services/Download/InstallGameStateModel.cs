using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Starward.Core;
using Starward.Helpers;
using Starward.Models;
using System;
using System.Diagnostics;

namespace Starward.Services.Download;

public partial class InstallGameStateModel : ObservableObject
{


    private const string PlayGlyph = "\uE768";


    private const string PauseGlyph = "\uE769";


    private const double GB = 1 << 30;




    internal InstallGameStateModel(InstallGameService service)
    {
        Service = service;
        GameBiz = Service.CurrentGameBiz;
        Icon = new GameBizIcon { GameBiz = Service.CurrentGameBiz };
        Service.StateChanged += _service_StateChanged;
        Service.InstallFailed += Service_InstallFailed;
    }



    internal InstallGameService Service { get; set; }


    public GameBiz GameBiz { get; set; }


    public GameBizIcon Icon { get; set; }



    public event EventHandler InstallStarted;


    public event EventHandler InstallFinished;


    public event EventHandler InstallFailed;


    public double SpeedBytesPerMiniute { get; set; }


    [ObservableProperty]
    private string _StateText;


    [ObservableProperty]
    private string _ButtonGlyph;


    [ObservableProperty]
    private double _Progress;


    [ObservableProperty]
    private string _ProgressText;


    [ObservableProperty]
    private bool _isActionButtonEnabled = true;


    private long _lastTimestamp;

    private long _lastFinishedBytes;


    [RelayCommand]
    private void ActionButton()
    {
        if (ButtonGlyph is PlayGlyph)
        {
            Service.Continue();
            InstallStarted?.Invoke(this, EventArgs.Empty);
        }
        else if (ButtonGlyph is PauseGlyph)
        {
            Service.Pause();
        }
    }




    public void UpdateState()
    {
        IsActionButtonEnabled = true;
        switch (Service.State)
        {
            case InstallGameState.None:
                StateText = "Paused";
                ButtonGlyph = PlayGlyph;
                break;
            case InstallGameState.Download:
                StateText = Lang.DownloadGamePage_Downloading;
                if (Service.TotalBytes == 0)
                {
                    Progress = 100;
                    ProgressText = "";
                }
                else
                {
                    Progress = 100d * Service.FinishBytes / Service.TotalBytes;
                    ProgressText = $"{Service.FinishBytes / GB:F2}/{Service.TotalBytes / GB:F2} GB";
                }
                ButtonGlyph = PauseGlyph;
                break;
            case InstallGameState.Verify:
                StateText = Lang.DownloadGamePage_Verifying;
                if (Service.TotalCount == 0)
                {
                    Progress = 100;
                    ProgressText = "";
                }
                else
                {
                    Progress = 100d * Service.FinishCount / Service.TotalCount;
                    ProgressText = $"{Service.FinishCount}/{Service.TotalCount}";
                }
                ButtonGlyph = PauseGlyph;
                break;
            case InstallGameState.Decompress:
                StateText = Lang.DownloadGamePage_Decompressing;
                if (Service.TotalBytes == 0)
                {
                    Progress = 100;
                    ProgressText = "";
                }
                else
                {
                    Progress = 100d * Service.FinishBytes / Service.TotalBytes;
                    ProgressText = $"{Service.FinishBytes / GB:F2}/{Service.TotalBytes / GB:F2} GB";
                }
                IsActionButtonEnabled = false;
                ButtonGlyph = PauseGlyph;
                break;
            case InstallGameState.Clean:
                IsActionButtonEnabled = false;
                ButtonGlyph = PauseGlyph;
                break;
            case InstallGameState.Finish:
                StateText = Lang.DownloadGamePage_Finished;
                Progress = 100;
                ProgressText = "";
                IsActionButtonEnabled = false;
                ButtonGlyph = PlayGlyph;
                InstallFinished?.Invoke(this, EventArgs.Empty);
                break;
            case InstallGameState.Error:
                StateText = Lang.DownloadGamePage_UnknownError;
                ButtonGlyph = PlayGlyph;
                InstallFailed?.Invoke(this, EventArgs.Empty);
                break;
            default:
                break;
        }
        long ts = Stopwatch.GetTimestamp();
        long bytes = Service.FinishBytes;
        SpeedBytesPerMiniute = Math.Clamp((double)(bytes - _lastFinishedBytes) / (ts - _lastTimestamp) / Stopwatch.Frequency, 0, long.MaxValue);
        _lastFinishedBytes = bytes;
        _lastTimestamp = ts;
    }



    private void _service_StateChanged(object? sender, InstallGameState e)
    {
        UpdateState();
    }


    private void Service_InstallFailed(object? sender, Exception e)
    {
        NotificationBehavior.Instance.Error(e, $"Game ({GameBiz.ToGameName()} - {GameBiz.ToGameServer()}) install failed.");

    }


}
