using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Starward.Core.HoYoPlay;
using Starward.Features.HoYoPlay;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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
    /// 获取背景图文件路径，保存在 UserData\bg
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [return: NotNullIfNotNull(nameof(name))]
    private static string? GetBgFilePath(string? name)
    {
        return Path.Join(AppConfig.UserDataFolder, "bg", name);
    }


    /// <summary>
    /// 获取自定义背景图文件路径
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static bool TryGetCustomBgFilePath(GameId gameId, out string? path)
    {
        path = null;
        if (gameId is null)
        {
            return false;
        }
        if (AppConfig.GetEnableCustomBg(gameId.GameBiz))
        {
            path = GetBgFilePath(AppConfig.GetCustomBg(gameId.GameBiz));
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
        if (AppConfig.GetUseVersionPoster(gameId.GameBiz))
        {
            path = Path.Join(AppConfig.UserDataFolder, "bg", AppConfig.GetVersionPoster(gameId.GameBiz));
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
    public static string? GetCachedBackgroundFile(GameId gameId)
    {
        if (gameId is null)
        {
            return null;
        }
        if (TryGetCustomBgFilePath(gameId, out string? path))
        {
            return path;
        }
        //if (TryGetVersionPosterBgFilePath(gameId, out path))
        //{
        //    return path;
        //}
        path = GetBgFilePath(AppConfig.GetBg(gameId.GameBiz));
        return File.Exists(path) ? path : null;
    }



    /// <summary>
    /// 获取游戏官方背景图 URL
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<List<string>> GetBackgroundImageUrlAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        var background = await _hoYoPlayService.GetGameBackgroundAsync(gameId, cancellationToken);
        return background.Backgrounds?.Select(x => x.Background.Url).ToList() ?? [];
    }


    /// <summary>
    /// 背景图和版本海报链接
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<string>> GetBackgroundAndPosterImageUrlsAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        List<string> urls = await GetBackgroundImageUrlAsync(gameId, cancellationToken);
        string? posterUrl = await GetVersionPosterUrlAsync(gameId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(posterUrl))
        {
            urls.Add(posterUrl);
        }
        return urls;
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
            if (!AppConfig.GetUseVersionPoster(gameId.GameBiz))
            {
                return null;
            }
            var info = await _hoYoPlayService.GetGameInfoAsync(gameId, cancellationToken);
            string? url = info.Display.Background?.Url;
            if (!string.IsNullOrWhiteSpace(url) && AppConfig.UserDataFolder is not null)
            {

                string bg = Path.Combine(AppConfig.UserDataFolder, "bg");
                Directory.CreateDirectory(bg);
                string name = Path.GetFileName(url);
                string path = Path.Combine(bg, name);
                if (!File.Exists(path))
                {
                    byte[] bytes = await _httpClient.GetByteArrayAsync(url, cancellationToken);
                    await File.WriteAllBytesAsync(path, bytes, cancellationToken);
                }
                AppConfig.SetVersionPoster(gameId.GameBiz, name);
                return path;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get version poster ({GameBiz})", gameId.GameBiz);
        }
        return null;
    }



    private async Task<string?> GetVersionPosterUrlAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        var info = await _hoYoPlayService.GetGameInfoAsync(gameId, cancellationToken);
        return info?.Display?.Background?.Url;
    }




    /// <summary>
    /// 获取背景图文件路径，最新的
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async IAsyncEnumerable<string?> GetBackgroundFileAsync(GameId gameId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (TryGetCustomBgFilePath(gameId, out string? file))
        {
            yield return file;
        }
        else
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield return GetFallbackBackgroundImage(gameId);
                yield break;
            }
            // 1s内api请求未完成或3s内文件下载未完成，先返回已缓存的图片
            (file, bool error) = await GetBackgroundFileInternalAsync(gameId, cancellationToken: cancellationToken);
            yield return file;
            if (error)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield return GetFallbackBackgroundImage(gameId);
                    yield break;
                }
                // 无timeout，直到图片下载完成
                (file, error) = await GetBackgroundFileInternalAsync(gameId, noTimeout: true, cancellationToken);
                yield return file;
            }
        }
    }



    /// <summary>
    /// 获取背景图文件路径，最新的
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<(string? File, bool Error)> GetBackgroundFileInternalAsync(GameId gameId, bool noTimeout = false, CancellationToken cancellationToken = default)
    {
        try
        {
            string? url;
            bool usePoster = false;
            var apiCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken apiCancelToken = apiCts.Token;
            CancellationToken downloadCancelToken = downloadCts.Token;
            if (!noTimeout)
            {
                apiCts.CancelAfter(1000);
                downloadCts.CancelAfter(3000);
            }
            List<string> urls = await GetBackgroundAndPosterImageUrlsAsync(gameId, apiCancelToken);
            string? lastBg = AppConfig.GetBg(gameId.GameBiz);
            if (!string.IsNullOrWhiteSpace(lastBg) && urls.FirstOrDefault(x => Path.GetFileName(x) == lastBg) is string lastUrl)
            {
                url = lastUrl;
            }
            else
            {
                url = urls.FirstOrDefault();
            }
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning("Background of mihoyo api is null ({gameBiz})", gameId);
                return (GetFallbackBackgroundImage(gameId), false);
            }
            string name = Path.GetFileName(url);
            string file = GetBgFilePath(name);
            if (!File.Exists(file))
            {
                var bytes = await _httpClient.GetByteArrayAsync(url, downloadCancelToken);
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                await File.WriteAllBytesAsync(file, bytes, downloadCancelToken);
            }
            if (usePoster)
            {
                AppConfig.SetVersionPoster(gameId.GameBiz, name);
            }
            else
            {
                AppConfig.SetBg(gameId.GameBiz, name);
            }
            return (file, false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Get background image timeout ({GameBiz})", gameId.GameBiz);
            return (GetFallbackBackgroundImage(gameId), true);
        }
        catch (Exception ex) when (ex is HttpRequestException or SocketException)
        {
            _logger.LogError(ex, "Get background image ({GameBiz})", gameId.GameBiz);
            return (GetFallbackBackgroundImage(gameId), true);
        }
    }



    /// <summary>
    /// 获取默认的背景图文件路径
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    private static string? GetFallbackBackgroundImage(GameId gameId)
    {
        string? bg = GetBgFilePath(AppConfig.GetBg(gameId.GameBiz));
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
        string bg = Path.Join(AppConfig.UserDataFolder, "bg");
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
    public static async Task<string?> ChangeCustomBackgroundFileAsync(StorageFile file)
    {
        string bg = Path.Join(AppConfig.UserDataFolder, "bg");
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
