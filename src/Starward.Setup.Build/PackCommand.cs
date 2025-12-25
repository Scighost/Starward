using Starward.Setup.Core;
using System.CommandLine;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace Starward.Setup.Build;

public class PackCommand
{

    private ZstdSharp.Compressor _zstdCompressor => new(17);


    public Command Command { get; set; } = new Command("pack", "Pack Starward files.");

    private Argument<string> rootPathArgument = new Argument<string>("rootPath") { Description = "The root path of the Starward build files.", DefaultValueFactory = (_) => "./build/Starward" };

    private Argument<string> outputPathArgument = new Argument<string>("outputPath") { Description = "Pack files output folder.", DefaultValueFactory = (_) => "./build/release" };

    private Option<string> versionOption = new Option<string>("--version", "-v") { Description = "Release version.", Required = true };

    private Option<Architecture> archOption = new Option<Architecture>("--arch", "-a") { Description = "Release architecture.", DefaultValueFactory = (_) => Architecture.X64 };

    private Option<InstallType> typeOption = new Option<InstallType>("--type", "-t") { Description = "Release type.", DefaultValueFactory = (_) => InstallType.Portable };


    private string rootPath;
    private string outputPath;
    private string version;
    private Architecture arch;
    private InstallType type;


    public PackCommand()
    {
        Command.Arguments.Add(rootPathArgument);
        Command.Arguments.Add(outputPathArgument);
        Command.Options.Add(versionOption);
        Command.Options.Add(archOption);
        Command.Options.Add(typeOption);
        Command.SetAction(Pack);
    }



    private void VerifyInputs(ParseResult parseResult)
    {
        rootPath = parseResult.GetValue(rootPathArgument)!;
        outputPath = parseResult.GetValue(outputPathArgument)!;
        version = parseResult.GetValue(versionOption)!;
        arch = parseResult.GetValue(archOption);
        type = parseResult.GetValue(typeOption);

        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Root path not found: {rootPath}");
        }
    }



    public async Task Pack(ParseResult parseResult)
    {
        VerifyInputs(parseResult);

        Console.WriteLine($"Packing Starward build ({version}, {arch}, {type}): {rootPath}");

        string manifestFolder = Path.Join(outputPath, "manifest");
        string outputFileFolder = Path.Join(outputPath, "file");
        string tempFolder = Path.Join(outputPath, "temp");
        Directory.CreateDirectory(manifestFolder);
        Directory.CreateDirectory(outputFileFolder);
        Directory.CreateDirectory(tempFolder);

        ReleaseManifest manifest = new ReleaseManifest
        {
            Version = version!,
            Architecture = arch,
            InstallType = type,
            DiffVersion = null,
            UrlPrefix = "https://starward-static-cf.scighost.com/release/file/",
            UrlSuffix = null,
            Files = new(),
        };

        string[] files = Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories);
        Console.WriteLine($"Found {files.Length} files to pack.");

        foreach (var item in files)
        {
            manifest.Files.Add(new ReleaseFile
            {
                Path = Path.GetRelativePath(rootPath, item),
            });
        }

        int count = 0;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        await Parallel.ForEachAsync(manifest.Files, async (item, _) =>
        {
            string file = Path.Join(rootPath, item.Path);
            byte[] bytes = await File.ReadAllBytesAsync(file);
            string hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            string id = $"{Convert.ToHexStringLower(XxHash3.Hash(bytes))}_{hash}";
            var zstdBytes = _zstdCompressor.Wrap(bytes);
            File.WriteAllBytes(Path.Join(outputFileFolder, id), zstdBytes);
            item.Id = id;
            item.Size = bytes.Length;
            item.CompressedSize = zstdBytes.Length;
            item.Hash = hash;
            item.CompressedHash = Convert.ToHexStringLower(SHA256.HashData(zstdBytes));

            Interlocked.Increment(ref count);
            Console.WriteLine($"[{count}/{files.Length}] Packed {item.Path} ({item.Size / 1024.0:N2} KB -> {item.CompressedSize / 1024.0:N2} KB)");
        });

        manifest.FileCount = manifest.Files.Count;
        manifest.Size = manifest.Files.Sum(f => f.Size);
        manifest.CompressedSize = manifest.Files.Sum(f => f.CompressedSize);

        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, new JsonSerializerOptions { WriteIndented = true });
        string manifestName = $"manifest_{version}_{arch}_{type}.json";
        await File.WriteAllBytesAsync(Path.Join(manifestFolder, manifestName.ToLower()), jsonBytes);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Packed {manifest.FileCount} files. Total size: {manifest.Size / 1024.0:N2} KB -> {manifest.CompressedSize / 1024.0:N2} KB");
        Console.ResetColor();
    }






}