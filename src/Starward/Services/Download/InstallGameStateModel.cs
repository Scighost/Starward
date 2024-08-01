using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Starward.Core;
using Starward.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services.Download;

public partial class InstallGameStateModel : ObservableObject
{


    private const string PlayGlyph = "\uE768";

    private const string PauseGlyph = "\uE769";

    private const double GB = 1 << 30;

    private const double MB = 1 << 20;

    private const double KB = 1 << 10;



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


    public event EventHandler InstallCanceled;


    public event EventHandler<Exception> InstallFailed;



    [ObservableProperty]
    private string _StateText;


    [ObservableProperty]
    private string _ButtonGlyph;


    [ObservableProperty]
    private double _ProgressValue;


    [ObservableProperty]
    private string _ProgressValueText;


    [ObservableProperty]
    private string _ProgressText;


    [ObservableProperty]
    private string? _SpeedText;


    [ObservableProperty]
    private string? _RemainingTimeText;


    [ObservableProperty]
    private bool _isContinueOrPauseButtonEnabled = true;


    private long _lastTimestamp;

    private long _lastFinishedBytes;

    public double _speedBytesPerSecond;

    private List<double> _recentSpeed = [];

    private readonly SynchronizationContext uiContext = SynchronizationContext.Current!;



    [RelayCommand]
    private void ContinueOrPause()
    {
        if (ButtonGlyph is PlayGlyph)
        {
            Task.Run(() =>
            {
                Task.WhenAll(Service.TaskItems).Wait();
                Service.Continue();
                InstallStarted?.Invoke(this, EventArgs.Empty);
            });
        }
        else if (ButtonGlyph is PauseGlyph)
        {
            Service.Pause();
        }
    }



    [RelayCommand]
    private void Cancel()
    {
        InstallCanceled?.Invoke(this, EventArgs.Empty);
    }



    public void UpdateState()
    {
        if (Service.HTTP_BUFFER_SIZE != InstallGameManager.BUFFER_SIZE || Service.IsEnableSpeedLimit != InstallGameManager.IsEnableSpeedLimit)
        {
            Interlocked.Exchange(ref Service.HTTP_BUFFER_SIZE, InstallGameManager.BUFFER_SIZE);
            Service.IsEnableSpeedLimit = InstallGameManager.IsEnableSpeedLimit;
            Service.Pause();
            Task.Run(() =>
            {
                Task.WhenAll(Service.TaskItems).Wait();
                Service.Continue();
                InstallStarted?.Invoke(this, EventArgs.Empty);
            });
        }
        try
        {
            IsContinueOrPauseButtonEnabled = true;
            switch (Service.State)
            {
                case InstallGameState.None:
                    StateText = Lang.DownloadGamePage_Paused;
                    ButtonGlyph = PlayGlyph;
                    break;
                case InstallGameState.Download:
                    StateText = Lang.DownloadGamePage_Downloading;
                    if (Service.TotalBytes == 0)
                    {
                        ProgressValue = 100;
                        ProgressText = "";
                    }
                    else
                    {
                        ProgressValue = 100d * Service.FinishBytes / Service.TotalBytes;
                        ProgressText = $"{Service.FinishBytes / GB:F2}/{Service.TotalBytes / GB:F2} GB";
                    }
                    ButtonGlyph = PauseGlyph;
                    break;
                case InstallGameState.Verify:
                    StateText = Lang.DownloadGamePage_Verifying;
                    if (Service.TotalCount == 0)
                    {
                        ProgressValue = 100;
                        ProgressText = "";
                    }
                    else if (Service.InstallTask is InstallGameTask.Repair)
                    {
                        ProgressValue = 100d * Service.FinishCount / Service.TotalCount;
                        ProgressText = $"{Service.FinishCount}/{Service.TotalCount}";
                    }
                    else
                    {
                        ProgressValue = 100d * Service.FinishBytes / Service.TotalBytes;
                        ProgressText = $"{Service.FinishBytes / GB:F2}/{Service.TotalBytes / GB:F2} GB";
                    }
                    ButtonGlyph = PauseGlyph;
                    break;
                case InstallGameState.Decompress:
                    StateText = Lang.DownloadGamePage_Decompressing;
                    if (Service.TotalBytes == 0)
                    {
                        ProgressValue = 100;
                        ProgressText = "";
                    }
                    else
                    {
                        ProgressValue = 100d * Service.FinishBytes / Service.TotalBytes;
                        ProgressText = $"{Service.FinishBytes / GB:F2}/{Service.TotalBytes / GB:F2} GB";
                    }
                    IsContinueOrPauseButtonEnabled = false;
                    ButtonGlyph = PauseGlyph;
                    break;
                case InstallGameState.Clean:
                    IsContinueOrPauseButtonEnabled = false;
                    ButtonGlyph = PauseGlyph;
                    break;
                case InstallGameState.Finish:
                    StateText = Lang.DownloadGamePage_Finished;
                    ProgressValue = 100;
                    ProgressText = "";
                    IsContinueOrPauseButtonEnabled = false;
                    ButtonGlyph = PlayGlyph;
                    InstallFinished?.Invoke(this, EventArgs.Empty);
                    break;
                case InstallGameState.Error:
                    StateText = Lang.DownloadGamePage_UnknownError;
                    ButtonGlyph = PlayGlyph;
                    break;
                default:
                    break;
            }
            ProgressValueText = $"{ProgressValue / 100:P2}";
            ComputeSpeed(Service.State);
        }
        catch { }
    }



    private void ComputeSpeed(InstallGameState state)
    {
        try
        {
            long ts = Stopwatch.GetTimestamp();
            if (ts - _lastTimestamp >= Stopwatch.Frequency)
            {
                long bytes = Service.FinishBytes;
                double averageSpeed = 0;
                _speedBytesPerSecond = Math.Clamp((double)(bytes - _lastFinishedBytes) / (ts - _lastTimestamp) * Stopwatch.Frequency, 0, long.MaxValue);
                _lastFinishedBytes = bytes;
                _lastTimestamp = ts;
                if (state is InstallGameState.None or InstallGameState.Finish or InstallGameState.Error)
                {
                    SpeedText = null;
                    RemainingTimeText = null;
                }
                else
                {
                    if (_speedBytesPerSecond == 0)
                    {
                        RemainingTimeText = null;
                    }
                    else
                    {
                        _recentSpeed.RemoveAll(value => Math.Abs(value - _speedBytesPerSecond) / _speedBytesPerSecond > 0.25);
                        _recentSpeed.RemoveRange(0, Math.Max(_recentSpeed.Count - 9, 0));
                        _recentSpeed.Add(_speedBytesPerSecond);
                        averageSpeed = _recentSpeed.Average();
                        var seconds = (Service.TotalBytes - Service.FinishBytes) / averageSpeed;
                        RemainingTimeText = TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
                    }
                    if (_speedBytesPerSecond >= MB)
                    {
                        SpeedText = $"{averageSpeed / MB:F2} MB/s";
                    }
                    else
                    {
                        SpeedText = $"{averageSpeed / KB:F2} KB/s";
                    }
                }
            }
        }
        catch { }
    }



    private void _service_StateChanged(object? sender, InstallGameState e)
    {
        uiContext.Post(_ => UpdateState(), null);
    }



    private void Service_InstallFailed(object? sender, Exception e)
    {
        InstallFailed?.Invoke(this, e);
    }


}
