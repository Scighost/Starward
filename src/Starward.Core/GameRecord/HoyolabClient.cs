using Starward.Core.GameRecord.Genshin.SpiralAbyss;
using Starward.Core.GameRecord.Genshin.TravelersDiary;
using Starward.Core.GameRecord.StarRail.ForgottenHall;
using Starward.Core.GameRecord.StarRail.PureFiction;
using Starward.Core.GameRecord.StarRail.SimulatedUniverse;
using Starward.Core.GameRecord.StarRail.TrailblazeCalendar;

namespace Starward.Core.GameRecord;

public class HoyolabClient : GameRecordClient
{


    protected override string UAContent => $"Mozilla/5.0 miHoYoBBSOversea/{AppVersion}";

    protected override string AppVersion => "2.47.0";

    protected override string ApiSalt => "okr4obncj8bw5a65hbnn5oo6ixjc3l9w";

    protected override string ApiSalt2 => "h4c1d6ywfq5bsbnbhm1bzq7bxzzv6srt";


    private string language;
    public string Language { get => Util.FilterLanguage(language); set => language = Util.FilterLanguage(value); }



    public HoyolabClient(HttpClient? httpClient = null) : base(httpClient)
    {

    }




    protected override Task<T> CommonSendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        request.Headers.Add(x_rpc_language, Language);
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
        var list = new List<GameRecordRole>();
        list.AddRange(await GetGenshinGameRolesAsync(cookie, cancellationToken));
        list.AddRange(await GetStarRailGameRolesAsync(cookie, cancellationToken));
        return list;
    }



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



    #endregion


}
