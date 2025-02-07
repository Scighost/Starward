// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Starward.Controls;
using Starward.Core;
using Starward.Helpers;
using Starward.Messages;
using Starward.Models;
using Starward.Services;
using Starward.Services.Download;
using Starward.Services.Launcher;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Timers;
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class GameLauncherPage : PageBase
{

    private readonly ILogger<GameLauncherPage> _logger = AppConfig.GetLogger<GameLauncherPage>();

    private readonly GameService _gameService = AppConfig.GetService<GameService>();

    private readonly HoYoPlayService _hoYoPlayService = AppConfig.GetService<HoYoPlayService>();

    private readonly LauncherContentService _launcherContentService = AppConfig.GetService<LauncherContentService>();

    private readonly PlayTimeService _playTimeService = AppConfig.GetService<PlayTimeService>();


    private readonly GameAccountService _gameAccountService = AppConfig.GetService<GameAccountService>();

    private readonly GameLauncherService _gameLauncherService = AppConfig.GetService<GameLauncherService>();

    private readonly GamePackageService _gamePackageService = AppConfig.GetService<GamePackageService>();


    public GameLauncherPage()
    {
        this.InitializeComponent();

        TextBlockHelper.Inlines(
            TextBlock_StartArgumentDesc.Inlines,
            Lang.GameSettingPage_StartArgumentDesc,
            ("{Unity Standalone Player Command Line Arguments}", "https://docs.unity3d.com/Manual/PlayerCommandLineArguments.html"));
    }



    protected override async void OnLoaded()
    {
        try
        {
            RegisterMessageHandler();
            InitializeCurrentGameBiz();

            await Task.Delay(16);
            CheckGameVersion();
            UpdateGameState();
            //AccountSwitcher.UpdateGameAccount();
            GetGameAccount();

            if (!AppConfig.LauncherPageFirstLoaded)
            {
                // 避免加载窗口和缓存图片同时进行可能导致的崩溃
                await Task.Delay(200);
                AppConfig.LauncherPageFirstLoaded = true;
            }
            await UpdateGameContentAsync();
            await UpdateGameNoticesAlertAsync();
        }
        catch { }
    }



    protected override void OnUnloaded()
    {
        GameProcess?.Dispose();
        processTimer?.Dispose();
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }



    private void RegisterMessageHandler()
    {
        WeakReferenceMessenger.Default.Register<GameAccountSwitcherDisabledChanged>(this, (_, _) =>
        {
            GetGameAccount();
            _ = UpdateGameNoticesAlertAsync();
        });
        WeakReferenceMessenger.Default.Register<GameNoticeRedHotDisabledChanged>(this, (_, _) => _ = UpdateGameNoticesAlertAsync());
        WeakReferenceMessenger.Default.Register<GameNoticesWindowClosedMessage>(this, (_, _) =>
        {
            MainWindow.Current.Show();
            _ = UpdateGameNoticesAlertAsync();
        });
        WeakReferenceMessenger.Default.Register<InstallGameFinishedMessage>(this, (_, m) =>
        {
            if (m.GameBiz == CurrentGameBiz)
            {
                CheckGameVersion();
            }
        });
    }



    private void InitializeCurrentGameBiz()
    {
        try
        {
            StartGameArgument = AppConfig.GetStartArgument(CurrentGameBiz);
            EnableThirdPartyTool = AppConfig.GetEnableThirdPartyTool(CurrentGameBiz);
            ThirdPartyToolPath = AppConfig.GetThirdPartyToolPath(CurrentGameBiz);
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
            enableCustomBg = AppConfig.GetEnableCustomBg(CurrentGameBiz);
            OnPropertyChanged(nameof(EnableCustomBg));
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field 
            CustomBg = AppConfig.GetCustomBg(CurrentGameBiz);
            if (CurrentGameBiz == GameBiz.clgm_cn)
            {
                Button_UninstallGame.IsEnabled = false;
                Button_SettingRepairGame.IsEnabled = false;
            }
        }
        catch { }
    }





    #region Banner & Post



    [ObservableProperty]
    private bool enableBannerAndPost = AppConfig.EnableBannerAndPost;
    partial void OnEnableBannerAndPostChanged(bool value)
    {
        AppConfig.EnableBannerAndPost = value;
        GameBannerAndPost.ShowBannerAndPost = value;
    }



    private async Task UpdateGameContentAsync()
    {
        try
        {
            if (CurrentGameBiz == GameBiz.clgm_cn)
            {
                GameBannerAndPost.GameContent = await _hoYoPlayService.GetGameContentAsync(GameBiz.hk4e_cn);
            }
            else
            {
                GameBannerAndPost.GameContent = await _hoYoPlayService.GetGameContentAsync(CurrentGameBiz);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Cannot get game launcher content ({CurrentGameBiz}): {error}", CurrentGameBiz, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get game launcher content ({CurrentGameBiz})", CurrentGameBiz);
        }
    }



    private async Task UpdateGameNoticesAlertAsync()
    {
        try
        {
            if (AppConfig.DisableGameNoticeRedHot || AppConfig.DisableGameAccountSwitcher || CurrentGameBiz.IsBilibili())
            {
                GameBannerAndPost.IsGameNoticesAlert = false;
                return;
            }
            long uid = 0;
            if (GameAccountList?.FirstOrDefault(x => x.IsLogin) is GameAccount account)
            {
                uid = account.Uid;
            }
            if (uid == 0)
            {
                GameBannerAndPost.IsGameNoticesAlert = false;
                return;
            }
            if (await _launcherContentService.IsNoticesAlertAsync(CurrentGameBiz, uid))
            {
                GameBannerAndPost.IsGameNoticesAlert = true;
            }
            else
            {
                GameBannerAndPost.IsGameNoticesAlert = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Game notices alert");
            GameBannerAndPost.IsGameNoticesAlert = false;
        }
    }





    #endregion




    #region Game Version


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInstallGameButtonEnable))]
    private string? installPath;
    partial void OnInstallPathChanged(string? value)
    {
        AppConfig.SetGameInstallPath(CurrentGameBiz, value);
    }


    [ObservableProperty]
    private bool isInstallPathRemovableTipEnabled;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStartGameButtonEnable))]
    [NotifyPropertyChangedFor(nameof(IsInstallGameButtonEnable))]
    [NotifyPropertyChangedFor(nameof(IsUpdateGameButtonEnable))]
    [NotifyPropertyChangedFor(nameof(IsPreInstallButtonEnable))]
    [NotifyPropertyChangedFor(nameof(IsRepairGameButtonEnable))]
    private Version? localGameVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStartGameButtonEnable))]
    [NotifyPropertyChangedFor(nameof(IsUpdateGameButtonEnable))]
    private Version? latestGameVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPreInstallButtonEnable))]
    private Version? preInstallGameVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStartGameButtonEnable))]
    [NotifyPropertyChangedFor(nameof(IsInstallGameButtonEnable))]
    private bool isGameExeExists;


    private GameBiz hardLinkGameBiz;


    private string? hardLinkPath;


    public bool IsGameSupportRepair => CurrentGameBiz.ToGame() != GameBiz.None && CurrentGameBiz != GameBiz.clgm_cn;


    public bool IsStartGameButtonEnable => LocalGameVersion != null && LocalGameVersion >= LatestGameVersion && IsGameExeExists && !IsGameRunning;


    public bool IsInstallGameButtonEnable => LocalGameVersion == null || !IsGameExeExists;


    public bool IsUpdateGameButtonEnable => LocalGameVersion != null && LatestGameVersion > LocalGameVersion;


    public bool IsPreInstallButtonEnable => LocalGameVersion != null && PreInstallGameVersion != null;


    public bool IsRepairGameButtonEnable => IsGameSupportRepair;



    [ObservableProperty]
    private bool isPreDownloadOK;


    private async void CheckGameVersion()
    {
        try
        {
            InstallPath = _gameLauncherService.GetGameInstallPath(CurrentGameBiz);
            if (AppConfig.GetGameInstallPathRemovable(CurrentGameBiz) && !Directory.Exists(InstallPath))
            {
                IsInstallPathRemovableTipEnabled = true;
            }
            _logger.LogInformation("Game install path of {biz}: {path}", CurrentGameBiz, InstallPath);
            IsGameExeExists = _gameLauncherService.IsGameExeExists(CurrentGameBiz);
            LocalGameVersion = await _gameLauncherService.GetLocalGameVersionAsync(CurrentGameBiz);
            (hardLinkGameBiz, hardLinkPath) = await _gameLauncherService.GetHardLinkInfoAsync(CurrentGameBiz);
            _logger.LogInformation("Acutal version and gamebiz of {biz} is {version}.", CurrentGameBiz, LocalGameVersion);
            LatestGameVersion = await _gameLauncherService.GetLatestGameVersionAsync(CurrentGameBiz);
            PreInstallGameVersion = await _gameLauncherService.GetPreDownloadGameVersionAsync(CurrentGameBiz);
            if (IsPreInstallButtonEnable)
            {
                IsPreDownloadOK = await _gamePackageService.CheckPreDownloadIsOKAsync(CurrentGameBiz);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check game version");
        }
    }




    #endregion




    #region Start Game





    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStartGameButtonEnable))]
    private bool isGameRunning;


    [ObservableProperty]
    private bool canStartGame = true;


    private Timer processTimer;


    [ObservableProperty]
    private Process? gameProcess;
    partial void OnGameProcessChanged(Process? oldValue, Process? newValue)
    {
        oldValue?.Dispose();
        processTimer?.Stop();
        if (processTimer is null)
        {
            processTimer = new(1000);
            processTimer.Elapsed += (_, _) => CheckGameExited();
        }
        if (newValue != null)
        {
            processTimer?.Start();
        }
        else
        {
            _logger.LogInformation("Game process exited");
            DispatcherQueue.TryEnqueue(GetGameAccount);
        }
    }



    private void UpdateGameState()
    {
        try
        {
            CanStartGame = true;
            IsGameRunning = false;
            if (IgnoreRunningGame)
            {
                GameProcess = null;
                return;
            }
            GameProcess = _gameLauncherService.GetGameProcess(CurrentGameBiz);
            if (GameProcess != null)
            {
                IsGameRunning = true;
                _logger.LogInformation("Game is running ({name}, {pid})", GameProcess.ProcessName, GameProcess.Id);
            }
        }
        catch { }
    }



    private void CheckGameExited()
    {
        try
        {
            if (GameProcess != null)
            {
                if (GameProcess.HasExited)
                {
                    DispatcherQueue.TryEnqueue(UpdateGameState);
                    GameProcess = null;
                }
            }
        }
        catch { }
    }



    [RelayCommand]
    private async Task StartGameAsync()
    {
        try
        {
            CanStartGame = false;
            if (!IgnoreRunningGame)
            {
                var p = _gameLauncherService.GetGameProcess(CurrentGameBiz);
                if (p != null)
                {
                    GameProcess = p;
                    return;
                }
            }
            var process1 = _gameLauncherService.StartGame(CurrentGameBiz, IgnoreRunningGame);
            if (process1 == null)
            {
                CanStartGame = true;
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new GameStartMessage(CurrentGameBiz));
                var action = AppConfig.AfterStartGameAction;
                if (action is AfterStartGameAction.Minimize)
                {
                    MainWindow.Current.Minimize();
                }
                else if (action is AfterStartGameAction.DoNothing)
                {
                    // do nothing
                }
                else
                {
                    MainWindow.Current.Hide();
                }
                _logger.LogInformation("Game started ({name}, {pid})", process1.ProcessName, process1.Id);
                if (AppConfig.IgnoreRunningGame)
                {
                    _ = _playTimeService.StartProcessToLogAsync(CurrentGameBiz);
                    CanStartGame = true;
                }
                else
                {
                    IsGameRunning = true;
                    var process2 = await _playTimeService.StartProcessToLogAsync(CurrentGameBiz);
                    GameProcess = process2 ?? process1;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start game");
            CanStartGame = true;
        }
    }




    #endregion




    #region Game Account



    private TextBox TextBox_AccountUid;


    [ObservableProperty]
    private List<GameAccount> gameAccountList;


    [ObservableProperty]
    private GameAccount? selectGameAccount;
    partial void OnSelectGameAccountChanged(GameAccount? value)
    {
        CanChangeGameAccount = value is not null;
    }


    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ChangeGameAccountCommand))]
    private bool canChangeGameAccount;


    private List<string> suggestionUids;


    private void GetGameAccount()
    {
        try
        {
            if (AppConfig.DisableGameAccountSwitcher || CurrentGameBiz.IsBilibili())
            {
                StackPanel_Account.Visibility = Visibility.Collapsed;
                return;
            }
            else
            {
                StackPanel_Account.Visibility = Visibility.Visible;
            }
            GameAccountList = _gameAccountService.GetGameAccounts(CurrentGameBiz);
            SelectGameAccount = GameAccountList.FirstOrDefault(x => x.IsLogin);
            CanChangeGameAccount = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot get game account ({biz})", CurrentGameBiz);
        }
    }




    [RelayCommand(CanExecute = nameof(CanChangeGameAccount))]
    private void ChangeGameAccount()
    {
        try
        {
            if (SelectGameAccount is not null)
            {
                _gameAccountService.ChangeGameAccount(SelectGameAccount);
                foreach (var item in GameAccountList)
                {
                    item.IsLogin = false;
                }
                CanChangeGameAccount = false;
                SelectGameAccount.IsLogin = true;
                if (IsGameRunning)
                {
                    NotificationBehavior.Instance.Warning(Lang.LauncherPage_AccountSwitchingCannotTakeEffectWhileGameIsRunning);
                }
                _ = UpdateGameNoticesAlertAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot change game {biz} account to {name}", CurrentGameBiz, SelectGameAccount?.Name);
        }
    }


    [RelayCommand]
    private async Task SaveGameAccountAsync()
    {
        try
        {
            if (SelectGameAccount is not null)
            {
                var acc = SelectGameAccount;
                if (GameAccountList.FirstOrDefault(x => x.SHA256 != acc.SHA256 && x.Uid == acc.Uid) is GameAccount lacc)
                {
                    var dialog = new ContentDialog
                    {
                        Title = Lang.Common_Attention,
                        Content = string.Format(Lang.LauncherPage_AccountSaveNew, acc.Uid),
                        PrimaryButtonText = Lang.LauncherPage_Replace,
                        SecondaryButtonText = Lang.LauncherPage_SaveNew,
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = this.XamlRoot,
                    };
                    if (await dialog.ShowAsync() is ContentDialogResult.Primary)
                    {
                        GameAccountList.Remove(lacc);
                        _gameAccountService.DeleteGameAccount(lacc);
                    }
                }
                SelectGameAccount.Time = DateTime.Now;
                _gameAccountService.SaveGameAccount(SelectGameAccount);
                FontIcon_SaveGameAccount.Glyph = "\uE8FB";
                _ = UpdateGameNoticesAlertAsync();
                await Task.Delay(3000);
                FontIcon_SaveGameAccount.Glyph = "\uE74E";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save game account");
        }
    }


    [RelayCommand]
    private void DeleteGameAccount()
    {
        try
        {
            if (SelectGameAccount is not null)
            {
                _gameAccountService.DeleteGameAccount(SelectGameAccount);
                GetGameAccount();
                _ = UpdateGameNoticesAlertAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete game account");
        }
    }



    private void AutoSuggestBox_Uid_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (TextBox_AccountUid is null)
            {
                var ele1 = VisualTreeHelper.GetChild(AutoSuggestBox_Uid, 0);
                var ele = VisualTreeHelper.GetChild(ele1, 0);
                if (ele is TextBox textBox)
                {
                    TextBox_AccountUid = textBox;
                    TextBox_AccountUid.InputScope = new InputScope { Names = { new InputScopeName { NameValue = InputScopeNameValue.Number } } };
                    TextBox_AccountUid.BeforeTextChanging += (s, e) =>
                    {
                        e.Cancel = !e.NewText.All(x => char.IsDigit(x));
                    };
                }
            }
        }
        catch { }
    }


    private void AutoSuggestBox_Uid_GotFocus(object sender, RoutedEventArgs e)
    {
        try
        {
            suggestionUids = _gameAccountService.GetSuggestionUids(CurrentGameBiz).Select(x => x.ToString()).ToList();
            UpdateSuggestionUids(AutoSuggestBox_Uid.Text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get suggestion uids");
        }
    }


    private void AutoSuggestBox_Uid_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        try
        {
            if (args.Reason is AutoSuggestionBoxTextChangeReason.UserInput)
            {
                UpdateSuggestionUids(sender.Text);
            }
        }
        catch { }
    }


    private void UpdateSuggestionUids(string text)
    {
        try
        {
            if (suggestionUids != null && suggestionUids.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    AutoSuggestBox_Uid.ItemsSource = suggestionUids;
                    AutoSuggestBox_Uid.IsSuggestionListOpen = true;
                }
                else
                {
                    var list = suggestionUids.Where(x => x != text && x.StartsWith(text)).ToList();
                    if (list.Count == 0)
                    {
                        AutoSuggestBox_Uid.IsSuggestionListOpen = false;
                    }
                    else
                    {
                        if (!(AutoSuggestBox_Uid.ItemsSource is List<string> source && source.SequenceEqual(list)))
                        {
                            AutoSuggestBox_Uid.ItemsSource = list;
                        }
                        AutoSuggestBox_Uid.IsSuggestionListOpen = true;
                    }
                }
            }
        }
        catch { }
    }



    #endregion




    #region New Install Game




    [ObservableProperty]
    private InstallGameStateModel _installGameModel;


    [ObservableProperty]
    private InstallGameStateModel _predownloadModel;




    private async Task<bool> CheckWritePermissionAsync()
    {
        try
        {
            if (Directory.Exists(InstallPath))
            {
                if (Directory.Exists(hardLinkPath))
                {
                    if (IsAdmin())
                    {
                        return true;
                    }
                }
                else
                {
                    try
                    {
                        string temp = Path.Combine(InstallPath, Random.Shared.Next(1000_0000, int.MaxValue).ToString());
                        File.Create(temp).Dispose();
                        File.Delete(temp);
                        return true;
                    }
                    catch (UnauthorizedAccessException) { }
                }
                var dialog = new ContentDialog
                {
                    Title = Lang.GameLauncherPage_NoWritePermission,
                    Content = Lang.GameLauncherPage_PleaseRestartAsAdministrator,
                    PrimaryButtonText = Lang.Common_Restart,
                    CloseButtonText = Lang.Common_Cancel,
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot,
                };
                if (await dialog.ShowAsync() is ContentDialogResult.Primary)
                {
                    string? exe = Process.GetCurrentProcess().MainModule?.FileName;
                    if (!File.Exists(exe))
                    {
                        exe = Path.Combine(AppContext.BaseDirectory, "Starward.exe");
                    }
                    if (File.Exists(exe))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = exe,
                            UseShellExecute = true,
                            Verb = "runas",
                        });
                        Environment.Exit(0);
                    }
                }
            }
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check write permission: {path}", InstallPath);
        }
        return false;
    }



    private static bool IsAdmin()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }



    private async Task<bool> IsHardLinkTargetLatestVersionAsync()
    {
        try
        {
            if (hardLinkGameBiz.ToGame() == CurrentGameBiz.ToGame() && hardLinkGameBiz != CurrentGameBiz)
            {
                var version = await _gameLauncherService.GetLocalGameVersionAsync(hardLinkGameBiz);
                return version >= LatestGameVersion;
            }
        }
        catch { }
        return false;
    }



    [RelayCommand]
    private async Task InstallGameAsync()
    {
        try
        {
            if (CurrentGameBiz == GameBiz.clgm_cn)
            {
                await Launcher.LaunchUriAsync(new Uri("https://ys.mihoyo.com/cloud/#/download"));
                return;
            }

            if (InstallGameManager.Instance.TryGetInstallService(CurrentGameBiz, out var _))
            {
                WeakReferenceMessenger.Default.Send(new ShowInstallGameControllerFlyoutMessage());
                return;
            }

            var dialog = new InstallGameDialog
            {
                CurrentGameBiz = this.CurrentGameBiz,
                InstallationPath = this.InstallPath!,
                XamlRoot = this.XamlRoot,
            };
            await dialog.ShowAsync();
            CheckGameVersion();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Install game");
        }
    }



    [RelayCommand]
    private async Task PredownloadAsync()
    {
        try
        {
            if (InstallGameManager.Instance.TryGetInstallService(CurrentGameBiz, out var _))
            {
                WeakReferenceMessenger.Default.Send(new ShowInstallGameControllerFlyoutMessage());
                return;
            }

            if (Directory.Exists(hardLinkPath))
            {
                var dialog = new ContentDialog
                {
                    Title = Lang.LauncherPage_HardLink,
                    Content = Lang.GameLauncherPage_HardLinkNotSupportPredownload,
                    PrimaryButtonText = Lang.Common_Confirm,
                    CloseButtonText = Lang.Common_Cancel,
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot,
                };
                if (await dialog.ShowAsync() is ContentDialogResult.Primary)
                {
                    if (InstallGameManager.Instance.TryGetInstallService(hardLinkGameBiz, out var _))
                    {
                        WeakReferenceMessenger.Default.Send(new ShowInstallGameControllerFlyoutMessage());
                        return;
                    }
                    if (await CheckWritePermissionAsync())
                    {
                        var service = InstallGameService.FromGameBiz(hardLinkGameBiz);
                        await service.InitializeAsync(hardLinkGameBiz, _gameLauncherService.GetGameInstallPath(hardLinkGameBiz)!);
                        await service.StartPredownloadAsync();
                        InstallGameManager.Instance.AddInstallService(service);
                    }
                }
                return;
            }

            if (await CheckWritePermissionAsync())
            {
                var service = InstallGameService.FromGameBiz(CurrentGameBiz);
                await service.InitializeAsync(CurrentGameBiz, InstallPath!);
                await service.StartPredownloadAsync();
                InstallGameManager.Instance.AddInstallService(service);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Predownload");
        }
    }



    [RelayCommand]
    private async Task UpdateAsync()
    {
        try
        {
            if (InstallGameManager.Instance.TryGetInstallService(CurrentGameBiz, out var _))
            {
                WeakReferenceMessenger.Default.Send(new ShowInstallGameControllerFlyoutMessage());
                return;
            }

            if (Directory.Exists(hardLinkPath))
            {
                if (await IsHardLinkTargetLatestVersionAsync())
                {
                    if (await CheckWritePermissionAsync())
                    {
                        var service = InstallGameService.FromGameBiz(CurrentGameBiz);
                        await service.InitializeAsync(CurrentGameBiz, InstallPath!);
                        await service.StartHardLinkAsync(hardLinkGameBiz);
                        InstallGameManager.Instance.AddInstallService(service);
                    }
                }
                else
                {
                    var dialog = new ContentDialog
                    {
                        Title = Lang.LauncherPage_HardLink,
                        Content = Lang.GameLauncherPage_HardLinkUpdateToLatest,
                        PrimaryButtonText = Lang.Common_Confirm,
                        CloseButtonText = Lang.Common_Cancel,
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = this.XamlRoot,
                    };
                    if (await dialog.ShowAsync() is ContentDialogResult.Primary)
                    {
                        if (InstallGameManager.Instance.TryGetInstallService(hardLinkGameBiz, out var _))
                        {
                            WeakReferenceMessenger.Default.Send(new ShowInstallGameControllerFlyoutMessage());
                            return;
                        }
                        if (await CheckWritePermissionAsync())
                        {
                            var service = InstallGameService.FromGameBiz(hardLinkGameBiz);
                            await service.InitializeAsync(hardLinkGameBiz, _gameLauncherService.GetGameInstallPath(hardLinkGameBiz)!);
                            await service.StartUpdateGameAsync();
                            InstallGameManager.Instance.AddInstallService(service);
                        }
                    }
                }
            }
            else
            {
                if (await CheckWritePermissionAsync())
                {
                    var service = InstallGameService.FromGameBiz(CurrentGameBiz);
                    await service.InitializeAsync(CurrentGameBiz, InstallPath!);
                    await service.StartUpdateGameAsync();
                    InstallGameManager.Instance.AddInstallService(service);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update");
        }
    }



    [RelayCommand]
    private async Task RepairAsync()
    {
        try
        {
            if (InstallGameManager.Instance.TryGetInstallService(CurrentGameBiz, out var _))
            {
                WeakReferenceMessenger.Default.Send(new ShowInstallGameControllerFlyoutMessage());
                return;
            }

            if (await CheckWritePermissionAsync())
            {
                var service = InstallGameService.FromGameBiz(CurrentGameBiz);
                await service.InitializeAsync(CurrentGameBiz, InstallPath!);
                if (Directory.Exists(hardLinkPath))
                {
                    await service.StartHardLinkAsync(hardLinkGameBiz);
                }
                else
                {
                    await service.StartRepairGameAsync();
                }
                InstallGameManager.Instance.AddInstallService(service);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Repair");
        }
    }




    #endregion




    #region Download Game




    private async Task<bool> CheckRedirectInstanceAsync()
    {
        var instance = App.FindInstanceForKey($"download_game_{CurrentGameBiz}");
        if (instance != null)
        {
            await instance.RedirectActivationToAsync(instance.GetActivatedEventArgs());
            return true;
        }
        else
        {
            return false;
        }
    }




    [RelayCommand]
    private async Task DownloadGameAsync()
    {
        try
        {
            if (CurrentGameBiz == GameBiz.clgm_cn)
            {
                await Launcher.LaunchUriAsync(new Uri("https://ys.mihoyo.com/cloud/#/download"));
                return;
            }

            if (await CheckRedirectInstanceAsync())
            {
                return;
            }

            string? temp_install_path = null;

            if (Directory.Exists(InstallPath))
            {
                if (LocalGameVersion is null)
                {
                    var folderDialog = new ContentDialog
                    {
                        Title = Lang.LauncherPage_SelectInstallFolder,
                        // 以下文件夹中有尚未完成的下载任务
                        Content = $"""
                        {Lang.LauncherPage_TheFollowingFolderContainsUnfinishedDownloadTasks}

                        {InstallPath}
                        """,
                        PrimaryButtonText = Lang.Common_Continue,
                        SecondaryButtonText = Lang.LauncherPage_Reselect,
                        CloseButtonText = Lang.Common_Cancel,
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = this.XamlRoot,
                    };
                    var result = await folderDialog.ShowAsync();
                    if (result is ContentDialogResult.Primary)
                    {
                        temp_install_path = InstallPath;
                    }
                    else if (result is ContentDialogResult.Secondary)
                    {
                        temp_install_path = await FileDialogHelper.PickFolderAsync(MainWindow.Current.WindowHandle);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    temp_install_path = InstallPath;
                }
            }
            else
            {
                var folderDialog = new ContentDialog
                {
                    Title = Lang.LauncherPage_SelectInstallFolder,
                    // 请选择一个空文件夹用于安装游戏，或者定位已安装游戏的文件夹。
                    Content = string.Format(Lang.LauncherPage_SelectInstallFolderDesc, _gameLauncherService.GetGameExeName(CurrentGameBiz)),
                    PrimaryButtonText = Lang.Common_Select,
                    SecondaryButtonText = Lang.Common_Cancel,
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot,
                };
                if (await folderDialog.ShowAsync() is ContentDialogResult.Primary)
                {
                    temp_install_path = await FileDialogHelper.PickFolderAsync(MainWindow.Current.WindowHandle);
                }
            }

            if (!Directory.Exists(temp_install_path))
            {
                return;
            }

            if (Path.GetPathRoot(temp_install_path) == temp_install_path)
            {
                NotificationBehavior.Instance.Warning(Lang.LauncherPage_PleaseDoNotSelectTheRootDirectoryOfADrive);
                return;
            }

            InstallPath = temp_install_path;

            var downloadResource = await _gamePackageService.GetNeedDownloadGamePackageResourceAsync(CurrentGameBiz, InstallPath);
            if (downloadResource is null)
            {
                CheckGameVersion();
                return;
            }
            var lang = await _gamePackageService.GetVoiceLanguageAsync(CurrentGameBiz, InstallPath);
            if (lang is AudioLanguage.None)
            {
                lang = AudioLanguage.All;
            }

            var content = new DownloadGameDialog
            {
                GameBiz = CurrentGameBiz,
                LanguageType = lang,
                GameResource = _gamePackageService.GetDownloadGameResourceAsync(downloadResource, InstallPath),
                PreDownloadMode = IsPreInstallButtonEnable,
            };
            var dialog = new ContentDialog
            {
                Title = IsUpdateGameButtonEnable ? Lang.LauncherPage_UpdateGame : (IsPreInstallButtonEnable ? Lang.LauncherPage_PreInstall : Lang.LauncherPage_InstallGame),
                Content = content,
                PrimaryButtonText = Lang.Common_Start,
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };

            if (await dialog.ShowAsync() is ContentDialogResult.Primary)
            {
                lang = content.LanguageType;
                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (!File.Exists(exe))
                {
                    exe = Path.Combine(AppContext.BaseDirectory, "Starward.exe");
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = true,
                    Arguments = $"""{(content.EnableRepairMode ? "repair" : "download")} --biz {CurrentGameBiz} --loc "{InstallPath}" --lang {(int)lang} """,
                    Verb = "runas",
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start to download game");
        }
    }




    [RelayCommand]
    private async Task PreDownloadGameAsync()
    {
        try
        {
            if (await CheckRedirectInstanceAsync())
            {
                return;
            }
            if (IsPreDownloadOK)
            {
                var dialog = new ContentDialog
                {
                    Title = Lang.LauncherPage_PreInstall,
                    // 预下载已完成，是否校验文件？
                    Content = Lang.LauncherPage_WouldYouLikeToVerifyTheFiles,
                    PrimaryButtonText = Lang.LauncherPage_StartVerification,
                    SecondaryButtonText = Lang.Common_Cancel,
                    DefaultButton = ContentDialogButton.Secondary,
                    XamlRoot = this.XamlRoot,
                };
                if (await dialog.ShowAsync() is ContentDialogResult.Primary)
                {
                    var lang = await _gamePackageService.GetVoiceLanguageAsync(CurrentGameBiz, InstallPath);
                    var exe = Process.GetCurrentProcess().MainModule?.FileName;
                    if (!File.Exists(exe))
                    {
                        exe = Path.Combine(AppContext.BaseDirectory, "Starward.exe");
                    }
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exe,
                        UseShellExecute = true,
                        Arguments = $"""download --biz {CurrentGameBiz} --loc "{InstallPath}" --lang {(int)lang} """,
                        Verb = "runas",
                    });
                }
            }
            else
            {
                await DownloadGameAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pre download game");
        }
    }




    [RelayCommand]
    private async Task RepairGameAsync()
    {
        try
        {
            var lang = await _gamePackageService.GetVoiceLanguageAsync(CurrentGameBiz, InstallPath);
            var exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (!File.Exists(exe))
            {
                exe = Path.Combine(AppContext.BaseDirectory, "Starward.exe");
            }
            var control = new DownloadGameDialog
            {
                GameBiz = CurrentGameBiz,
                LanguageType = lang,
                RepairMode = true,
            };
            var dialog = new ContentDialog
            {
                Title = Lang.LauncherPage_RepairGame,
                Content = control,
                PrimaryButtonText = Lang.Common_Confirm,
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };
            if (await dialog.ShowAsync() is not ContentDialogResult.Primary)
            {
                return;
            }
            lang = control.LanguageType;
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true,
                Arguments = $"""repair --biz {CurrentGameBiz} --loc "{InstallPath}" --lang {(int)lang} """,
                Verb = "runas",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start repair game");
        }
    }





    [RelayCommand]
    private async Task UninstallGameAsync()
    {
        try
        {
            if (_gameService.GetGameProcess(CurrentGameBiz) != null)
            {
                NotificationBehavior.Instance.Warning(Lang.LauncherPage_GameIsRunning);
                return;
            }
            UninstallStep enableSteps = UninstallStep.CleanRegistry | UninstallStep.DeleteTempFiles;
            if (Directory.Exists(InstallPath))
            {
                enableSteps |= UninstallStep.DeleteGameAssets;
            }
            if (LocalGameVersion != null)
            {
                enableSteps |= UninstallStep.BackupScreenshot;
            }
            var control = new UninstallGameDialog
            {
                Steps = enableSteps,
                EnableSteps = enableSteps,
            };
            var dialog = new ContentDialog
            {
                Title = Lang.LauncherPage_UninstallGame,
                Content = control,
                PrimaryButtonText = Lang.Common_Confirm,
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot,
            };
            if (await dialog.ShowAsync() is ContentDialogResult.Primary)
            {
                UninstallStep steps = control.Steps;
                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (!File.Exists(exe))
                {
                    exe = Path.Combine(AppContext.BaseDirectory, "Starward.exe");
                }
                string argu = $"""uninstall --biz {CurrentGameBiz} --loc "{InstallPath}" --steps {(int)steps}""";
                _logger.LogInformation("Start to uninstall game with argu: {argu}", argu);
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = true,
                    Arguments = argu,
                    Verb = "runas",
                });
                if (p != null)
                {
                    await p.WaitForExitAsync();
                    if (p.ExitCode != 0)
                    {
                        _logger.LogError("Uninstallation error, exit code {code}", p.ExitCode);
                        NotificationBehavior.Instance.Warning(Lang.LauncherPage_UninstallationError, Lang.LauncherPage_PleaseCheckTheRelatedLogs);
                    }
                    else
                    {
                        _logger.LogInformation("Uninstall finished.");
                        NotificationBehavior.Instance.Success(Lang.LauncherPage_UninstallationCompleted);
                    }
                    CheckGameVersion();
                    GetGameAccount();
                }
                else
                {
                    _logger.LogWarning("Uninstall process not started.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uninstall game");
        }
    }




    #endregion




    #region Game Setting





    [ObservableProperty]
    private bool enableThirdPartyTool;
    partial void OnEnableThirdPartyToolChanged(bool value)
    {
        AppConfig.SetEnableThirdPartyTool(CurrentGameBiz, value);
    }


    [ObservableProperty]
    private string? thirdPartyToolPath;
    partial void OnThirdPartyToolPathChanged(string? value)
    {
        AppConfig.SetThirdPartyToolPath(CurrentGameBiz, value);
    }



    [ObservableProperty]
    private string? startGameArgument;
    partial void OnStartGameArgumentChanged(string? value)
    {
        AppConfig.SetStartArgument(CurrentGameBiz, value);
    }



    [ObservableProperty]
    private bool ignoreRunningGame = AppConfig.IgnoreRunningGame;
    partial void OnIgnoreRunningGameChanged(bool value)
    {
        AppConfig.IgnoreRunningGame = value;
        UpdateGameState();
    }



    [RelayCommand]
    private async Task ChangeGameInstallPathAsync()
    {
        try
        {
            var folder = await FileDialogHelper.PickFolderAsync(MainWindow.Current.WindowHandle);
            if (!Directory.Exists(folder))
            {
                return;
            }
            _logger.LogInformation("Change game install path ({biz}): {path}", CurrentGameBiz, folder);
            InstallPath = folder;
            AppConfig.SetGameInstallPathRemovable(CurrentGameBiz, DriveHelper.IsDeviceRemovableOrOnUSB(folder));
            CheckGameVersion();
            UpdateGameState();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change game install path ({biz})", CurrentGameBiz);
        }
    }



    [RelayCommand]
    private async Task ChangeThirdPartyPathAsync()
    {
        try
        {
            var file = await FileDialogHelper.PickSingleFileAsync(MainWindow.Current.WindowHandle);
            if (File.Exists(file))
            {
                ThirdPartyToolPath = file;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change third party tool path ({biz})", CurrentGameBiz);
        }
    }


    [RelayCommand]
    private async Task OpenGameInstallFolderAsync()
    {
        try
        {
            if (Directory.Exists(InstallPath))
            {
                await Windows.System.Launcher.LaunchFolderPathAsync(InstallPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open game install folder {folder}", InstallPath);
        }
    }



    [RelayCommand]
    private async Task OpenThirdPartyToolFolderAsync()
    {
        try
        {
            if (File.Exists(ThirdPartyToolPath))
            {
                var folder = Path.GetDirectoryName(ThirdPartyToolPath);
                var file = await StorageFile.GetFileFromPathAsync(ThirdPartyToolPath);
                var option = new FolderLauncherOptions();
                option.ItemsToSelect.Add(file);
                await Windows.System.Launcher.LaunchFolderPathAsync(folder, option);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open third party tool folder {folder}", ThirdPartyToolPath);
        }
    }


    [RelayCommand]
    private void DeleteGameInstallPath()
    {
        InstallPath = null;
        AppConfig.SetGameInstallPathRemovable(CurrentGameBiz, false);
        CheckGameVersion();
    }


    [RelayCommand]
    private void DeleteThirdPartyToolPath()
    {
        ThirdPartyToolPath = null;
    }




    private void TextBlock_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        if (sender.FontSize == 14 && sender.IsTextTrimmed)
        {
            sender.FontSize = 12;
        }
    }



    #endregion




    #region Background Setting



    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VideoBgVolumeButtonIcon))]
    private int videoBgVolume = AppConfig.VideoBgVolume;
    partial void OnVideoBgVolumeChanged(int value)
    {
        if (MainWindow.Current?.mainPage is not null)
        {
            MainWindow.Current.mainPage.VideoBgVolume = value;
        }
    }


    [ObservableProperty]
    private bool useOneBg = AppConfig.UseOneBg;
    partial void OnUseOneBgChanged(bool value)
    {
        AppConfig.UseOneBg = value;
        AppConfig.SetCustomBg(CurrentGameBiz, CustomBg);
        AppConfig.SetEnableCustomBg(CurrentGameBiz, EnableCustomBg);

    }


    public string VideoBgVolumeButtonIcon => VideoBgVolume switch
    {
        > 66 => "\uE995",
        > 33 => "\uE994",
        > 1 => "\uE993",
        _ => "\uE992",
    };


    private int notMuteVolume = 100;

    [RelayCommand]
    private void Mute()
    {
        if (VideoBgVolume > 0)
        {
            notMuteVolume = VideoBgVolume;
            VideoBgVolume = 0;
        }
        else
        {
            VideoBgVolume = notMuteVolume;
        }
    }


    [ObservableProperty]
    private bool enableCustomBg;
    partial void OnEnableCustomBgChanged(bool value)
    {
        AppConfig.SetEnableCustomBg(CurrentGameBiz, value);
        WeakReferenceMessenger.Default.Send(new UpdateBackgroundImageMessage(true));
    }


    [ObservableProperty]
    private string? customBg;


    [RelayCommand]
    private async Task ChangeCustomBgAsync()
    {
        var file = await _launcherContentService.ChangeCustomBgAsync();
        if (file is not null)
        {
            CustomBg = file;
            AppConfig.SetCustomBg(CurrentGameBiz, file);
            WeakReferenceMessenger.Default.Send(new UpdateBackgroundImageMessage(true));
        }
    }


    [RelayCommand]
    private async Task OpenCustomBgAsync()
    {
        await _launcherContentService.OpenCustomBgAsync(CustomBg);
    }


    [RelayCommand]
    private void DeleteCustomBg()
    {
        AppConfig.SetCustomBg(CurrentGameBiz, null);
        CustomBg = null;
        WeakReferenceMessenger.Default.Send(new UpdateBackgroundImageMessage(true));
    }




    #endregion




    #region Side Menu



    [RelayCommand]
    private void OpenCloseGameSetting()
    {
        SplitView_Content.IsPaneOpen = !SplitView_Content.IsPaneOpen;
    }


    [RelayCommand]
    private void OpenGameNoticesWindow()
    {
        WindowManager.Active(new GameNoticesWindow { GameBiz = CurrentGameBiz });
    }


    [RelayCommand]
    private void OpenGamePackageWindow()
    {
        MainWindow.Current.OverlayFrameNavigateTo(typeof(GameResourcePage), CurrentGameBiz);
    }


    [RelayCommand]
    private async Task DebugAsync()
    {
        try
        {

        }
        catch (Exception ex)
        {

        }
    }


    #endregion



}
