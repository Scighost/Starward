using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Starward.Launcher.Services;

internal class FindExecutableService(
    ILogger<FindExecutableService> logger,
    VersionService versionService)
{
    public string? FindExecutable(string basePath)
    {
        return FromVersion(basePath) ?? FromSubdirectories(basePath);
    }

    private string? FromVersion(string basePath)
    {
        var path = Path.Combine(basePath, "version");
        var exePath = versionService.TryReadJson($"{path}.json") ??
                      versionService.TryReadIni($"{path}.ini");
        if (File.Exists(exePath)) return exePath;
        logger.LogWarning("Failed to load executable path from version file, or file not found: {path}",
            exePath);
        return null;

    }

    private string? FromSubdirectories(string basePath)
    {
        var ret = Directory.EnumerateDirectories(basePath)
            .Select(p => Path.Combine(p, "Starward.exe"))
            .Where(File.Exists)
            .MaxBy(File.GetLastWriteTime);
        if (ret is null) logger.LogWarning("No executable found in subdirectories of {basePath}", basePath);
        return ret;
    }
}