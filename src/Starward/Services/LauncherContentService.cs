using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Launcher;
using Starward.Helpers;
using Starward.Services.Cache;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.System;

namespace Starward.Services;

public class LauncherContentService
{

    private readonly ILogger<LauncherContentService> _logger;

    private readonly HttpClient _httpClient;

    private readonly LauncherClient _launcherClient;




    public LauncherContentService(ILogger<LauncherContentService> logger, HttpClient httpClient, LauncherClient launcherClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _launcherClient = launcherClient;
    }





    public async Task<ContentWrapper> GetLauncherContentAsync(GameBiz gameBiz, CancellationToken cancellationToken = default)
    {
        string lang = CultureInfo.CurrentUICulture.Name;
        var content = MemoryCache.Instance.GetItem<ContentWrapper>($"LauncherContent_{gameBiz}_{lang}", TimeSpan.FromSeconds(10));
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

            string url;
            if (gameBiz is GameBiz.hk4e_cloud)
            {
                var image = await _launcherClient.GetCloudGameBackgroundAsync(gameBiz, tokenSource.Token);
                url = image.Url;
            }
            /*else if (gameBiz is GameBiz.nap_cn)
            {
                url = await _launcherClient.GetZZZCBT3BackgroundAsync(gameBiz, tokenSource.Token);
            }*/
            else
            {
                url = await _launcherClient.GetBackgroundAsync(gameBiz, tokenSource.Token);
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
            string? path = gameBiz.ToGame() switch
            {
                GameBiz.Honkai3rd => Path.Combine(baseFolder, @"Assets\Image\poster_honkai.png"),
                GameBiz.GenshinImpact => Path.Combine(baseFolder, @"Assets\Image\poster_genshin.png"),
                GameBiz.StarRail => Path.Combine(baseFolder, @"Assets\Image\poster_starrail.png"),
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
                await Launcher.LaunchUriAsync(new Uri(file));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open custom background");
        }
    }




}
