using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Serilog;
using Starward.Core;
using Starward.Core.Gacha.Genshin;
using Starward.Core.Gacha.StarRail;
using Starward.Core.GameRecord;
using Starward.Core.Launcher;
using Starward.Core.Metadata;
using Starward.Core.SelfQuery;
using Starward.Services;
using Starward.Services.Gacha;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using Windows.ApplicationModel;
using Windows.Storage;

namespace Starward;

internal static class AppConfig
{


#if (DEBUG || DEV) && !DISABLE_DEV
    private const string REG_KEY_NAME = @"HKEY_CURRENT_USER\Software\Starward_Dev";
#else
    private const string REG_KEY_NAME = @"HKEY_CURRENT_USER\Software\Starward";
#endif


    public static bool MsixPackaged { get; private set; }

    public static string? AppVersion { get; private set; }


    public static IConfigurationRoot Configuration { get; private set; }


    public static string LogFile { get; private set; }


    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };


    private static IServiceProvider _serviceProvider;


    private static bool reg;


    private static readonly ApplicationDataContainer _container;



    static AppConfig()
    {
        try
        {
            _ = Package.Current.Id;
            MsixPackaged = true;
            _container = ApplicationData.Current.LocalSettings;
        }
        catch { }
        LoadConfiguration();
    }




    #region UriSource


    public static Uri EmojiPaimon = new Uri("ms-appx:///Assets/Image/UI_EmotionIcon5.png");

    public static Uri EmojiPom = new Uri("ms-appx:///Assets/Image/20008.png");

    public static Uri EmojiAI = new Uri("ms-appx:///Assets/Image/bdfd19c3bdad27a395890755bb60b162.png");


    #endregion



    #region UserData


    private static bool enableConsole;
    public static bool EnableConsole
    {
        get => enableConsole;
        set
        {
            enableConsole = value;
            SaveConfiguration();
        }
    }

    private static int windowSizeMode;
    public static int WindowSizeMode
    {
        get => windowSizeMode;
        set
        {
            windowSizeMode = value;
            SaveConfiguration();
        }
    }

    private static string? language;
    public static string? Language
    {
        get => language;
        set
        {
            language = value;
            SaveConfiguration();
        }
    }

    private static string userDataFolder;
    public static string UserDataFolder
    {
        get => userDataFolder;
        set
        {
            userDataFolder = value;
            SaveConfiguration();
        }
    }



    private static void LoadConfiguration()
    {
        if (MsixPackaged)
        {
            try
            {
                var v = Package.Current.Id.Version;
                AppVersion = $"{v.Major}.{v.Minor}.{v.Build}";
                Configuration = new ConfigurationBuilder().AddCommandLine(Environment.GetCommandLineArgs()).Build();
                enableConsole = LocalSettingGetValue<bool>(nameof(EnableConsole));
                windowSizeMode = LocalSettingGetValue<int>(nameof(WindowSizeMode));
                language = LocalSettingGetValue<string>(nameof(Language));
                string? dir = LocalSettingGetValue<string>(nameof(UserDataFolder));
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    if (Path.IsPathFullyQualified(dir) && Directory.Exists(dir))
                    {
                        userDataFolder = dir;
                    }
                }
            }
            catch { }
        }
        else
        {
            try
            {
                AppVersion = typeof(AppConfig).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                var webviewFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Starward\webview");
                Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", webviewFolder, EnvironmentVariableTarget.Process);

                string? baseDir = Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd('\\'));
                string exe = Path.Join(baseDir, "Starward.exe");
                var builder = new ConfigurationBuilder();
                if (File.Exists(exe))
                {
                    string ini = Path.Join(baseDir, "config.ini");
                    if (File.Exists(ini))
                    {
                        builder.AddIniFile(ini);
                    }
                    Configuration = builder.AddCommandLine(Environment.GetCommandLineArgs()).Build();
                    enableConsole = Configuration.GetValue<bool>(nameof(EnableConsole));
                    windowSizeMode = Configuration.GetValue<int>(nameof(WindowSizeMode));
                    language = Configuration.GetValue<string>(nameof(Language));
                    string? dir = Configuration.GetValue<string>(nameof(UserDataFolder));
                    if (!string.IsNullOrWhiteSpace(dir))
                    {
                        string folder;
                        if (Path.IsPathFullyQualified(dir))
                        {
                            folder = dir;
                        }
                        else
                        {
                            folder = Path.Join(baseDir, dir);
                        }
                        if (Directory.Exists(folder))
                        {
                            userDataFolder = Path.GetFullPath(folder);
                        }
                    }
                }
                else
                {
                    reg = true;
                    Configuration = builder.AddCommandLine(Environment.GetCommandLineArgs()).Build();
                    enableConsole = Registry.GetValue(REG_KEY_NAME, nameof(EnableConsole), null) is 1;
                    string? dir = Registry.GetValue(REG_KEY_NAME, nameof(UserDataFolder), null) as string;
                    if (Directory.Exists(dir))
                    {
                        userDataFolder = Path.GetFullPath(dir);
                    }
                }
            }
            catch
            {
                Configuration ??= new ConfigurationBuilder().AddCommandLine(Environment.GetCommandLineArgs()).Build();
            }
        }
    }



    private static void SaveConfiguration()
    {
        if (MsixPackaged)
        {
            try
            {
                LocalSettingSetValue(nameof(EnableConsole), EnableConsole);
                LocalSettingSetValue(nameof(WindowSizeMode), WindowSizeMode);
                LocalSettingSetValue(nameof(Language), Language);
                LocalSettingSetValue(nameof(UserDataFolder), UserDataFolder);
            }
            catch { }
        }
        else
        {
            try
            {
                if (reg)
                {
                    Registry.SetValue(REG_KEY_NAME, nameof(EnableConsole), EnableConsole ? 1 : 0);
                    Registry.SetValue(REG_KEY_NAME, nameof(UserDataFolder), UserDataFolder);
                }
                else
                {
                    string dataFolder = UserDataFolder;
                    string baseDir = Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd('\\'))!;
                    if (dataFolder?.StartsWith(baseDir) ?? false)
                    {
                        dataFolder = Path.GetRelativePath(baseDir, dataFolder);
                    }
                    File.WriteAllText(Path.Combine(baseDir, "config.ini"), $"""
                         {nameof(EnableConsole)}={EnableConsole}
                         {nameof(WindowSizeMode)}={WindowSizeMode}
                         {nameof(Language)}={Language}
                         {nameof(UserDataFolder)}={dataFolder}
                         """);
                }
            }
            catch { }
        }
    }





    public static T? LocalSettingGetValue<T>(string key, T? defaultValue = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return defaultValue;
            }
            var value = _container.Values[key];
            if (value is T t)
            {
                return t;
            }
            else if (value is string s)
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter == null)
                {
                    return defaultValue;
                }
                return (T?)converter.ConvertFromString(s);
            }
            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }

    }


    public static void LocalSettingSetValue<T>(string key, T? value)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }
            _container.Values[key] = value;
        }
        catch { }
    }





    #endregion




    #region Service Provider


    public static void ResetServiceProvider()
    {
        cache.Clear();
        _serviceProvider = null!;
        DatabaseService = null!;
    }


    private static void BuildServiceProvider()
    {
        if (_serviceProvider == null)
        {
            var logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Starward\log");
            Directory.CreateDirectory(logFolder);
            LogFile = Path.Combine(logFolder, $"Starward_{DateTime.Now:yyMMdd_HHmmss}.log");
            Log.Logger = new LoggerConfiguration().WriteTo.File(path: LogFile, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u4}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}")
                                                  .Enrich.FromLogContext()
                                                  .CreateLogger();
            Log.Information($"Welcome to Starward v{AppVersion}\r\nSystem: {Environment.OSVersion}\r\nCommand Line: {Environment.CommandLine}");

            var sc = new ServiceCollection();
            sc.AddLogging(c => c.AddSimpleConsole(c => c.TimestampFormat = "HH:mm:ss.fff\r\n"));
            sc.AddLogging(c => c.AddSerilog(Log.Logger));
            sc.AddTransient(_ =>
            {
                var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { DefaultRequestVersion = HttpVersion.Version20 };
                client.DefaultRequestHeaders.Add("User-Agent", $"Starward/{AppVersion}");
                return client;
            });

            sc.AddSingleton<GenshinGachaClient>();
            sc.AddSingleton<StarRailGachaClient>();
            sc.AddSingleton<HyperionClient>();
            sc.AddSingleton<HyperionClient>();
            sc.AddSingleton<HoyolabClient>();
            sc.AddSingleton<LauncherClient>();
            sc.AddSingleton<SelfQueryClient>();
            sc.AddSingleton(p => new MetadataClient(ApiCDNIndex, p.GetService<HttpClient>()));

            sc.AddSingleton<DatabaseService>();
            sc.AddSingleton<GameService>();
            sc.AddSingleton<UpdateService>();
            sc.AddSingleton<LauncherService>();
            sc.AddSingleton<GenshinGachaService>();
            sc.AddSingleton<StarRailGachaService>();
            sc.AddSingleton<PlayTimeService>();
            sc.AddSingleton<DownloadGameService>();
            sc.AddSingleton<GameSettingService>();
            sc.AddSingleton<GameRecordService>();
            sc.AddSingleton<WelcomeService>();
            sc.AddSingleton<SystemTrayService>();
            sc.AddSingleton<SelfQueryService>();

            _serviceProvider = sc.BuildServiceProvider();
            if (!string.IsNullOrWhiteSpace(UserDataFolder))
            {
                _serviceProvider.GetService<DatabaseService>()!.SetDatabase(UserDataFolder);
            }
        }
    }


    public static T GetService<T>()
    {
        BuildServiceProvider();
        return _serviceProvider.GetService<T>()!;
    }


    public static ILogger<T> GetLogger<T>()
    {
        BuildServiceProvider();
        return _serviceProvider.GetService<ILogger<T>>()!;
    }




    #endregion





    #region Static Setting



    public static int ApiCDNIndex
    {
        get => GetValue<int>();
        set => SetValue(value);
    }


    public static bool EnablePreviewRelease
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }


    public static string? IgnoreVersion
    {
        get => GetValue<string>();
        set => SetValue(value);
    }


    public static bool EnableBannerAndPost
    {
        get => GetValue(true);
        set => SetValue(value);
    }


    public static bool IgnoreRunningGame
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }


    public static GameBiz SelectGameBiz
    {
        get => GetValue<GameBiz>();
        set => SetValue(value);
    }


    public static bool ShowNoviceGacha
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }


    public static string? GachaLanguage
    {
        get => GetValue<string>();
        set => SetValue(value);
    }


    public static bool EnableDynamicAccentColor
    {
        get => GetValue(true);
        set => SetValue(value);
    }


    public static string? AccentColor
    {
        get => GetValue<string>();
        set => SetValue(value);
    }


    public static int VideoBgVolume
    {
        get => Math.Clamp(GetValue(100), 0, 100);
        set => SetValue(value);
    }


    public static bool UseOneBg
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }


    public static bool AcceptHoyolabToolboxAgreement
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }


    public static bool HoyolabToolboxPaneOpen
    {
        get => GetValue(true);
        set => SetValue(value);
    }


    public static bool EnableSystemTrayIcon
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }


    public static bool ExitWhenClosing
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }


    #endregion





    #region Dynamic Setting


    public static string? GetBg(GameBiz biz)
    {
        return GetValue<string>(default, $"bg_{biz}");
    }

    public static void SetBg(GameBiz biz, string? value)
    {
        SetValue(value, $"bg_{biz}");
    }



    public static string? GetCustomBg(GameBiz biz)
    {
        return GetValue<string>(default, UseOneBg ? $"custom_bg_{GameBiz.All}" : $"custom_bg_{biz}");
    }

    public static void SetCustomBg(GameBiz biz, string? value)
    {
        SetValue(value, UseOneBg ? $"custom_bg_{GameBiz.All}" : $"custom_bg_{biz}");
    }



    public static bool GetEnableCustomBg(GameBiz biz)
    {
        return GetValue<bool>(default, UseOneBg ? $"enable_custom_bg_{GameBiz.All}" : $"enable_custom_bg_{biz}");
    }

    public static void SetEnableCustomBg(GameBiz biz, bool value)
    {
        SetValue(value, UseOneBg ? $"enable_custom_bg_{GameBiz.All}" : $"enable_custom_bg_{biz}");
    }



    public static string? GetGameInstallPath(GameBiz biz)
    {
        return GetValue<string>(default, $"install_path_{biz}");
    }

    public static void SetGameInstallPath(GameBiz biz, string? value)
    {
        SetValue(value, $"install_path_{biz}");
    }



    public static bool GetEnableThirdPartyTool(GameBiz biz)
    {
        return GetValue<bool>(default, $"enable_third_party_tool_{biz}");
    }

    public static void SetEnableThirdPartyTool(GameBiz biz, bool value)
    {
        SetValue(value, $"enable_third_party_tool_{biz}");
    }



    public static string? GetThirdPartyToolPath(GameBiz biz)
    {
        return GetValue<string>(default, $"third_party_tool_path_{biz}");
    }

    public static void SetThirdPartyToolPath(GameBiz biz, string? value)
    {
        SetValue(value, $"third_party_tool_path_{biz}");
    }



    public static string? GetStartArgument(GameBiz biz)
    {
        return GetValue<string>(default, $"start_argument_{biz}");
    }

    public static void SetStartArgument(GameBiz biz, string? value)
    {
        SetValue(value, $"start_argument_{biz}");
    }



    public static long GetLastUidInGachaLogPage(GameBiz biz)
    {
        return GetValue<long>(default, $"last_gacha_uid_{biz}");
    }

    public static void SetLastUidInGachaLogPage(GameBiz biz, long value)
    {
        SetValue(value, $"last_gacha_uid_{biz}");
    }


    public static GameBiz GetLastRegionOfGame(GameBiz game)
    {
        return GetValue<GameBiz>(default, $"last_region_of_{game}");
    }

    public static void SetLastRegionOfGame(GameBiz game, GameBiz value)
    {
        SetValue(value, $"last_region_of_{game}");
    }




    #endregion




    #region Setting Method



    private static DatabaseService DatabaseService;

    private static Dictionary<string, string?> cache;


    private static void InitializeSettingProvider()
    {
        try
        {
            DatabaseService ??= GetService<DatabaseService>();
            if (cache is null)
            {
                if (reg)
                {
                    cache = new();
                }
                else
                {
                    using var dapper = DatabaseService.CreateConnection();
                    cache = dapper.Query<(string Key, string? Value)>("SELECT Key, Value FROM Setting;").ToDictionary(x => x.Key, x => x.Value);
                }
            }
        }
        catch { }
    }



    private static T? GetValue<T>(T? defaultValue = default, [CallerMemberName] string? key = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return defaultValue;
        }
        if (string.IsNullOrWhiteSpace(UserDataFolder))
        {
            return defaultValue;
        }
        InitializeSettingProvider();
        try
        {
            if (cache.TryGetValue(key, out string? value))
            {
                return ConvertFromString(value, defaultValue);
            }
            if (reg)
            {
                value = Registry.GetValue(REG_KEY_NAME, key, null) as string;
            }
            else
            {
                using var dapper = DatabaseService.CreateConnection();
                value = dapper.QueryFirstOrDefault<string>("SELECT Value FROM Setting WHERE Key=@key LIMIT 1;", new { key });
            }
            cache[key] = value;
            return ConvertFromString(value, defaultValue);
        }
        catch
        {
            return defaultValue;
        }
    }


    private static T? ConvertFromString<T>(string? value, T? defaultValue = default)
    {
        if (value is null)
        {
            return defaultValue;
        }
        var converter = TypeDescriptor.GetConverter(typeof(T));
        if (converter == null)
        {
            return defaultValue;
        }
        return (T?)converter.ConvertFromString(value);
    }


    private static void SetValue<T>(T? value, [CallerMemberName] string? key = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(UserDataFolder))
        {
            return;
        }
        InitializeSettingProvider();
        try
        {
            string? val = value?.ToString();
            if (cache.TryGetValue(key, out string? cacheValue) && cacheValue == val)
            {
                return;
            }
            cache[key] = val;
            if (reg)
            {
                if (val is null)
                {
#if (DEBUG || DEV) && !DISABLE_DEV
                    Registry.CurrentUser.OpenSubKey(@"Software\Starward_Dev", true)?.DeleteValue(key, false);
#else
                    Registry.CurrentUser.OpenSubKey(@"Software\Starward", true)?.DeleteValue(key, false);
#endif
                }
                else
                {
                    Registry.SetValue(REG_KEY_NAME, key, val);
                }
            }
            else
            {
                using var dapper = DatabaseService.CreateConnection();
                dapper.Execute("INSERT OR REPLACE INTO Setting (Key, Value) VALUES (@key, @val);", new { key, val });
            }
        }
        catch { }
    }



    public static void DeleteAllSettings()
    {
        if (reg)
        {
#if (DEBUG || DEV) && !DISABLE_DEV
            Registry.CurrentUser.OpenSubKey(@"Software", true)?.DeleteSubKeyTree("Starward_Dev");
#else
            Registry.CurrentUser.OpenSubKey(@"Software", true)?.DeleteSubKeyTree("Starward");
#endif
        }
        else
        {
            using var dapper = DatabaseService.CreateConnection();
            dapper.Execute("DELETE FROM Setting WHERE TRUE;");
        }
    }


    #endregion


}
