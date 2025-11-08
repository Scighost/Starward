using Polly;
using Polly.Retry;
using System.CommandLine;
using System.Diagnostics;
using System.IO.Hashing;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace BuildTool;

public class DiffCommand
{

    private readonly ResiliencePipeline _polly;

    private ZstdSharp.Decompressor _zstdDecompressor => new();


    private readonly HttpClient _httpClient = new(new SocketsHttpHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.All,
    });


    public Command Command { get; set; } = new Command("diff", "Create diff files.");

    private Argument<string> _outputPathArgument = new Argument<string>("outputPath") { Description = "Pack files output folder.", DefaultValueFactory = (_) => "./build/release" };

    private Option<Architecture> _archOption = new Option<Architecture>("--arch", "-a") { Description = "Release architecture.", DefaultValueFactory = (_) => Architecture.X64 };

    private Option<ReleaseType> _typeOption = new Option<ReleaseType>("--type", "-t") { Description = "Release type.", DefaultValueFactory = (_) => ReleaseType.Portable };

    private Option<string> _newVersionOption = new Option<string>("--new-version", "-nv") { Description = "New version.", Required = true };

    private Option<string> _newPathOption = new Option<string>("--new-path", "-np") { Description = "New version path." };

    private Option<string> _oldVersionOption = new Option<string>("--old-version", "-ov") { Description = "Old version.", Required = true };

    private Option<string> _oldPathOption = new Option<string>("--old-path", "-op") { Description = "Old version path" };


    private Architecture arch;
    private ReleaseType type;
    private string outputPath;
    private string newVersion;
    private string newPath;
    private string oldVersion;
    private string oldPath;


    private string outputManifestFolder;
    private string outputFileFolder;
    private string tempFolder;



    public DiffCommand()
    {
        Command.Arguments.Add(_outputPathArgument);
        Command.Options.Add(_archOption);
        Command.Options.Add(_typeOption);
        Command.Options.Add(_newVersionOption);
        Command.Options.Add(_newPathOption);
        Command.Options.Add(_oldVersionOption);
        Command.Options.Add(_oldPathOption);
        Command.SetAction(DiffAsync);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Starward Build Tool");
        _polly = new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 5,
            BackoffType = DelayBackoffType.Linear
        }).Build();
    }



    private void VerifyInputs(ParseResult parseResult)
    {
        outputPath = parseResult.GetValue(_outputPathArgument)!;
        arch = parseResult.GetValue(_archOption);
        type = parseResult.GetValue(_typeOption);
        newVersion = parseResult.GetValue(_newVersionOption)!;
        newPath = parseResult.GetValue(_newPathOption)!;
        oldVersion = parseResult.GetValue(_oldVersionOption)!;
        oldPath = parseResult.GetValue(_oldPathOption)!;

        if (!string.IsNullOrWhiteSpace(oldPath) && !Directory.Exists(oldPath))
        {
            throw new DirectoryNotFoundException($"Old path not found: {oldPath}");
        }
        if (!string.IsNullOrWhiteSpace(newPath) && !Directory.Exists(newPath))
        {
            throw new DirectoryNotFoundException($"New path not found: {newPath}");
        }
        if (Directory.Exists(newPath))
        {
            string newVersionManifestPath = Path.Join(outputPath, "manifest", $"manifest_{newVersion}_{arch}_{type}.json".ToLower());
            if (!File.Exists(newVersionManifestPath))
            {
                throw new FileNotFoundException($"New version manifest not found: {newVersionManifestPath}");
            }
        }

        outputManifestFolder = Path.Join(outputPath, "manifest");
        outputFileFolder = Path.Join(outputPath, "file");
        tempFolder = Path.Join(outputPath, "temp");
        Directory.CreateDirectory(outputManifestFolder);
        Directory.CreateDirectory(outputFileFolder);
        Directory.CreateDirectory(tempFolder);
    }



    public async Task DiffAsync(ParseResult parseResult)
    {
        VerifyInputs(parseResult);

        Console.WriteLine($"Creating diff files for version {oldVersion} -> {newVersion} ({arch}, {type})");

        ReleaseManifest oldManifest, newManifest;
        string oldVersionManifestPath = Path.Join(outputPath, "manifest", $"manifest_{oldVersion}_{arch}_{type}.json");
        string newVersionManifestPath = Path.Join(outputPath, "manifest", $"manifest_{newVersion}_{arch}_{type}.json");
        if (Directory.Exists(oldPath) && File.Exists(oldVersionManifestPath))
        {
            oldManifest = JsonSerializer.Deserialize<ReleaseManifest>(File.ReadAllText(oldVersionManifestPath)) ?? throw new NullReferenceException("Manifest is null.");
        }
        else
        {
            oldManifest = await GetManifestAsync(oldVersion, arch, type);
        }
        if (Directory.Exists(newPath) && File.Exists(newVersionManifestPath))
        {
            newManifest = JsonSerializer.Deserialize<ReleaseManifest>(File.ReadAllText(newVersionManifestPath)) ?? throw new NullReferenceException("Manifest is null.");
        }
        else
        {
            newManifest = await GetManifestAsync(newVersion, arch, type);
        }

        newManifest.DiffVersion = oldVersion;
        int count = 0;

        Console.ForegroundColor = ConsoleColor.DarkGray;
        await Parallel.ForEachAsync(newManifest.Files, async (newItem, _) =>
        {
            if (oldManifest.Files.FirstOrDefault(x => x.Size == newItem.Size && string.Equals(x.Hash, newItem.Hash, StringComparison.OrdinalIgnoreCase)) is ReleaseFile oldItem)
            {
                newItem.Patch = new ReleaseFilePatch
                {
                    OldPath = oldItem.Path,
                    OldFileSize = oldItem.Size,
                    OldFileHash = oldItem.Hash,
                };
            }
            else if (MatchOldFile(newItem.Path, oldManifest.Files) is ReleaseFile oldItem2)
            {
                string? newFilePath = Path.Join(newPath, newItem.Path);
                string? oldFilePath = Path.Join(oldPath, oldItem2.Path);
                if (!(File.Exists(newFilePath) && Convert.FromHexString(newItem.Hash).SequenceEqual(SHA256.HashData(await File.ReadAllBytesAsync(newFilePath)))))
                {
                    newFilePath = await DownloadFileAsync(newManifest.UrlPrefix + newItem.Id, newItem.Hash);
                }
                if (!(File.Exists(oldFilePath) && Convert.FromHexString(oldItem2.Hash).SequenceEqual(SHA256.HashData(await File.ReadAllBytesAsync(oldFilePath)))))
                {
                    oldFilePath = await DownloadFileAsync(oldManifest.UrlPrefix + oldItem2.Id, oldItem2.Hash);
                }
                string diffTempPath = Path.Combine(tempFolder, $"diff_{newItem.Id}_{oldItem2.Id}");
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "hdiffz",
                    Arguments = $"""
                             "{oldFilePath}" "{newFilePath}" "{diffTempPath}" -c-zstd-17
                             """,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
                if (p is not null)
                {
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                    {
                        throw new Exception($"hdiffz exited with code {p.ExitCode}: {p.StandardError.ReadToEnd()}");
                    }
                }
                if (File.Exists(diffTempPath))
                {
                    byte[] diffBytes = File.ReadAllBytes(diffTempPath);
                    string diffHash = Convert.ToHexStringLower(SHA256.HashData(diffBytes));
                    newItem.Patch = new ReleaseFilePatch
                    {
                        Id = $"{Convert.ToHexStringLower(XxHash3.Hash(diffBytes))}_{diffHash}",
                        OldPath = oldItem2.Path,
                        OldFileSize = oldItem2.Size,
                        OldFileHash = oldItem2.Hash,
                        PatchSize = diffBytes.Length,
                        PatchHash = diffHash,
                    };
                    File.Move(diffTempPath, Path.Join(outputFileFolder, newItem.Patch.Id), true);
                }
            }
            Interlocked.Increment(ref count);
            Console.WriteLine($"[{count}/{newManifest.Files.Count}] Processed {newItem.Path}");
        });

        newManifest.DiffFileCount = newManifest.Files.Count(f => f.Patch == null || f.Patch.Id != null);
        newManifest.DiffSize = newManifest.Files.Where(f => f.Patch != null).Sum(f => f.Patch!.PatchSize) + newManifest.Files.Where(x => x.Patch == null).Sum(x => x.CompressedSize);
        if (type is ReleaseType.Setup)
        {
            newManifest.DeleteFiles = oldManifest.Files.Select(x => x.Path).Except(newManifest.Files.Select(f => f.Path)).ToList();
        }

        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(newManifest, new JsonSerializerOptions { WriteIndented = true });
        string manifestName = $"manifest_{newVersion}_{arch}_{type}_diff_{oldVersion}.json".ToLower();
        File.WriteAllBytes(Path.Join(outputManifestFolder, manifestName), jsonBytes);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Diff manifest created: {manifestName}. Diff size ({newManifest.DiffSize / 1024.0:N2} KB)");
        Console.ResetColor();
    }



    private async Task<ReleaseManifest> GetManifestAsync(string version, Architecture arch, ReleaseType type)
    {
        string name = $"manifest_{version}_{arch}_{type}".ToLower();
        string url = $"https://starward-static.scighost.com/release/manifest/{name}.json";
        var manifest = await _polly.ExecuteAsync(async _ => await _httpClient.GetFromJsonAsync<ReleaseManifest>(url));
        return manifest ?? throw new NullReferenceException($"Manifest {name} not exists.");
    }


    private static ReleaseFile? MatchOldFile(string newFile, List<ReleaseFile> oldFiles)
    {
        var fileName = Path.GetFileName(newFile.AsSpan());
        int splitCount = newFile.AsSpan().Count(Path.DirectorySeparatorChar);
        var span = newFile.AsSpan();
        while (true)
        {
            foreach (var item in oldFiles)
            {
                var pathSpan = item.Path.AsSpan();
                if (pathSpan.EndsWith(span) && pathSpan.Count(Path.DirectorySeparatorChar) == splitCount && Path.GetFileName(pathSpan).SequenceEqual(fileName))
                {
                    return item;
                }
            }
            int index = span.IndexOf(Path.DirectorySeparatorChar);
            if (index >= 0)
            {
                span = span[(index + 1)..];
            }
            else
            {
                return null;
            }
        }
    }


    private async Task<string> DownloadFileAsync(string url, string hash)
    {
        string name = Path.GetFileName(url);
        string path = Path.Join(tempFolder, name);
        if (File.Exists(path) && string.Equals(hash, Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(path)))))
        {
            return path;
        }
        await _polly.ExecuteAsync(async _ =>
        {
            string tempPath = path + "_tmp";
            byte[] zstdBytes = await _httpClient.GetByteArrayAsync(url);
            using var _zstd = _zstdDecompressor;
            var bytes = _zstd.Unwrap(zstdBytes);
            if (!string.Equals(hash, Convert.ToHexStringLower(SHA256.HashData(bytes)), StringComparison.OrdinalIgnoreCase))
            {
                throw new System.Security.VerificationException($"Checksum failed: {url}");
            }
            File.WriteAllBytes(tempPath, bytes);
            File.Move(tempPath, path, true);
        });
        return path;
    }


}
