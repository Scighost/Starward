using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Launcher;
using Starward.Service.Cache;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Starward.Service;

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
        var name = AppConfig.GetBg(gameBiz);
        var file = Path.Join(AppConfig.ConfigDirectory, "bg", name);
        if (File.Exists(file))
        {
            return file;
        }
        else
        {
            return null;
        }
    }





    public async Task<string> GetBackgroundImageAsync(GameBiz gameBiz)
    {
        string? name, file;
        if (AppConfig.GetEnableCustomBg(gameBiz))
        {
            name = AppConfig.GetBg(gameBiz);
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
        if (File.Exists(file))
        {
            return file;
        }

        var bytes = await _httpClient.GetByteArrayAsync(url);
        Directory.CreateDirectory(Path.Combine(AppConfig.ConfigDirectory, "bg"));
        await File.WriteAllBytesAsync(file, bytes);
        AppConfig.GetBg(gameBiz);
        return file;
    }







}
