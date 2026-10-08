using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Starward.GameManagement.Step;


internal class ConfigIniStep : StepBase
{

    private readonly ILogger<ConfigIniStep> _logger;

    public ConfigIniStep(GameInstallContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<ConfigIniStep>>();
    }



    protected override async Task ExecuteInternalAsync(CancellationToken cancellation = default)
    {
        _context.ClearProgress();
        _context.Step = GameInstallStep.Cleaning;
        _context.ProgressDisplay = ProgressDisplay.None;

        CleanTempFiles(cancellation);

        await GameConfigService.SetGameConfigIniAsync(_context.InstallPath, _context.ConfigIni, cancellation);
    }



    private void CleanTempFiles(CancellationToken cancellation)
    {
        int count = 0;
        foreach (string pattern in new string[] { "*.tmp", "*_tmp" })
        {
            foreach (string file in Directory.GetFiles(_context.InstallPath, pattern, SearchOption.AllDirectories))
            {
                cancellation.ThrowIfCancellationRequested();
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temp file: {file}.", file);
                }
            }
        }
        if (count > 0)
        {
            _logger.LogInformation("Deleted {count} temp files.", count);
        }
    }

}
