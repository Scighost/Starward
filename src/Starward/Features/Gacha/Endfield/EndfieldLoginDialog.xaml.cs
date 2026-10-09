using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Text.Json.Nodes;

namespace Starward.Features.Gacha.Endfield;

public sealed partial class EndfieldLoginDialog : ContentDialog
{
    private const string LoginUrl = "https://user.hypergryph.com/";
    private const string BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.6422.112 Safari/537.36";

    private const string LoginCaptureScript = """
        (() => {
            if (window.__starwardEndfieldLoginInstalled) return;
            window.__starwardEndfieldLoginInstalled = true;
            let hasSent = false;

            function sendToken(token) {
                if (hasSent || typeof token !== 'string' || token.length === 0) return;
                hasSent = true;
                window.chrome.webview.postMessage({ action: 'loginToken', token });
            }

            const originalOpen = XMLHttpRequest.prototype.open;
            const originalSend = XMLHttpRequest.prototype.send;
            XMLHttpRequest.prototype.open = function(method, url) {
                this.__starwardUrl = String(url || '');
                return originalOpen.apply(this, arguments);
            };
            XMLHttpRequest.prototype.send = function(body) {
                this.addEventListener('load', function() {
                    try {
                        if (this.__starwardUrl.includes('as.hypergryph.com/user/auth')) {
                            const result = JSON.parse(this.responseText);
                            if (result.status === 0 && result.data?.token) sendToken(result.data.token);
                        }
                    } catch (_) {}
                });
                return originalSend.apply(this, arguments);
            };

            const originalFetch = window.fetch.bind(window);
            window.fetch = async function(...args) {
                const response = await originalFetch(...args);
                try {
                    if (response.url?.includes('as.hypergryph.com/user/auth')) {
                        response.clone().json().then(result => {
                            if (result.status === 0 && result.data?.token) sendToken(result.data.token);
                        }).catch(() => {});
                    }
                } catch (_) {}
                return response;
            };

            const timer = window.setInterval(() => {
                if (hasSent) {
                    window.clearInterval(timer);
                    return;
                }
                originalFetch('https://web-api.hypergryph.com/account/info/hg', {
                    method: 'GET',
                    credentials: 'include'
                }).then(response => response.json()).then(result => {
                    if (result.code === 0 && result.data?.content) sendToken(result.data.content);
                }).catch(() => {});
            }, 1500);
        })();
        """;

    private readonly ILogger<EndfieldLoginDialog> _logger = AppConfig.GetLogger<EndfieldLoginDialog>();
    private bool _initialized;

    public EndfieldLoginDialog()
    {
        InitializeComponent();
    }

    public string? LoginToken { get; private set; }

    private async void ContentDialog_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;
        try
        {
            await WebView_Login.EnsureCoreWebView2Async();
            CoreWebView2 core = WebView_Login.CoreWebView2;
            core.Settings.UserAgent = BrowserUserAgent;
            core.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
            core.NavigationStarting += Core_NavigationStarting;
            core.NavigationCompleted += Core_NavigationCompleted;
            core.NewWindowRequested += Core_NewWindowRequested;
            core.WebMessageReceived += Core_WebMessageReceived;
            core.ProcessFailed += Core_ProcessFailed;
            await core.AddScriptToExecuteOnDocumentCreatedAsync(LoginCaptureScript);
            core.Navigate(LoginUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize Endfield login WebView2 failed.");
            ShowError("登录页面初始化失败，请确认 WebView2 Runtime 可用后重试。");
        }
    }

    private void Core_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        if (!Uri.TryCreate(args.Uri, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttps ||
             !(uri.Host.Equals("hypergryph.com", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.EndsWith(".hypergryph.com", StringComparison.OrdinalIgnoreCase))))
        {
            args.Cancel = true;
        }
    }

    private void Core_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        ProgressRing_Loading.IsActive = false;
        if (args.IsSuccess)
        {
            WebView_Login.Visibility = Visibility.Visible;
        }
        else
        {
            ShowError($"登录页面加载失败（{args.WebErrorStatus}）。");
        }
    }

    private void Core_NewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        args.Handled = true;
        sender.Navigate(args.Uri);
    }

    private void Core_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            if (!Uri.TryCreate(args.Source, UriKind.Absolute, out Uri? source) ||
                !source.Host.Equals("user.hypergryph.com", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            JsonNode? message = JsonNode.Parse(args.WebMessageAsJson);
            if (message?["action"]?.ToString() != "loginToken")
            {
                return;
            }
            string? token = message["token"]?.ToString();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }
            LoginToken = token;
            InfoBar_Status.Severity = InfoBarSeverity.Success;
            InfoBar_Status.Message = "登录成功，正在读取终末地角色。";
            DispatcherQueue.TryEnqueue(Hide);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Read Endfield login result failed: {ErrorType}", ex.GetType().Name);
            ShowError("无法读取登录结果，请重试。");
        }
    }

    private void Core_ProcessFailed(CoreWebView2 sender, CoreWebView2ProcessFailedEventArgs args)
    {
        ShowError("登录页面进程异常退出，请关闭弹窗后重试。");
    }

    private void ShowError(string message)
    {
        ProgressRing_Loading.IsActive = false;
        InfoBar_Status.Severity = InfoBarSeverity.Error;
        InfoBar_Status.Message = message;
    }

    private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        if (WebView_Login.CoreWebView2 is not CoreWebView2 core)
        {
            return;
        }
        core.NavigationStarting -= Core_NavigationStarting;
        core.NavigationCompleted -= Core_NavigationCompleted;
        core.NewWindowRequested -= Core_NewWindowRequested;
        core.WebMessageReceived -= Core_WebMessageReceived;
        core.ProcessFailed -= Core_ProcessFailed;
        WebView_Login.Close();
    }
}
