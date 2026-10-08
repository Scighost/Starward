using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core;

namespace Starward.GameManagement.Step;


internal class PrepareStep : StepBase
{

    private readonly ILogger<PrepareStep> _logger;

    public PrepareStep(GameInstallContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<PrepareStep>>();
    }



    protected override async Task ExecuteInternalAsync(CancellationToken cancellation = default)
    {
        _logger.LogInformation("Prepare install task, path: {path}, audio: {audio}, category: {category}.", _context.InstallPath, _context.AudioLanguage, _context.CategoryScenario);

        _context.ClearProgress();
        _context.Step = GameInstallStep.Pending;
        _context.ProgressDisplay = ProgressDisplay.None;

        Directory.CreateDirectory(_context.InstallPath);

        if (_context.AudioLanguage is not AudioLanguage.None)
        {
            await _gameConfigService.SetAudioLanguageAsync(_context.GameId, _context.InstallPath, _context.AudioLanguage, cancellation);
        }

        if (!string.IsNullOrWhiteSpace(_context.CategoryScenario))
        {
            await _gameConfigService.SetCategoryScenarioAsync(_context.GameId, _context.InstallPath, _context.CategoryScenario, cancellation);
        }

        foreach (string file in Directory.GetFiles(_context.InstallPath, "*", SearchOption.AllDirectories))
        {
            cancellation.ThrowIfCancellationRequested();
            File.SetAttributes(file, FileAttributes.Normal);
        }
    }


}
