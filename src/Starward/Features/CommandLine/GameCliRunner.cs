using Microsoft.Extensions.Configuration;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.GameInstall;
using Starward.Features.GameLauncher;
using Starward.RPC.GameInstall;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace Starward.Features.CommandLine;

internal static class GameCliRunner
{


    private enum UpdateGameAction
    {
        Check,
        Update,
        Repair,
    }


    public static async Task StartGameAsync(string[] args)
    {
        try
        {
            IConfiguration config = new ConfigurationBuilder().AddCommandLine(args).Build();
            GameBiz biz = ParseGameBiz(config.GetValue<string>("biz"));
            string? installPath = GetInstallPath(config) ?? GameLauncherService.GetGameInstallPath(biz);
            GameId gameId = GameId.FromGameBiz(biz) ?? throw new ArgumentException($"Unknown game biz: {biz}.");
            Process? process = await AppConfig.GetService<GameLauncherService>().StartGameAsync(gameId, installPath);
            if (process is null)
            {
                Log($"启动游戏失败: biz={biz}, reason=未获取到进程");
            }
            else
            {
                Log($"启动游戏成功: biz={biz}, pid={process.Id}, install_path={installPath ?? "<null>"}");
            }
        }
        catch (Exception ex)
        {
            Log($"启动游戏失败: {ex.Message}");
        }
    }



    public static async Task UpdateGameAsync(string[] args)
    {
        try
        {
            IConfiguration config = new ConfigurationBuilder().AddCommandLine(args).Build();
            UpdateGameAction action = ParseAction(config.GetValue<string>("action"));

            GameBiz biz = ParseGameBiz(config.GetValue<string>("biz"));
            GameId gameId = GameId.FromGameBiz(biz) ?? throw new ArgumentException($"Unknown game biz: {biz}.");
            string? installPath = GetInstallPath(config) ?? GameLauncherService.GetGameInstallPath(biz);

            GameLauncherService gameLauncherService = AppConfig.GetService<GameLauncherService>();
            GamePackageService gamePackageService = AppConfig.GetService<GamePackageService>();
            GameInstallService gameInstallService = AppConfig.GetService<GameInstallService>();

            switch (action)
            {
                case UpdateGameAction.Check:
                {
                    await CheckUpdateStateAsync(gameLauncherService, gamePackageService, gameId, biz, installPath);
                    return;
                }

                case UpdateGameAction.Update:
                {
                    await RunUpdateOrRepairAsync(gameLauncherService, gamePackageService, gameInstallService, gameId, biz, installPath, isRepair: false);
                    return;
                }

                case UpdateGameAction.Repair:
                {
                    await RunUpdateOrRepairAsync(gameLauncherService, gamePackageService, gameInstallService, gameId, biz, installPath, isRepair: true);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Log($"updategame 执行失败: {ex.Message}");
        }
    }



    private static async Task CheckUpdateStateAsync(GameLauncherService gameLauncherService, GamePackageService gamePackageService, GameId gameId, GameBiz biz, string? installPath)
    {
        if (string.IsNullOrWhiteSpace(installPath))
        {
            Log($"检查预下载/更新失败: biz={biz}, reason=install_path_not_found");
            return;
        }

        Version? localVersion = await gameLauncherService.GetLocalGameVersionAsync(gameId, installPath).ConfigureAwait(false);
        (Version? latestVersion, Version? predownloadVersion) = await gameLauncherService.GetLatestGameVersionAsync(gameId).ConfigureAwait(false);

        bool needUpdate = localVersion is not null && latestVersion > localVersion;
        bool hasPredownload = localVersion is not null && predownloadVersion > localVersion;
        bool predownloadFinished = false;
        if (hasPredownload)
        {
            predownloadFinished = await gamePackageService.CheckPreDownloadFinishedAsync(gameId, installPath).ConfigureAwait(false);
        }

        Log($"检查预下载/更新成功: biz={biz}, local={localVersion?.ToString() ?? "<null>"}, latest={latestVersion?.ToString() ?? "<null>"}, predownload={predownloadVersion?.ToString() ?? "<null>"}, need_update={needUpdate}, need_predownload={hasPredownload && !predownloadFinished}, predownload_finished={predownloadFinished}");
    }



    private static async Task RunUpdateOrRepairAsync(GameLauncherService gameLauncherService, GamePackageService gamePackageService, GameInstallService gameInstallService, GameId gameId, GameBiz biz, string? installPath, bool isRepair)
    {
        string actionName = isRepair ? "修复" : "更新";

        if (string.IsNullOrWhiteSpace(installPath))
        {
            Log($"{actionName}游戏失败: biz={biz}, reason=install_path_not_found");
            return;
        }

        if (!isRepair)
        {
            Version? localVersion = await gameLauncherService.GetLocalGameVersionAsync(gameId, installPath).ConfigureAwait(false);
            (Version? latestVersion, _) = await gameLauncherService.GetLatestGameVersionAsync(gameId).ConfigureAwait(false);
            if (localVersion is null || latestVersion is null || latestVersion <= localVersion)
            {
                Log($"更新游戏无需执行: biz={biz}, local={localVersion?.ToString() ?? "<null>"}, latest={latestVersion?.ToString() ?? "<null>"}");
                return;
            }
        }

        AudioLanguage audio = await gamePackageService.GetAudioLanguageAsync(gameId, installPath).ConfigureAwait(false);
        GameInstallContext? task = isRepair
            ? await gameInstallService.StartRepairAsync(gameId, installPath, audio).ConfigureAwait(false)
            : await gameInstallService.StartUpdateAsync(gameId, installPath, audio).ConfigureAwait(false);

        if (task is null)
        {
            Log($"{actionName}游戏失败: biz={biz}, reason=start_{(isRepair ? "repair" : "update")}_failed");
            return;
        }

        Log($"{actionName}任务已启动: biz={biz}, state={task.State}, install_path={installPath}");
        await TrackInstallTaskAsync(gameInstallService, gameId, task, actionName).ConfigureAwait(false);
    }



    private static async Task<bool> TrackInstallTaskAsync(GameInstallService gameInstallService, GameId gameId, GameInstallContext task, string action)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromHours(12));

        GameInstallState? lastState = null;
        string? lastLine = null;

        while (!cts.IsCancellationRequested)
        {
            GameInstallContext current = gameInstallService.GetGameInstallTask(gameId) ?? task;
            task = current;

            string line = BuildProgressLine(action, task);
            if (task.State != lastState || line != lastLine)
            {
                Log(line);
                lastState = task.State;
                lastLine = line;
            }

            if (task.State is GameInstallState.Finish)
            {
                Log($"{action}完成: state={task.State}");
                return true;
            }

            if (task.State is GameInstallState.Stop or GameInstallState.Error)
            {
                Log($"{action}失败: state={task.State}, error={task.ErrorMessage ?? "<null>"}");
                return false;
            }

            await Task.Delay(1000, cts.Token).ConfigureAwait(false);
        }

        Log($"{action}超时: state={task.State}, error={task.ErrorMessage ?? "<null>"}");
        return false;
    }



    private static string BuildProgressLine(string action, GameInstallContext task)
    {
        double progress = GameInstallProgressFormatter.GetProgressPercent(task);
        string percent = $"{progress:P1}";
        string downloadBytes = GameInstallProgressFormatter.ToBytesText(task.Progress_DownloadFinishBytes, task.Progress_DownloadTotalBytes) ?? "-";
        string installBytes = GameInstallProgressFormatter.ToBytesText(task.Progress_WriteFinishBytes, task.Progress_WriteTotalBytes) ?? "-";
        string downloadSpeed = GameInstallProgressFormatter.ToSpeedText(task.NetworkDownloadSpeed);
        string installSpeed = GameInstallProgressFormatter.ToSpeedText(task.StorageWriteSpeed);
        string verifySpeed = GameInstallProgressFormatter.ToSpeedText(task.StorageReadSpeed);
        string remain = task.State is GameInstallState.Finish
            ? "00:00:00"
            : GameInstallProgressFormatter.ToRemainTimeText(task.RemainTimeSeconds);
        string stateText = GameInstallProgressFormatter.GetInstallStateText(task.State);

        return $"{action}进度: state={task.State}({stateText}), percent={percent}, download={downloadBytes}, write={installBytes}, net={downloadSpeed}, write_speed={installSpeed}, verify_speed={verifySpeed}, remain={remain}";
    }



    private static UpdateGameAction ParseAction(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "check" => UpdateGameAction.Check,
            "update" => UpdateGameAction.Update,
            "repair" => UpdateGameAction.Repair,
            _ => throw new ArgumentException("Missing or invalid argument --action, valid values: check | update | repair."),
        };
    }



    private static GameBiz ParseGameBiz(string? value)
    {
        if (!GameBiz.TryParse(value, out GameBiz biz))
        {
            throw new ArgumentException("Missing or invalid argument --biz.");
        }
        return biz;
    }



    private static string? GetInstallPath(IConfiguration config)
    {
        return config.GetValue<string>("install_path")
            ?? config.GetValue<string>("install-path")
            ?? config.GetValue<string>("path");
    }



    static GameCliRunner()
    {
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        Console.OutputEncoding = utf8;
        Console.SetOut(new System.IO.StreamWriter(Console.OpenStandardOutput(), utf8) { AutoFlush = true });
        Console.SetError(new System.IO.StreamWriter(Console.OpenStandardError(), utf8) { AutoFlush = true });
    }



    private static void Log(string message)
    {
        Console.Write($"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
        Console.Out.Flush();
    }
}

