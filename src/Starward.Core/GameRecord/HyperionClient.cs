using Starward.Core.GameRecord.Genshin.SpiralAbyss;
using Starward.Core.GameRecord.Genshin.TravelersDiary;
using Starward.Core.GameRecord.StarRail.ForgottenHall;
using Starward.Core.GameRecord.StarRail.PureFiction;
using Starward.Core.GameRecord.StarRail.SimulatedUniverse;
using Starward.Core.GameRecord.StarRail.TrailblazeCalendar;
using System.Text.RegularExpressions;

namespace Starward.Core.GameRecord;


public class HyperionClient : GameRecordClient
{


    protected override string UAContent => $"Mozilla/5.0 miHoYoBBS/{AppVersion}";

    protected override string AppVersion => "2.68.1";

    protected override string ApiSalt => "t0qEgfub6cvueAPgR5m9aQWWVciEer7v";

    protected override string ApiSalt2 => "xV8v4Qu54lUKrEYFZkJhB8cuOh9Asafs";



    public HyperionClient(HttpClient? httpClient = null) : base(httpClient)
    {

    }



    // https://webstatic.mihoyo.com/bbs/event/signin-ys/index.html?bbs_auth_required=true&act_id=e202009291139501&utm_source=bbs&utm_medium=mys&utm_campaign=icon
    // https://webstatic.mihoyo.com/ys/event/e20200709ysjournal/index.html?bbs_presentation_style=fullscreen&bbs_auth_required=true&utm_source=bbs&utm_medium=mys&utm_campaign=icon
    // https://webstatic.mihoyo.com/app/community-game-records/?game_id=2&utm_source=bbs&utm_medium=mys&utm_campaign=box
    // https://webstatic.mihoyo.com/bbs/event/signin/hkrpg/index.html?bbs_auth_required=true&act_id=e202304121516551&bbs_auth_required=true&bbs_presentation_style=fullscreen&utm_source=bbs&utm_medium=mys&utm_campaign=icon
    // https://webstatic.mihoyo.com/app/community-game-records/rpg/index.html?mhy_presentation_style=fullscreen&game_id=6&utm_source=bbs&utm_medium=mys&utm_campaign=icon
    // https://webstatic.mihoyo.com/sr/event/rpg-srledger/index.html?mhy_game_role_required=hkrpg_cn&mhy_presentation_style=fullscreen&utm_source=bbs&utm_medium=mys&utm_campaign=icon




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
        var request = new HttpRequestMessage(HttpMethod.Get, "https://bbs-api.miyoushe.com/user/wapi/getUserFullInfo");
        request.Headers.Add(Cookie, cookie);
        request.Headers.Add(Referer, "https://www.miyoushe.com/");
        request.Headers.Add(DS, CreateSecret());
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_device_id, DeviceId);
        request.Headers.Add(x_rpc_client_type, "5");
        var data = await CommonSendAsync<GameRecordUserWrapper>(request, cancellationToken);
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
        var url = "https://api-takumi.mihoyo.com/binding/api/getUserGameRolesByCookie?game_biz=hk4e_cn";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(X_Request_With, com_mihoyo_hyperion);
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/");
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
        var url = $"https://api-takumi-record.mihoyo.com/game_record/app/genshin/api/spiralAbyss?schedule_type={schedule}&server={role.Region}&role_id={role.Uid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(x_rpc_device_id, Regex.Match(role.Cookie ?? "", @"_MHYUUID=([^;]+)").Groups[1].Value);
        request.Headers.Add(x_rpc_device_fp, Regex.Match(role.Cookie ?? "", @"DEVICEFP=([^;]+)").Groups[1].Value);
        request.Headers.Add(X_Request_With, com_mihoyo_hyperion);
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
        var url = $"https://hk4e-api.mihoyo.com/event/ys_ledger/monthInfo?month={month}&bind_uid={role.Uid}&bind_region={role.Region}&bbs_presentation_style=fullscreen&bbs_auth_required=true&utm_source=bbs&utm_medium=mys&utm_campaign=icon";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/");
        request.Headers.Add(X_Request_With, com_mihoyo_hyperion);
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
        var url = $"https://hk4e-api.mihoyo.com/event/ys_ledger/monthDetail?page={page}&month={month}&limit={limit}&type={type}&bind_uid={role.Uid}&bind_region={role.Region}&bbs_presentation_style=fullscreen&bbs_auth_required=true&utm_source=bbs&utm_medium=mys&utm_campaign=icon";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/");
        request.Headers.Add(X_Request_With, com_mihoyo_hyperion);
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
        const string url = "https://api-takumi.mihoyo.com/binding/api/getUserGameRolesByCookie?game_biz=hkrpg_cn";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(X_Request_With, com_mihoyo_hyperion);
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/");
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
        var url = $"https://api-takumi-record.mihoyo.com/game_record/app/hkrpg/api/challenge?schedule_type={schedule}&server={role.Region}&role_id={role.Uid}&need_all=true";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_device_id, Regex.Match(role.Cookie ?? "", @"_MHYUUID=([^;]+)").Groups[1].Value);
        request.Headers.Add(x_rpc_device_fp, Regex.Match(role.Cookie ?? "", @"DEVICEFP=([^;]+)").Groups[1].Value);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hyperion);
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
        var url = $"https://api-takumi-record.mihoyo.com/game_record/app/hkrpg/api/challenge_story?schedule_type={schedule}&server={role.Region}&role_id={role.Uid}&isPrev=1&need_all=true";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_device_id, Regex.Match(role.Cookie ?? "", @"_MHYUUID=([^;]+)").Groups[1].Value);
        request.Headers.Add(x_rpc_device_fp, Regex.Match(role.Cookie ?? "", @"DEVICEFP=([^;]+)").Groups[1].Value);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hyperion);
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
        var url = $"https://api-takumi-record.mihoyo.com/game_record/app/hkrpg/api/rogue?role_id={role.Uid}&server={role.Region}&schedule_type=3&need_detail={detail.ToString().ToLower()}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateSecret2(url));
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_device_id, Regex.Match(role.Cookie ?? "", @"_MHYUUID=([^;]+)").Groups[1].Value);
        request.Headers.Add(x_rpc_device_fp, Regex.Match(role.Cookie ?? "", @"DEVICEFP=([^;]+)").Groups[1].Value);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hyperion);
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
        var url = $"https://api-takumi.mihoyo.com/event/srledger/month_info?uid={role.Uid}&region={role.Region}&month={month}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/");
        request.Headers.Add(X_Request_With, com_mihoyo_hyperion);
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
        // 
        var url = $"https://api-takumi.mihoyo.com/event/srledger/month_detail?uid={role.Uid}&region={role.Region}&month={month}&type={type}&current_page={page}&page_size={page_size}&total=0";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/");
        request.Headers.Add(X_Request_With, com_mihoyo_hyperion);
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
