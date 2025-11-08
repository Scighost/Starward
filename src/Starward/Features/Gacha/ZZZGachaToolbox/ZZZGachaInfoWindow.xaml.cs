using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Starward.Core;
using Starward.Core.Gacha.ZZZ;
using Starward.Features.Database;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;


namespace Starward.Features.Gacha.ZZZGachaToolbox;

[INotifyPropertyChanged]
public sealed partial class ZZZGachaInfoWindow : WindowEx
{

    private readonly ILogger<ZZZGachaInfoWindow> _logger = AppConfig.GetLogger<ZZZGachaInfoWindow>();


    private readonly HttpClient _httpClient = AppConfig.GetService<HttpClient>();


    private const string Source_cn = "https://act.mihoyo.com/zzz/gt/character-builder-h/index.html";
    private const string Source_global = "https://act.hoyolab.com/zzz/gt/character-builder-h/index.html";


    public ZZZGachaInfoWindow()
    {
        this.InitializeComponent();
        InitializeWindow();
    }



    private void InitializeWindow()
    {
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        Title = Lang.ToolboxSetting_ZZZGachaItemImages;
        RootGrid.RequestedTheme = ShouldAppsUseDarkMode() ? ElementTheme.Dark : ElementTheme.Light;
        SystemBackdrop = new DesktopAcrylicBackdrop();
        AdaptTitleBarButtonColorToActuallTheme();
        SetIcon();
    }


    private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await webview2.EnsureCoreWebView2Async();
            coreWebView2 = webview2.CoreWebView2;
            coreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
            coreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZZZGachaInfoWindow: EnsureCoreWebView2");
        }
    }


    private void RootGrid_Unloaded(object sender, RoutedEventArgs e)
    {
        RootGrid.Loaded -= RootGrid_Loaded;
        RootGrid.Unloaded -= RootGrid_Unloaded;
        GridView_Languages.SelectionChanged -= GridView_Languages_SelectionChanged;
        if (coreWebView2 is not null)
        {
            coreWebView2.WebResourceResponseReceived -= CoreWebView2_WebResourceResponseReceived;
        }
        cts.Cancel();
        GachaInfoResult.Clear();
        GachaInfoResult = null!;
        gachaInfoDict = null!;
        iconInfoDict = null!;
        itemListDict = null!;
        headers = null!;
    }



    private CancellationTokenSource cts = new();


    private CoreWebView2 coreWebView2;


    private string? url;

    private List<KeyValuePair<string, string>> headers;



    public ObservableCollection<string> GachaInfoResult { get; set => SetProperty(ref field, value); } = new();


    private Dictionary<string, List<ZZZGachaInfo>> gachaInfoDict = new();

    private Dictionary<string, IconInfo> iconInfoDict = new();

    private Dictionary<string, ItemList> itemListDict = new();



    [RelayCommand]
    private void NavigateToMiyoushe()
    {
        try
        {
            webview2.Visibility = Visibility.Visible;
            webview2.CoreWebView2.Navigate(Source_cn);
            Button_GetAllLanguages.IsEnabled = false;
        }
        catch { }
    }



    [RelayCommand]
    private void NavigateToHoYoLAB()
    {
        try
        {
            webview2.Visibility = Visibility.Visible;
            webview2.CoreWebView2.Navigate(Source_global);
            Button_GetAllLanguages.IsEnabled = false;
        }
        catch { }
    }



    private async void CoreWebView2_WebResourceResponseReceived(CoreWebView2 sender, CoreWebView2WebResourceResponseReceivedEventArgs args)
    {
        try
        {
            if (Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out Uri? uri))
            {
                if (uri.AbsolutePath.Contains("/icon_info") && args.Response.StatusCode == 200)
                {
                    url = uri.OriginalString;
                    headers = args.Request.Headers.ToList();

                    string biz = "";
                    string lang = "";
                    if (uri.OriginalString.Contains("mihoyo.com"))
                    {
                        biz = GameBiz.nap_cn;
                        lang = "zh-cn";
                        Button_GetAllLanguages.IsEnabled = false;
                    }
                    if (uri.OriginalString.Contains("hoyolab.com"))
                    {
                        biz = GameBiz.nap_global;
                        string cookie = args.Request.Headers.GetHeader("Cookie");
                        lang = Regex.Match(cookie, @"mi18nLang=([^;]+);?").Groups[1].Value;
                        Button_GetAllLanguages.IsEnabled = true;
                    }
                    if (string.IsNullOrWhiteSpace(biz) || string.IsNullOrWhiteSpace(lang))
                    {
                        return;
                    }
                    string key = $"{biz}.{lang}";
                    await AddIconInfoAndGetItemListAsync(key, args);
                    UpdateGachaInfo(key);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZZZGachaInfoWindow: CoreWebView2_WebResourceResponseReceived");
        }
    }



    private async Task AddIconInfoAndGetItemListAsync(string key, CoreWebView2WebResourceResponseReceivedEventArgs args)
    {
        try
        {

            var stream = await args.Response.GetContentAsync();
            if (stream is not null)
            {
                var wrapper = await JsonSerializer.DeserializeAsync<miHoYoApiWrapper<IconInfo>>(stream.AsStream(), cancellationToken: cts.Token);
                if (wrapper is not null && wrapper.Data is not null)
                {
                    iconInfoDict.TryAdd(key, wrapper.Data);
                }
            }
            {
                string url = args.Request.Uri.Replace("/icon_info", "/item_list") + "&avatar_id=1011";
                var headers = args.Request.Headers.ToList();
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                var response = await _httpClient.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();
                var wrapper = await response.Content.ReadFromJsonAsync<miHoYoApiWrapper<ItemList>>(cts.Token);
                if (wrapper is not null && wrapper.Data is not null)
                {
                    itemListDict.TryAdd(key, wrapper.Data);
                }
            }

        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get ZZZGachaInfo for specific langugae");
        }
    }



    [RelayCommand]
    private async Task GetAllLanguagesInfoAsync()
    {
        try
        {
            if (url?.Contains("hoyolab.com") ?? false && headers is not null)
            {
                foreach (var lang in LanguageUtil.GetAllLanguages())
                {
                    string key = $"nap_global.{lang}";
                    if (gachaInfoDict.ContainsKey(key))
                    {
                        continue;
                    }
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, url);
                        foreach (var header in headers)
                        {
                            string value = header.Value;
                            if (header.Key.Equals("cookie", StringComparison.OrdinalIgnoreCase))
                            {
                                value = Regex.Replace(value, @"mi18nLang=[^;]+", $"mi18nLang={lang}");
                            }
                            if (header.Key.Equals("x-rpc-lang", StringComparison.OrdinalIgnoreCase))
                            {
                                value = lang;
                            }
                            request.Headers.Add(header.Key, value);
                        }
                        var response = await _httpClient.SendAsync(request, cts.Token);
                        response.EnsureSuccessStatusCode();
                        var wrapper = await response.Content.ReadFromJsonAsync<miHoYoApiWrapper<IconInfo>>(cts.Token);
                        if (wrapper is not null && wrapper.Data is not null)
                        {
                            iconInfoDict.TryAdd(key, wrapper.Data);
                        }
                    }
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, url.Replace("/icon_info", "/item_list") + "&avatar_id=1011");
                        foreach (var header in headers)
                        {
                            string value = header.Value;
                            if (header.Key.Equals("cookie", StringComparison.OrdinalIgnoreCase))
                            {
                                value = Regex.Replace(value, @"mi18nLang=[^;]+", $"mi18nLang={lang}");
                            }
                            if (header.Key.Equals("x-rpc-lang", StringComparison.OrdinalIgnoreCase))
                            {
                                value = lang;
                            }
                            request.Headers.Add(header.Key, value);
                        }
                        var response = await _httpClient.SendAsync(request, cts.Token);
                        response.EnsureSuccessStatusCode();
                        var wrapper = await response.Content.ReadFromJsonAsync<miHoYoApiWrapper<ItemList>>(cts.Token);
                        if (wrapper is not null && wrapper.Data is not null)
                        {
                            itemListDict.TryAdd(key, wrapper.Data);
                        }
                    }
                    UpdateGachaInfo(key);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get ZZZGachaInfo for all languages");
        }
    }



    private void UpdateGachaInfo(string key)
    {
        try
        {
            var iconInfo = iconInfoDict.GetValueOrDefault(key);
            var itemList = itemListDict.GetValueOrDefault(key);
            if (iconInfo is not null && itemList is not null)
            {
                var list = new List<ZZZGachaInfo>();
                foreach (var item in itemList.Avatars)
                {
                    var info = new ZZZGachaInfo
                    {
                        Id = item.Id,
                        Name = item.NameMi18n,
                        Rarity = item.Rarity switch
                        {
                            "S" or "s" => 4,
                            "A" or "a" => 3,
                            "B" or "b" => 2,
                            _ => 0,
                        },
                        ElementType = item.ElementType,
                        Profession = item.AvatarProfession,
                    };
                    info.Icon = iconInfo.AvatarIcons.GetValueOrDefault(item.Id.ToString())?.SquareAvatar ?? "";
                    list.Add(info);
                }
                foreach (var item in itemList.Weapons)
                {
                    var info = new ZZZGachaInfo
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Icon = item.Icon,
                        Rarity = item.Rarity switch
                        {
                            "S" or "s" => 4,
                            "A" or "a" => 3,
                            "B" or "b" => 2,
                            _ => 0,
                        },
                        Profession = item.Profession,
                    };
                    list.Add(info);
                }
                foreach (var item in itemList.Buddies)
                {
                    var info = new ZZZGachaInfo
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Rarity = item.Rarity switch
                        {
                            "S" or "s" => 4,
                            "A" or "a" => 3,
                            "B" or "b" => 2,
                            _ => 0,
                        },
                    };
                    info.Icon = iconInfo.BuddyIcons.GetValueOrDefault(item.Id.ToString())?.SquareAvatar ?? "";
                    list.Add(info);
                }

                gachaInfoDict.TryAdd(key, list);
                GachaInfoResult.Remove(key);
                GachaInfoResult.Add(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZZZGachaWindows: Update GachaInfo");
        }
    }



    private void GridView_Languages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (sender is GridView gridView)
            {
                Button_SaveToDatabase.IsEnabled = gridView.SelectedItems.Count == 1;
                Button_ExportFiles.IsEnabled = gridView.SelectedItems.Count > 0;
            }
        }
        catch { }
    }



    [RelayCommand]
    private void SaveToDatabase()
    {
        try
        {
            if (GridView_Languages.SelectedItems.Count != 1)
            {
                return;
            }
            if (GridView_Languages.SelectedItems[0] is string key)
            {
                if (gachaInfoDict.TryGetValue(key, out var list))
                {
                    using var dapper = DatabaseService.CreateConnection();
                    using var t = dapper.BeginTransaction();
                    dapper.Execute("""
                        INSERT OR REPLACE INTO ZZZGachaInfo (Id, Name, Icon, Rarity, ElementType, Profession)
                        VALUES (@Id, @Name, @Icon, @Rarity, @ElementType, @Profession);
                        """, list, t);
                    t.Commit();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save ZZZGachaInfo to database");
        }
    }



    [RelayCommand]
    private async Task ExportToFolderAsync()
    {
        try
        {
            if (GridView_Languages.SelectedItems.Count > 0)
            {
                string? folder = await FileDialogHelper.PickFolderAsync(this.Content.XamlRoot);
                if (Directory.Exists(folder))
                {
                    foreach (string key in GridView_Languages.SelectedItems.Cast<string>())
                    {
                        if (gachaInfoDict.TryGetValue(key, out var list))
                        {
                            var obj = new miHoYoApiWrapper<ZZZGachaWiki>
                            {
                                Retcode = 0,
                                Message = "",
                                Data = new ZZZGachaWiki
                                {
                                    Game = GameBiz.nap,
                                    Language = key[^5..],
                                    List = list.OrderBy(x => x.Id).ToList(),
                                },
                            };
                            string json = JsonSerializer.Serialize(obj, AppConfig.JsonSerializerOptions);
                            string path = Path.Combine(folder, $"ZZZGachaInfo.{key}.json");
                            await File.WriteAllTextAsync(path, json, cts.Token);
                        }
                    }
                    await Launcher.LaunchUriAsync(new Uri(folder));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export ZZZGachaInfo to folder");
        }
    }


}


