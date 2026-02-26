using Starward.Core.GameRecord.BH3.DailyNote;
using Starward.Core.GameRecord.Genshin.DailyNote;
using Starward.Core.GameRecord.Genshin.ImaginariumTheater;
using Starward.Core.GameRecord.Genshin.SpiralAbyss;
using Starward.Core.GameRecord.Genshin.StygianOnslaught;
using Starward.Core.GameRecord.Genshin.TravelersDiary;
using Starward.Core.GameRecord.StarRail.ApocalypticShadow;
using Starward.Core.GameRecord.StarRail.ChallengePeak;
using Starward.Core.GameRecord.StarRail.DailyNote;
using Starward.Core.GameRecord.StarRail.ForgottenHall;
using Starward.Core.GameRecord.StarRail.PureFiction;
using Starward.Core.GameRecord.StarRail.SimulatedUniverse;
using Starward.Core.GameRecord.StarRail.TrailblazeCalendar;
using Starward.Core.GameRecord.ZZZ.DailyNote;
using Starward.Core.GameRecord.ZZZ.DeadlyAssault;
using Starward.Core.GameRecord.ZZZ.GachaRecord;
using Starward.Core.GameRecord.ZZZ.InterKnotReport;
using Starward.Core.GameRecord.ZZZ.ShiyuDefense;
using Starward.Core.GameRecord.ZZZ.ThresholdSimulation;
using Starward.Core.GameRecord.ZZZ.UpgradeGuide;

namespace Starward.Core.GameRecord;

public class HoyolabClient : GameRecordClient
{


    public override string UAContent => $"Mozilla/5.0 (Linux; Android 13; Pixel 5 Build/TQ3A.230901.001; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/118.0.0.0 Mobile Safari/537.36 miHoYoBBSOversea/{AppVersion}";

    public override string AppVersion => "3.13.0";

    protected override string ApiSalt => "okr4obncj8bw5a65hbnn5oo6ixjc3l9w";

    protected override string ApiSalt2 => "h4c1d6ywfq5bsbnbhm1bzq7bxzzv6srt";


    private string language;
    public string Language { get => LanguageUtil.FilterLanguage(language); set => language = LanguageUtil.FilterLanguage(value); }



    public HoyolabClient(HttpClient? httpClient = null) : base(httpClient)
    {

    }




    protected override Task<T> CommonSendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        request.Headers.Add(x_rpc_language, Language);
        request.Headers.Add("x-rpc-lang", Language);
        return base.CommonSendAsync<T>(request, cancellationToken);
    }





    /// <summary>
    /// 米游社账号信息
    /// </summary>
    /// <param name="cookie"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public override async Task<GameRecordUser> GetGameRecordUserAsync(string cookie, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cookie))
        {
            throw new ArgumentNullException(nameof(cookie));
        }
        var request = new HttpRequestMessage(HttpMethod.Get, "https://bbs-api-os.hoyolab.com/community/user/wapi/getUserFullInfo");
        request.Headers.Add(Cookie, cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(DS, CreateSecret());
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_device_id, DeviceId);
        request.Headers.Add(x_rpc_client_type, "5");
        var data = await CommonSendAsync<GameRecordUserWrapper>(request, cancellationToken);
        data.User.IsHoyolab = true;
        data.User.Cookie = cookie;
        return data.User;
    }



    /// <summary>
    /// 所有游戏账号
    /// </summary>
    /// <param name="cookie"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<List<GameRecordRole>> GetAllGameRolesAsync(string cookie, CancellationToken cancellationToken = default)
    {
        Lock @lock = new();
        var list = new List<GameRecordRole>();
        await Parallel.ForEachAsync([GameBiz.bh3_global, GameBiz.hk4e_global, GameBiz.hkrpg_global, GameBiz.nap_global], cancellationToken, async (GameBiz gameBiz, CancellationToken token) =>
        {
            var roles = await GetGameRolesAsync(cookie, gameBiz, token);
            if (roles.Count > 0)
            {
                lock (@lock)
                {
                    list.AddRange(roles);
                }
            }
        });
        return list;
    }



    /// <summary>
    /// 获取游戏账号信息
    /// </summary>
    /// <param name="cookie"></param>
    /// <param name="gameBiz"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<List<GameRecordRole>> GetGameRolesAsync(string cookie, GameBiz gameBiz, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cookie))
        {
            throw new ArgumentNullException(nameof(cookie));
        }
        string url = $"https://api-account-os.hoyolab.com/binding/api/getUserGameRolesByCookieToken?game_biz={gameBiz}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, cookie);
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        request.Headers.Add(Referer, "https://act.hoyolab.com");
        var data = await CommonSendAsync<GameRecordRoleWrapper>(request, cancellationToken);
        if (data.List is not null)
        {
            foreach (var item in data.List)
            {
                item.Cookie = cookie;
                try
                {
                    item.HeadIcon = await GetGameRoleHeadIconAsync(item, cancellationToken);
                }
                catch (miHoYoApiException) { }

            }
        }
        return data.List ?? new List<GameRecordRole>();
    }



    /// <summary>
    /// 获取游戏账号头像
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected override async Task<string> GetGameRoleHeadIconAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        string url = role.GameBiz switch
        {
            GameBiz.bh3_global => $"https://bbs-api-os.hoyolab.com/game_record/app/honkai3rd/api/index?server={role.Region}&role_id={role.Uid}",
            GameBiz.hk4e_global => $"https://sg-public-api.hoyolab.com/event/game_record/app/genshin/api/index?avatar_list_type=1&server={role.Region}&role_id={role.Uid}",
            GameBiz.hkrpg_global => $"https://sg-public-api.hoyolab.com/event/game_record/app/hkrpg/api/index?server={role.Region}&role_id={role.Uid}",
            GameBiz.nap_global => $"https://sg-public-api.hoyolab.com/event/game_record_zzz/api/zzz/index?server={role.Region}&role_id={role.Uid}",
            _ => throw new ArgumentOutOfRangeException($"Unsupport GameBiz: {role.GameBiz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(x_rpc_device_id, DeviceId);
        request.Headers.Add(x_rpc_device_fp, DeviceFp);
        var data = await CommonSendAsync<GameRecordIndex>(request, cancellationToken);
        return data.HeadIcon;
    }



    /// <summary>
    /// 获取设备指纹信息
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<string> GetDeviceFpAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(DeviceFp);
    }




    #region BH3


    /// <summary>
    /// 崩坏3实时便笺
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<BH3DailyNote> GetBH3DailyNoteAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        string url = $"https://bbs-api-os.hoyolab.com/game_record/app/honkai3rd/api/note?server={role.Region}&role_id={role.Uid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://act.mihoyo.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        return await CommonSendAsync<BH3DailyNote>(request, cancellationToken);
    }



    #endregion




    #region Genshin


    /// <summary>
    /// 获取原神账号信息
    /// </summary>
    /// <param name="cookie"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>

    public override async Task<List<GameRecordRole>> GetGenshinGameRolesAsync(string cookie, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cookie))
        {
            throw new ArgumentNullException(nameof(cookie));
        }
        const string url = "https://api-account-os.hoyolab.com/binding/api/getUserGameRolesByCookieToken?game_biz=hk4e_global";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, cookie);
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        request.Headers.Add(Referer, "https://act.hoyolab.com");
        var data = await CommonSendAsync<GameRecordRoleWrapper>(request, cancellationToken);
        data.List?.ForEach(x => x.Cookie = cookie);
        return data.List ?? new List<GameRecordRole>();
    }


    /// <summary>
    /// 深境螺旋
    /// </summary>
    /// <param name="role"></param>
    /// <param name="schedule">1当期，2上期</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<SpiralAbyssInfo> GetSpiralAbyssInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var url = $"https://bbs-api-os.hoyolab.com/game_record/app/genshin/api/spiralAbyss?role_id={role.Uid}&server={role.Region}&schedule_type={schedule}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var data = await CommonSendAsync<SpiralAbyssInfo>(request, cancellationToken);
        data.Uid = role.Uid;
        return data;
    }


    /// <summary>
    /// 旅行札记总览
    /// </summary>
    /// <param name="role"></param>
    /// <param name="month">0 当前月</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>

    public override async Task<TravelersDiarySummary> GetTravelsDiarySummaryAsync(GameRecordRole role, int month = 0, CancellationToken cancellationToken = default)
    {
        var url = $"https://sg-hk4e-api.hoyolab.com/event/ysledgeros/month_info?month={month}&region={role.Region}&uid={role.Uid}&lang={Language}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        return await CommonSendAsync<TravelersDiarySummary>(request, cancellationToken);
    }


    /// <summary>
    /// 旅行札记收入详情
    /// </summary>
    /// <param name="role"></param>
    /// <param name="month"></param>
    /// <param name="type">1原石，2摩拉</param>
    /// <param name="page">从1开始</param>
    /// <param name="limit">最大100</param>
    /// <param name="cancellationToken"></param>
    /// <returns>返回一页收入记录</returns>
    public override async Task<TravelersDiaryDetail> GetTravelsDiaryDetailByPageAsync(GameRecordRole role, int month, int type, int page, int limit = 100, CancellationToken cancellationToken = default)
    {
        var url = $"https://sg-hk4e-api.hoyolab.com/event/ysledgeros/month_detail?month={month}&current_page={page}&type={type}&region={role.Region}&uid={role.Uid}&lang={Language}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var data = await CommonSendAsync<TravelersDiaryDetail>(request, cancellationToken);
        foreach (var item in data.List)
        {
            item.Type = type;
        }
        return data;
    }


    /// <summary>
    /// 旅行札记收入详情
    /// </summary>
    /// <param name="role"></param>
    /// <param name="month"></param>
    /// <param name="type">1原石，2摩拉</param>
    /// <param name="limit">最大100</param>
    /// <param name="cancellationToken"></param>
    /// <returns>返回该月所有收入记录</returns>
    public override async Task<TravelersDiaryDetail> GetTravelsDiaryDetailAsync(GameRecordRole role, int month, int type, int limit = 100, CancellationToken cancellationToken = default)
    {
        var data = await GetTravelsDiaryDetailByPageAsync(role, month, type, 1, limit, cancellationToken);
        if (data.List.Count < limit)
        {
            return data;
        }
        for (int i = 2; ; i++)
        {
            var addData = await GetTravelsDiaryDetailByPageAsync(role, month, type, i, limit, cancellationToken);
            data.List.AddRange(addData.List);
            if (addData.List.Count < limit)
            {
                break;
            }
        }
        return data;
    }



    /// <summary>
    /// 幻想真境剧诗
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<List<ImaginariumTheaterInfo>> GetImaginariumTheaterInfosAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        var url = $"https://bbs-api-os.hoyolab.com/game_record/app/genshin/api/role_combat?server={role.Region}&role_id={role.Uid}&need_detail=true";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(x_rpc_device_id, DeviceId);
        request.Headers.Add(x_rpc_device_fp, DeviceFp);
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var warpper = await CommonSendAsync<ImaginariumTheaterWarpper>(request, cancellationToken);
        foreach (var item in warpper.Data)
        {
            item.Uid = role.Uid;
            item.ScheduleId = item.Schedule.ScheduleId;
            item.StartTime = item.Schedule.StartDateTime;
            item.EndTime = item.Schedule.EndDateTime;
            item.DifficultyId = item.Stat.DifficultyId;
            item.MaxRoundId = item.Stat.MaxRoundId + item.Stat.TarotFinishedCnt;
            item.Heraldry = item.Stat.Heraldry;
            item.MedalNum = item.Stat.GetMedalRoundList.Count(x => x == 1);
        }
        return warpper.Data;
    }



    /// <summary>
    /// 幽境危战
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<List<StygianOnslaughtInfo>> GetStygianOnslaughtInfosAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        var url = $"https://bbs-api-os.hoyolab.com/game_record/app/genshin/api/hard_challenge?server={role.Region}&role_id={role.Uid}&need_detail=true";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(x_rpc_device_id, DeviceId);
        request.Headers.Add(x_rpc_device_fp, DeviceFp);
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var warpper = await CommonSendAsync<StygianOnslaughtWrapper>(request, cancellationToken);
        foreach (var item in warpper.Data)
        {
            item.Uid = role.Uid;
            item.ScheduleId = item.Schedule.ScheduleId;
            item.StartDateTime = item.Schedule.StartDateTime;
            item.EndDateTime = item.Schedule.EndDateTime;
            item.Difficulty = item.SinglePlayer.Best?.Difficulty ?? 0;
            item.Second = item.SinglePlayer.Best?.Seconds ?? 0;
        }
        return warpper.Data;
    }



    /// <summary>
    /// 原神每日便笺
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override async Task<GenshinDailyNote> GetGenshinDailyNoteAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-public-api.hoyolab.com/event/game_record/app/genshin/api/dailyNote?server={role.Region}&role_id={role.Uid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(x_rpc_device_id, DeviceId);
        request.Headers.Add(x_rpc_device_fp, DeviceFp);
        return await CommonSendAsync<GenshinDailyNote>(request, cancellationToken);
    }



    #endregion




    #region StarRail


    /// <summary>
    /// 获取星穹铁道账号信息
    /// </summary>
    /// <param name="cookie"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">输入的 <c>cookie</c> 为空</exception>

    public override async Task<List<GameRecordRole>> GetStarRailGameRolesAsync(string cookie, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cookie))
        {
            throw new ArgumentNullException(nameof(cookie));
        }
        const string url = "https://api-account-os.hoyolab.com/binding/api/getUserGameRolesByCookieToken?game_biz=hkrpg_global";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, cookie);
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        request.Headers.Add(Referer, "https://act.hoyolab.com");
        var data = await CommonSendAsync<GameRecordRoleWrapper>(request, cancellationToken);
        data.List?.ForEach(x => x.Cookie = cookie);
        return data.List ?? new List<GameRecordRole>();
    }


    /// <summary>
    /// 忘却之庭
    /// </summary>
    /// <param name="role"></param>
    /// <param name="schedule">1当期，2上期</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<ForgottenHallInfo> GetForgottenHallInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var url = $"https://bbs-api-os.hoyolab.com/game_record/app/hkrpg/api/challenge?server={role.Region}&role_id={role.Uid}&schedule_type={schedule}&need_all=true";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://act.hoyolab.com");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var data = await CommonSendAsync<ForgottenHallInfo>(request, cancellationToken);
        data.Uid = role.Uid;
        return data;
    }


    /// <summary>
    /// 虚构叙事
    /// </summary>
    /// <param name="role"></param>
    /// <param name="schedule">1当期，2上期</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<PureFictionInfo> GetPureFictionInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var url = $"https://bbs-api-os.hoyolab.com/game_record/app/hkrpg/api/challenge_story?schedule_type={schedule}&server={role.Region}&role_id={role.Uid}&isPrev=1&need_all=true";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://act.hoyolab.com");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var data = await CommonSendAsync<PureFictionInfo>(request, cancellationToken);
        data.Uid = role.Uid;
        if (data.Metas?.Count > 0)
        {
            if (schedule == 1)
            {
                data.ScheduleId = data.Metas[0].ScheduleId;
                data.BeginTime = data.Metas[0].BeginTime;
                data.EndTime = data.Metas[0].EndTime;
            }
            if (schedule == 2 && data.Metas.Count > 1)
            {
                data.ScheduleId = data.Metas[1].ScheduleId;
                data.BeginTime = data.Metas[1].BeginTime;
                data.EndTime = data.Metas[1].EndTime;
            }
        }
        return data;
    }


    /// <summary>
    /// 末日幻影
    /// </summary>
    /// <param name="role"></param>
    /// <param name="schedule">1当期，2上期</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<ApocalypticShadowInfo> GetApocalypticShadowInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var url = $"https://bbs-api-os.hoyolab.com/game_record/app/hkrpg/api/challenge_boss?schedule_type={schedule}&server={role.Region}&role_id={role.Uid}&isPrev=1&need_all=true";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://act.hoyolab.com");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var data = await CommonSendAsync<ApocalypticShadowInfo>(request, cancellationToken);
        data.Uid = role.Uid;
        if (data.Metas?.Count > 0)
        {
            if (schedule == 1)
            {
                data.ScheduleId = data.Metas[0].ScheduleId;
                data.BeginTime = data.Metas[0].BeginTime;
                data.EndTime = data.Metas[0].EndTime;
                data.UpperBossIcon = data.Metas[0].UpperBoss.Icon;
                data.LowerBossIcon = data.Metas[0].LowerBoss.Icon;
            }
            if (schedule == 2 && data.Metas.Count > 1)
            {
                data.ScheduleId = data.Metas[1].ScheduleId;
                data.BeginTime = data.Metas[1].BeginTime;
                data.EndTime = data.Metas[1].EndTime;
                data.UpperBossIcon = data.Metas[0].UpperBoss.Icon;
                data.LowerBossIcon = data.Metas[0].LowerBoss.Icon;
            }
        }
        return data;
    }


    /// <summary>
    /// 模拟宇宙
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<SimulatedUniverseInfo> GetSimulatedUniverseInfoAsync(GameRecordRole role, bool detail = false, CancellationToken cancellationToken = default)
    {
        var url = $"https://bbs-api-os.hoyolab.com/game_record/app/hkrpg/api/rogue?server={role.Region}&role_id={role.Uid}&schedule_type=3&need_detail={detail.ToString().ToLower()}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://act.hoyolab.com");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var data = await CommonSendAsync<SimulatedUniverseInfo>(request, cancellationToken);
        return data;
    }


    /// <summary>
    /// 开拓月历总结
    /// </summary>
    /// <param name="role"></param>
    /// <param name="month">还不清楚规律，可能是 202304</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<TrailblazeCalendarSummary> GetTrailblazeCalendarSummaryAsync(GameRecordRole role, string month = "", CancellationToken cancellationToken = default)
    {
        var url = $"https://sg-public-api.hoyolab.com/event/srledger/month_info?lang={Language}&uid={role.Uid}&region={role.Region}&month={month}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        return await CommonSendAsync<TrailblazeCalendarSummary>(request, cancellationToken);
    }


    /// <summary>
    /// 开拓月历收入详情
    /// </summary>
    /// <param name="role"></param>
    /// <param name="month">202304</param>
    /// <param name="type">1 星琼 2 星轨票</param>
    /// <param name="page">从1开始</param>
    /// <param name="page_size">最大100</param>
    /// <param name="cancellationToken"></param>
    /// <returns>返回一页收入记录</returns>
    public override async Task<TrailblazeCalendarDetail> GetTrailblazeCalendarDetailByPageAsync(GameRecordRole role, string month, int type, int page, int page_size = 100, CancellationToken cancellationToken = default)
    {
        var url = $"https://sg-public-api.hoyolab.com/event/srledger/month_detail?lang={Language}&uid={role.Uid}&region={role.Region}&month={month}&type={type}&current_page={page}&page_size={page_size}&total=0 ";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var data = await CommonSendAsync<TrailblazeCalendarDetail>(request, cancellationToken);
        foreach (var item in data.List)
        {
            item.Type = type;
        }
        return data;
    }


    /// <summary>
    /// 开拓月历收入详情
    /// </summary>
    /// <param name="role"></param>
    /// <param name="month">202304</param>
    /// <param name="type">1 星琼 2 星轨票</param>
    /// <param name="page_size">最大100</param>
    /// <param name="cancellationToken"></param>
    /// <returns>返回该月所有收入记录</returns>
    public override async Task<TrailblazeCalendarDetail> GetTrailblazeCalendarDetailAsync(GameRecordRole role, string month, int type, int page_size = 100, CancellationToken cancellationToken = default)
    {
        page_size = Math.Clamp(page_size, 20, 100);
        var data = await GetTrailblazeCalendarDetailByPageAsync(role, month, type, 1, page_size, cancellationToken);
        if (data.List.Count < page_size)
        {
            return data;
        }
        for (int i = 2; ; i++)
        {
            var addData = await GetTrailblazeCalendarDetailByPageAsync(role, month, type, i, page_size, cancellationToken);
            data.List.AddRange(addData.List);
            if (addData.List.Count < page_size)
            {
                break;
            }
        }
        return data;
    }



    /// <summary>
    /// 星穹铁道实时便笺
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override async Task<StarRailDailyNote> GetStarRailDailyNoteAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-public-api.hoyolab.com/event/game_record/app/hkrpg/api/note?server={role.Region}&role_id={role.Uid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(x_rpc_device_id, DeviceId);
        request.Headers.Add(x_rpc_device_fp, DeviceFp);
        return await CommonSendAsync<StarRailDailyNote>(request, cancellationToken);
    }



    /// <summary>
    /// 星穹铁道异相仲裁
    /// </summary>
    /// <param name="role"></param>
    /// <param name="scheduleType">1 当期，3 最近三期</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<ChallengePeakData> GetStarRailChallengePeakDataAsync(GameRecordRole role, int scheduleType, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-public-api.hoyolab.com/event/game_record/app/hkrpg/api/challenge_peak?server={role.Region}&role_id={role.Uid}&schedule_type={scheduleType}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(x_rpc_device_id, DeviceId);
        request.Headers.Add(x_rpc_device_fp, DeviceFp);
        return await CommonSendAsync<ChallengePeakData>(request, cancellationToken);
    }




    #endregion




    #region ZZZ


    /// <summary>
    /// 获取绝区零账号信息
    /// </summary>
    /// <param name="cookie"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<List<GameRecordRole>> GetZZZGameRolesAsync(string cookie, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cookie))
        {
            throw new ArgumentNullException(nameof(cookie));
        }
        var url = "https://api-account-os.hoyolab.com/binding/api/getUserGameRolesByCookieToken?game_biz=nap_global";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, cookie);
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        request.Headers.Add(Referer, "https://act.hoyolab.com");
        var data = await CommonSendAsync<GameRecordRoleWrapper>(request, cancellationToken);
        data.List?.ForEach(x => x.Cookie = cookie);
        return data.List ?? new List<GameRecordRole>();
    }


    /// <summary>
    /// 绝区零抽卡记录
    /// </summary>
    /// <param name="role"></param>
    /// <param name="gachaType"></param>
    /// <param name="endId">首次请求不传</param>
    /// <param name="language"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<ZZZGachaRecordData> GetZZZGachaRecordAsync(GameRecordRole role, int gachaType, long? endId = null, string? language = null, CancellationToken cancellationToken = default)
    {
        var url = $"https://sg-public-api.hoyolab.com/event/game_record_zzz/api/zzz/gacha_record?uid={role.Uid}&region={role.Region}&gacha_type={gachaType}";
        long validEndId = endId.GetValueOrDefault();
        if (validEndId > 0)
        {
            url += $"&end_id={validEndId}";
        }
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(x_rpc_device_id, DeviceId);
        request.Headers.Add(x_rpc_device_fp, DeviceFp);
        return await CommonSendAsync<ZZZGachaRecordData>(request, cancellationToken);
    }


    /// <summary>
    /// 式舆防卫战
    /// </summary>
    /// <param name="role"></param>
    /// <param name="schedule">1当期，2上期</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<ShiyuDefenseWrapper> GetShiyuDefenseInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var url = $"https://sg-public-api.hoyolab.com/event/game_record_zzz/api/zzz/hadal_info_v2?schedule_type={schedule}&server={role.Region}&role_id={role.Uid}&need_all=true";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://act.hoyolab.com");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        return await CommonSendAsync<ShiyuDefenseWrapper>(request, cancellationToken);
    }


    /// <summary>
    /// 危局强袭战
    /// </summary>
    /// <param name="role"></param>
    /// <param name="schedule">1当期，2上期</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<DeadlyAssaultInfo> GetDeadlyAssaultInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var url = $"https://sg-public-api.hoyolab.com/event/game_record_zzz/api/zzz/mem_detail?schedule_type={schedule}&region={role.Region}&uid={role.Uid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://act.hoyolab.com");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        return await CommonSendAsync<DeadlyAssaultInfo>(request, cancellationToken);
    }


    /// <summary>
    /// 绳网月报总结
    /// </summary>
    /// <param name="role"></param>
    /// <param name="month">202409</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<InterKnotReportSummary> GetInterKnotReportSummaryAsync(GameRecordRole role, string month = "", CancellationToken cancellationToken = default)
    {
        var url = $"https://sg-public-api.hoyolab.com/event/nap_ledger/month_info?uid={role.Uid}&region={role.Region}&month={month}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        return await CommonSendAsync<InterKnotReportSummary>(request, cancellationToken);
    }

    /// <summary>
    /// 绳网月报收入详情
    /// </summary>
    /// <param name="role"></param>
    /// <param name="month">202409</param>
    /// <param name="type"></param>
    /// <param name="page">从1开始</param>
    /// <param name="page_size">最大100</param>
    /// <param name="cancellationToken"></param>
    /// <returns>返回一页收入记录</returns>
    public override async Task<InterKnotReportDetail> GetInterKnotReportDetailByPageAsync(GameRecordRole role, string month, string type, int page, int page_size = 100, CancellationToken cancellationToken = default)
    {
        var url = $"https://sg-public-api.hoyolab.com/event/nap_ledger/month_detail?uid={role.Uid}&region={role.Region}&month={month}&type={type}&current_page={page}&page_size={page_size}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        return await CommonSendAsync<InterKnotReportDetail>(request, cancellationToken);
    }


    /// <summary>
    /// 绳网月报收入详情
    /// </summary>
    /// <param name="role"></param>
    /// <param name="month">202409</param>
    /// <param name="type"></param>
    /// <param name="page_size">最大100</param>
    /// <param name="cancellationToken"></param>
    /// <returns>返回该月所有收入记录</returns>
    public override async Task<InterKnotReportDetail> GetInterKnotReportDetailAsync(GameRecordRole role, string month, string type, int page_size = 100, CancellationToken cancellationToken = default)
    {
        page_size = Math.Clamp(page_size, 20, 100);
        var data = await GetInterKnotReportDetailByPageAsync(role, month, type, 1, page_size, cancellationToken);
        if (data.List.Count < page_size)
        {
            return data;
        }
        for (int i = 2; ; i++)
        {
            var addData = await GetInterKnotReportDetailByPageAsync(role, month, type, i, page_size, cancellationToken);
            data.List.AddRange(addData.List);
            if (addData.List.Count < page_size)
            {
                break;
            }
        }
        return data;
    }



    /// <summary>
    /// 养成指南，不可用，返回未登录错误
    /// </summary>
    /// <param name="role"></param>
    /// <param name="avatar_id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [Obsolete("不可用，返回未登录错误", true)]
    public override async Task<UpgradeGuideItemList> GetZZZUpgradeGuideItemListAsync(GameRecordRole role, int avatar_id = 1011, CancellationToken cancellationToken = default)
    {
        var url = $"https://sg-public-api.hoyolab.com/event/nap_cultivate_tool/user/item_list?uid={role.Uid}&region={role.Region}&avatar_id={avatar_id}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_device_id, DeviceId);
        request.Headers.Add(x_rpc_device_fp, DeviceFp);
        return await CommonSendAsync<UpgradeGuideItemList>(request, cancellationToken);
    }



    /// <summary>
    /// 养成指南，不可用，返回未登录错误
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [Obsolete("不可用，返回未登录错误", true)]
    public override async Task<UpgradeGuidIconInfo> GetZZZUpgradeGuideIconInfoAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        var url = $"https://sg-public-api.hoyolab.com/event/nap_cultivate_tool/user/icon_info?uid={role.Uid}&region={role.Region}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_device_id, DeviceId);
        request.Headers.Add(x_rpc_device_fp, DeviceFp);
        return await CommonSendAsync<UpgradeGuidIconInfo>(request, cancellationToken);
    }



    /// <summary>
    /// 绝区零实时便笺
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<ZZZDailyNote> GetZZZDailyNoteAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-public-api.hoyolab.com/event/game_record_zzz/api/zzz/note?server={role.Region}&role_id={role.Uid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(x_rpc_device_id, DeviceId);
        request.Headers.Add(x_rpc_device_fp, DeviceFp);
        return await CommonSendAsync<ZZZDailyNote>(request, cancellationToken);
    }


    /// <summary>
    /// 绝区零临界推演
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<ThresholdSimulationAbstractInfo> GetZZZThresholdSimulationAbstractInfoAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-public-api.hoyolab.com/event/game_record_zzz/api/zzz/void_front_battle_abstract_info?region={role.Region}&uid={role.Uid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(x_rpc_device_id, DeviceId);
        request.Headers.Add(x_rpc_device_fp, DeviceFp);
        return await CommonSendAsync<ThresholdSimulationAbstractInfo>(request, cancellationToken);
    }



    /// <summary>
    /// 绝区零临界推演
    /// </summary>
    /// <param name="role"></param>
    /// <param name="void_front_id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<ThresholdSimulationDetailInfo> GetZZZThresholdSimulationDetailInfoAsync(GameRecordRole role, int void_front_id, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-public-api.hoyolab.com/event/game_record_zzz/api/zzz/void_front_battle_detail?region={role.Region}&uid={role.Uid}&void_front_id={void_front_id}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(x_rpc_device_id, DeviceId);
        request.Headers.Add(x_rpc_device_fp, DeviceFp);
        return await CommonSendAsync<ThresholdSimulationDetailInfo>(request, cancellationToken);
    }



    #endregion




    #region Check-In


    public override Task<CheckIn.CheckInInfo> GetCheckInInfoAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Check-in is only supported for Chinese server.");
    }


    public override Task<CheckIn.CheckInResult> CheckInAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Check-in is only supported for Chinese server.");
    }


    #endregion




}
