using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls;

[INotifyPropertyChanged]
public sealed partial class BBSWebBridge : UserControl
{


    private readonly ILogger<BBSWebBridge> _logger = AppConfig.GetLogger<BBSWebBridge>();


    private readonly HttpClient _httpClient = AppConfig.GetService<HttpClient>();



    private const string miHoYoJSInterface = """
        if (window.MiHoYoJSInterface === undefined) {
            window.MiHoYoJSInterface = {
                postMessage: function(arg) { chrome.webview.postMessage(arg) },
                closePage: function() { this.postMessage('{"method":"closePage"}') },
            };
        }
        """;


    private const string HideScrollBarScript = """
        let st = document.createElement('style');
        st.innerHTML = '::-webkit-scrollbar{display:none}';
        document.querySelector('body').appendChild(st);
        """;




    public BBSWebBridge()
    {
        this.InitializeComponent();
    }



    private bool initialized = false;



    public GameBiz CurrentGameBiz { get; set; }



    private GameRecordClient _gameRecordClient;



    private Dictionary<string, string> cookieDic = new();


    [ObservableProperty]
    private GameRecordRole _GameRecordRole;
    partial void OnGameRecordRoleChanged(GameRecordRole value)
    {
        try
        {
            if (initialized)
            {
                _ = LoadPageAsync(true);
            }
        }
        catch { }
    }



    public event EventHandler<object> WebPageClosed;



    private async Task InitializeWebViewAsync()
    {
        try
        {
            if (initialized)
            {
                return;
            }
            if (CurrentGameBiz.IsGlobalServer())
            {
                _gameRecordClient = AppConfig.GetService<HoyolabClient>();
            }
            else
            {
                _gameRecordClient = AppConfig.GetService<HyperionClient>();
            }
            await webview2.EnsureCoreWebView2Async();
            var coreWebView2 = webview2.CoreWebView2;
            coreWebView2.Settings.UserAgent = _gameRecordClient.UAContent;

            coreWebView2.NavigationStarting -= Corewebview2_NavigationStarting;
            coreWebView2.NavigationStarting += Corewebview2_NavigationStarting;
            coreWebView2.DOMContentLoaded -= Corewebview2_DOMContentLoaded;
            coreWebView2.DOMContentLoaded += Corewebview2_DOMContentLoaded;
            coreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
            coreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            initialized = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }




    public async Task LoadPageAsync(bool force = false)
    {
        try
        {
            await InitializeWebViewAsync();

            var coreWebView2 = webview2.CoreWebView2;
            if (coreWebView2.Source is "about:blank" || force)
            {
                var manager = coreWebView2.CookieManager;
                var cookies = await manager.GetCookiesAsync(CurrentGameBiz.IsGlobalServer() ? "https://act.hoyolab.com" : "https://webstatic.mihoyo.com");
                foreach (var cookie in cookies)
                {
                    manager.DeleteCookie(cookie);
                }

                await Task.Delay(60);
                ParseCookie();
                foreach (var cookie in cookieDic)
                {
                    manager.AddOrUpdateCookie(manager.CreateCookie(cookie.Key, cookie.Value, CurrentGameBiz.IsGlobalServer() ? ".hoyolab.com" : ".mihoyo.com", "/"));
                }

                string? url = (CurrentGameBiz.IsGlobalServer(), CurrentGameBiz.ToGame()) switch
                {
                    (true, GameBiz.GenshinImpact) => "https://act.hoyolab.com/app/community-game-records-sea/m.html?gid=2",
                    (true, GameBiz.StarRail) => "https://act.hoyolab.com/app/community-game-records-sea/m.html?gid=6",
                    (true, GameBiz.ZZZ) => "https://act.hoyolab.com/app/mihoyo-zzz-game-record/m.html",
                    (false, GameBiz.GenshinImpact) => "https://webstatic.mihoyo.com/app/community-game-records/?game_id=2",
                    (false, GameBiz.StarRail) => "https://webstatic.mihoyo.com/app/community-game-records/?game_id=6",
                    (false, GameBiz.ZZZ) => "https://act.mihoyo.com/app/mihoyo-zzz-game-record/m.html",
                    _ => null,
                };
                if (url is not null)
                {
                    coreWebView2.Navigate(url);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }




    private void ParseCookie()
    {
        cookieDic.Clear();
        var cookies = GameRecordRole?.Cookie?.Split(';');
        if (cookies is null)
        {
            return;
        }
        foreach (var item in cookies)
        {
            var kv = item.Split('=');
            if (kv.Length == 2)
            {
                var key = kv[0].Trim();
                var value = kv[1].Trim();
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    cookieDic[key] = value;
                }
            }
        }
    }







    #region Core WebView



    private async void Corewebview2_NavigationStarting(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
    {
        try
        {
            await webview2.ExecuteScriptAsync(miHoYoJSInterface);
        }
        catch { }
    }


    private async void Corewebview2_DOMContentLoaded(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2DOMContentLoadedEventArgs args)
    {
        try
        {
            await webview2.ExecuteScriptAsync(HideScrollBarScript);
        }
        catch { }
    }


    private async void CoreWebView2_WebMessageReceived(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            string message = args.TryGetWebMessageAsString();
            Debug.WriteLine(message);
            JsParam param = JsonSerializer.Deserialize<JsParam>(message)!;
            JsResult? result = await HandleJsMessageAsync(param);
            await CallbackAsync(param.Callback, result);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }


    #endregion




    #region Js Message Method




    private async Task CallbackAsync(string? callback, JsResult? result)
    {
        if (callback == null)
        {
            return;
        }
        var js = $"""
            javascript:mhyWebBridge("{callback}"{(result == null ? "" : "," + result.ToString())})
            """;

        await webview2.ExecuteScriptAsync(js);
    }



    private async Task<JsResult?> HandleJsMessageAsync(JsParam param)
    {
        return param.Method switch
        {
            "closePage" => ClosePage(param),
            "configure_share" => null,
            "eventTrack" => null,
            //"getActionTicket" => await GetActionTicketAsync(param),
            "getCookieInfo" => GetCookieInfo(param),
            "getCookieToken" => GetCookieToken(param),
            "getDS" => GetDynamicSecrectV1(param),
            "getDS2" => GetDynamicSecrectV2(param),
            "getHTTPRequestHeaders" => GetHttpRequestHeader(param),
            "getStatusBarHeight" => GetStatusBarHeight(param),
            "getUserInfo" => GetUserInfo(param),
            "hideLoading" => null,
            "login" => null,
            "pushPage" => PushPage(param),
            "showLoading" => null,
            "share" => await HandleShareAsync(param),
            "getCurrentLocale" => GetCurrentLocale(param),
            _ => null,
        };
    }



    private async Task<JsResult?> HandleShareAsync(JsParam param)
    {
        if (param.Payload?["type"]?.ToString() is "screenshot")
        {
            await CaptureScreenshotAsync();
        }
        else if (param.Payload?["imageUrls"]?[0]?.ToString() is string { Length: > 0 } url)
        {
            await DownloadScreenshotAsync(url);
        }
        return null;
    }



    private byte[]? screenshotBytes;


    private async Task CaptureScreenshotAsync()
    {
        try
        {
            string data = await webview2.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", """{"captureBeyondViewport": true}""");
            string? base64 = JsonNode.Parse(data)?["data"]?.ToString();
            if (base64 is not null)
            {
                screenshotBytes = Convert.FromBase64String(base64);
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(new MemoryStream(screenshotBytes).AsRandomAccessStream());
                Image_Screenshot.Source = bitmap;
                Grid_Screenshot.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "capture screenshot");
            Grid_Screenshot.Visibility = Visibility.Collapsed;
        }
    }



    private async Task DownloadScreenshotAsync(string url)
    {
        try
        {
            var source = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            screenshotBytes = await _httpClient.GetByteArrayAsync(url, source.Token);
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(new MemoryStream(screenshotBytes).AsRandomAccessStream());
            Image_Screenshot.Source = bitmap;
            Grid_Screenshot.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "download screenshot");
            Grid_Screenshot.Visibility = Visibility.Collapsed;
        }
    }




    [RelayCommand]
    private async Task SaveScreenshotAsync()
    {
        try
        {
            if (screenshotBytes is not null)
            {
                string name = $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.png";
                string? file = await FileDialogHelper.OpenSaveFileDialogAsync(MainWindow.Current.WindowHandle, name, ("Png File", ".png"));
                if (!string.IsNullOrWhiteSpace(file))
                {
                    await File.WriteAllBytesAsync(file, screenshotBytes);
                    CloseScreenshotGrid();
                    var storage = await StorageFile.GetFileFromPathAsync(file);
                    var options = new FolderLauncherOptions();
                    options.ItemsToSelect.Add(storage);
                    await Launcher.LaunchFolderAsync(await storage.GetParentAsync(), options);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save screenshot");
        }
    }



    [RelayCommand]
    private async Task CopyScreenshotAsync()
    {
        try
        {
            if (screenshotBytes is not null)
            {
                string file = Path.GetTempFileName();
                await File.WriteAllBytesAsync(file, screenshotBytes);
                var storage = await StorageFile.GetFileFromPathAsync(file);
                ClipboardHelper.SetStorageItems(DataPackageOperation.Copy, storage);
                NotificationBehavior.Instance.Information(Lang.BBSWebBridge_ImageCopiedToClipboard);
                CloseScreenshotGrid();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy screenshot");
        }
    }


    [RelayCommand]
    private void CloseScreenshotGrid()
    {
        try
        {
            screenshotBytes = null;
            Grid_Screenshot.Visibility = Visibility.Collapsed;
        }
        catch { }
    }



    private JsResult? GetCurrentLocale(JsParam param)
    {
        int offset = TimeZoneInfo.Local.BaseUtcOffset.Hours;
        return new()
        {
            Data = new()
            {
                ["language"] = LanguageUtil.FilterLanguage(CultureInfo.CurrentUICulture.Name),
                ["timeZone"] = offset switch
                {
                    > 0 => $"GMT+{offset}",
                    < 0 => $"GMT{offset}",
                    _ => "GMT",
                },
            }
        };
    }



    private JsResult? GetCookieToken(JsParam param)
    {
        return new()
        {
            Data = new()
            {
                ["cookie_token"] = cookieDic.GetValueOrDefault("cookie_token") ?? cookieDic.GetValueOrDefault("cookie_token_v2") ?? "",
            },
        };
    }


    private JsResult? ClosePage(JsParam param)
    {
        if (webview2.CoreWebView2.CanGoBack)
        {
            webview2.CoreWebView2.GoBack();
        }
        else
        {
            WebPageClosed?.Invoke(this, EventArgs.Empty);
        }
        return null;
    }



    private JsResult? PushPage(JsParam param)
    {
        webview2.CoreWebView2.Navigate(param.Payload?["page"]?.ToString());
        return null;
    }


    private JsResult? GetUserInfo(JsParam param)
    {
        return new()
        {
            Data = new()
            {
                ["id"] = GameRecordRole.Uid,
                ["gender"] = "",
                ["nickname"] = GameRecordRole.Nickname!,
                ["introduce"] = "",
                ["avatar_url"] = "",
            },
        };
    }


    private JsResult? GetStatusBarHeight(JsParam param)
    {
        return new()
        {
            Data = new()
            {
                ["statusBarHeight"] = 0
            }
        };
    }


    private JsResult? GetCookieInfo(JsParam param)
    {
        return new()
        {
            Data = cookieDic.ToDictionary(x => x.Key, x => (object)x.Value),
        };
    }


    private JsResult? GetHttpRequestHeader(JsParam param)
    {
        return new()
        {
            Data = new()
            {
                ["x-rpc-client_type"] = "5",
                ["x-rpc-app_version"] = _gameRecordClient.AppVersion,
                ["x-rpc-device_fp"] = _gameRecordClient.DeviceFp,
                ["x-rpc-device_id"] = _gameRecordClient.DeviceId,
            },
        };
    }


    #endregion




    #region Dynamic Secret




    private JsResult? GetDynamicSecrectV1(JsParam param)
    {
        string ApiSalt;
        if (CurrentGameBiz.IsGlobalServer())
        {
            ApiSalt = "okr4obncj8bw5a65hbnn5oo6ixjc3l9w";
        }
        else
        {
            ApiSalt = "t0qEgfub6cvueAPgR5m9aQWWVciEer7v";
        }

        var t = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string r = GetRandomString(t);
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"salt={ApiSalt}&t={t}&r={r}"));
        var check = Convert.ToHexString(bytes).ToLower();
        return new JsResult
        {
            Data = new()
            {
                ["DS"] = $"{t},{r},{check}",
            }
        };
    }


    private static string GetRandomString(int timestamp)
    {
        var sb = new StringBuilder(6);
        var random = new Random((int)timestamp);
        for (int i = 0; i < 6; i++)
        {
            int v8 = random.Next(0, 32768) % 26;
            int v9 = 87;
            if (v8 < 10)
            {
                v9 = 48;
            }
            _ = sb.Append((char)(v8 + v9));
        }
        return sb.ToString();
    }


    private JsResult? GetDynamicSecrectV2(JsParam param)
    {
        string ApiSalt2;
        if (CurrentGameBiz.IsGlobalServer())
        {
            ApiSalt2 = "h4c1d6ywfq5bsbnbhm1bzq7bxzzv6srt";
        }
        else
        {
            ApiSalt2 = "xV8v4Qu54lUKrEYFZkJhB8cuOh9Asafs";
        }

        int t = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string r = Random.Shared.Next(100000, 200000).ToString();
        var d = JsonSerializer.Deserialize<Dictionary<string, object>>(param.Payload?["query"]);
        string? b = param.Payload?["body"]?.ToString();
        string? q = null;
        if (d?.Any() ?? false)
        {
            q = string.Join('&', d.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value}"));
        }
        q = q?.Replace("True", "true").Replace("False", "false");
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"salt={ApiSalt2}&t={t}&r={r}&b={b}&q={q}"));
        var check = Convert.ToHexString(bytes).ToLower();
        string result = $"{t},{r},{check}";

        return new()
        {
            Data = new()
            {
                ["DS"] = result,
            }
        };
    }


    #endregion




    #region WebView Message Object



    private class JsParam
    {
        /// <summary>
        /// 方法名称
        /// </summary>
        [JsonPropertyName("method")]
        public string Method { get; set; }

        /// <summary>
        /// 数据 可以为空
        /// </summary>
        [JsonPropertyName("payload")]
        public JsonNode? Payload { get; set; }

        /// <summary>
        /// 回调的名称，调用 JavaScript:mhyWebBridge 时作为首个参数传入
        /// </summary>
        [JsonPropertyName("callback")]
        public string? Callback { get; set; }
    }



    private class JsResult
    {
        /// <summary>
        /// 代码
        /// </summary>
        [JsonPropertyName("retcode")]
        public int Code { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 数据
        /// </summary>
        [JsonPropertyName("data")]
        public Dictionary<string, object> Data { get; set; } = default!;


        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

    }


    #endregion



}
