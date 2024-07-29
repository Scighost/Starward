using CommunityToolkit.Mvvm.Messaging;
using Starward.Core;
using Starward.Helpers;
using Starward.Messages;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Starward.Services.Download;

internal class InstallGameManager
{


    private readonly ConcurrentDictionary<GameBiz, InstallGameStateModel> _services = new();


    private InstallGameManager()
    {
        _services = new();
        long speed = AppConfig.SpeedLimitKBPerSecond * 1024;
        SpeedLimitBytesPerSecond = speed == 0 ? long.MaxValue : speed;
    }


    private static InstallGameManager _instance;
    public static InstallGameManager Instance => _instance ??= new();



    public static long DownloadBytesInSecond;


    public static long SpeedLimitBytesPerSecond { get; set; }


    public static bool IsExceedSpeedLimit => Interlocked.Read(ref DownloadBytesInSecond) >= SpeedLimitBytesPerSecond;


    private long _lastTimeStamp;


    public void UpdateSpeedState()
    {
        long ts = Stopwatch.GetTimestamp();
        if (ts - _lastTimeStamp >= Stopwatch.Frequency)
        {
            DownloadBytesInSecond = 0;
        }
    }



    public event EventHandler<InstallGameStateModel> InstallTaskAdded;



    public event EventHandler<InstallGameStateModel> InstallTaskRemoved;




    public bool TryGetInstallService(GameBiz gameBiz, [NotNullWhen(true)] out InstallGameService? service)
    {
        if (_services.TryGetValue(gameBiz, out var model))
        {
            service = model.Service;
            return true;
        }
        else
        {
            service = null;
            return false;
        }
    }



    public void AddInstallService(InstallGameService service)
    {
        var model = new InstallGameStateModel(service);
        _services[service.CurrentGameBiz] = model;
        model.InstallFinished -= Model_InstallFinished;
        model.InstallFinished += Model_InstallFinished;
        model.InstallFailed -= Model_InstallFailed;
        model.InstallFailed += Model_InstallFailed;
        model.InstallCanceled -= Model_InstallCanceled;
        model.InstallCanceled += Model_InstallCanceled;
        InstallTaskAdded?.Invoke(this, model);
    }



    private void Model_InstallFinished(object? sender, EventArgs e)
    {
        if (sender is InstallGameStateModel model)
        {
            model.Service.ClearState();
            _services.TryRemove(model.GameBiz, out _);
            model.InstallFinished -= Model_InstallFinished;
            model.InstallFailed -= Model_InstallFailed;
            model.InstallCanceled -= Model_InstallCanceled;
            InstallTaskRemoved?.Invoke(this, model);
            WeakReferenceMessenger.Default.Send(new InstallGameFinishedMessage(model.GameBiz));
            NotificationBehavior.Instance.Success(Lang.InstallGameManager_DownloadTaskCompleted, $"{InstallTaskToString(model.Service.InstallTask)} - {model.GameBiz.ToGameName()} - {model.GameBiz.ToGameServer()}", 0);
        }
    }



    private void Model_InstallFailed(object? sender, Exception e)
    {
        if (sender is InstallGameStateModel model)
        {
            NotificationBehavior.Instance.Error(e, $"{Lang.InstallGameManager_DownloadTaskFailed} ({InstallTaskToString(model.Service.InstallTask)} - {model.GameBiz.ToGameName()} - {model.GameBiz.ToGameServer()})", 0);
        }
    }



    private void Model_InstallCanceled(object? sender, EventArgs e)
    {
        if (sender is InstallGameStateModel model)
        {
            model.Service.Pause();
            model.Service.ClearState();
            _services.TryRemove(model.GameBiz, out _);
            model.InstallFinished -= Model_InstallFinished;
            model.InstallFailed -= Model_InstallFailed;
            model.InstallCanceled -= Model_InstallCanceled;
            InstallTaskRemoved?.Invoke(this, model);
        }
    }



    public static string InstallTaskToString(InstallGameTask task)
    {
        return task switch
        {
            InstallGameTask.Install => Lang.LauncherPage_InstallGame,
            InstallGameTask.Repair => Lang.LauncherPage_RepairGame,
            InstallGameTask.Predownload => Lang.LauncherPage_PreInstall,
            InstallGameTask.Update => Lang.LauncherPage_UpdateGame,
            InstallGameTask.Symbol => Lang.InstallGameDialog_SymbolicLink,
            _ => "",
        };
    }


}
