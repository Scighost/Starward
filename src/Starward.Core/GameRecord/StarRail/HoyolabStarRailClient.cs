using Starward.Core.GameRecord.StarRail.ApocalypticShadow;
using Starward.Core.GameRecord.StarRail.ChallengePeak;
using Starward.Core.GameRecord.StarRail.DailyNote;
using Starward.Core.GameRecord.StarRail.ForgottenHall;
using Starward.Core.GameRecord.StarRail.PureFiction;
using Starward.Core.GameRecord.StarRail.SimulatedUniverse;
using Starward.Core.GameRecord.StarRail.TrailblazeCalendar;

namespace Starward.Core.GameRecord.StarRail;

public class HoyolabStarRailClient : GameRecordClientBase, IGameRecordClient, IGameRecordStarRailClient
{

    public override string AppVersion => HoyolabAppVersion;

    public override string UAContent => HoyolabUAContent;

    public override ServerRegion ServerRegion => ServerRegion.HoYoverse;


    public HoyolabStarRailClient(HttpClient? httpClient = null) : base(httpClient)
    {

    }


    /// <summary>
    /// 获取游戏账号信息
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <param name="cookie"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameRecordRole>> GetGameRolesAsync(GameBiz gameBiz, string cookie, CancellationToken cancellationToken = default)
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
        var data = await CommonSendAsync<GameRecordRoleWrapper>(request, cancellationToken).ConfigureAwait(false);
        if (data.List is not null)
        {
            foreach (var item in data.List)
            {
                item.Cookie = cookie;
                try
                {
                    item.HeadIcon = await GetGameRoleHeadIconAsync(item, cancellationToken).ConfigureAwait(false);
                }
                catch (miHoYoApiException) { }
            }
        }
        return data.List ?? [];
    }


    /// <summary>
    /// 获取游戏账号头像
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> GetGameRoleHeadIconAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-act-public-api.hoyolab.com/event/game_record/app/hkrpg/api/index?server={role.Region}&role_id={role.Uid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        var data = await CommonSendAsync<GameRecordIndex>(request, cancellationToken).ConfigureAwait(false);
        return data.HeadIcon;
    }


    /// <summary>
    /// 忘却之庭
    /// </summary>
    /// <param name="role"></param>
    /// <param name="schedule">1当期，2上期</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ForgottenHallInfo> GetForgottenHallInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-act-public-api.hoyolab.com/event/game_record/app/hkrpg/api/challenge?server={role.Region}&role_id={role.Uid}&schedule_type={schedule}&need_all=true";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var data = await CommonSendAsync<ForgottenHallInfo>(request, cancellationToken).ConfigureAwait(false);
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
    public async Task<PureFictionInfo> GetPureFictionInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-act-public-api.hoyolab.com/event/game_record/app/hkrpg/api/challenge_story?schedule_type={schedule}&server={role.Region}&role_id={role.Uid}&isPrev=1&need_all=true";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var data = await CommonSendAsync<PureFictionInfo>(request, cancellationToken).ConfigureAwait(false);
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
    public async Task<ApocalypticShadowInfo> GetApocalypticShadowInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-act-public-api.hoyolab.com/event/game_record/app/hkrpg/api/challenge_boss?schedule_type={schedule}&server={role.Region}&role_id={role.Uid}&isPrev=1&need_all=true";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var data = await CommonSendAsync<ApocalypticShadowInfo>(request, cancellationToken).ConfigureAwait(false);
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
                data.TierceBossIcon = data.Metas[0].TierceBoss?.Icon;
            }
            if (schedule == 2 && data.Metas.Count > 1)
            {
                data.ScheduleId = data.Metas[1].ScheduleId;
                data.BeginTime = data.Metas[1].BeginTime;
                data.EndTime = data.Metas[1].EndTime;
                data.UpperBossIcon = data.Metas[1].UpperBoss.Icon;
                data.LowerBossIcon = data.Metas[1].LowerBoss.Icon;
                data.TierceBossIcon = data.Metas[1].TierceBoss?.Icon;
            }
        }
        return data;
    }


    /// <summary>
    /// 模拟宇宙
    /// </summary>
    /// <param name="role"></param>
    /// <param name="detail"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SimulatedUniverseInfo> GetSimulatedUniverseInfoAsync(GameRecordRole role, bool detail = false, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-act-public-api.hoyolab.com/event/game_record/app/hkrpg/api/rogue?server={role.Region}&role_id={role.Uid}&schedule_type=3&need_detail={detail.ToString().ToLower()}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        return await CommonSendAsync<SimulatedUniverseInfo>(request, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// 开拓月历总结
    /// </summary>
    /// <param name="role"></param>
    /// <param name="month">还不清楚规律，可能是 202304</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TrailblazeCalendarSummary> GetTrailblazeCalendarSummaryAsync(GameRecordRole role, string month = "", CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-act-public-api.hoyolab.com/event/srledger/month_info?lang={Language}&uid={role.Uid}&region={role.Region}&month={month}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        return await CommonSendAsync<TrailblazeCalendarSummary>(request, cancellationToken).ConfigureAwait(false);
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
    public async Task<TrailblazeCalendarDetail> GetTrailblazeCalendarDetailByPageAsync(GameRecordRole role, string month, int type, int page, int page_size = 100, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-act-public-api.hoyolab.com/event/srledger/month_detail?lang={Language}&uid={role.Uid}&region={role.Region}&month={month}&type={type}&current_page={page}&page_size={page_size}&total=0";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var data = await CommonSendAsync<TrailblazeCalendarDetail>(request, cancellationToken).ConfigureAwait(false);
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
    public async Task<TrailblazeCalendarDetail> GetTrailblazeCalendarDetailAsync(GameRecordRole role, string month, int type, int page_size = 100, CancellationToken cancellationToken = default)
    {
        page_size = Math.Clamp(page_size, 20, 100);
        var data = await GetTrailblazeCalendarDetailByPageAsync(role, month, type, 1, page_size, cancellationToken).ConfigureAwait(false);
        if (data.List.Count < page_size)
        {
            return data;
        }
        for (int i = 2; ; i++)
        {
            var addData = await GetTrailblazeCalendarDetailByPageAsync(role, month, type, i, page_size, cancellationToken).ConfigureAwait(false);
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
    public async Task<StarRailDailyNote> GetStarRailDailyNoteAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-act-public-api.hoyolab.com/event/game_record/app/hkrpg/api/note?server={role.Region}&role_id={role.Uid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        return await CommonSendAsync<StarRailDailyNote>(request, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// 星穹铁道异相仲裁
    /// </summary>
    /// <param name="role"></param>
    /// <param name="scheduleType">1 当期，3 最近三期</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ChallengePeakData> GetStarRailChallengePeakDataAsync(GameRecordRole role, int scheduleType, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-act-public-api.hoyolab.com/event/game_record/app/hkrpg/api/challenge_peak?server={role.Region}&role_id={role.Uid}&schedule_type={scheduleType}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        return await CommonSendAsync<ChallengePeakData>(request, cancellationToken).ConfigureAwait(false);
    }

}
