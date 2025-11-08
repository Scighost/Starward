using Polly;
using Polly.Retry;
using System.CommandLine;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace BuildTool;

public class ReleaseCommand
{

    private readonly HttpClient _httpClient;

    private readonly ResiliencePipeline _polly;


    public Command Command { get; set; } = new Command("release", "Create release info file.");

    private Command _createCommand = new Command("create", "Create release info file.");

    private Command _combineCommand = new Command("combine", "Combine release info files.");

    private Argument<string> _outputFileArgument = new Argument<string>("outputFile") { Description = "Output file path of release info." };

    private Option<string> _versionOption = new Option<string>("--version", "-v") { Description = "Release version.", Required = true };

    private Option<Architecture> _archOption = new Option<Architecture>("--arch", "-a") { Description = "Release architecture.", DefaultValueFactory = (_) => Architecture.X64 };

    private Option<InstallType> _typeOption = new Option<InstallType>("--type", "-t") { Description = "Release type.", DefaultValueFactory = (_) => InstallType.Portable };

    private Option<DateTimeOffset> _timeOption = new Option<DateTimeOffset>("--time") { Description = "Build time.", DefaultValueFactory = (_) => DateTimeOffset.Now };

    private Option<string> _packageOption = new Option<string>("--package", "-p") { Description = "Package file." };

    private Option<List<string>> _diffVersionsOption = new Option<List<string>>("--diff", "-d") { Description = "Diff versions.", AllowMultipleArgumentsPerToken = true };

    private Option<List<string>> _inputFilesOption = new Option<List<string>>("--input", "-i") { Description = "Input release info files to combine.", AllowMultipleArgumentsPerToken = true, Required = true };


    private string outputFile;
    private string version;
    private Architecture arch;
    private InstallType type;
    private DateTimeOffset buildTime;
    private string package;
    private List<string> diffVersions;
    private List<string> inputFiles;


    public ReleaseCommand()
    {
        _createCommand.Arguments.Add(_outputFileArgument);
        _createCommand.Options.Add(_versionOption);
        _createCommand.Options.Add(_archOption);
        _createCommand.Options.Add(_typeOption);
        _createCommand.Options.Add(_timeOption);
        _createCommand.Options.Add(_packageOption);
        _createCommand.Options.Add(_diffVersionsOption);
        _createCommand.SetAction(Release);
        _combineCommand.Arguments.Add(_outputFileArgument);
        _combineCommand.Options.Add(_inputFilesOption);
        _combineCommand.SetAction(Combine);
        Command.Subcommands.Add(_createCommand);
        Command.Subcommands.Add(_combineCommand);

        _httpClient = new(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All });
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Starward Build Tool");
        _polly = new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Linear
        }).Build();
    }



    private void VerifyInputs(ParseResult parseResult)
    {
        outputFile = parseResult.GetValue(_outputFileArgument)!;
        version = parseResult.GetValue(_versionOption)!;
        arch = parseResult.GetValue(_archOption);
        type = parseResult.GetValue(_typeOption);
        buildTime = parseResult.GetValue(_timeOption);
        package = parseResult.GetValue(_packageOption)!;
        diffVersions = parseResult.GetValue(_diffVersionsOption)!;
        inputFiles = parseResult.GetValue(_inputFilesOption)!;
    }



    public async Task Release(ParseResult parseResult)
    {
        VerifyInputs(parseResult);

        var releaseInfo = new ReleaseInfo
        {
            Version = version,
            Releases = new(),
        };

        string manifestName = $"manifest_{version}_{arch}_{type}".ToLower();
        var detail = new ReleaseInfoDetail
        {
            Version = version,
            Architecture = arch,
            InstallType = type,
            BuildTime = buildTime,
            ManifestUrl = $"https://starward-static.scighost.com/release/manifest/{manifestName}.json",
        };

        if (File.Exists(package))
        {
            byte[] bytes = await File.ReadAllBytesAsync(package);
            detail.PackageUrl = $"https://starward-static.scighost.com/release/package/{Path.GetFileName(package)}";
            detail.PackageSize = bytes.Length;
            detail.PackageHash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        }

        if (diffVersions != null && diffVersions.Count > 0)
        {
            detail.Diffs = new();
            foreach (var diffVersion in diffVersions)
            {
                var diff = new ReleaseInfoDiff
                {
                    DiffVersion = diffVersion,
                    ManifestUrl = $"https://starward-static.scighost.com/release/manifest/{manifestName}_diff_{diffVersion}.json",
                };
                detail.Diffs.Add(diffVersion, diff);
            }
        }

        releaseInfo.Releases.Add($"{arch}-{type}".ToLower(), detail);

        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(releaseInfo, new JsonSerializerOptions { WriteIndented = true });
        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
        await File.WriteAllBytesAsync(outputFile, jsonBytes);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Release info file created: {outputFile}");
        Console.ResetColor();
    }


    public async Task Combine(ParseResult parseResult)
    {
        VerifyInputs(parseResult);

        var list = new List<ReleaseInfo>();
        foreach (var item in inputFiles)
        {
            var info = JsonSerializer.Deserialize<ReleaseInfo>(await File.ReadAllTextAsync(item));
            if (info is not null)
            {
                list.Add(info);
            }
        }

        if (list.Select(x => x.Version).Distinct().Count() != 1)
        {
            throw new ArgumentOutOfRangeException("Input release info files have different versions.");
        }

        var combined = new ReleaseInfo
        {
            Version = list.First().Version,
            Releases = new(),
        };
        foreach (var info in list)
        {
            foreach (var kv in info.Releases)
            {
                combined.Releases[kv.Key] = kv.Value;
            }
        }

        foreach (var item in combined.Releases.ToList())
        {
            if (!await IsUrlValidAsync(item.Value.ManifestUrl))
            {
                combined.Releases.Remove(item.Key);
                continue;
            }
            if (!await IsUrlValidAsync(item.Value.PackageUrl))
            {
                item.Value.PackageUrl = null;
                item.Value.PackageHash = null;
                item.Value.PackageSize = 0;
            }
            if (item.Value.Diffs is not null)
            {
                foreach (var diff in item.Value.Diffs.ToList())
                {
                    if (!await IsUrlValidAsync(diff.Value.ManifestUrl))
                    {
                        item.Value.Diffs.Remove(diff.Key);
                    }
                }
            }
        }

        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(combined, new JsonSerializerOptions { WriteIndented = true });
        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
        await File.WriteAllBytesAsync(outputFile, jsonBytes);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Combined release info file created: {outputFile} ({combined.Releases.Count} releases).");
        Console.ResetColor();
    }


    private async Task<bool> IsUrlValidAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }
        try
        {
            return await _polly.ExecuteAsync(async _ =>
            {
                using var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return true;
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"URL validation failed for {url}: {ex.Message}");
            return false;
        }
    }


}
