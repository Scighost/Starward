using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.GameLauncher;
using Starward.GameInstall;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;


namespace Starward.Features.GameInstall;

public sealed partial class CutsceneResourceWindow : WindowBase
{

    private readonly ILogger<GameLauncherSettingDialog> _logger = AppConfig.GetLogger<GameLauncherSettingDialog>();

    private readonly HoYoPlayService _hoyoPlayService = AppConfig.GetService<HoYoPlayService>();

    private readonly GameInstallService _gameInstallService = AppConfig.GetService<GameInstallService>();


    public GameId CurrentGameId { get; set; }


    public CutsceneResourceWindow()
    {
        InitializeComponent();
        InitializeWindow();
    }



    private void InitializeWindow()
    {
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        Title = Lang.GameLauncherSettingDialog_CutsceneResourceManagement;
        RootGrid.RequestedTheme = ShouldAppsUseDarkMode() ? ElementTheme.Dark : ElementTheme.Light;
        AdaptTitleBarButtonColorToActuallTheme();
        SetIcon();
        DisplayArea display = DisplayArea.GetFromWindowId(MainWindowId, DisplayAreaFallback.Nearest);
        int height = (int)(display.WorkArea.Height / UIScale * 0.9);
        CenterInScreen(624, Math.Min(height, 1000));
    }



    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();


    private CancellationToken CloseCancellationToken => _cancellationTokenSource.Token;

    private string _gameInstallPath;


    private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _gameInstallPath = GameLauncherService.GetGameInstallPath(CurrentGameId)!;
            if (string.IsNullOrWhiteSpace(_gameInstallPath))
            {
                Close();
                return;
            }
            await webview.EnsureCoreWebView2Async();
            webview.CoreWebView2.Profile.PreferredColorScheme = ShouldAppsUseDarkMode() ? CoreWebView2PreferredColorScheme.Dark : CoreWebView2PreferredColorScheme.Light;
            webview.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            webview.CoreWebView2.AddWebResourceRequestedFilter("https://starward.local/*", CoreWebView2WebResourceContext.All);
            var branch = await _hoyoPlayService.GetGameBranchAsync(CurrentGameId, CloseCancellationToken);
            string rel = CurrentGameId.GameBiz.IsChinaServer() ? "CNRELWin" : "OSRELWin";
            string lang = LanguageUtil.FilterLanguage(CultureInfo.CurrentUICulture.Name);
            string url = $"https://starward-static.scighost.com/game-assets/genshin/cutscene/index.html?version={rel}{branch?.Main.Tag}&lang={lang}";
            webview.CoreWebView2.Navigate(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize cutscene resource manage window.");
            Close();
        }
    }



    private void RootGrid_Unloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            RootGrid.Loaded -= RootGrid_Loaded;
            RootGrid.Unloaded -= RootGrid_Unloaded;
            webview.CoreWebView2.WebResourceRequested -= CoreWebView2_WebResourceRequested;
            _cancellationTokenSource.Cancel();
            if (_progress is not null && _progress.State is not RunnerState.Stop and not RunnerState.Finish)
            {
                _ = _gameInstallService.StopTaskAsync(_progress);
            }
        }
        catch { }
    }



    private const string CNVideoPath = @"YuanShen_Data\StreamingAssets\VideoAssets\StandaloneWindows64";

    private const string OSVideoPath = @"GenshinImpact_Data\StreamingAssets\VideoAssets\StandaloneWindows64";

    private const string CorsHeaders = """
        Access-Control-Allow-Origin: *
        Access-Control-Allow-Methods: GET, POST, OPTIONS
        Access-Control-Allow-Headers: Content-Type
        Access-Control-Max-Age: 600
        Cache-Control: no-store
        """;

    private const string JsonHeaders = """
        Access-Control-Allow-Origin: *
        Access-Control-Allow-Methods: GET, POST, OPTIONS
        Access-Control-Allow-Headers: Content-Type
        Access-Control-Max-Age: 600
        Cache-Control: no-store
        Content-Type: application/json
        """;



    private async void CoreWebView2_WebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        using var deferral = args.GetDeferral();
        try
        {
            if (args.Request.Method == "OPTIONS")
            {
                args.Response = sender.Environment.CreateWebResourceResponse(null, 204, "No Content", CorsHeaders);
                return;
            }
            if (args.Request.Uri.StartsWith("https://starward.local/api/cutscene/listfile"))
            {
                List<string> list = new();
                string folder = Path.Combine(_gameInstallPath, CurrentGameId.GameBiz.IsChinaServer() ? CNVideoPath : OSVideoPath);
                if (Directory.Exists(folder))
                {
                    string[] files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
                    list.AddRange(files.Select(Path.GetFileName)!);
                }
                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(list);
                args.Response = sender.Environment.CreateWebResourceResponse(new ReadOnlyMemoryStream(bytes).AsRandomAccessStream(), 200, "OK", JsonHeaders);
            }

            if (args.Request.Uri.StartsWith("https://starward.local/api/cutscene/update"))
            {
                List<string> list = await JsonSerializer.DeserializeAsync<List<string>>(args.Request.Content.AsStream()) ?? [];
                var result = await UpdateCutsceneAsync(list);
                args.Response = sender.Environment.CreateWebResourceResponse(new ReadOnlyMemoryStream(result.ToBytes()).AsRandomAccessStream(), 200, "OK", JsonHeaders);
            }

            if (args.Request.Uri.StartsWith("https://starward.local/api/cutscene/progress"))
            {
                var result = GetProgress();
                args.Response = sender.Environment.CreateWebResourceResponse(new ReadOnlyMemoryStream(result.ToBytes()).AsRandomAccessStream(), 200, "OK", JsonHeaders);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CoreWebView2_WebResourceRequested: {args.Request.Uri}");
            var result = ApiResult.Fail(ex.Message);
            args.Response = sender.Environment.CreateWebResourceResponse(new ReadOnlyMemoryStream(result.ToBytes()).AsRandomAccessStream(), 200, "OK", JsonHeaders);
        }
        finally
        {
            deferral.Complete();
        }
    }



    private async Task<ApiResult> UpdateCutsceneAsync(List<string> blockList)
    {
        try
        {
            blockList = blockList.Where(x => x.Contains("VideoAssets/StandaloneWindows64") && !x.Contains("..")).ToList();
            _progress = await _gameInstallService.UpdateCutsceneAsync(CurrentGameId, _gameInstallPath, blockList, CloseCancellationToken);
            if (_progress != null)
            {
                return ApiResult.OK(_progress);
            }
            else
            {
                return ApiResult.Fail("ERROR_CANCELLED");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateCutsceneAsync");
            return ApiResult.Fail(ex.Message);
        }
    }


    private GameInstallProgress? _progress;

    private ApiResult GetProgress()
    {
        try
        {
            _progress ??= _gameInstallService.GetGameInstallTask(CurrentGameId);
            if (_progress != null)
            {
                return ApiResult.OK(_progress);
            }
            else
            {
                return ApiResult.Fail("NoProgress");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetProgressAsync");
            return ApiResult.Fail(ex.Message);
        }
    }




    private class ApiResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public object? Data { get; set; }

        public static ApiResult OK(object data)
        {
            return new ApiResult
            {
                Success = true,
                Data = data,
            };
        }

        public static ApiResult Fail(string message)
        {
            return new ApiResult { Message = message };
        }

        public byte[] ToBytes()
        {
            return JsonSerializer.SerializeToUtf8Bytes(this);
        }

    }

}
