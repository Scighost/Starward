using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Starward.Core;
using Starward.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Vanara.PInvoke;

namespace Starward.Services;

internal class UrlProtocolService
{



    public static void RegisterProtocol()
    {
        UnregisterProtocol();
        string exe;
        if (AppConfig.IsPortable)
        {
            exe = Path.Join(Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd('\\', '/')), "Starward.exe");
        }
        else
        {
            exe = Path.Join(AppContext.BaseDirectory, "Starward.exe");
        }
        string command = $"""
            "{exe}" "%1"
            """;
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\Starward", "", "URL:Starward Protocol");
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\Starward", "URL Protocol", "");
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\Starward\DefaultIcon", "", "Starward.exe,1");
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\Starward\Shell\Open\Command", "", command);
    }



    public static void UnregisterProtocol()
    {
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Starward", false);
    }



    public static async Task<bool> HandleUrlProtocolAsync(string url)
    {
        var log = AppConfig.GetLogger<UrlProtocolService>();
        try
        {
            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri? uri))
            {
                if (uri.Host is "test")
                {
                    return false;
                }
                if (string.IsNullOrWhiteSpace(AppConfig.UserDataFolder))
                {
                    log.LogWarning("UserDataFolder is null");
                    return false;
                }
                if (uri.Host is "startgame")
                {
                    if (Enum.TryParse(uri.AbsolutePath.Trim('/'), out GameBiz biz))
                    {
                        var kvs = HttpUtility.ParseQueryString(uri.Query);
                        string? uidStr = kvs["uid"];
                        string? installPath = kvs["install_path"];
                        var gameService = AppConfig.GetService<GameService>();
                        if (int.TryParse(uidStr, out int uid))
                        {
                            var gameAccountService = AppConfig.GetService<GameAccountService>();
                            var accounts = gameAccountService.GetGameAccountsFromDatabase(biz);
                            if (accounts.FirstOrDefault(x => x.Uid == uid) is GameAccount account)
                            {
                                gameAccountService.ChangeGameAccount(account);
                                log.LogInformation("Changed game account ({biz}, {uid}).", biz, uid);
                            }
                            else
                            {
                                log.LogWarning("Game account ({biz}, {uid}) is not saved.", biz, uid);
                            }
                        }
                        else
                        {
                            log.LogWarning("Cannot parse the uid '{uid}'", uidStr);
                        }
                        var p = gameService.StartGame(biz, false, installPath);
                        if (p != null)
                        {
                            await AppConfig.GetService<PlayTimeService>().StartProcessToLogAsync(biz);
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot parse the game biz '{uri.AbsolutePath.Trim('/')}'");
                    }
                    return true;
                }
                if (uri.Host is "playtime")
                {
                    if (Enum.TryParse(uri.AbsolutePath.Trim('/'), out GameBiz biz))
                    {
                        var kvs = HttpUtility.ParseQueryString(uri.Query);
                        await AppConfig.GetService<PlayTimeService>().StartProcessToLogAsync(biz);
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot parse the game biz '{uri.AbsolutePath.Trim('/')}'");
                    }
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Handle url protocol");
            User32.MessageBox(HWND.NULL, ex.Message, "Starward");
            return true;
        }
        return false;
    }





}
