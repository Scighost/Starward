using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Launcher;
using Starward.Services.Cache;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

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





    public string? GetCachedBackgroundImage(GameBiz gameBiz)
    {
        string? name = null, file = null;
        if (AppConfig.GetEnableCustomBg(gameBiz))
        {
            name = AppConfig.GetCustomBg(gameBiz);
            file = Path.Join(AppConfig.ConfigDirectory, "bg", name);
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
            name = AppConfig.GetCustomBg(gameBiz);
            file = Path.Join(AppConfig.ConfigDirectory, "bg", name);
            if (File.Exists(file))
            {
                return file;
            }
        }

        var content = await GetLauncherContentAsync(gameBiz);
        string url = content.BackgroundImage.Background;
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







}
