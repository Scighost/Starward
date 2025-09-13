using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.Extensions.Logging;

namespace Starward.Launcher.Services;

internal class LaunchService(
    ILogger<LaunchService> logger,
    FindExecutableService findExecutableService)
{
    private string? _executablePath;
    public string BasePath { get; set; } = AppContext.BaseDirectory;


    public Process? Launch(params IEnumerable<string> args)
    {
        try
        {
            _executablePath ??= findExecutableService.FindExecutable(BasePath);
            if (string.IsNullOrEmpty(_executablePath))
            {
                logger.LogCritical("Starward.exe not found");
                ShowNotFoundDialog();
                return null;
            }

            logger.LogInformation("Launching: {executable}", _executablePath);
            return Process.Start(_executablePath, args);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Launch failed");
            ShowNotFoundDialog();
        }

        return null;
    }

    public void RemoveOldVersions()
    {
        if (string.IsNullOrEmpty(_executablePath)) throw new InvalidOperationException("Call Launch first");

        try
        {
            var executable = new FileInfo(_executablePath);
            var current = executable.Directory!.Name;

            var oldVersions = new DirectoryInfo(BasePath)
                .EnumerateDirectories("app-*")
                .Select(d => d.Name)
                .Where(v => v != current);
            foreach (var version in oldVersions)
            {
                logger.LogInformation("Removing old version: {version}", version);
                Directory.Delete(version, true);
            }
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to remove old versions");
        }
    }

    private static void ShowNotFoundDialog()
    {
        PInvoke.SetProcessDPIAware();
        var result = PInvoke.MessageBox(
            HWND.Null,
            "Starward files not found.\r\nWould you like to download it now?\r\nhttps://github.com/Scighost/Starward",
            "Starward",
            MESSAGEBOX_STYLE.MB_ICONWARNING | MESSAGEBOX_STYLE.MB_OKCANCEL);
        if (result == MESSAGEBOX_RESULT.IDOK) Process.Start("https://github.com/Scighost/Starward");
    }
}