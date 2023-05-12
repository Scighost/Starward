using Starward.Core.Hyperion.StarRail.Ledger;

namespace Starward.Core.Hyperion.StarRail;

public class HyperionStarRailClient : HyperionClient
{



    public HyperionStarRailClient(HttpClient? httpClient = null) : base(httpClient)
    {
        _starRail = this;
    }



    /// <summary>
    /// 获取星穹铁道账号信息
    /// </summary>
    /// <param name="cookie"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">输入的 <c>cookie</c> 为空</exception>
    public async Task<List<StarRailRole>> GetStarRailRolesAsync(string cookie, CancellationToken? cancellationToken = null)
    {
        if (string.IsNullOrWhiteSpace(cookie))
        {
            throw new ArgumentNullException(nameof(cookie));
        }
        var url = "https://api-takumi.mihoyo.com/binding/api/getUserGameRolesByCookie?game_biz=hkrpg_cn";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, cookie);
        request.Headers.Add(DS, DynamicSecret.CreateSecret2(url));
        request.Headers.Add(X_Reuqest_With, com_mihoyo_hyperion);
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/app/community-game-records/rpg/index.html?mhy_presentation_style=fullscreen&game_id=6&utm_source=bbs&utm_medium=mys&utm_campaign=icon");
        var data = await CommonSendAsync<StarRailRoleWrapper>(request, cancellationToken);
        data.List?.ForEach(x => x.Cookie = cookie);
        return data.List ?? new List<StarRailRole>();
    }




    /// <summary>
    /// 开拓月历总结
    /// </summary>
    /// <param name="role"></param>
    /// <param name="month">还不清楚规律，可能是 202304</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<LedgerSummary> GetLedgerSummaryAsync(StarRailRole role, string month = "", CancellationToken? cancellationToken = null)
    {
        var url = $"https://api-takumi.mihoyo.com/event/srledger/month_info?uid={role.Uid}&region={role.Region}&month={month}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/sr/event/rpg-srledger/index.html?mhy_game_role_required=hkrpg_cn&mhy_presentation_style=fullscreen&utm_source=bbs&utm_medium=mys&utm_campaign=icon");
        request.Headers.Add(X_Reuqest_With, com_mihoyo_hyperion);
        return await CommonSendAsync<LedgerSummary>(request, cancellationToken);
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
    private async Task<LedgerDetail> GetLedgerDetailByPageAsync(StarRailRole role, string month, int type, int page, int page_size = 100, CancellationToken? cancellationToken = null)
    {
        // 
        var url = $"https://api-takumi.mihoyo.com/event/srledger/month_detail?uid={role.Uid}&region={role.Region}&month={month}&type={type}&current_page={page}&page_size={page_size}&total=0";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/sr/event/rpg-srledger/index.html?mhy_game_role_required=hkrpg_cn&mhy_presentation_style=fullscreen&utm_source=bbs&utm_medium=mys&utm_campaign=icon");
        request.Headers.Add(X_Reuqest_With, com_mihoyo_hyperion);
        var data = await CommonSendAsync<LedgerDetail>(request);
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
    public async Task<LedgerDetail> GetLedgerDetailAsync(StarRailRole role, string month, int type, int page_size = 100, CancellationToken? cancellationToken = null)
    {
        page_size = Math.Clamp(page_size, 20, 100);
        var data = await GetLedgerDetailByPageAsync(role, month, type, 1, page_size);
        if (data.List.Count < page_size)
        {
            return data;
        }
        for (int i = 2; ; i++)
        {
            var addData = await GetLedgerDetailByPageAsync(role, month, type, i, page_size);
            data.List.AddRange(addData.List);
            if (addData.List.Count < page_size)
            {
                break;
            }
        }
        return data;
    }


    /// <summary>
    /// 忘却之庭
    /// </summary>
    /// <param name="role"></param>
    /// <param name="schedule">1当期，2上期</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [Obsolete("还没玩到这", true)]
    public async Task<object> GetForgottenHallAsync(StarRailRole role, int schedule, CancellationToken? cancellationToken = null)
    {
        var url = $"https://api-takumi-record.mihoyo.com/game_record/app/hkrpg/api/challenge?schedule_type={schedule}&server={role.Region}&role_id={role.Uid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, DynamicSecret.CreateSecret2(url));
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/app/community-game-records/rpg/index.html?mhy_presentation_style=fullscreen&game_id=6&utm_source=bbs&utm_medium=mys&utm_campaign=icon\r\n");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Reuqest_With, com_mihoyo_hyperion);
        var data = await CommonSendAsync<object>(request);
        //data.Uid = role.Uid;
        return data;
    }





}
