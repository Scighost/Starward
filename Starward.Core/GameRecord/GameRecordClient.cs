using Starward.Core.GameRecord.Ledger;
using System.Net;
#if !DEBUG
using System.Net.Http.Json;
#endif
using System.Text.Json;

namespace Starward.Core.GameRecord;


/// <summary>
/// 米游社 API 请求类
/// </summary>
public class GameRecordClient
{

    #region Constant

    private const string Accept = "Accept";
    private const string Cookie = "Cookie";
    private const string UserAgent = "User-Agent";
    private const string X_Reuqest_With = "X-Requested-With";
    private const string DS = "DS";
    private const string Referer = "Referer";
    private const string Application_Json = "application/json";
    private const string com_mihoyo_hyperion = "com.mihoyo.hyperion";
    private const string x_rpc_app_version = "x-rpc-app_version";
    private const string x_rpc_device_id = "x-rpc-device_id";
    private const string x_rpc_client_type = "x-rpc-client_type";
    private const string UAContent = $"Mozilla/5.0 miHoYoBBS/{AppVersion}";
    private const string AppVersion = "2.49.1";
    private static readonly string DeviceId = Guid.NewGuid().ToString("D");

    #endregion


    private readonly HttpClient _httpClient;



    /// <summary>
    /// 传入参数为空时会自动构造新的 <see cref="HttpClient"/>
    /// </summary>
    /// <param name="httpClient"></param>
    public GameRecordClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });
    }



    private async Task<T> CommonSendAsync<T>(HttpRequestMessage request, CancellationToken? cancellationToken = null) where T : class
    {
        request.Headers.Add(Accept, Application_Json);
        request.Headers.Add(UserAgent, UAContent);
        var response = await _httpClient.SendAsync(request, cancellationToken ?? CancellationToken.None);
        response.EnsureSuccessStatusCode();
#if DEBUG
        var content = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize(content, typeof(MihoyoApiWrapper<T>), GameRecordJsonContext.Default) as MihoyoApiWrapper<T>;
#else
        var responseData = await response.Content.ReadFromJsonAsync(typeof(MihoyoApiWrapper<T>), GameRecordJsonContext.Default) as MihoyoApiWrapper<T>;
#endif
        if (responseData is null)
        {
            throw new MihoyoApiException(-1, "Can not parse the response body.");
        }
        if (responseData.Retcode != 0)
        {
            throw new MihoyoApiException(responseData.Retcode, responseData.Message);
        }
        return responseData.Data;
    }



    private async Task CommonSendAsync(HttpRequestMessage request, CancellationToken? cancellationToken = null)
    {
        await CommonSendAsync<object>(request, cancellationToken ?? CancellationToken.None);
        return;
    }




    /// <summary>
    /// 获取星穹铁道账号信息
    /// </summary>
    /// <param name="cookie"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">输入的 <c>cookie</c> 为空</exception>
    public async Task<List<GameRoleInfo>> GetGameRoleInfosAsync(string cookie, CancellationToken? cancellationToken = null)
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
        var data = await CommonSendAsync<GameRoleWrapper>(request, cancellationToken);
        data.List?.ForEach(x => x.Cookie = cookie);
        return data.List ?? new List<GameRoleInfo>();
    }





    /// <summary>
    /// 开拓月历总结
    /// </summary>
    /// <param name="role"></param>
    /// <param name="month">还不清楚规律，可能是 202304</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<LedgerSummary> GetLedgerSummaryAsync(GameRoleInfo role, string month = "", CancellationToken? cancellationToken = null)
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
    private async Task<LedgerDetail> GetLedgerDetailByPageAsync(GameRoleInfo role, string month, int type, int page, int page_size = 100, CancellationToken? cancellationToken = null)
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
    public async Task<LedgerDetail> GetLedgerDetailAsync(GameRoleInfo role, string month, int type, int page_size = 100, CancellationToken? cancellationToken = null)
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
    public async Task<object> GetSpiralAbyssInfoAsync(GameRoleInfo role, int schedule, CancellationToken? cancellationToken = null)
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