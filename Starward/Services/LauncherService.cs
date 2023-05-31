using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Launcher;
using Starward.Helpers;
using Starward.Services.Cache;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.System;

namespace Starward.Services;

public class LauncherService
{

    private readonly ILogger<LauncherService> _logger;

    private readonly HttpClient _httpClient;

    private readonly LauncherClient _launcherClient;




    public LauncherService(ILogger<LauncherService> logger, HttpClient httpClient, LauncherClient launcherClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _launcherClient = launcherClient;
    }





    public async Task<LauncherContent> GetLauncherContentAsync(GameBiz gameBiz)
    {
        var content = MemoryCache.Instance.GetItem<LauncherContent>($"LauncherContent_{gameBiz}", TimeSpan.FromSeconds(10));
        if (content != null)
        {
            return content;
        }
        content = await _launcherClient.GetLauncherContentAsync(gameBiz);
        MemoryCache.Instance.SetItem($"LauncherContent_{gameBiz}", content);
        return content;
    }




    public static string? GetBackgroundFilePath(string? name)
    {
        return Path.GetExtension(name) switch
        {
            ".flv" or ".mkv" or ".mov" or ".mp4" or ".webm" => name,
            _ => Path.Join(AppConfig.ConfigDirectory, "bg", name),
        };
    }



    public string? GetCachedBackgroundImage(GameBiz gameBiz)
    {
        string? name = null, file = null;
        if (AppConfig.GetEnableCustomBg(gameBiz))
        {
            file = GetBackgroundFilePath(AppConfig.GetCustomBg(gameBiz));
            if (File.Exists(file))
            {
                return file;
            }
            _logger.LogWarning("Image file not found '{file}'", file);
        }
        name = AppConfig.GetBg(gameBiz);
        file = Path.Join(AppConfig.ConfigDirectory, "bg", name);
        if (File.Exists(file))
        {
            return file;
        }
        return null;
    }





    public async Task<string> GetBackgroundImageAsync(GameBiz gameBiz)
    {
        string? name, file;
        if (AppConfig.GetEnableCustomBg(gameBiz))
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
            var image = await _launcherClient.GetCloudGameBackgroundAsync(gameBiz);
            url = image.Url;
        }
        else
        {
            var content = await GetLauncherContentAsync(gameBiz);
            url = content.BackgroundImage.Background;
        }
        name = Path.GetFileName(url);
        file = Path.Join(AppConfig.ConfigDirectory, "bg", name);
        if (!File.Exists(file))
        {
            var bytes = await _httpClient.GetByteArrayAsync(url);
            Directory.CreateDirectory(Path.Combine(AppConfig.ConfigDirectory, "bg"));
            await File.WriteAllBytesAsync(file, bytes);
        }
        AppConfig.SetBg(gameBiz, name);
        return file;
    }





    public async Task<string?> ChangeCustomBgAsync()
    {
        try
        {
            var filter = new List<(string, string)>
            {
                ("Image", ".bmp"),
                ("Image", ".jpg"),
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
            var file = await FileDialogHelper.PickSingleFileAsync(MainWindow.Current.HWND, filter.ToArray());
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
                    var dest = Path.Combine(AppConfig.ConfigDirectory, "bg", name);
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
