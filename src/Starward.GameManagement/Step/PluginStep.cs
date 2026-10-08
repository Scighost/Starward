using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core.HoYoPlay;
using Starward.GameManagement.Service;
using System.Diagnostics;
using System.IO.Compression;

namespace Starward.GameManagement.Step;

internal class PluginStep : StepBase
{

    private readonly ILogger<PluginStep> _logger;

    public PluginStep(GameInstallContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<PluginStep>>();
    }


    private List<GamePlugin>? _plugins;



    public override async Task PrepareAsync(CancellationToken cancellation = default)
    {
        GamePluginRelease? release = await _hoYoPlayService.GetGamePluginAsync(_context.GameId, cancellation);
        if (release is null)
        {
            _logger.LogInformation("Plugin resource is null, skip.");
            SkipStep = true;
        }
        else
        {
            List<GamePlugin> plugins = new();
            foreach (var item in release.Plugins)
            {
                if (item.PluginPackage.Url.Contains("DXSETUP"))
                {
                    continue;
                }
                string key = $"plugin_{item.PluginId}_version";
                if (_context.ConfigIni.Settings.TryGetValue(key, out string? value))
                {
                    if (value == item.Version)
                    {
                        continue;
                    }
                }
                plugins.Add(item);
            }
            if (plugins.Count > 0)
            {
                _plugins = plugins;
                _logger.LogInformation("Found {count} plugin(s) to install.", plugins.Count);
            }
            else
            {
                _logger.LogInformation("All plugins are up to date, skip.");
                SkipStep = true;
            }
        }
    }




    protected override async Task ExecuteInternalAsync(CancellationToken cancellation = default)
    {
        if (_plugins is null)
        {
            return;
        }
        _context.ClearProgress();
        _context.ProgressDisplay = ProgressDisplay.NetworkSpeed | ProgressDisplay.NetworkBytes;
        _context.DownloadTotalBytes = _plugins.Sum(x => x.PluginPackage.Size);

        foreach (var plugin in _plugins)
        {
            try
            {
                _context.Step = GameInstallStep.Downloading;
                string key = $"plugin_{plugin.PluginId}_version";
                string version = _context.ConfigIni.Settings.GetValueOrDefault(key, "");
                long size = plugin.PluginPackage.Size;
                if (version == plugin.Version)
                {
                    _logger.LogInformation("Plugin {pluginId} is up to date, skip.", plugin.PluginId);
                    _context.AddNetworkBytes(size);
                    continue;
                }
                string path = Path.Join(_context.InstallPath, Helper.GetUrlFileName(plugin.PluginPackage.Url));
                _logger.LogInformation("Start installing plugin {pluginId}, version {version}, size: {size} bytes.", plugin.PluginId, plugin.Version, size);
                await _polly.ExecuteAsync(async token =>
                {
                    await _downloadService.DownloadToFileAsync(path, plugin.PluginPackage.Url, size, _context, token);
                    if (!await HashService.CheckFileMD5Async(path, size, plugin.PluginPackage.MD5, token))
                    {
                        File.Delete(path);
                        _context.AddNetworkBytes(-size);
                        throw new FileMD5FailedException(path, plugin.PluginPackage.MD5);
                    }
                }, cancellation);

                _context.Step = GameInstallStep.Decompressing;
                ZipFile.ExtractToDirectory(path, _context.InstallPath, true);

                string command = plugin.PluginPackage.Command;
                if (!string.IsNullOrWhiteSpace(command))
                {
                    string exe, arg;
                    int space = command.IndexOf(' ');
                    if (space > 0)
                    {
                        exe = command.Substring(0, space);
                        arg = command.Substring(space + 1);
                    }
                    else
                    {
                        exe = command;
                        arg = "";
                    }
                    exe = Path.GetFullPath(Path.Join(_context.InstallPath, exe));
                    arg = arg.Trim();
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arg,
                        WorkingDirectory = _context.InstallPath,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                    };
                    var process = Process.Start(startInfo);
                    if (process is not null)
                    {
                        Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellation);
                        await process.WaitForExitAsync(cancellation);
                        if (process.ExitCode is not 0)
                        {
                            string output = await outputTask;
                            _logger.LogWarning("Plugin {pluginId} command exited with code {exitCode}. Output content:\r\n{Output}", plugin.PluginId, process.ExitCode, output);
                        }
                    }
                }

                File.Delete(path);
                _context.ConfigIni.Settings[key] = plugin.Version;
                _logger.LogInformation("Plugin {pluginId} installed, version {version}.", plugin.PluginId, plugin.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Install plugin {pluginId} failed.", plugin.PluginId);
            }
        }
    }





}