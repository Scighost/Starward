using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Launcher;
using Starward.Helpers;
using Starward.Services.Cache;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace Starward.Services;

public class LauncherContentService
{

    private readonly ILogger<LauncherContentService> _logger;

    private readonly HttpClient _httpClient;

    private readonly LauncherClient _launcherClient;

    private readonly HoYoPlayService _hoYoPlayService;


    public LauncherContentService(ILogger<LauncherContentService> logger, HttpClient httpClient, LauncherClient launcherClient, HoYoPlayService hoYoPlayService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _launcherClient = launcherClient;
        _hoYoPlayService = hoYoPlayService;
    }





    public async Task<LauncherContent> GetLauncherContentAsync(GameBiz gameBiz, CancellationToken cancellationToken = default)
    {
        string lang = CultureInfo.CurrentUICulture.Name;
        var content = MemoryCache.Instance.GetItem<LauncherContent>($"LauncherContent_{gameBiz}_{lang}", TimeSpan.FromSeconds(10));
        if (content != null)
        {
            return content;
        }
        content = await _launcherClient.GetLauncherContentAsync(gameBiz, lang, cancellationToken);
        MemoryCache.Instance.SetItem($"LauncherContent_{gameBiz}_{lang}", content);
        return content;
    }



    public async Task<bool> IsNoticesAlertAsync(GameBiz gameBiz, long uid, CancellationToken cancellationToken = default)
    {
        return await _launcherClient.IsNoticesAlertAsync(gameBiz, uid, CultureInfo.CurrentUICulture.Name, cancellationToken);
    }



    public static string? GetBackgroundFilePath(string? name)
    {
        return Path.GetExtension(name) switch
        {
            ".flv" or ".mkv" or ".mov" or ".mp4" or ".webm" => name,
            _ => Path.Join(AppConfig.UserDataFolder, "bg", name),
        };
    }



    public string? GetCachedBackgroundImage(GameBiz gameBiz, bool disableCustom = false)
    {
        string? name = null, file = null;
        if (AppConfig.GetEnableCustomBg(gameBiz) && !disableCustom)
        {
            file = GetBackgroundFilePath(AppConfig.GetCustomBg(gameBiz));
            if (File.Exists(file))
            {
                return file;
            }
            _logger.LogWarning("Image file not found '{file}'", file);
        }
        name = AppConfig.GetBg(gameBiz);
        file = Path.Join(AppConfig.UserDataFolder, "bg", name);
        if (File.Exists(file))
        {
            return file;
        }
        return null;
    }





    public async Task<string?> GetBackgroundImageAsync(GameBiz gameBiz, bool disableCustom = false)
    {
        try
        {
            var tokenSource = new CancellationTokenSource(10000);
            string? name, file;
            if (AppConfig.GetEnableCustomBg(gameBiz) && !disableCustom)
            {
                file = GetBackgroundFilePath(AppConfig.GetCustomBg(gameBiz));
                if (File.Exists(file))
                {
                    return file;
                }
            }

            string? url;
            if (gameBiz.ToGame() == GameBiz.bh3 && gameBiz.IsGlobalServer())
            {
                var content = await GetLauncherContentAsync(gameBiz, tokenSource.Token);
                url = content.BackgroundImage?.Background;
            }
            else if (gameBiz == GameBiz.clgm_cn)
            {
                var background = await _hoYoPlayService.GetGameInfoAsync(GameBiz.hk4e_cn);
                url = background.Display.Background.Url;
            }
            else
            {
                var background = await _hoYoPlayService.GetGameBackgroundAsync(gameBiz);
                url = background.Backgrounds.FirstOrDefault()?.Background.Url;
            }
            if (string.IsNullOrWhiteSpace(url))
            {
                url = GetFallbackBackgroundImage(gameBiz);
                _logger.LogWarning("Background of mihoyo api is null ({gameBiz})", gameBiz);
            }
            name = Path.GetFileName(url);
            file = Path.Join(AppConfig.UserDataFolder, "bg", name);
            if (!File.Exists(file))
            {
                var bytes = await _httpClient.GetByteArrayAsync(url, tokenSource.Token);
                Directory.CreateDirectory(Path.Combine(AppConfig.UserDataFolder, "bg"));
                await File.WriteAllBytesAsync(file, bytes);
            }
            AppConfig.SetBg(gameBiz, name);
            return file;
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException or SocketException)
        {
            _logger.LogError(ex, "Get background image");
            return GetFallbackBackgroundImage(gameBiz);
        }
    }



    private string? GetFallbackBackgroundImage(GameBiz gameBiz)
    {
        string? bg = Path.Join(AppConfig.UserDataFolder, "bg", AppConfig.GetBg(gameBiz));
        if (File.Exists(bg))
        {
            return bg;
        }
        else
        {
            string baseFolder = AppContext.BaseDirectory;
            string? path = gameBiz.ToGame().Value switch
            {
                GameBiz.bh3 => Path.Combine(baseFolder, @"Assets\Image\poster_honkai.png"),
                GameBiz.hk4e => Path.Combine(baseFolder, @"Assets\Image\poster_genshin.png"),
                GameBiz.hkrpg => Path.Combine(baseFolder, @"Assets\Image\poster_starrail.png"),
                _ => null,
            };
            if (File.Exists(path))
            {
                return path;
            }
            else
            {
                return null;
            }
        }
    }




    public async Task<string?> ChangeCustomBgAsync()
    {
        try
        {
            var filter = new List<(string, string)>
            {
                ("Image", ".bmp"),
                ("Image", ".jpg"),
                ("Image", ".jxl"),
                ("Image", ".png"),
                ("Image", ".avif"),
                ("Image", ".heic"),
                ("Image", ".webp"),
                ("Video", ".flv"),
                ("Video", ".mkv"),
                ("Video", ".mov"),
                ("Video", ".mp4"),
                ("Video", ".webm"),
            };
            var file = await FileDialogHelper.PickSingleFileAsync(MainWindow.Current.WindowHandle, filter.ToArray());
            if (File.Exists(file))
            {
                _logger.LogInformation("Background file is '{file}'", file);
                if (Path.GetExtension(file) is ".flv" or ".mkv" or ".mov" or ".mp4" or ".webm")
                {
                    return file;
                }
                else
                {
                    using var fs = File.OpenRead(file);
                    var decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream());
                    var name = Path.GetFileName(file);
                    var dest = Path.Combine(AppConfig.UserDataFolder, "bg", name);
                    if (file != dest)
                    {
                        File.Copy(file, dest, true);
                        _logger.LogInformation("File copied to '{dest}'", dest);
                    }
                    return name;
                }
            }
        }
        catch (COMException ex)
        {
            // 0x88982F50
            _logger.LogError(ex, "Decode error or others");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change custom background");
        }
        return null;
    }




    public async Task OpenCustomBgAsync(string? name)
    {
        try
        {
            var file = GetBackgroundFilePath(name);
            if (File.Exists(file))
            {
                _logger.LogError("Open image or video file '{file}'", file);
                await Windows.System.Launcher.LaunchUriAsync(new Uri(file));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open custom background");
        }
    }




}
