using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Starward.Features.GameAccount;

[INotifyPropertyChanged]
public sealed partial class GameAccountSwitcher : UserControl
{

    private readonly ILogger<GameAccountService> _logger = AppConfig.GetLogger<GameAccountService>();


    private readonly GameAccountService _gameAccountService = AppConfig.GetService<GameAccountService>();


    public GameAccountSwitcher()
    {
        this.InitializeComponent();
        this.Loaded += GameAccountSwitcher_Loaded;
        this.Unloaded += GameAccountSwitcher_Unloaded;
    }



    private void GameAccountSwitcher_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateGameAccount();
    }


    private void GameAccountSwitcher_Unloaded(object sender, RoutedEventArgs e)
    {
        SelectGameAccount = null;
        GameAccountList = null!;
        suggestionUids = null!;
    }



    public GameBiz CurrentGameBiz { get; set; }



    public List<GameAccount> GameAccountList { get; set => SetProperty(ref field, value); }


    public GameAccount? SelectGameAccount
    {
        get; set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                CanChangeGameAccount = true;
            }
        }
    }


    public bool CanChangeGameAccount { get; set => SetProperty(ref field, value); }


    public void UpdateGameAccount()
    {
        try
        {
            GameAccountList = _gameAccountService.GetGameAccounts(CurrentGameBiz);
            SelectGameAccount = GameAccountList.FirstOrDefault();
            CanChangeGameAccount = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot get game account ({biz})", CurrentGameBiz);
        }
    }


    [RelayCommand]
    private void ChangeGameAccount()
    {
        try
        {
            if (SelectGameAccount is not null)
            {
                _gameAccountService.ChangeGameAccount(SelectGameAccount);
                CanChangeGameAccount = false;
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
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete game account");
        }
    }



    private TextBox TextBox_AccountUid;


    private List<string> suggestionUids;


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
                    TextBox_AccountUid.BeforeTextChanging += TextBox_AccountUid_BeforeTextChanging;
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


    private void TextBox_AccountUid_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
    {
        args.Cancel = !args.NewText.All(x => char.IsDigit(x));
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
