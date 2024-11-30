using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.HoYoPlay;
using Starward.Core.Launcher;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services.Launcher;

internal class GameLauncherService
{


    private readonly ILogger<GameLauncherService> _logger;


    private readonly HoYoPlayService _hoYoPlayService;


    private readonly LauncherClient _launcherClient;


    private readonly HttpClient _httpClient;



    public GameLauncherService(ILogger<GameLauncherService> logger, HoYoPlayService hoYoPlayService, LauncherClient launcherClient, HttpClient httpClient)
    {
        _logger = logger;
        _hoYoPlayService = hoYoPlayService;
        _launcherClient = launcherClient;
        _httpClient = httpClient;
    }






    /// <summary>
    /// 游戏内公告网页URL
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <param name="uid"></param>
    /// <returns></returns>
    //public Task<string> GetGameNoticesWebURLAsync(GameBiz gameBiz, long uid)
    //{

    //}


    /// <summary>
    /// 是否显示公告提醒
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <param name="uid"></param>
    /// <returns></returns>
    //public Task<bool> IsGameNoticesAlertAsync(GameBiz gameBiz, long uid)
    //{

    //}




    /// <summary>
    /// 游戏安装目录，为空时未找到
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <returns></returns>
    public string? GetGameInstallPath(GameBiz gameBiz)
    {
        var path = AppConfig.GetGameInstallPath(gameBiz);
        if (string.IsNullOrWhiteSpace(path))
        {
            path = GetDefaultGameInstallPath(gameBiz);
        }
        if (Directory.Exists(path))
        {
            return Path.GetFullPath(path);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(path) && AppConfig.GetGameInstallPathRemovable(gameBiz))
            {
                return path;
            }
            else
            {
                AppConfig.SetGameInstallPath(gameBiz, null);
                AppConfig.SetGameInstallPathRemovable(gameBiz, false);
            }
            return null;
        }
    }



    private string? GetDefaultGameInstallPath(GameBiz gameBiz)
    {
        if (gameBiz.IsChinaOfficial())
        {
            return Registry.GetValue($@"HKEY_CURRENT_USER\Software\miHoYo\HYP\1_1\{gameBiz}", "GameInstallPath", null) as string;
        }
        else if (gameBiz.IsGlobalOfficial())
        {
            if (gameBiz.ToGame() is GameBiz.Honkai3rd)
            {
                return GachaLogClient.GetGameInstallPathFromRegistry(gameBiz);
            }
            else
            {
                return Registry.GetValue($@"HKEY_CURRENT_USER\Software\Cognosphere\HYP\1_0\{gameBiz}", "GameInstallPath", null) as string;
            }
        }
        else if (gameBiz.IsBilibili())
        {
            return Registry.GetValue($@"HKEY_CURRENT_USER\Software\miHoYo\HYP\standalone\14_0\{gameBiz}\{LauncherId.FromGameBiz(gameBiz)}\{gameBiz}", "GameInstallPath", null) as string;
        }
        else if (gameBiz.IsChinaCloud())
        {
            return GachaLogClient.GetGameInstallPathFromRegistry(gameBiz);
        }
        else
        {
            return null;
        }
    }



    /// <summary>
    /// 最新游戏版本
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <returns></returns>
    public async Task<Version?> GetLatestGameVersionAsync(GameBiz gameBiz)
    {
        if (gameBiz.IsChinaOfficial() || gameBiz.IsGlobalOfficial() || gameBiz.IsBilibili())
        {
            var package = await _hoYoPlayService.GetGamePackageAsync(gameBiz);
            return TryParseVersion(package.Main.Major?.Version);
        }
        else
        {
            return null;
        }
    }


    /// <summary>
    /// 本地游戏版本
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <returns></returns>
    public async Task<Version?> GetLocalGameVersionAsync(GameBiz gameBiz, string? installPath = null)
    {
        installPath ??= GetGameInstallPath(gameBiz);
        if (gameBiz is GameBiz.hk4e_cloud)
        {
            var exe = Path.Join(installPath, GetGameExeName(gameBiz));
            if (File.Exists(exe))
            {
                return TryParseVersion(FileVersionInfo.GetVersionInfo(exe).ProductVersion);
            }
            else
            {
                return null;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(installPath))
            {
                return null;
            }
            var config = Path.Join(installPath, "config.ini");
            if (File.Exists(config))
            {
                var str = await File.ReadAllTextAsync(config);
                return TryParseVersion(Regex.Match(str, @"game_version=(.+)").Groups[1].Value);
            }
            else
            {
                _logger.LogWarning("config.ini not found: {path}", config);
                return null;
            }
        }
    }


    /// <summary>
    /// 硬链接信息
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <returns></returns>
    public async Task<(GameBiz, string?)> GetHardLinkInfoAsync(GameBiz gameBiz, string? installPath = null)
    {
        installPath ??= GetGameInstallPath(gameBiz);
        if (gameBiz is GameBiz.hk4e_cloud)
        {
            return (GameBiz.None, null);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(installPath))
            {
                return (GameBiz.None, null);
            }
            var config = Path.Join(installPath, "config.ini");
            if (File.Exists(config))
            {
                var str = await File.ReadAllTextAsync(config);
                Enum.TryParse(Regex.Match(str, @"hardlink_gamebiz=(.+)").Groups[1].Value, out GameBiz biz);
                var path = Regex.Match(str, @"hardlink_path=(.+)").Groups[1].Value;
                return (biz, path);
            }
            else
            {
                _logger.LogWarning("config.ini not found: {path}", config);
                return (GameBiz.None, null);
            }
        }
    }


    /// <summary>
    /// 预下载版本
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <returns></returns>
    public async Task<Version?> GetPreDownloadGameVersionAsync(GameBiz gameBiz)
    {
        if (gameBiz.IsChinaOfficial() || gameBiz.IsGlobalOfficial() || gameBiz.IsBilibili())
        {
            var package = await _hoYoPlayService.GetGamePackageAsync(gameBiz);
            return TryParseVersion(package.PreDownload?.Major?.Version);
        }
        else
        {
            return null;
        }
    }



    private static Version? TryParseVersion(string? version)
    {
        if (Version.TryParse(version, out var result))
        {
            return result;
        }
        else
        {
            return null;
        }
    }



    /// <summary>
    /// 游戏进程名，带 .exe 扩展名
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <returns></returns>
    public string GetGameExeName(GameBiz gameBiz)
    {
        return gameBiz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => "YuanShen.exe",
            GameBiz.hk4e_global => "GenshinImpact.exe",
            GameBiz.hk4e_cloud => "Genshin Impact Cloud Game.exe",
            _ => gameBiz.ToGame() switch
            {
                GameBiz.StarRail => "StarRail.exe",
                GameBiz.Honkai3rd => "BH3.exe",
                GameBiz.ZZZ => "ZenlessZoneZero.exe",
                _ => throw new ArgumentOutOfRangeException($"Unknown region {gameBiz}"),
            },
        };
    }


    /// <summary>
    /// 游戏进程文件是否存在
    /// </summary>
    /// <param name="biz"></param>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public bool IsGameExeExists(GameBiz biz, string? installPath = null)
    {
        installPath ??= GetGameInstallPath(biz);
        if (!string.IsNullOrWhiteSpace(installPath))
        {
            var exe = Path.Join(installPath, GetGameExeName(biz));
            return File.Exists(exe);
        }
        return false;
    }



    /// <summary>
    /// 获取游戏进程
    /// </summary>
    /// <param name="biz"></param>
    /// <returns></returns>
    public Process? GetGameProcess(GameBiz biz)
    {
        int currentSessionId = Process.GetCurrentProcess().SessionId;
        var name = GetGameExeName(biz).Replace(".exe", "");
        return Process.GetProcessesByName(name).Where(x => x.SessionId == currentSessionId).FirstOrDefault();
    }



    /// <summary>
    /// 启动游戏
    /// </summary>
    /// <returns></returns>
    public async Task<Process?> StartGame(GameBiz biz, bool ignoreRunningGame = false, string? installPath = null)
    {
        const int ERROR_CANCELLED = 0x000004C7;
        try
        {
            if (!ignoreRunningGame)
            {
                if (GetGameProcess(biz) != null)
                {
                    throw new Exception("Game process is running.");
                }
            }
            string? exe = null, arg = null, verb = null;
            if (Directory.Exists(installPath))
            {
                var e = Path.Join(installPath, GetGameExeName(biz));
                if (File.Exists(e))
                {
                    exe = e;
                }
            }
            if (string.IsNullOrWhiteSpace(exe) && AppConfig.GetEnableThirdPartyTool(biz))
            {
                exe = AppConfig.GetThirdPartyToolPath(biz);
                if (File.Exists(exe))
                {
                    verb = Path.GetExtension(exe) is ".exe" or ".bat" ? "runas" : "";
                }
                else
                {
                    exe = null;
                    AppConfig.SetThirdPartyToolPath(biz, null);
                    _logger.LogWarning("Third party tool not found: {path}", exe);
                }
            }
            if (string.IsNullOrWhiteSpace(exe))
            {
                var folder = GetGameInstallPath(biz);
                var name = GetGameExeName(biz);
                exe = Path.Join(folder, name);
                arg = AppConfig.GetStartArgument(biz)?.Trim();
                verb = (biz is GameBiz.hk4e_cloud) ? "" : "runas";
                if (!File.Exists(exe))
                {
                    _logger.LogWarning("Game exe not found: {path}", exe);
                    throw new FileNotFoundException("Game exe not found", name);
                }
            }
            if (AppConfig.EnableLoginAuthTicket is true)
            {
                string? ticket = await CreateAuthTicketByGameBiz(biz);
                if (!string.IsNullOrWhiteSpace(ticket))
                {
                    arg += $" login_auth_ticket={ticket}";
                }
            }
            _logger.LogInformation("Start game ({biz})\r\npath: {exe}\r\narg: {arg}", biz, exe, arg);
            var info = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arg,
                UseShellExecute = true,
                Verb = verb,
                WorkingDirectory = Path.GetDirectoryName(exe),
            };
            return Process.Start(info);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_CANCELLED)
        {
            // Operation canceled
            _logger.LogInformation("Start game operation canceled.");
        }
        return null;
    }




    #region Game Auth Login



    private long? hyperionAid;



    public async Task<long?> GetHyperionAidAsync()
    {
        if (!hyperionAid.HasValue)
        {
            await VerifyStokenAsync();
        }
        return hyperionAid;
    }



    public async Task VerifyStokenAsync()
    {
        if (string.IsNullOrWhiteSpace(AppConfig.stoken) || string.IsNullOrWhiteSpace(AppConfig.mid))
        {
            return;
        }
        var obj = new
        {
            token = new
            {
                token_type = 1,
                token = AppConfig.stoken,
            },
            refresh = true,
            mid = AppConfig.mid,
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "https://passport-api.mihoyo.com/account/ma-cn-session/app/verify")
        {
            Content = JsonContent.Create(obj),
        };
        request.Headers.Add("x-rpc-app_id", "ddxf5dufpuyo");
        request.Headers.Add("x-rpc-client_type", "3");
        request.Headers.Add("x-rpc-game_biz", "hyp_cn");
        await AppConfig.GetService<GameRecordService>().UpdateDeviceFpAsync();
        request.Headers.Add("x-rpc-device_fp", AppConfig.HyperionDeviceFp);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var node = await response.Content.ReadFromJsonAsync<Root<Data>>();
        if (node!.Retcode != 0)
        {
            throw new miHoYoApiException(node.Retcode, node.Message);
        }
        hyperionAid = node.Data.UserInfo.Aid;
        if (node.Data.NewToken != null && node.Data.NewToken.Token != AppConfig.stoken)
        {
            AppConfig.stoken = node.Data.NewToken.Token;
            AppConfig.SaveConfiguration();
        }
    }



    private async Task<string?> CreateAuthTicketByGameBiz(GameBiz gameBiz)
    {
        try
        {
            long? aid = await GetHyperionAidAsync();
            if (!hyperionAid.HasValue)
            {
                return null;
            }
            var obj = new
            {
                game_biz = gameBiz.ToString(),
                stoken = AppConfig.stoken,
                uid = hyperionAid,
                mid = AppConfig.mid,
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "https://passport-api.mihoyo.com/account/ma-cn-verifier/app/createAuthTicketByGameBiz")
            {
                Content = JsonContent.Create(obj),
            };
            request.Headers.Add("x-rpc-app_id", "ddxf5dufpuyo");
            request.Headers.Add("x-rpc-client_type", "3");
            request.Headers.Add("x-rpc-game_biz", "hyp_cn");
            var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await AppConfig.GetService<GameRecordService>().UpdateDeviceFpAsync(false, cancel.Token);
            request.Headers.Add("x-rpc-device_fp", AppConfig.HyperionDeviceFp);
            var response = await _httpClient.SendAsync(request, cancel.Token);
            response.EnsureSuccessStatusCode();
            var node = await response.Content.ReadFromJsonAsync<Root<AuthTicket>>(cancel.Token);
            if (node!.Retcode != 0)
            {
                throw new miHoYoApiException(node.Retcode, node.Message);
            }
            return node.Data.Ticket;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAuthTicketByGameBiz");
            NotificationBehavior.Instance.Error($"Failed to create auth ticket: {ex.Message}");
        }
        return null;
    }




    public class Data
    {
        [JsonPropertyName("user_info")]
        public UserInfo UserInfo { get; set; }

        [JsonPropertyName("realname_info")]
        public object RealnameInfo { get; set; }

        [JsonPropertyName("need_realperson")]
        public bool NeedRealperson { get; set; }

        [JsonPropertyName("new_token")]
        public NewToken NewToken { get; set; }
    }

    public class NewToken
    {
        [JsonPropertyName("token_type")]
        public int TokenType { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }
    }

    public class Root<T>
    {
        [JsonPropertyName("retcode")]
        public int Retcode { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }
    }

    public class UserInfo
    {
        [JsonPropertyName("aid")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long Aid { get; set; }

        [JsonPropertyName("mid")]
        public string Mid { get; set; }

        [JsonPropertyName("account_name")]
        public string AccountName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("is_email_verify")]
        public int IsEmailVerify { get; set; }

        [JsonPropertyName("area_code")]
        public string AreaCode { get; set; }

        [JsonPropertyName("mobile")]
        public string Mobile { get; set; }

        [JsonPropertyName("safe_area_code")]
        public string SafeAreaCode { get; set; }

        [JsonPropertyName("safe_mobile")]
        public string SafeMobile { get; set; }

        [JsonPropertyName("realname")]
        public string Realname { get; set; }

        [JsonPropertyName("identity_code")]
        public string IdentityCode { get; set; }

        [JsonPropertyName("rebind_area_code")]
        public string RebindAreaCode { get; set; }

        [JsonPropertyName("rebind_mobile")]
        public string RebindMobile { get; set; }

        [JsonPropertyName("rebind_mobile_time")]
        public string RebindMobileTime { get; set; }

        [JsonPropertyName("links")]
        public List<object> Links { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("password_time")]
        public string PasswordTime { get; set; }

        [JsonPropertyName("unmasked_email")]
        public string UnmaskedEmail { get; set; }

        [JsonPropertyName("unmasked_email_type")]
        public int UnmaskedEmailType { get; set; }
    }


    public class AuthTicket
    {
        [JsonPropertyName("ticket")]
        public string Ticket { get; set; }
    }



    #endregion









}
