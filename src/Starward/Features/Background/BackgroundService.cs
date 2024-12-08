using Microsoft.Extensions.Logging;
using Starward.Core.HoYoPlay;
using Starward.Features.HoYoPlay;
using Starward.Frameworks;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.Background;

public class BackgroundService
{




    private readonly ILogger<BackgroundService> _logger;


    private readonly HoYoPlayService _hoYoPlayService;


    private readonly HttpClient _httpClient;




    public BackgroundService(ILogger<BackgroundService> logger, HoYoPlayService hoYoPlayService, HttpClient httpClient)
    {
        _logger = logger;
        _hoYoPlayService = hoYoPlayService;
        _httpClient = httpClient;
    }




    /// <summary>
    /// 获取官方背景图文件路径，保存在 LocalAppData\Starward\cache
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [return: NotNullIfNotNull(nameof(name))]
    private static string? GetBgFilePath(string? name)
    {
        string cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Starward\cache");
        return Path.Join(cache, name);
    }



    /// <summary>
    /// 获取自定义背景图文件路径，保存在 UserData\bg
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [return: NotNullIfNotNull(nameof(name))]
    private static string? GetCustomBgFilePath(string? name)
    {
        return Path.Join(AppSetting.UserDataFolder, "bg", name);
    }


    /// <summary>
    /// 获取自定义背景图文件路径
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    private static bool TryGetCustomBgFilePath(GameId gameId, out string? path)
    {
        path = null;
        if (AppSetting.GetEnableCustomBg(gameId.GameBiz))
        {
            path = GetCustomBgFilePath(AppSetting.GetCustomBg(gameId.GameBiz));
            if (File.Exists(path))
            {
                return true;
            }
        }
        return false;
    }



    /// <summary>
    /// 文件是否是支持的视频格式，支持 mp4、mkv、flv、webm
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static bool FileIsSupportedVideo(string? name)
    {
        return Path.GetExtension(name) switch
        {
            ".mp4" or ".mkv" or ".flv" or ".webm" => true,
            _ => false,
        };
    }




    /// <summary>
    /// 获取已缓存的背景图文件路径
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public string? GetCachedBackgroundFile(GameId gameId)
    {
        if (TryGetCustomBgFilePath(gameId, out string? path))
        {
            return path;
        }
        path = GetBgFilePath(AppSetting.GetBg(gameId.GameBiz));
        return File.Exists(path) ? path : null;
    }



    /// <summary>
    /// 获取游戏官方背景图 URL
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string?> GetBackgroundImageUrlAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        var background = await _hoYoPlayService.GetGameBackgroundAsync(gameId);
        return background.Backgrounds.FirstOrDefault()?.Background.Url;
    }



    /// <summary>
    /// 获取背景图文件路径，最新的
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string?> GetBackgroundFileAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenSource = new CancellationTokenSource(10000);
            string? name, file;
            if (TryGetCustomBgFilePath(gameId, out file))
            {
                return file;
            }

            string? url = await GetBackgroundImageUrlAsync(gameId, tokenSource.Token);
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning("Background of mihoyo api is null ({gameBiz})", gameId);
                return GetCachedBackgroundFile(gameId);
            }
            name = Path.GetFileName(url);
            file = GetBgFilePath(name);
            if (!File.Exists(file))
            {
                var bytes = await _httpClient.GetByteArrayAsync(url, tokenSource.Token);
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                await File.WriteAllBytesAsync(file, bytes);
            }
            AppSetting.SetBg(gameId.GameBiz, name);
            return file;
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException or SocketException)
        {
            _logger.LogError(ex, "Get background image");
            return GetFallbackBackgroundImage(gameId);
        }
    }



    /// <summary>
    /// 获取默认的背景图文件路径
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    private string? GetFallbackBackgroundImage(GameId gameId)
    {
        string? bg = GetBgFilePath(AppSetting.GetBg(gameId.GameBiz));
        if (!File.Exists(bg))
        {
            string baseFolder = AppContext.BaseDirectory;
            string path = Path.Combine(baseFolder, @"Assets\Image\UI_CutScene_1130320101A.png");
            bg = File.Exists(path) ? path : null;
        }
        return bg;
    }



}
