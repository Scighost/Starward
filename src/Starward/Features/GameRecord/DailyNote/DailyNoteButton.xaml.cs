using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.BH3.DailyNote;
using Starward.Core.GameRecord.Genshin.DailyNote;
using Starward.Core.GameRecord.StarRail.DailyNote;
using Starward.Core.GameRecord.ZZZ.DailyNote;
using Starward.Core.HoYoPlay;
using Starward.Features.GameRecord.BH3;
using Starward.Features.GameRecord.Genshin;
using Starward.Features.GameRecord.StarRail;
using Starward.Features.GameRecord.ZZZ;
using System;
using System.Threading.Tasks;


namespace Starward.Features.GameRecord.DailyNote;

[INotifyPropertyChanged]
public sealed partial class DailyNoteButton : UserControl
{

    private readonly ILogger<DailyNoteButton> _logger = AppConfig.GetLogger<DailyNoteButton>();


    private readonly GameRecordBH3Service _gameRecordBH3Service = AppConfig.GetService<GameRecordBH3Service>();

    private readonly GameRecordGenshinService _gameRecordGenshinService = AppConfig.GetService<GameRecordGenshinService>();

    private readonly GameRecordStarRailService _gameRecordStarRailService = AppConfig.GetService<GameRecordStarRailService>();

    private readonly GameRecordZZZService _gameRecordZZZService = AppConfig.GetService<GameRecordZZZService>();

    private readonly GameRecordRoleService _gameRecordRoleService = AppConfig.GetService<GameRecordRoleService>();


    public DailyNoteButton()
    {
        this.InitializeComponent();
        this.Visibility = Visibility.Collapsed;
    }



    public GameId CurrentGameId
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(IsBH3Enabled));
                OnPropertyChanged(nameof(IsGenshinEnabled));
                OnPropertyChanged(nameof(IsStarRailEnabled));
                OnPropertyChanged(nameof(IsZZZEnabled));
            }
        }
    }



    public bool IsBH3Enabled => CurrentGameId?.GameBiz.Game is GameBiz.bh3;

    public bool IsGenshinEnabled => CurrentGameId?.GameBiz.Game is GameBiz.hk4e;

    public bool IsStarRailEnabled => CurrentGameId?.GameBiz.Game is GameBiz.hkrpg;

    public bool IsZZZEnabled => CurrentGameId?.GameBiz.Game is GameBiz.nap;



    private GameRecordRole? GameRecordRole { get; set => SetProperty(ref field, value); }


    public BH3DailyNote BH3DailyNote { get; set => SetProperty(ref field, value); }

    public GenshinDailyNote GenshinDailyNote { get; set => SetProperty(ref field, value); }

    public StarRailDailyNote StarRailDailyNote { get; set => SetProperty(ref field, value); }

    public ZZZDailyNote ZZZDailyNote { get; set => SetProperty(ref field, value); }


    public string? ErrorMessage { get; set => SetProperty(ref field, value); }



    private void Button_DailyNote_Loaded(object sender, RoutedEventArgs e)
    {
        if (CurrentGameId is null)
        {
            return;
        }
        if (!GameFeatureConfig.FromGameId(CurrentGameId).SupportDailyNote)
        {
            return;
        }
        RefreshDailyNoteCommand.Execute(false);
    }



    private void Button_DailyNote_Unloaded(object sender, RoutedEventArgs e)
    {
        GameRecordRole = null;
        BH3DailyNote = null!;
        GenshinDailyNote = null!;
        StarRailDailyNote = null!;
        ZZZDailyNote = null!;
    }



    [RelayCommand]
    private async Task RefreshDailyNoteAsync(bool? forceUpdate)
    {
        try
        {
            if (!forceUpdate.HasValue)
            {
                forceUpdate = true;
            }
            GameBiz gameBiz = CurrentGameId.GameBiz;
            GameRecordRole = GameRecordRoleService.GetLastSelectGameRecordRoleOrTheFirstOne(gameBiz);
            if (GameRecordRole is not null)
            {
                GameRecordRole role = GameRecordRole;
                this.Visibility = Visibility.Visible;
                ErrorMessage = null;
                if (IsBH3Enabled)
                {
                    BH3DailyNote = await GetDailyNoteWithRetryAsync(role, () => _gameRecordBH3Service.GetBH3DailyNoteAsync(role, forceUpdate.Value));
                }
                if (IsGenshinEnabled)
                {
                    GenshinDailyNote = await GetDailyNoteWithRetryAsync(role, () => _gameRecordGenshinService.GetGenshinDailyNoteAsync(role, forceUpdate.Value));
                }
                if (IsStarRailEnabled)
                {
                    StarRailDailyNote = await GetDailyNoteWithRetryAsync(role, () => _gameRecordStarRailService.GetStarRailDailyNoteAsync(role, forceUpdate.Value));
                }
                if (IsZZZEnabled)
                {
                    ZZZDailyNote = await GetDailyNoteWithRetryAsync(role, () => _gameRecordZZZService.GetZZZDailyNoteAsync(role, forceUpdate.Value));
                }
            }
        }
        catch (Exception ex)
        {
            if (ex is miHoYoApiException)
            {
                ErrorMessage = $"Error Code: {ex.Message}";
            }
            _logger.LogError(ex, "Refresh daily note failed (Biz: {GameBiz}, Server: {GameServer}, Uid: {Uid})", CurrentGameId?.GameBiz, GameRecordRole?.Region, GameRecordRole?.Uid);
        }
    }





    /// <summary>
    /// 获取每日便笺，登录失效时通过关联的通行证账号刷新 Cookie 后重试
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="role"></param>
    /// <param name="getDailyNote"></param>
    /// <returns></returns>
    private async Task<T> GetDailyNoteWithRetryAsync<T>(GameRecordRole role, Func<Task<T>> getDailyNote)
    {
        try
        {
            return await getDailyNote();
        }
        catch (miHoYoApiException ex) when (ex.ReturnCode is -100 or 10001)
        {
            await _gameRecordRoleService.UpdateGameRoleAsync(role);
            return await getDailyNote();
        }
    }




    public static string ZZZMemberCardRemainingDaysToString(int remainingDays)
    {
        return string.Format(Lang.DailyNoteButton_0DaySRemaining, remainingDays);
    }





}
