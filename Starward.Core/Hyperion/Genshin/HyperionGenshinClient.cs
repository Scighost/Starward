using Starward.Core.Hyperion.Genshin.SpiralAbyss;

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
    public async Task<List<GenshinRole>> GetGenshinRoleInfosAsync(string cookie, CancellationToken? cancellationToken = null)
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
        var data = await CommonSendAsync<GenshinRoleWrapper>(request, cancellationToken);
        data.List?.ForEach(x => x.Cookie = cookie);
        return data.List ?? new List<GenshinRole>();
    }




    /// <summary>
    /// 深境螺旋
    /// </summary>
    /// <param name="role"></param>
    /// <param name="schedule">1当期，2上期</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SpiralAbyssInfo> GetSpiralAbyssInfoAsync(GenshinRole role, int schedule, CancellationToken? cancellationToken = null)
    {
        var url = $"https://api-takumi-record.mihoyo.com/game_record/app/genshin/api/spiralAbyss?schedule_type={schedule}&server={role.Region}&role_id={role.Uid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, DynamicSecret.CreateSecret2(url));
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/app/community-game-records/?game_id=2&utm_source=bbs&utm_medium=mys&utm_campaign=box");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        request.Headers.Add(X_Reuqest_With, com_mihoyo_hyperion);
        var data = await CommonSendAsync<SpiralAbyssInfo>(request);
        data.Uid = role.Uid;
        return data;
    }






}
