using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core.HoYoPlay;
using Starward.GameInstall.Service;
using System.Diagnostics;
using System.IO.Compression;

namespace Starward.GameInstall.Step;

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
        GamePluginRelease? release = await _hoYoPlayService.GetGamePluginAsync(_context.GameId, cancellation).ConfigureAwait(false);
        if (release is null)
        {
            SkipStep = true;
        }
        else
        {
            List<GamePlugin> plugins = new();
            foreach (var item in release.Plugins)
            {
                if (item.PluginPackage.Url.Contains("DXSETUP", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
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
        _context.NetworkTotalBytes = _plugins.Sum(x => x.PluginPackage.Size);

        foreach (GamePlugin plugin in _plugins)
        {
            long size = plugin.PluginPackage.Size;
            string key = $"plugin_{plugin.PluginId}_version";
            try
            {
                _context.Step = RunnerStep.Verifying;
                if (await GameResourceService.VerifyPluginAsync(_context.InstallPath, plugin.PluginPackage, cancellation).ConfigureAwait(false))
                {
                    _context.AddNetworkBytes(size);
                    _context.ConfigIni.Settings[key] = plugin.Version;
                    _logger.LogInformation("Plugin {pluginId} verify successfully, skip.", plugin.PluginId);
                    continue;
                }
                _context.Step = RunnerStep.Downloading;
                string path = Path.Join(_context.InstallPath, Helper.GetUrlFileName(plugin.PluginPackage.Url));
                _logger.LogInformation("Start installing plugin {pluginId}, version {version}, size: {size} bytes.", plugin.PluginId, plugin.Version, size);
                await _polly.ExecuteAsync(async token =>
                {
                    await _downloadService.DownloadToFileAsync(path, plugin.PluginPackage.Url, size, _context, token).ConfigureAwait(false);
                    if (!await HashService.CheckFileMD5Async(path, size, plugin.PluginPackage.MD5, token).ConfigureAwait(false))
                    {
                        File.Delete(path);
                        _context.AddNetworkBytes(-size);
                        throw new FileMD5FailedException(path, plugin.PluginPackage.MD5);
                    }
                }, cancellation).ConfigureAwait(false);

                _context.Step = RunnerStep.Decompressing;
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
                        await process.WaitForExitAsync(cancellation).ConfigureAwait(false);
                        if (process.ExitCode is not 0)
                        {
                            string output = await outputTask.ConfigureAwait(false);
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