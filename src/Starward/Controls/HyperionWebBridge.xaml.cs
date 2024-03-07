using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core.GameRecord;
using Starward.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls;

public sealed partial class HyperionWebBridge : UserControl
{

    private const string AppVersion = "2.68.1";

    private static string DeviceId = Guid.NewGuid().ToString("D");


    private const string miHoYoJSInterface = """
        window.MiHoYoJSInterface = {
            postMessage: function(arg) { chrome.webview.postMessage(arg) },
            closePage: function() { this.postMessage('{"method":"closePage"}') },
        };
        """;


    private const string HideScrollBarScript = """
        let st = document.createElement('style');
        st.innerHTML = '::-webkit-scrollbar{display:none}';
        document.querySelector('body').appendChild(st);
        """;



    private Dictionary<string, string> cookieDic = new();


    public GameRecordRole GameRecordRole { get; set; }

    public string TargetUrl { get; set; }


    public HyperionWebBridge()
    {
        this.InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeAsync();
    }



    private async Task InitializeAsync()
    {
        try
        {
            await webview2.EnsureCoreWebView2Async();
            var coreWebView2 = webview2.CoreWebView2;
            if (!coreWebView2.Settings.UserAgent.Contains("miHoYoBBS"))
            {
                coreWebView2.Settings.UserAgent = $"Mozilla/5.0 (Linux; Android 13) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/113.0.5672.131 Mobile Safari/537.36 miHoYoBBS/{AppVersion}";
            }
            var manager = coreWebView2.CookieManager;
            var cookies = await manager.GetCookiesAsync("https://webstatic.mihoyo.com");
            foreach (var cookie in cookies)
            {
                manager.DeleteCookie(cookie);
            }

            await Task.Delay(60);
            ParseCookie();
            foreach (var cookie in cookieDic)
            {
                manager.AddOrUpdateCookie(manager.CreateCookie(cookie.Key, cookie.Value, ".mihoyo.com", "/"));
            }

            coreWebView2.NavigationStarting += Corewebview2_NavigationStarting;
            coreWebView2.DOMContentLoaded += Corewebview2_DOMContentLoaded;
            coreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            var url = TargetUrl?.Replace("{role_id}", GameRecordRole?.Uid.ToString() ?? "")?.Replace("{server}", GameRecordRole?.Region?.ToString() ?? "");
            coreWebView2.Navigate(url);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }


    private void ParseCookie()
    {
        cookieDic.Clear();
        var cookies = GameRecordRole.Cookie?.Split(';');
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


    private async void Corewebview2_NavigationStarting(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
    {
        try
        {
            await webview2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(miHoYoJSInterface);
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

            JsResult? result = param.Method switch
            {
                "closePage" => ClosePage(param),
                "configure_share" => null,
                "eventTrack" => null,
                //"getActionTicket" => await GetActionTicketAsync(param).ConfigureAwait(false),
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
                _ => null,
            };

            await CallbackAsync(param.Callback, result);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
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


    private JsResult? GetDynamicSecrectV2(JsParam param)
    {
        const string ApiSalt2 = "xV8v4Qu54lUKrEYFZkJhB8cuOh9Asafs";

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


    private JsResult? ClosePage(JsParam param)
    {
        if (webview2.CoreWebView2.CanGoBack)
        {
            webview2.CoreWebView2.GoBack();
        }
        else
        {
            this.DispatcherQueue.TryEnqueue(() => WeakReferenceMessenger.Default.Send(new GameRecordPageNavigationGoBackMessage()));

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

    private JsResult? GetDynamicSecrectV1(JsParam param)
    {
        const string ApiSalt = "t0qEgfub6cvueAPgR5m9aQWWVciEer7v";

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
                ["x-rpc-app_version"] = AppVersion,
                ["x-rpc_device_fp"] = cookieDic.GetValueOrDefault("DEVICEFP") ?? "",
                ["x-rpc-device_id"] = cookieDic.GetValueOrDefault("_MHYUUID") ?? "",
            },
        };
    }


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


}
