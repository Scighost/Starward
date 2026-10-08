using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Starward.Core.HoYoPlay;
using Starward.GameManagement.Service;

namespace Starward.GameManagement.Step;

internal class StepBase
{

    private bool _completed;

    protected readonly GameInstallContext _context;

    protected readonly HoYoPlayService _hoYoPlayService;

    protected readonly DownloadService _downloadService;

    protected readonly GameConfigService _gameConfigService;

    protected readonly GameResourceService _gameResourceService;

    protected readonly ILogger _stepLogger;

    protected readonly ResiliencePipeline _polly;


    public StepBase(GameInstallContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _hoYoPlayService = serviceProvider.GetRequiredService<HoYoPlayService>();
        _downloadService = serviceProvider.GetRequiredService<DownloadService>();
        _gameConfigService = serviceProvider.GetRequiredService<GameConfigService>();
        _gameResourceService = serviceProvider.GetRequiredService<GameResourceService>();
        _stepLogger = serviceProvider.GetRequiredService<ILogger<StepBase>>();
        _polly = new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            BackoffType = DelayBackoffType.Linear,
            OnRetry = args =>
            {
                _stepLogger.LogWarning(args.Outcome.Exception, "Operation failed, retry attempt {attempt}.", args.AttemptNumber + 1);
                return default;
            }
        }).Build();
    }


    public bool SkipStep { get; protected set; }



    public async Task ExecuteAsync(CancellationToken cancellation = default)
    {
        if (_completed)
        {
            return;
        }
        string step = GetType().Name;
        using IDisposable? scope = _stepLogger.BeginScope(new Dictionary<string, object?> { ["ExtraContext"] = $"[GameBiz={_context.GameId.GameBiz}, Operation={_context.Operation}, Step={step}]" });
        _stepLogger.LogInformation("Step started.");
        try
        {
            await ExecuteInternalAsync(cancellation);
            await PostAsync(cancellation);
            _completed = true;
            _stepLogger.LogInformation("Step finished.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _stepLogger.LogError(ex, "Step failed.");
            throw;
        }
    }


    public virtual Task PrepareAsync(CancellationToken cancellation = default)
    {
        return Task.CompletedTask;
    }


    protected virtual Task ExecuteInternalAsync(CancellationToken cancellation = default)
    {
        return Task.CompletedTask;
    }


    protected virtual Task PostAsync(CancellationToken cancellation = default)
    {
        return Task.CompletedTask;
    }



    protected static void CreateDirectoryForFile(string path)
    {
        string? folder = Path.GetDirectoryName(path);
        if (folder is not null)
        {
            Directory.CreateDirectory(folder);
        }
    }


    protected virtual Task MoveCachedResourceAsync(CancellationToken cancellation = default)
    {
        GameConfig config = _context.GameConfig;

        if (!string.IsNullOrWhiteSpace(config.AudioPackageResDir) && !string.IsNullOrWhiteSpace(config.AudioPackageCacheDir))
        {
            cancellation.ThrowIfCancellationRequested();
            string cacheDir = Path.Join(_context.InstallPath, config.AudioPackageCacheDir);
            if (Directory.Exists(cacheDir))
            {
                string resDir = Path.Join(_context.InstallPath, config.AudioPackageResDir);
                int count = MoveAllFiles(cacheDir, resDir, cancellation);
                if (count > 0)
                {
                    _stepLogger.LogInformation("Moved {count} audio package cache files from {cacheDir} to {resDir}.", count, cacheDir, resDir);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(config.LocalResDir) && !string.IsNullOrWhiteSpace(config.LocalResCacheDir))
        {
            cancellation.ThrowIfCancellationRequested();
            string resDir = Path.Join(_context.InstallPath, config.LocalResDir);
            string cacheDir = Path.Join(_context.InstallPath, config.LocalResCacheDir);
            if (Directory.Exists(resDir) && Directory.Exists(cacheDir))
            {
                int count = 0;
                foreach (string targetDir in Directory.GetDirectories(resDir))
                {
                    string sourceDir = Path.Join(cacheDir, Path.GetFileName(targetDir));
                    if (Directory.Exists(sourceDir))
                    {
                        count += MoveAllFiles(sourceDir, targetDir, cancellation);
                    }
                }
                if (count > 0)
                {
                    _stepLogger.LogInformation("Moved {count} local resource cache files from {cacheDir} to {resDir}.", count, cacheDir, resDir);
                }
            }
        }

        return Task.CompletedTask;
    }


    private static int MoveAllFiles(string sourceDir, string targetDir, CancellationToken cancellation = default)
    {
        int count = 0;
        foreach (string source in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            cancellation.ThrowIfCancellationRequested();
            string relative = Path.GetRelativePath(sourceDir, source);
            string target = Path.GetFullPath(Path.Combine(targetDir, relative));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Move(source, target, true);
            count++;
        }
        return count;
    }


}
