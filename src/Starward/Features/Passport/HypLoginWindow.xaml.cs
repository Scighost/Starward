using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Starward.Core;
using Starward.Features.UrlProtocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Vanara.PInvoke;
using Windows.System;


namespace Starward.Features.Passport;

public sealed partial class HypLoginWindow : WindowBase
{

    private readonly ILogger<HypLoginWindow> _logger = AppConfig.GetLogger<HypLoginWindow>();


    public nint ParentWindowHandle { get; set; }

    public ServerRegion ServerRegion { get; set; }


    private string? WebHost => ServerRegion switch
    {
        ServerRegion.miHoYo => "https://hyp-api.mihoyo.com",
        ServerRegion.HoYoverse => "https://sg-hyp-api.hoyoverse.com",
        _ => null,
    };



    public HypLoginWindow()
    {
        InitializeComponent();
        InitializeWindow();
        this.Closed += HypLoginWindow_Closed;
        WeakReferenceMessenger.Default.Register<HypThirdPartyLoginMessage>(this, HandleThirdPartyLoginMessage);
    }



    private void InitializeWindow()
    {
        try
        {
            Title = "Starward HYP Login";
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            AdaptTitleBarButtonColorToActuallTheme();
            UrlProtocolService.RegisterHypLoginProtocol();
        }
        catch { }
    }


    public new void Activate()
    {
        User32.SetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_HWNDPARENT, ParentWindowHandle);
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsModal = true;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.PreferredMaximumWidth = 0;
            presenter.PreferredMinimumWidth = 0;
        }
        CenterInScreen(520, 740);
        base.Activate();
    }



    [RelayCommand]
    private void CloseWindow()
    {
        _loginTaskSource?.TrySetResult(null);
        this.Close();
    }



    private TaskCompletionSource<PassportUserInfoModel?> _loginTaskSource;


    public async Task<PassportUserInfoModel?> LoginAsync()
    {
        try
        {
            if (ServerRegion is not ServerRegion.HoYoverse and not ServerRegion.miHoYo)
            {
                throw new NotSupportedException($"Server region '{ServerRegion}' is not supported.");
            }
            _loginTaskSource = new();
            Activate();
            return await _loginTaskSource.Task;
        }
        finally
        {
            Close();
        }
    }


    private void HypLoginWindow_Closed(object sender, WindowEventArgs args)
    {
        try
        {
            UrlProtocolService.UnregisterHypLoginProtocol();
            _loginTaskSource?.TrySetResult(null);
        }
        catch { }
    }


    private async void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeWebViewAsync();
    }


    private async Task InitializeWebViewAsync()
    {
        try
        {
            await webview.EnsureCoreWebView2Async();
            webview.CoreWebView2.Settings.IsWebMessageEnabled = true;
            webview.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            webview.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            webview.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
            webview.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;

            await webview.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(JsScript);
            webview.CoreWebView2.AddWebResourceRequestedFilter($"{WebHost}/*", CoreWebView2WebResourceContext.All);

            string url = $"{WebHost}/index.html{BuildUrlQuery()}";
            webview.CoreWebView2.Navigate(url);
        }
        catch (Exception ex)
        {
            _loginTaskSource?.TrySetException(ex);
        }
    }


    private string BuildUrlQuery()
    {
        WindowsDeviceFpService deviceFpService = AppConfig.GetService<WindowsDeviceFpService>();
        var query = new List<KeyValuePair<string, string>>();

        if (ServerRegion is ServerRegion.miHoYo)
        {
            query.Add(new("channel", "1"));
            query.Add(new("subChannel", "1"));
            query.Add(new("clientEnv", "production"));
            query.Add(new("cps", "hyp_mihoyo"));
            query.Add(new("launcherId", "jGHBHlcOq1"));
            query.Add(new("primaryGame", "hyp_cn"));
            query.Add(new("version", "1.20.1.392"));
            query.Add(new("language", "zh-cn"));
        }
        else
        {
            query.Add(new("channel", "1"));
            query.Add(new("subChannel", "0"));
            query.Add(new("clientEnv", "production"));
            query.Add(new("cps", "hyp_hoyoverse"));
            query.Add(new("launcherId", "VYTpXlbWo8"));
            query.Add(new("primaryGame", "hyp_global"));
            query.Add(new("version", "1.20.1.392"));
            query.Add(new("language", LanguageUtil.FilterLanguage(CultureInfo.CurrentUICulture.Name)));
        }

        query.Add(new("schema", "starward-hyp-login"));
        query.Add(new("deviceId", deviceFpService.GetDeviceId()));
        query.Add(new("deviceFp", deviceFpService.GetDeviceFp()));
        query.Add(new("deviceName", Environment.MachineName));
        query.Add(new("deviceModel", "x86_64"));
        query.Add(new("osSystem", Environment.OSVersion.Version.Build >= 22000 ? "Windows 11 Version 2009" : "Windows 10 Version 1909"));
        query.Add(new("useLogin", "true"));
        query.Add(new("forceLogin", "true"));
        query.Add(new("useMultiAccount", "false"));
        query.Add(new("enableBetaLogin", "false"));
        query.Add(new("enablePay", "false"));
        query.Add(new("standalone", "false"));
        query.Add(new("clientPreview", "false"));
        query.Add(new("uploadLog", "false"));
        query.Add(new("showUpdateToast", "false"));
        query.Add(new("isGoogleNative", "false"));
        query.Add(new("hasGoogleParams", "false"));
        query.Add(new("cpuInfo", ""));
        query.Add(new("uapc", ""));

        if (ServerRegion is ServerRegion.miHoYo)
        {
            query.Add(new("loginThirdparty", "tp"));
        }
        else
        {
            query.Add(new("loginThirdparty", "gl"));
            query.Add(new("loginThirdparty", "fb"));
            query.Add(new("loginThirdparty", "ap"));
            query.Add(new("loginThirdparty", "tw"));
        }

        var sb = new StringBuilder("?");
        foreach (var item in query)
        {
            sb.Append(item.Key);
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(item.Value));
            sb.Append('&');
        }
        return sb.ToString();
    }


    private async void CoreWebView2_WebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        if (!args.Request.Uri.Contains(WebHost ?? "") || args.Request.Uri.Contains("hyp-connect"))
        {
            return;
        }
        var deferral = args.GetDeferral();
        try
        {
            string path = new Uri(args.Request.Uri).LocalPath;
            if (path.EndsWith('/'))
            {
                path += "index.html";
            }

            string local = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Assets", "Web", ServerRegion is ServerRegion.miHoYo ? "hyp_cn" : "hyp_global", path.TrimStart('/')));
            if (File.Exists(local))
            {
                string mime = Path.GetExtension(local) switch
                {
                    ".html" => "text/html; charset=utf-8",
                    ".css" => "text/css; charset=utf-8",
                    ".js" => "application/javascript; charset=utf-8",
                    _ => "application/octet-stream",
                };
                FileStream stream = File.OpenRead(local);
                args.Response = sender.Environment.CreateWebResourceResponse(stream.AsRandomAccessStream(), 200, "OK", $"Content-Type: {mime}");
            }
        }
        catch
        {
            args.Response = sender.Environment.CreateWebResourceResponse(null, 500, "Internal Error", "Content-Type: text/plain; charset=utf-8");
        }
        finally
        {
            deferral.Complete();
        }
    }


    private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        string json = args.TryGetWebMessageAsString();
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }
        try
        {
            JsonNode? node = JsonNode.Parse(json);
            if (node is null)
            {
                return;
            }
            switch (node["kind"]?.ToString())
            {
                case "query":
                    HandleHostQuery(sender, node);
                    break;
                case "invoke":
                    HandleHostInvoke(node);
                    break;
                case "emitted":
                case "listen":
                case "unlisten":
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hyp login web message received.");
        }
    }


    private void HandleHostQuery(CoreWebView2 sender, JsonNode node)
    {
        string id = node["id"]?.ToString() ?? "";
        string action = node["action"]?.ToString() ?? "";
        //string request = node["request"]?.ToString() ?? "";

        string reply;
        if (action is "getSettings")
        {
            string lang = LanguageUtil.FilterLanguage(CultureInfo.CurrentUICulture.NativeName);
            reply = $$"""
                {
                    "kind": "resolve",
                    "id": "{{id}}",
                    "json": "{"language":"{{lang}}"}"
                }
                """;
        }
        else
        {
            reply = $$"""
                {
                    "kind": "resolve",
                    "id": "{{id}}",
                    "json": "{}"
                }
                """;
        }
        sender.PostWebMessageAsJson(reply);
    }


    private async void HandleHostInvoke(JsonNode node)
    {
        string name = node["name"]?.ToString() ?? "";
        switch (name)
        {
            case "openInBrowser":
                await OpenInBrowserAsync(node);
                break;
            case "exitApp":
                _loginTaskSource?.SetResult(null);
                break;
            case "notifyFEAppInfo":
            case "syncGameInfo":
            case "updateRoute":
            case "trackBiz":
            case "openDirectory":
            case "hideWindow":
            case "minimizeWindow":
            case "restoreWindow":
            case "dragWindow":
            default:
                break;
        }
    }


    private static async Task OpenInBrowserAsync(JsonNode node)
    {
        string? openUrl = null;
        JsonNode? arg = node["arg"];
        if (arg is null || arg.GetValueKind() is JsonValueKind.Null)
        {
            return;
        }
        if (arg.GetValueKind() is JsonValueKind.String)
        {
            string url = arg.ToString().Trim('"');
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }
            else
            {
                openUrl = url;
            }
        }
        if (arg.GetValueKind() is JsonValueKind.Object)
        {
            if (arg["url"] is JsonValue url && url.GetValueKind() is JsonValueKind.String)
            {
                openUrl = arg["url"]?.ToString();
            }
            else if (arg["link"] is JsonValue link && link.GetValueKind() is JsonValueKind.String)
            {
                openUrl = arg["link"]?.ToString();
            }
            else if (arg["href"] is JsonValue href && href.GetValueKind() is JsonValueKind.String)
            {
                openUrl = arg["link"]?.ToString();
            }
        }
        if (!string.IsNullOrWhiteSpace(openUrl))
        {
            await Launcher.LaunchUriAsync(new(openUrl));
        }
    }


    private void CoreWebView2_DOMContentLoaded(CoreWebView2 sender, CoreWebView2DOMContentLoadedEventArgs args)
    {
        webview.Visibility = Visibility.Visible;
    }


    private async void CoreWebView2_WebResourceResponseReceived(CoreWebView2 sender, CoreWebView2WebResourceResponseReceivedEventArgs args)
    {
        if (!args.Request.Uri.Contains("/account/") || !string.Equals(args.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        try
        {
            using var stream = await args.Response.GetContentAsync();
            using var reader = new StreamReader(stream.AsStreamForRead(), Encoding.UTF8);
            string body = await reader.ReadToEndAsync();
            PassportUserInfoModel? info = GetPassportInfoFromBody(body);
            if (info is not null)
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await AppConfig.GetService<PassportService>().UpdateBbsUserInfoAsync(info, cts.Token);
                _loginTaskSource?.TrySetResult(info);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hyp login web response received.");
        }
    }



    private PassportUserInfoModel? GetPassportInfoFromBody(string body)
    {
        try
        {
            JsonNode? node = JsonNode.Parse(body);
            if (node is null)
            {
                return null;
            }
            string? aid = null, mid = null, stoken = null, nickname = null;
            if (node["data"] is JsonObject data)
            {
                if (data["token"] is JsonObject token)
                {
                    if (token["token_type"]?.GetValueKind() is JsonValueKind.Number)
                    {
                        if (token["token"]?.GetValueKind() is JsonValueKind.String)
                        {
                            stoken = token["token"]?.ToString();
                        }
                    }

                }
                if (data["tokens"] is JsonArray tokens)
                {
                    foreach (JsonNode? item in tokens)
                    {
                        if (item?["token_type"]?.GetValueKind() is JsonValueKind.Number)
                        {
                            if (item["token"]?.GetValueKind() is JsonValueKind.String)
                            {
                                stoken = item["token"]?.ToString();
                                break;
                            }
                        }
                    }
                }
                if (data["user_info"] is JsonObject user_info)
                {
                    if (user_info["aid"]?.GetValueKind() is JsonValueKind.String)
                    {
                        aid = user_info["aid"]?.ToString();
                    }
                    if (user_info["mid"]?.GetValueKind() is JsonValueKind.String)
                    {
                        mid = user_info["mid"]?.ToString();
                    }
                    if (user_info["mobile"]?.GetValueKind() is JsonValueKind.String)
                    {
                        nickname = user_info["mobile"]?.ToString();
                    }
                    if (string.IsNullOrWhiteSpace(nickname) && user_info["email"]?.GetValueKind() is JsonValueKind.String)
                    {
                        nickname = user_info["email"]?.ToString();
                    }
                    if (string.IsNullOrWhiteSpace(nickname) && user_info["links"] is JsonArray array)
                    {
                        nickname = array.FirstOrDefault()?["nickname"]?.ToString();
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(aid) || string.IsNullOrWhiteSpace(mid) || string.IsNullOrWhiteSpace(stoken))
            {
                return null;
            }

            return new PassportUserInfoModel
            {
                Region = ServerRegion,
                Aid = long.Parse(aid),
                Mid = mid,
                Stoken = stoken,
                Nickname = string.IsNullOrWhiteSpace(nickname) ? "-" : nickname,
                AvatarUrl = ServerRegion switch
                {
                    ServerRegion.HoYoverse => "ms-appx:///Assets/Image/icon_hoyolab.png",
                    ServerRegion.miHoYo => "ms-appx:///Assets/Image/icon_hyperion.png",
                    _ => "ms-appx:///Assets/Image/hyp.png",
                },
            };
        }
        catch
        {
            return null;
        }
    }


    private void HandleThirdPartyLoginMessage(object sender, HypThirdPartyLoginMessage m)
    {
        if (m.Url.StartsWith("starward-hyp-login://thirdLogin"))
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    var dict = HttpUtility.ParseQueryString(new Uri(m.Url).Query);
                    webview.CoreWebView2.PostWebMessageAsJson($$"""
                        {
                            "kind": "event",
                            "name": "updateThirdPartyToken",
                            "payload": {
                                "token": "{{dict["token"]}}",
                                "messageId": "{{dict["message_id"]}}"
                            }
                        }
                        """);
                }
                catch { }
            });
        }
    }



    private const string JsScript = """
        (function () {
            localStorage.clear();
            if (window.__swHostInstalled) { return; }
            window.__swHostInstalled = true;

            var listeners = {};
            var pending = {};
            var seq = 0;

            function post(obj) {
                try { window.chrome.webview.postMessage(JSON.stringify(obj)); } catch (e) { }
            }

            function resolve(id, json, error, message) {
                var options = pending[id];
                if (!options) { return; }
                delete pending[id];
                if (error === undefined || error === null) {
                    try { options.onSuccess(json); } catch (e) { }
                }
                else {
                    try { options.onFailure(error, message); } catch (e) { }
                }
            }

            function emit(name, payload) {
                var callback = listeners[name];
                post({ kind: 'emitted', name: name, hasListener: !!callback, payload: payload });
                if (!callback) { return; }
                try { callback(payload); } catch (e) { }
            }

            var receiverAttached = false;
            function attachReceiver() {
                if (receiverAttached) { return true; }
                try {
                    if (!window.chrome || !window.chrome.webview) { return false; }
                    window.chrome.webview.addEventListener('message', function (e) {
                        var message = e.data;
                        if (!message || !message.kind) { return; }
                        if (message.kind === 'resolve') { resolve(message.id, message.json, message.error, message.message); }
                        else if (message.kind === 'event') { emit(message.name, message.payload); }
                    });
                    receiverAttached = true;
                    return true;
                } catch (e) { return false; }
            }

            window.HYPClient = {
                invokeMethod: function (name) {
                    post({ kind: 'invoke', name: name, arg: arguments.length > 1 ? arguments[1] : null });
                },
                addEventListener: function (name, callback) {
                    listeners[name] = callback;
                    post({ kind: 'listen', name: name });
                },
                removeEventListener: function (name) {
                    delete listeners[name];
                    post({ kind: 'unlisten', name: name });
                }
            };

            window.CefViewQuery = function (options) {
                var id = 'q' + (++seq);
                pending[id] = options;
                var action = null;
                try { action = JSON.parse(options.request).action; } catch (e) { }
                post({ kind: 'query', id: id, action: action, request: options.request });
            };

            var REGISTER_URL = 'https://account.hoyoverse.com/#/login';

            document.addEventListener('click', function (e) {
                var target = e.target;
                if (!target || !target.closest) { return; }
                var popup = document.getElementById('hyp-account-popup');
                if (!popup || !popup.contains(target)) { return; }

                if (target.closest('.el-dialog__headerbtn')) {
                    post({ kind: 'invoke', name: 'exitApp' });
                    return;
                }

                var register = target.closest('a.to-register-link');
                if (register) {
                    e.preventDefault();
                    e.stopPropagation();
                    window.HYPClient.invokeMethod('openInBrowser', { url: REGISTER_URL });
                }
            }, true);

            if (!attachReceiver()) {
                var timer = setInterval(function () {
                    if (attachReceiver()) { clearInterval(timer); }
                }, 20);
            }
        })();

        (function () {
            var CSS = '#root{opacity:0!important;visibility:hidden!important;pointer-events:none!important}'
                + 'html,body{background:transparent!important}'
                + '#hyp-account-popup .el-overlay{background:transparent!important}';

            var style = document.createElement('style');
            style.id = 'sw-login-only-style';
            style.textContent = CSS;

            function attach() {
                var host = document.head || document.documentElement;
                if (!host) {
                    new MutationObserver(function (_, observer) {
                        if (attach()) { observer.disconnect(); }
                    }).observe(document, { childList: true, subtree: true });
                    return false;
                }
                host.appendChild(style);
                return true;
            }

            attach();
        })();
        """;


}
