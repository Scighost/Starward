using Starward.Core.Hyperion.Genshin.SpiralAbyss;
using Starward.Core.Hyperion.Genshin.TravelersDiary;

namespace Starward.Core.Hyperion.Genshin;

public class HyperionGenshinClient : HyperionClient
{



    public HyperionGenshinClient(HttpClient? httpClient = null) : base(httpClient)
    {
        _genshin = this;
    }




    /// <summary>
    /// 获取原神账号信息
    /// </summary>
    /// <param name="cookie"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">输入的 <c>cookie</c> 为空</exception>
    public async Task<List<HyperionGameRole>> GetGenshinGameRolesAsync(string cookie, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cookie))
        {
            throw new ArgumentNullException(nameof(cookie));
        }
        var url = "https://api-takumi.mihoyo.com/binding/api/getUserGameRolesByCookie?game_biz=hk4e_cn";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, cookie);
        request.Headers.Add(DS, DynamicSecret.CreateSecret2(url));
        request.Headers.Add(X_Reuqest_With, com_mihoyo_hyperion);
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/app/community-game-records/?game_id=2&utm_source=bbs&utm_medium=mys&utm_campaign=box");
        var data = await CommonSendAsync<HyperionGameRoleWrapper>(request, cancellationToken);
        data.List?.ForEach(x => x.Cookie = cookie);
        return data.List ?? new List<HyperionGameRole>();
    }



    /// <summary>
    /// 旅行札记总览
    /// </summary>
    /// <param name="role"></param>
    /// <param name="month">0 当前月</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TravelersDiarySummary> GetTravelsDiarySummaryAsync(HyperionGameRole role, int month = 0, CancellationToken cancellationToken = default)
    {
        var url = $"https://hk4e-api.mihoyo.com/event/ys_ledger/monthInfo?month={month}&bind_uid={role.Uid}&bind_region={role.Region}&bbs_presentation_style=fullscreen&bbs_auth_required=true&utm_source=bbs&utm_medium=mys&utm_campaign=icon";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/ys/event/e20200709ysjournal/index.html?bbs_presentation_style=fullscreen&bbs_auth_required=true&utm_source=bbs&utm_medium=mys&utm_campaign=icon");
        request.Headers.Add(X_Reuqest_With, com_mihoyo_hyperion);
        return await CommonSendAsync<TravelersDiarySummary>(request, cancellationToken);
    }


    /// <summary>
    /// 旅行札记收入详情
    /// </summary>
    /// <param name="role"></param>
    /// <param name="month"></param>
    /// <param name="type"></param>
    /// <param name="page">从1开始</param>
    /// <param name="limit">最大100</param>
    /// <param name="cancellationToken"></param>
    /// <returns>返回一页收入记录</returns>
    private async Task<TravelersDiaryDetail> GetTravelsDiaryDetailByPageAsync(HyperionGameRole role, int month, TravelersDiaryAwardType type, int page, int limit = 100, CancellationToken cancellationToken = default)
    {
        var url = $"https://hk4e-api.mihoyo.com/event/ys_ledger/monthDetail?page={page}&month={month}&limit={limit}&type={(int)type}&bind_uid={role.Uid}&bind_region={role.Region}&bbs_presentation_style=fullscreen&bbs_auth_required=true&utm_source=bbs&utm_medium=mys&utm_campaign=icon";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/ys/event/e20200709ysjournal/index.html?bbs_presentation_style=fullscreen&bbs_auth_required=true&utm_source=bbs&utm_medium=mys&utm_campaign=icon");
        request.Headers.Add(X_Reuqest_With, com_mihoyo_hyperion);
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
    /// <param name="type"></param>
    /// <param name="limit">最大100</param>
    /// <param name="cancellationToken"></param>
    /// <returns>返回该月所有收入记录</returns>
    public async Task<TravelersDiaryDetail> GetTravelsDiaryDetailAsync(HyperionGameRole role, int month, TravelersDiaryAwardType type, int limit = 100, CancellationToken cancellationToken = default)
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
    /// 深境螺旋
    /// </summary>
    /// <param name="role"></param>
    /// <param name="schedule">1当期，2上期</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SpiralAbyssInfo> GetSpiralAbyssInfoAsync(HyperionGameRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var url = $"https://api-takumi-record.mihoyo.com/game_record/app/genshin/api/spiralAbyss?schedule_type={schedule}&server={role.Region}&role_id={role.Uid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, DynamicSecret.CreateSecret2(url));
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/app/community-game-records/?game_id=2&utm_source=bbs&utm_medium=mys&utm_campaign=box");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Reuqest_With, com_mihoyo_hyperion);
        var data = await CommonSendAsync<SpiralAbyssInfo>(request, cancellationToken);
        data.Uid = role.Uid;
        return data;
    }






}
