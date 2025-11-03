using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Core.Gacha.Genshin;
using Starward.Features.GameLauncher;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;


namespace Starward.Features.Gacha;

public sealed partial class GenshinBeyondGachaPage : PageBase
{


    private readonly ILogger<GenshinBeyondGachaPage> _logger = AppConfig.GetLogger<GenshinBeyondGachaPage>();

    private readonly GenshinBeyondGachaService _gachaLogService = AppConfig.GetService<GenshinBeyondGachaService>();



    public GenshinBeyondGachaPage()
    {
        InitializeComponent();
    }


    public ObservableCollection<long> UidList { get; set => SetProperty(ref field, value); }


    [ObservableProperty]
    public partial long? SelectUid { get; set; }
    partial void OnSelectUidChanged(long? value)
    {
        AppConfig.SetLastUidInGachaLogPage("hk4eugc", value ?? 0);
        UpdateGachaTypeStats(value);
    }




    protected override async void OnLoaded()
    {
        await Task.Delay(16);
        Initialize();
        await UpdateGachaInfoAsync();
    }



    protected override void OnUnloaded()
    {
        GachaStatsType1000 = null;
        GachaStatsType2000 = null;
        GachaItemStats = null;
    }



    private void Initialize()
    {
        try
        {
            SelectUid = null;
            UidList = new(_gachaLogService.GetUids());
            var lastUid = AppConfig.GetLastUidInGachaLogPage("hk4eugc");
            if (UidList.Contains(lastUid))
            {
                SelectUid = lastUid;
            }
            else
            {
                SelectUid = UidList.FirstOrDefault();
            }
            if (UidList.Count == 0)
            {
                StackPanel_Emoji.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize");
        }
    }


    public GenshinBeyondGachaTypeStats? GachaStatsType1000 { get; set => SetProperty(ref field, value); }

    public GenshinBeyondGachaTypeStats? GachaStatsType2000 { get; set => SetProperty(ref field, value); }

    public List<GenshinBeyondGachaItemEx>? GachaItemStats { get; set => SetProperty(ref field, value); }


    private int errorCount = 0;


    private void UpdateGachaTypeStats(long? uid)
    {
        try
        {
            GachaStatsType1000 = null;
            GachaStatsType2000 = null;

            if (uid.HasValue && uid.Value != 0)
            {
                GachaStatsType1000 = _gachaLogService.GetGachaTypeStatsType1000(uid.Value);
                GachaStatsType2000 = _gachaLogService.GetGachaTypeStatsType2000(uid.Value);
                GachaItemStats = _gachaLogService.GetGachaItemStats(uid.Value);
            }

            if (GachaStatsType1000 is null && GachaStatsType2000 is null)
            {
                StackPanel_Emoji.Visibility = Visibility.Visible;
            }
            else
            {
                StackPanel_Emoji.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateGachaTypeStats");
        }
    }




    private async Task UpdateGachaInfoAsync()
    {
        try
        {
            await _gachaLogService.UpdateGachaInfoAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update wiki data hk4eugc");
        }
    }



    [RelayCommand]
    private async Task UpdateGachaLogAsync(string? param = null)
    {
        try
        {
            string? url = null;
            if (param is "cache")
            {
                if (SelectUid is null or 0)
                {
                    return;
                }
                url = _gachaLogService.GetGachaLogUrlByUid(SelectUid.Value);
                if (string.IsNullOrWhiteSpace(url))
                {
                    // 无法找到 uid {uid} 的已缓存 URL
                    InAppToast.MainWindow?.Warning(null, string.Format(Lang.GachaLogPage_CannotFindSavedURLOfUid, SelectUid));
                    return;
                }
            }
            else
            {
                var path = GameLauncherService.GetGameInstallPath(CurrentGameId);
                if (!Directory.Exists(path))
                {
                    // 游戏未安装
                    InAppToast.MainWindow?.Warning(null, Lang.GachaLogPage_GameNotInstalled);
                    return;
                }
                url = _gachaLogService.GetGachaLogUrlFromWebCache(CurrentGameBiz, path);
                if (string.IsNullOrWhiteSpace(url))
                {
                    // 无法找到 URL，请在游戏中打开抽卡记录页面
                    errorCount++;
                    if (errorCount > 2 && IsGachaCacheFileExists())
                    {
                        errorCount = 0;
                        InAppToast.MainWindow?.ShowWithButton(InfoBarSeverity.Warning,
                                                                     Lang.GachaLogPage_AlwaysFailedToGetGachaRecords,
                                                                     null,
                                                                     Lang.GachaLogPage_ClearURLCacheFiles,
                                                                     () => _ = DeleteGachaCacheFileAsync());
                    }
                    else
                    {
                        InAppToast.MainWindow?.Warning(null, Lang.GachaLogPage_CannotFindURL);
                    }
                    return;
                }
            }
            await UpdateGachaLogInternalAsync(url, param is "all");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update gacha log");
            InAppToast.MainWindow?.Error(ex);
        }
    }



    private async Task UpdateGachaLogInternalAsync(string url, bool all = false)
    {
        try
        {
            var uid = await _gachaLogService.GetUidFromGachaLogUrl(url);
            var cancelSource = new CancellationTokenSource();
            var button = new Button
            {
                // 取消
                Content = Lang.Common_Cancel,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            var infoBar = new InfoBar
            {
                Severity = InfoBarSeverity.Informational,
                Background = Application.Current.Resources["CustomAcrylicBrush"] as Brush,
                ActionButton = button,
            };
            button.Click += (_, _) =>
            {
                cancelSource.Cancel();
                // 操作已取消
                infoBar.Message = Lang.GachaLogPage_OperationCanceled;
                infoBar.ActionButton = null;
            };
            InAppToast.MainWindow?.Show(infoBar);
            var progress = new Progress<string>((str) => infoBar.Message = str);
            var newUid = await _gachaLogService.GetGachaLogAsync(url, all, GachaLanguage, progress, cancelSource.Token);
            infoBar.Title = $"Uid {newUid}";
            infoBar.Severity = InfoBarSeverity.Success;
            infoBar.ActionButton = null;
            if (SelectUid == uid)
            {
                UpdateGachaTypeStats(uid);
            }
            else
            {
                if (!UidList.Contains(uid))
                {
                    UidList.Add(uid);
                }
                SelectUid = uid;
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Get gacha log canceled");
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogWarning("Request mihoyo api error: {error}", ex.Message);
            // 原铁 -101 绝 -1
            if (ex.ReturnCode is -101 or -1)
            {
                // authkey timeout
                // 请在游戏中打开抽卡记录页面后再重试
                errorCount++;
                if (errorCount > 1 && IsGachaCacheFileExists())
                {
                    errorCount = 0;
                    InAppToast.MainWindow?.ShowWithButton(InfoBarSeverity.Warning,
                                                                 Lang.GachaLogPage_AlwaysFailedToGetGachaRecords,
                                                                 null,
                                                                 Lang.GachaLogPage_ClearURLCacheFiles,
                                                                 () => _ = DeleteGachaCacheFileAsync());
                }
                else
                {
                    InAppToast.MainWindow?.Warning("Authkey Timeout", Lang.GachaLogPage_PleaseOpenTheGachaRecordsPageInGameAndTryAgain);
                }
            }
            else
            {
                InAppToast.MainWindow?.Warning(null, ex.Message);
            }
        }
    }



    [RelayCommand]
    private async Task InputUrlAsync()
    {
        try
        {
            var textbox = new TextBox();
            var dialog = new ContentDialog
            {
                // 输入 URL
                Title = Lang.GachaLogPage_InputURL,
                Content = textbox,
                // 确认
                PrimaryButtonText = Lang.Common_Confirm,
                // 取消
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var url = textbox.Text;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    await UpdateGachaLogInternalAsync(url);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Input url");
            InAppToast.MainWindow?.Error(ex);
        }
    }



    public string? GachaLanguage
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                AppConfig.GachaLanguage = value;
            }
        }
    } = AppConfig.GachaLanguage;



    [RelayCommand]
    private async Task CopyUrlAsync()
    {
        try
        {
            if (SelectUid is null or 0)
            {
                return;
            }
            var url = _gachaLogService.GetGachaLogUrlByUid(SelectUid.Value);
            if (!string.IsNullOrWhiteSpace(url))
            {
                ClipboardHelper.SetText(url);
                FontIcon_CopyUrl.Glyph = "\uE8FB"; // accept
                await Task.Delay(1000);
                FontIcon_CopyUrl.Glyph = "\uE8C8";  // copy
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy url");
        }
    }



    [RelayCommand]
    private async Task DeleteGachaCacheFileAsync()
    {
        try
        {
            var installPath = GameLauncherService.GetGameInstallPath(CurrentGameId);
            if (Directory.Exists(installPath))
            {
                var path = GenshinBeyondGachaClient.GetGachaCacheFilePath(CurrentGameBiz, installPath);
                if (File.Exists(path))
                {
                    var file = await StorageFile.GetFileFromPathAsync(path);
                    if (file != null)
                    {
                        var option = new FolderLauncherOptions();
                        option.ItemsToSelect.Add(file);
                        await Launcher.LaunchFolderAsync(await file.GetParentAsync(), option);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete gacha cache file");
        }
    }



    private bool IsGachaCacheFileExists()
    {
        try
        {
            var installPath = GameLauncherService.GetGameInstallPath(CurrentGameId);
            if (Directory.Exists(installPath))
            {
                var path = GenshinBeyondGachaClient.GetGachaCacheFilePath(CurrentGameBiz, installPath);
                return File.Exists(path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check gacha cache file exists");
        }
        return false;
    }



    [RelayCommand]
    private void OpenItemStatsPane()
    {
        SplitView_Content.IsPaneOpen = true;
    }


}
