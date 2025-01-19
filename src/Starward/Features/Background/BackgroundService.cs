using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Starward.Core.HoYoPlay;
using Starward.Features.HoYoPlay;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Storage;

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
        string cache = Path.Combine(AppSetting.CacheFolder, "cache");
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
        if (gameId is null)
        {
            return false;
        }
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
    /// 获取版本海报文件路径
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    private static bool TryGetVersionPosterBgFilePath(GameId gameId, out string? path)
    {
        path = null;
        if (gameId is null)
        {
            return false;
        }
        if (AppSetting.GetUseVersionPoster(gameId.GameBiz))
        {
            path = Path.Join(AppSetting.UserDataFolder, "bg", AppSetting.GetVersionPoster(gameId.GameBiz));
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
        if (gameId is null)
        {
            return null;
        }
        if (TryGetCustomBgFilePath(gameId, out string? path))
        {
            return path;
        }
        if (TryGetVersionPosterBgFilePath(gameId, out path))
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
    /// 更新版本海报
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<string?> GetVersionPosterAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!AppSetting.GetUseVersionPoster(gameId.GameBiz))
            {
                return null;
            }
            var info = await _hoYoPlayService.GetGameInfoAsync(gameId);
            string url = info.Display.Background.Url;
            string bg = Path.Combine(AppSetting.UserDataFolder, "bg");
            Directory.CreateDirectory(bg);
            string name = Path.GetFileName(url);
            string path = Path.Combine(bg, name);
            if (!File.Exists(path))
            {
                byte[] bytes = await _httpClient.GetByteArrayAsync(url, cancellationToken);
                await File.WriteAllBytesAsync(path, bytes);
            }
            AppSetting.SetVersionPoster(gameId.GameBiz, name);
            return path;
        }
        catch (Exception ex)
        {
            return null;
        }
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
            string? name, file;
            if (TryGetCustomBgFilePath(gameId, out file))
            {
                return file;
            }

            file = await GetVersionPosterAsync(gameId, cancellationToken);
            if (file is not null)
            {
                return file;
            }

            string? url = await GetBackgroundImageUrlAsync(gameId, cancellationToken);
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning("Background of mihoyo api is null ({gameBiz})", gameId);
                return GetCachedBackgroundFile(gameId);
            }
            name = Path.GetFileName(url);
            file = GetBgFilePath(name);
            if (!File.Exists(file))
            {
                var bytes = await _httpClient.GetByteArrayAsync(url, cancellationToken);
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



    /// <summary>
    /// 更改自定义背景图文件，保存在 UserData\bg，返回文件名
    /// </summary>
    /// <param name="xamlRoot"></param>
    /// <returns></returns>
    public async Task<string?> ChangeCustomBackgroundFileAsync(XamlRoot xamlRoot)
    {
        string? file = await PickBackgroundFileAsync(xamlRoot);
        if (file is null)
        {
            return null;
        }
        await CheckBackgroundFileAvailableAsync(file);
        string bg = Path.Join(AppSetting.UserDataFolder, "bg");
        Directory.CreateDirectory(bg);
        string name = Path.GetFileName(file);
        string path = Path.Combine(bg, name);
        if (path != file)
        {
            File.Copy(file, path, true);
        }
        return name;
    }



    /// <summary>
    /// 更改自定义背景图文件，保存在 UserData\bg，返回文件名
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public async Task<string?> ChangeCustomBackgroundFileAsync(StorageFile file)
    {
        string bg = Path.Join(AppSetting.UserDataFolder, "bg");
        if (Path.GetDirectoryName(file.Path) != bg)
        {
            if (FileIsSupportedVideo(file.Name))
            {
                using var source = MediaSource.CreateFromStorageFile(file);
                await source.OpenAsync();
            }
            else
            {
                using var fs = await file.OpenReadAsync();
                var decoder = await BitmapDecoder.CreateAsync(fs);
            }
            {
                Directory.CreateDirectory(bg);
                string path = Path.Combine(bg, file.Name);
                using var dest = File.OpenWrite(path);
                using var stream = await file.OpenReadAsync();
                await stream.AsStream().CopyToAsync(dest);
            }
        }
        return file.Name;
    }


    /// <summary>
    /// 选择背景图文件
    /// </summary>
    /// <param name="xamlRoot"></param>
    /// <returns></returns>
    private async Task<string?> PickBackgroundFileAsync(XamlRoot xamlRoot)
    {
        var filter = new (string, string)[]
            {
                ("Image", ".bmp"),
                ("Image", ".jpg"),
                ("Image", ".png"),
                ("Image", ".webp"),
                ("Image", ".avif"),
                ("Video", ".mp4"),
                ("Video", ".mkv"),
                ("Video", ".flv"),
                ("Video", ".webm"),
            };
        return await FileDialogHelper.PickSingleFileAsync(xamlRoot, filter);
    }



    /// <summary>
    /// 检查背景图文件是否可用
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private async Task CheckBackgroundFileAvailableAsync(string file)
    {
        if (FileIsSupportedVideo(file))
        {
            // 0xC00D36C4
            using var source = MediaSource.CreateFromUri(new Uri(file));
            await source.OpenAsync();
        }
        else
        {
            // 0x88982F8B
            using var fs = File.OpenRead(file);
            var decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream());
        }
    }






}
