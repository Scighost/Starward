using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Helpers;
using Starward.Models;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls;

[INotifyPropertyChanged]
public sealed partial class AccountSwitcher : UserControl
{

    public GameBiz CurrentGameBiz { get; set; }


    private readonly ILogger<AccountSwitcher> _logger = AppConfig.GetLogger<AccountSwitcher>();

    //_gameAccountService
    private readonly GameAccountService _gameAccountService = AppConfig.GetService<GameAccountService>();


    public AccountSwitcher()
    {
        this.InitializeComponent();
        Loaded += (s, e) => UpdateGameAccount();
    }


    public bool IsGameRunning { get; set; }


    public event EventHandler<long?> GameAccountChanged;



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



    public void UpdateGameAccount()
    {
        try
        {
            if (AppConfig.DisableGameAccountSwitcher || CurrentGameBiz.IsBilibili() || CurrentGameBiz.ToGame() == GameBiz.nap)
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
                OnGameAccountChanged();
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
                OnGameAccountChanged();
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
                UpdateGameAccount();
                OnGameAccountChanged();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete game account");
        }
    }



    private void OnGameAccountChanged()
    {
        long? uid = null;
        if (GameAccountList?.FirstOrDefault(x => x.IsLogin) is GameAccount account)
        {
            uid = account.Uid;
        }
        GameAccountChanged?.Invoke(this, uid);
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






}
