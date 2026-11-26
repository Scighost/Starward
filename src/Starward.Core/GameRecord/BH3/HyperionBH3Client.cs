using Starward.Core.GameRecord.BH3.DailyNote;
using static Starward.Core.Passport.DynamicSecret;

namespace Starward.Core.GameRecord.BH3;

public class HyperionBH3Client : GameRecordClientBase, IGameRecordClient, IGameRecordBH3Client
{

    public override string AppVersion => HyperionAppVersion;

    public override string DefaultUAContent => HyperionUAContent;

    public override ServerRegion ServerRegion => ServerRegion.miHoYo;


    public HyperionBH3Client(HttpClient? httpClient = null) : base(httpClient)
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
        string url = $"https://passport-api.mihoyo.com/binding/api/getUserGameRolesByCookieToken?game_biz={gameBiz}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, cookie);
        request.Headers.Add(Referer, "https://act.mihoyo.com/");
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
        string url = $"https://api-takumi-record.mihoyo.com/game_record/app/honkai3rd/api/index?server={role.Region}&role_id={role.Uid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateWebDS2(ServerRegion, url));
        request.Headers.Add(Referer, "https://webstatic.mihoyo.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        var data = await CommonSendAsync<GameRecordIndex>(request, cancellationToken).ConfigureAwait(false);
        return data.HeadIcon;
    }


    /// <summary>
    /// 崩坏3实时便笺
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<BH3DailyNote> GetBH3DailyNoteAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        string url = $"https://act-api-takumi.mihoyo.com/game_record/appv2/honkai3rd/api/note?server={role.Region}&role_id={role.Uid}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(Cookie, role.Cookie);
        request.Headers.Add(DS, CreateWebDS2(ServerRegion, url));
        request.Headers.Add(Referer, "https://act.mihoyo.com/");
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_client_type, "5");
        return await CommonSendAsync<BH3DailyNote>(request, cancellationToken).ConfigureAwait(false);
    }

}
