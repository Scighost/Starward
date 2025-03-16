using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.GameLauncher;
using Starward.Features.PlayTime;
using Starward.Frameworks;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Vanara.PInvoke;

namespace Starward.Features.UrlProtocol;

internal class UrlProtocolService
{



    public static void RegisterProtocol()
    {
        UnregisterProtocol();
        string exe;
        if (AppSetting.IsPortable)
        {
            exe = AppSetting.StarwardLauncherExecutePath ?? Path.Join(Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd('\\', '/')), "Starward.exe");
        }
        else
        {
            exe = AppSetting.StarwardExecutePath;
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
        var log = AppService.GetLogger<UrlProtocolService>();
        try
        {
            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri? uri))
            {
                if (uri.Host is "test")
                {
                    return false;
                }
                if (string.IsNullOrWhiteSpace(AppSetting.UserDataFolder))
                {
                    log.LogWarning("UserDataFolder is null");
                    return false;
                }
                if (uri.Host is "startgame")
                {
                    if (GameBiz.TryParse(uri.AbsolutePath.Trim('/'), out GameBiz biz) && GameId.FromGameBiz(biz) is GameId gameId)
                    {
                        var kvs = HttpUtility.ParseQueryString(uri.Query);
                        string? installPath = kvs["install_path"];
                        await AppService.GetService<GameLauncherService>().StartGameAsync(gameId, installPath);
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot parse the game_biz \"{uri.AbsolutePath.Trim('/')}\".");
                    }
                    return true;
                }
                if (uri.Host is "playtime")
                {
                    if (GameBiz.TryParse(uri.AbsolutePath.Trim('/'), out GameBiz biz) && GameId.FromGameBiz(biz) is GameId gameId)
                    {
                        var kvs = HttpUtility.ParseQueryString(uri.Query);
                        if (int.TryParse(kvs["pid"], out int pid))
                        {
                            await AppService.GetService<PlayTimeService>().StartProcessToLogAsync(gameId, pid);
                        }
                        else
                        {
                            await AppService.GetService<PlayTimeService>().StartProcessToLogAsync(gameId);
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot parse the game_biz \"{uri.AbsolutePath.Trim('/')}\".");
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
