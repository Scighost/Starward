using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.GameManagement.Model;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Starward.GameManagement;

public partial class GameConfigService
{

    private readonly ILogger<GameConfigService> _logger;

    private readonly HoYoPlayService _hoYoPlayService;



    public GameConfigService(ILogger<GameConfigService> logger, HoYoPlayService hoYoPlayService)
    {
        _logger = logger;
        _hoYoPlayService = hoYoPlayService;
    }



    public static async Task<GameConfigIni?> GetGameConfigIniAsync(string installPath, CancellationToken cancellation = default)
    {
        string iniFile = Path.Combine(installPath, "config.ini");
        if (!File.Exists(iniFile))
        {
            return null;
        }
        string content = await File.ReadAllTextAsync(iniFile, cancellation).ConfigureAwait(false);
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        MatchCollection matches = IniKeyValueRegex.Matches(content);
        foreach (Match match in matches)
        {
            values[match.Groups["key"].Value] = match.Groups["value"].Value;
        }

        int GetInt(string key) => int.TryParse(values.GetValueOrDefault(key), out int value) ? value : 0;
        Version? GetVersion(string key) => Version.TryParse(values.GetValueOrDefault(key), out Version? value) ? value : null;
        string GetString(string key) => values.GetValueOrDefault(key) ?? "";

        GameConfigIni config = new()
        {
            Channel = GetInt("channel"),
            SubChannel = GetInt("sub_channel"),
            Cps = GetString("cps"),
            GameBiz = GetString("game_biz"),
            GameVersion = GetVersion("game_version"),
            SdkVersion = GetString("sdk_version"),
            WpfVersion = GetString("wpf_version"),
            PluginVersion = GetVersion("plugin_version"),
            Uapc = GetString("uapc"),
            DownloadingMode = GetString("downloading_mode"),
            Settings = values,
        };
        return config;
    }


    [GeneratedRegex(@"^(?<key>[^\s=\[\]]+)\s*=\s*(?<value>[^\r\n]*?)\s*$", RegexOptions.Multiline)]
    private static partial Regex IniKeyValueRegex { get; }



    public static async Task SetGameConfigIniAsync(string installPath, GameConfigIni configIni, CancellationToken cancellation)
    {
        StringBuilder sb = new();
        sb.AppendLine("[General]");
        foreach ((string key, string value) in configIni.Settings)
        {
            if (!PropertyKeys.Contains(key))
            {
                sb.AppendLine($"{key}={value}");
            }
        }
        AppendIfNotEmpty(sb, "channel", configIni.Channel.ToString());
        AppendIfNotEmpty(sb, "sub_channel", configIni.SubChannel.ToString());
        AppendIfNotEmpty(sb, "cps", configIni.Cps);
        AppendIfNotEmpty(sb, "game_biz", configIni.GameBiz);
        AppendIfNotEmpty(sb, "game_version", configIni.GameVersion?.ToString());
        AppendIfNotEmpty(sb, "sdk_version", configIni.SdkVersion);
        AppendIfNotEmpty(sb, "wpf_version", configIni.WpfVersion);
        AppendIfNotEmpty(sb, "uapc", configIni.Uapc);
        AppendIfNotEmpty(sb, "downloading_mode", configIni.DownloadingMode);

        string path = Path.Join(installPath, "config.ini");
        await File.WriteAllTextAsync(path, sb.ToString(), cancellation);
    }


    private static void AppendIfNotEmpty(StringBuilder sb, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            sb.AppendLine($"{key}={value}");
        }
    }

    private static HashSet<string> PropertyKeys =
    [
        "channel",
        "sub_channel",
        "cps",
        "game_biz",
        "game_version",
        "sdk_version",
        "wpf_version",
        "uapc",
        "downloading_mode"
    ];



    public async Task<Version?> GetGameCurrentVersionAsync(GameId gameId, CancellationToken cancellation = default)
    {
        GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(gameId, cancellation).ConfigureAwait(false);
        config.ThrowIfNull(gameId);
        string? version;
        if (config.DefaultDownloadMode is DownloadMode.DOWNLOAD_MODE_FILE)
        {
            GamePackage? package = await _hoYoPlayService.GetGamePackageAsync(gameId, cancellation).ConfigureAwait(false);
            version = package?.Main?.Major?.Version;
        }
        else
        {
            GameBranch? branch = await _hoYoPlayService.GetGameBranchAsync(gameId, cancellation).ConfigureAwait(false);
            version = branch?.Main?.Tag;
        }
        _ = Version.TryParse(version, out Version? result);
        return result;
    }




    public async Task<AudioLanguage> GetAudioLanguageAsync(GameId gameId, string installPath, CancellationToken cancellation = default)
    {
        if (!Directory.Exists(installPath))
        {
            return AudioLanguage.None;
        }
        GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(gameId, cancellation);
        if (string.IsNullOrWhiteSpace(config?.AudioPackageScanDir))
        {
            return AudioLanguage.None;
        }
        AudioLanguage flag = AudioLanguage.None;
        string file = Path.Join(installPath, config.AudioPackageScanDir);
        if (File.Exists(file))
        {
            var lines = await File.ReadAllLinesAsync(file, cancellation);
            if (lines.Any(x => x.Contains("Chinese"))) { flag |= AudioLanguage.Chinese; }
            if (lines.Any(x => x.Contains("English(US)"))) { flag |= AudioLanguage.English; }
            if (lines.Any(x => x.Contains("Japanese"))) { flag |= AudioLanguage.Japanese; }
            if (lines.Any(x => x.Contains("Korean"))) { flag |= AudioLanguage.Korean; }
        }
        return flag;
    }




    public async Task SetAudioLanguageAsync(GameId gameId, string installPath, AudioLanguage language, CancellationToken cancellation = default)
    {
        GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(gameId, cancellation);
        if (string.IsNullOrWhiteSpace(config?.AudioPackageScanDir))
        {
            return;
        }
        string file = Path.Join(installPath, config.AudioPackageScanDir);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var lines = new List<string>(4);
        if (language.HasFlag(AudioLanguage.Chinese)) { lines.Add("Chinese"); }
        if (language.HasFlag(AudioLanguage.English)) { lines.Add("English(US)"); }
        if (language.HasFlag(AudioLanguage.Japanese)) { lines.Add("Japanese"); }
        if (language.HasFlag(AudioLanguage.Korean)) { lines.Add("Korean"); }
        await File.WriteAllLinesAsync(file, lines, cancellation);
    }



    public async Task<string?> GetCategoryScenarioAsync(GameId gameId, string installPath, CancellationToken cancellation = default)
    {
        if (!Directory.Exists(installPath))
        {
            return null;
        }
        GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(gameId, cancellation);
        if (config?.EnableScenarioPackage ?? false)
        {
            string file = Path.Join(installPath, config?.LocalScenarioConfigPath);
            if (!File.Exists(file))
            {
                return null;
            }
            using FileStream fs = File.OpenRead(file);
            JsonNode? node = await JsonNode.ParseAsync(fs, cancellationToken: cancellation);
            string? type = node?["packageType"]?.GetValue<string>();
            return type switch
            {
                "FULL" => CategoryScenario.CATEGORY_SCENARIO_FULL,
                "BASE" => CategoryScenario.CATEGORY_SCENARIO_BASE,
                _ => null,
            };
        }
        return null;
    }



    public async Task SetCategoryScenarioAsync(GameId gameId, string installPath, string scenario, CancellationToken cancellation = default)
    {
        GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(gameId, cancellation);
        if (config?.EnableScenarioPackage ?? false && !string.IsNullOrWhiteSpace(config?.LocalScenarioConfigPath))
        {
            string file = Path.Join(installPath, config?.LocalScenarioConfigPath);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            string? content = scenario switch
            {
                CategoryScenario.CATEGORY_SCENARIO_FULL => """{"packageType":"FULL"}""",
                CategoryScenario.CATEGORY_SCENARIO_BASE => """{"packageType":"BASE"}""",
                _ => null,
            };
            if (content is not null)
            {
                await File.WriteAllTextAsync(file, content, cancellation);
            }
        }
    }



    public async Task<HashSet<string>> GetBlackListFileNamesAsync(GameId gameId, string installPath, CancellationToken cancellation = default)
    {
        HashSet<string> hashSet = [];
        GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(gameId, cancellation);
        if (config?.EnableResourceBlacklist ?? false)
        {
            string file = Path.Join(installPath, config?.BlacklistDir);
            if (File.Exists(file))
            {
                using FileStream fs = File.OpenRead(file);
                IAsyncEnumerable<BlacklistItem?> lines = JsonSerializer.DeserializeAsyncEnumerable<BlacklistItem>(fs, true, cancellationToken: cancellation);
                await foreach (BlacklistItem? item in lines)
                {
                    if (!string.IsNullOrWhiteSpace(item?.FileName))
                    {
                        hashSet.Add(item.FileName);
                    }
                }
            }
        }
        return hashSet;
    }



    public async Task<HashSet<string>> GetIgnoreMatchingFieldsAsync(GameId gameId, string installPath, CancellationToken cancellation = default)
    {
        HashSet<string> hashSet = [];
        GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(gameId, cancellation);
        string file = Path.Join(installPath, config?.ResCategoryDir);
        if (File.Exists(file))
        {
            using FileStream fs = File.OpenRead(file);
            IAsyncEnumerable<IgnoreMatchingField?> lines = JsonSerializer.DeserializeAsyncEnumerable<IgnoreMatchingField>(fs, true, cancellationToken: cancellation);
            await foreach (IgnoreMatchingField? item in lines)
            {
                // eg. {"category":"10302","is_delete":true}
                if (item?.IsDelete is true && !string.IsNullOrWhiteSpace(item.Category))
                {
                    hashSet.Add(item.Category);
                }
            }
        }
        return hashSet;
    }



    public static HashSet<string> GetMatchedCategoryIds(GameBranchPackage package, string scenarios, AudioLanguage audio, HashSet<string>? excludeIds = null)
    {
        scenarios ??= CategoryScenario.CATEGORY_SCENARIO_FULL;
        HashSet<string> hashSet = new();
        foreach (var item in package.Categories)
        {
            if (item.Scenarios.Contains(scenarios))
            {
                if (item.Type is CategoryType.CATEGORY_TYPE_RESOURCE)
                {
                    hashSet.Add(item.CategoryId);
                }
                if (item.Type is CategoryType.CATEGORY_TYPE_AUDIO && MatcheAudioLanguage(item.MatchingField, audio))
                {
                    hashSet.Add(item.CategoryId);
                }
            }
        }
        if (excludeIds is not null)
        {
            hashSet.ExceptWith(excludeIds);
        }
        return hashSet;
    }



    private static bool MatcheAudioLanguage(string? matchingField, AudioLanguage audio)
    {
        if (string.IsNullOrEmpty(matchingField) || audio is AudioLanguage.None)
        {
            return false;
        }
        return (audio.HasFlag(AudioLanguage.Chinese) && matchingField.Contains(AudioLanguage.Chinese.ToDescription()))
            || (audio.HasFlag(AudioLanguage.English) && matchingField.Contains(AudioLanguage.English.ToDescription()))
            || (audio.HasFlag(AudioLanguage.Japanese) && matchingField.Contains(AudioLanguage.Japanese.ToDescription()))
            || (audio.HasFlag(AudioLanguage.Korean) && matchingField.Contains(AudioLanguage.Korean.ToDescription()));
    }


}
