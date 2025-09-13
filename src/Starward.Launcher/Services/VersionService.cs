using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Starward.Launcher.Services;

internal partial class VersionService(ILogger<VersionService> logger)
{
    public string? TryReadIni(string path)
    {
        if (!File.Exists(path))
        {
            logger.LogWarning("Ini file not found: {path}", path);
            return null;
        }

        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to read ini file: {path}", path);
            return null;
        }

        var result = ExePathRegex().Match(text).Groups[1];
        if (result.Success) return result.Value.Trim();
        logger.LogWarning("Failed to parse exe_path from ini file: {path}", path);
        return null;

    }

    public string? TryReadJson(string path)
    {
        if (!File.Exists(path))
        {
            logger.LogWarning("Json file not found: {path}", path);
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<VersionConfiguration>(stream,
                VersionSerializerContext.Default.VersionConfiguration)?.ExePath;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to open or parse json file: {path}", path);
            return null;
        }
    }

    [GeneratedRegex("exe_path=(.*)")]
    private static partial Regex ExePathRegex();

    private class VersionConfiguration
    {
        public string ExePath { get; set; }
    }

    [JsonSerializable(typeof(VersionConfiguration))]
    [JsonSerializable(typeof(string))]
    private partial class VersionSerializerContext : JsonSerializerContext;
}