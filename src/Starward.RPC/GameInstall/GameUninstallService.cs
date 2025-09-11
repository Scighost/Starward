using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace Starward.RPC.GameInstall;

internal class GameUninstallService
{

    private readonly ILogger<GameUninstallService> _logger;


    private readonly HoYoPlayClient _hoYoPlayClient;


    public GameUninstallService(ILogger<GameUninstallService> logger, HoYoPlayClient hoYoPlayClient)
    {
        _logger = logger;
        _hoYoPlayClient = hoYoPlayClient;
    }





    public async Task UninstallGameAsync(UninstallGameRequest request, CancellationToken cancellationToken = default)
    {
        string installPath = request.InstallPath;
        if (!Directory.Exists(installPath))
        {
            _logger.LogWarning("Game folder does not exist: {installPath}", installPath);
        }
        if (Path.GetPathRoot(installPath) == installPath)
        {
            _logger.LogError("Game folder is the root of drive.");
            throw new InvalidOperationException("Game folder is the root of drive.");
        }
        _logger.LogInformation("Start to uninstall game ({gameBiz}): {installPath}", request.GameBiz, installPath);
        GameId gameId = new GameId { GameBiz = request.GameBiz, Id = request.GameId };
        GameConfig? gameConfig = null;
        try
        {
            gameConfig = await _hoYoPlayClient.GetGameConfigAsync(LauncherId.FromGameId(gameId)!, "en-us", gameId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game config.");
        }
        string[] files = Directory.GetFiles(installPath, "*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
        BackupScreenshot(request, gameConfig);
        _logger.LogInformation("Deleting folder {installPath} ({count} files).", installPath, files.Length);
        Directory.Delete(installPath, true);
        ClearCacheDir(request, gameConfig);
        _logger.LogInformation("Finished uninstall game ({gameBiz}): {installPath}", request.GameBiz, installPath);
    }




    private void BackupScreenshot(UninstallGameRequest request, GameConfig? gameConfig)
    {
        string userDataFolder = request.UserDataFolder;
        string sourceScreenshotFolder;
        if (gameConfig is null)
        {
            string[] dirs = Directory.GetDirectories(request.InstallPath, "Screenshot*", SearchOption.AllDirectories);
            sourceScreenshotFolder = dirs.FirstOrDefault() ?? "";
        }
        else
        {
            sourceScreenshotFolder = Path.Join(request.InstallPath, gameConfig.GameScreenshotDir);
        }

        string backupBaseFolder;
        string backupScreenshotFolder;
        if (Directory.Exists(request.ScreenshotFolder))
        {
            backupBaseFolder = request.ScreenshotFolder;
        }
        else
        {
            backupBaseFolder = Path.Join(userDataFolder, "Screenshots");
        }
        if (!string.IsNullOrWhiteSpace(request.GameExeName))
        {
            backupScreenshotFolder = Path.Join(backupBaseFolder, Path.GetFileNameWithoutExtension(request.GameExeName));
        }
        else if (!string.IsNullOrWhiteSpace(gameConfig?.ExeFileName))
        {
            backupScreenshotFolder = Path.Join(backupBaseFolder, Path.GetFileNameWithoutExtension(gameConfig.ExeFileName));
        }
        else
        {
            backupScreenshotFolder = Path.Join(backupBaseFolder, ((GameBiz)request.GameBiz).Game);
        }
        if (Directory.Exists(userDataFolder) && Directory.Exists(sourceScreenshotFolder))
        {
            bool canHardLink = CanHardLink(sourceScreenshotFolder, backupScreenshotFolder);
            Directory.CreateDirectory(backupScreenshotFolder);
            string[] files = Directory.GetFiles(sourceScreenshotFolder);
            int count = 0;
            foreach (string file in files)
            {
                string target = Path.Join(backupScreenshotFolder, Path.GetFileName(file));
                if (!File.Exists(target))
                {
                    bool result = false;
                    if (canHardLink)
                    {
                        result = Kernel32.CreateHardLink(target, file);
                    }
                    if (!result)
                    {
                        File.Copy(file, target, true);
                    }
                    count++;
                }
            }
            _logger.LogInformation("Backed up {count} screenshots.", count);
        }
    }



    private void ClearCacheDir(UninstallGameRequest request, GameConfig? gameConfig)
    {
        if (gameConfig is null)
        {
            return;
        }
        if (!string.IsNullOrWhiteSpace(gameConfig.GameLogGenDir))
        {
            string path = Environment.ExpandEnvironmentVariables(gameConfig.GameLogGenDir);
            if (Path.IsPathFullyQualified(path))
            {
                if (path.Contains("miHoYo", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("Cognosphere", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("HoYoverse", StringComparison.OrdinalIgnoreCase))
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                        _logger.LogInformation("Deleted folder {path}", path);
                    }
                }
            }
        }
        if (!string.IsNullOrWhiteSpace(gameConfig.GameCrashFileGenDir))
        {
            string path = Environment.ExpandEnvironmentVariables(gameConfig.GameCrashFileGenDir);
            if (Path.IsPathFullyQualified(path))
            {
                // 防止原神把 %UserProfile%/AppData/Local/Temp 删了
                if (path.Contains("miHoYo", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("Cognosphere", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("HoYoverse", StringComparison.OrdinalIgnoreCase))
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                        _logger.LogInformation("Deleted folder {path}", path);
                    }
                }
            }
        }
    }




    private static bool CanHardLink(string source, string dest)
    {
        return Path.GetPathRoot(source) == Path.GetPathRoot(dest) && DriveHelper.GetDriveFormat(dest) is "NTFS";
    }




}
