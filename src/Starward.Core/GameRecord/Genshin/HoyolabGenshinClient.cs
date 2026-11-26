using Starward.Core.GameRecord.Genshin.DailyNote;
using Starward.Core.GameRecord.Genshin.ImaginariumTheater;
using Starward.Core.GameRecord.Genshin.SpiralAbyss;
using Starward.Core.GameRecord.Genshin.StygianOnslaught;
using Starward.Core.GameRecord.Genshin.TravelersDiary;

namespace Starward.Core.GameRecord.Genshin;

public class HoyolabGenshinClient : GameRecordClientBase, IGameRecordClient, IGameRecordGenshinClient
{

    public override string AppVersion => HoyolabAppVersion;

    public override string DefaultUAContent => HoyolabUAContent;

    public override ServerRegion ServerRegion => ServerRegion.HoYoverse;


    public HoyolabGenshinClient(HttpClient? httpClient = null) : base(httpClient)
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
        string url = $"https://sg-act-public-api.hoyolab.com/event/game_record/app/genshin/api/index?avatar_list_type=1&server={role.Region}&role_id={role.Uid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        var data = await CommonSendAsync<GameRecordIndex>(request, cancellationToken).ConfigureAwait(false);
        return data.HeadIcon;
    }


    /// <summary>
    /// 深境螺旋
    /// </summary>
    /// <param name="role"></param>
    /// <param name="schedule">1当期，2上期</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SpiralAbyssInfo> GetSpiralAbyssInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-act-public-api.hoyolab.com/event/game_record/app/genshin/api/spiralAbyss?role_id={role.Uid}&server={role.Region}&schedule_type={schedule}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var data = await CommonSendAsync<SpiralAbyssInfo>(request, cancellationToken).ConfigureAwait(false);
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
    public async Task<TravelersDiarySummary> GetTravelsDiarySummaryAsync(GameRecordRole role, int month = 0, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-hk4e-api.hoyolab.com/event/ysledgeros/month_info?month={month}&region={role.Region}&uid={role.Uid}&lang={Language}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        return await CommonSendAsync<TravelersDiarySummary>(request, cancellationToken).ConfigureAwait(false);
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
    public async Task<TravelersDiaryDetail> GetTravelsDiaryDetailByPageAsync(GameRecordRole role, int month, int type, int page, int limit = 100, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-hk4e-api.hoyolab.com/event/ysledgeros/month_detail?month={month}&current_page={page}&type={type}&region={role.Region}&uid={role.Uid}&lang={Language}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var data = await CommonSendAsync<TravelersDiaryDetail>(request, cancellationToken).ConfigureAwait(false);
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
    public async Task<TravelersDiaryDetail> GetTravelsDiaryDetailAsync(GameRecordRole role, int month, int type, int limit = 100, CancellationToken cancellationToken = default)
    {
        var data = await GetTravelsDiaryDetailByPageAsync(role, month, type, 1, limit, cancellationToken).ConfigureAwait(false);
        if (data.List.Count < limit)
        {
            return data;
        }
        for (int i = 2; ; i++)
        {
            var addData = await GetTravelsDiaryDetailByPageAsync(role, month, type, i, limit, cancellationToken).ConfigureAwait(false);
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
    public async Task<List<ImaginariumTheaterInfo>> GetImaginariumTheaterInfosAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-act-public-api.hoyolab.com/event/game_record/app/genshin/api/role_combat?server={role.Region}&role_id={role.Uid}&need_detail=true";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var warpper = await CommonSendAsync<ImaginariumTheaterWarpper>(request, cancellationToken).ConfigureAwait(false);
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
    public async Task<List<StygianOnslaughtInfo>> GetStygianOnslaughtInfosAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-act-public-api.hoyolab.com/event/game_record/app/genshin/api/hard_challenge?server={role.Region}&role_id={role.Uid}&need_detail=true";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Request_With, com_mihoyo_hoyolab);
        var warpper = await CommonSendAsync<StygianOnslaughtWrapper>(request, cancellationToken).ConfigureAwait(false);
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
    public async Task<GenshinDailyNote> GetGenshinDailyNoteAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        string url = $"https://sg-act-public-api.hoyolab.com/event/game_record/app/genshin/api/dailyNote?server={role.Region}&role_id={role.Uid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://act.hoyolab.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        return await CommonSendAsync<GenshinDailyNote>(request, cancellationToken).ConfigureAwait(false);
    }

}
