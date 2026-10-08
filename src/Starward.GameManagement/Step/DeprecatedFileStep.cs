using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core.HoYoPlay;

namespace Starward.GameManagement.Step;

internal class DeprecatedFileStep : StepBase
{

    private readonly ILogger<DeprecatedFileStep> _logger;

    public DeprecatedFileStep(GameInstallContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<DeprecatedFileStep>>();
    }


    private GameDeprecatedFileConfig? _deprecated;



    public override async Task PrepareAsync(CancellationToken cancellation)
    {
        _deprecated ??= await _hoYoPlayService.GetGameDeprecatedFileConfigAsync(_context.GameId, cancellation);
        if (_deprecated is null)
        {
            _logger.LogInformation("Deprecated file config is null, skip.");
            SkipStep = true;
        }
    }



    protected override async Task ExecuteInternalAsync(CancellationToken cancellation)
    {
        if (_deprecated is not null)
        {
            int count = 0;
            foreach (GameDeprecatedFile item in _deprecated.DeprecatedFiles)
            {
                string path = Path.Join(_context.InstallPath, item.Name);
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fail to delete file: {path}", path);
                }
            }
            if (count > 0)
            {
                _logger.LogInformation("Deleted {count} deprecated files.", count);
            }
        }
    }

}
