﻿using CommunityToolkit.Mvvm.Messaging;
using Starward.Core;
using Starward.Helpers;
using Starward.Messages;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;

namespace Starward.Services.Download;

internal class InstallGameManager
{


    private readonly ConcurrentDictionary<GameBiz, InstallGameStateModel> _services = new();


    private InstallGameManager()
    {
        _services = new();
        int speed = AppConfig.SpeedLimitKBPerSecond * 1024;
        rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokensPerPeriod = SpeedLimitBytesPerPeriod,
            ReplenishmentPeriod = SpeedLimitReplenishmentPeriod,
            TokenLimit = SpeedLimitBytesPerPeriod,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });
    }


    private static InstallGameManager _instance;
    public static InstallGameManager Instance => _instance ??= new();



    public static long DownloadBytesInSecond;


    public static int SpeedLimitBytesPerSecond { get; set; }


    public static int SpeedLimitBytesPerPeriod => Math.Max((int)Math.Floor((double)SpeedLimitBytesPerSecond / 40), 1 << 14);


    // 25: 将每秒切割为上面的40份，间隔越小速度越精准。
    // 因为buffer较小，间隔极小的话请求令牌的速度将大于补充令牌逻辑运行的时间，导致无论限速多少实际速度只会在1MB/s左右。
    public static TimeSpan SpeedLimitReplenishmentPeriod => SpeedLimitBytesPerPeriod == 1 << 14 ? TimeSpan.FromSeconds((1 << 14) / (double)SpeedLimitBytesPerSecond) : TimeSpan.FromMilliseconds(25);


    public static TokenBucketRateLimiter rateLimiter;


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
            InstallGameTask.HardLink => Lang.LauncherPage_HardLink,
            _ => "",
        };
    }


}
