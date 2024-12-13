using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace Starward.Core.SelfQuery;

public class SelfQueryClient
{


    protected readonly HttpClient _httpClient;




    public SelfQueryClient(HttpClient? httpClient = null)
    {
        if (httpClient is null)
        {
            _httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All });
        }
        else
        {
            _httpClient = httpClient;
        }
    }




    private async Task<T> CommonGetAsync<T>(string url, CancellationToken cancellationToken = default) where T : class
    {
        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var wrapper = await response.Content.ReadFromJsonAsync(typeof(miHoYoApiWrapper<T>), SelfQueryJsonContext.Default, cancellationToken) as miHoYoApiWrapper<T>;
        if (wrapper is null)
        {
            throw new miHoYoApiException(-1, "Can not parse the response body.");
        }
        if (wrapper.Retcode != 0)
        {
            throw new miHoYoApiException(wrapper.Retcode, wrapper.Message);
        }
        return wrapper.Data;
    }



    private GameBiz gameBiz;

    private string? authQuery;

    private string? prefixUrl;



    public SelfQueryUserInfo? UserInfo { get; private set; }



    public async Task<SelfQueryUserInfo> InitializeAsync(string url, GameBiz gameBiz, CancellationToken cancellationToken = default)
    {
        Reset();
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentNullException(nameof(url));
        }
        string game_biz = Regex.Match(url, "game_biz=([^&#]+)").Groups[1].Value;
        if (game_biz != gameBiz.ToString())
        {
            throw new ArgumentException($"Input url doesn't match the game region ({gameBiz}).", nameof(url));
        }
        this.gameBiz = gameBiz;
        authQuery = new Uri(url).Query;
        if (gameBiz.ToGame() == GameBiz.hk4e)
        {
            if (url.StartsWith("https://webstatic.mihoyo.com/csc-service-center-fe/index.html") || url.StartsWith("https://webstatic.mihoyo.com/static/mihoyo-new-csc-service-hall-fe/index.html"))
            {
                prefixUrl = "https://public-operation-hk4e.mihoyo.com";
            }
            if (url.StartsWith("https://cs.hoyoverse.com/csc-service-center-fe/index.html") || url.StartsWith("https://cs.hoyoverse.com/static/hoyoverse-new-csc-service-hall-fe/index.html"))
            {
                prefixUrl = "https://public-operation-hk4e-sg.hoyoverse.com";
            }
            if (string.IsNullOrWhiteSpace(prefixUrl))
            {
                throw new ArgumentException($"Input url is invalid.", nameof(url));
            }
            await GetGenshinUserInfoAsync(cancellationToken);
        }
        if (gameBiz.ToGame() == GameBiz.hkrpg)
        {
            if (url.StartsWith("https://webstatic.mihoyo.com/csc-service-center-fe/index.html") || url.StartsWith("https://webstatic.mihoyo.com/static/mihoyo-new-csc-service-hall-fe/index.html"))
            {
                prefixUrl = "https://api-takumi.mihoyo.com";
            }
            if (url.StartsWith("https://cs.hoyoverse.com/csc-service-center-fe/index.html") || url.StartsWith("https://cs.hoyoverse.com/static/hoyoverse-new-csc-service-hall-fe/index.html"))
            {
                prefixUrl = "https://public-operation-hkrpg-sg.hoyoverse.com";
            }
            if (string.IsNullOrWhiteSpace(prefixUrl))
            {
                throw new ArgumentException($"Input url is invalid.", nameof(url));
            }
            await GetStarRailUserInfoAsync(cancellationToken);
        }
        if (gameBiz.ToGame() == GameBiz.nap)
        {
            if (url.StartsWith("https://webstatic.mihoyo.com/csc-service-center-fe/index.html") || url.StartsWith("https://webstatic.mihoyo.com/static/mihoyo-new-csc-service-hall-fe/index.html"))
            {
                prefixUrl = "https://public-operation-nap.mihoyo.com";
            }
            if (url.StartsWith("https://cs.hoyoverse.com/csc-service-center-fe/index.html") || url.StartsWith("https://cs.hoyoverse.com/static/hoyoverse-new-csc-service-hall-fe/index.html"))
            {
                prefixUrl = "https://public-operation-nap-sg.hoyoverse.com";
            }
            if (string.IsNullOrWhiteSpace(prefixUrl))
            {
                throw new ArgumentException($"Input url is invalid.", nameof(url));
            }
            await GetZZZUserInfoAsync(cancellationToken);
        }
        if (UserInfo is null)
        {
            throw new ArgumentException($"Input url is invalid.", nameof(url));
        }
        return UserInfo;
    }


    public void Reset()
    {
        gameBiz = GameBiz.None;
        authQuery = null;
        prefixUrl = null;
        UserInfo = null;
    }



    public void EnsureInitialized()
    {
        if (!gameBiz.IsKnown()
            || UserInfo is null
            || string.IsNullOrWhiteSpace(authQuery)
            || string.IsNullOrWhiteSpace(prefixUrl))
        {
            throw new Exception("Not initialized.");
        }
    }




    #region Genshin



    public async Task<SelfQueryUserInfo> GetGenshinUserInfoAsync(CancellationToken cancellationToken = default)
    {
        string url = $"{prefixUrl}/common/hk4e_self_help_query/User/GetUserInfo{authQuery}";
        UserInfo ??= await CommonGetAsync<SelfQueryUserInfo>(url, cancellationToken);
        UserInfo.GameBiz = gameBiz;
        return UserInfo;
    }




    public async Task<List<GenshinQueryItem>> GetGenshinQueryItemsAsync(GenshinQueryType type, long endId, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        string url = type switch
        {
            GenshinQueryType.Crystal => $"{prefixUrl}/common/hk4e_self_help_query/User/GetCrystalLog{authQuery}&size=20&selfquery_type=1&end_id={endId}",
            GenshinQueryType.Primogem => $"{prefixUrl}/common/hk4e_self_help_query/User/GetPrimogemLog{authQuery}&size=20&selfquery_type=1&end_id={endId}",
            GenshinQueryType.Resin => $"{prefixUrl}/common/hk4e_self_help_query/User/GetResinLog{authQuery}&size=20&selfquery_type=4&end_id={endId}",
            GenshinQueryType.Artifact => $"{prefixUrl}/common/hk4e_self_help_query/User/GetArtifactLog{authQuery}&size=20&selfquery_type=2&end_id={endId}",
            GenshinQueryType.Weapon => $"{prefixUrl}/common/hk4e_self_help_query/User/GetWeaponLog{authQuery}&size=20&selfquery_type=4&end_id={endId}",
            _ => throw new ArgumentOutOfRangeException($"Unknown query type ({type})", nameof(type)),
        };
        var wrapper = await CommonGetAsync<SelfQueryListWrapper<GenshinQueryItem>>(url, cancellationToken);
        var list = wrapper.List ?? new List<GenshinQueryItem>(0);
        long uid = UserInfo?.Uid ?? 0;
        foreach (var item in list)
        {
            item.Uid = uid;
            item.Type = type;
        }
        return list;
    }




    #endregion






    #region Star Rail



    public async Task<SelfQueryUserInfo> GetStarRailUserInfoAsync(CancellationToken cancellationToken = default)
    {
        string url = $"{prefixUrl}/common/hkrpg_self_help_inquiry/UserInfo/GetUserInfo{authQuery}";
        UserInfo ??= await CommonGetAsync<SelfQueryUserInfo>(url, cancellationToken);
        UserInfo.GameBiz = gameBiz;
        return UserInfo;
    }




    public async Task<List<StarRailQueryItem>> GetStarRailQueryItemsAsync(StarRailQueryType type, long endId, int size = 20, DateTime? beginTime = null, DateTime? endTime = null, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        string url = type switch
        {
            StarRailQueryType.Stellar => $"{prefixUrl}/common/hkrpg_self_help_inquiry/Stellar/GetList{authQuery}&end_id={endId}&size={size}&begin_time={beginTime}&end_time={endTime}",
            StarRailQueryType.Dreams => $"{prefixUrl}/common/hkrpg_self_help_inquiry/Dreams/GetList{authQuery}&end_id={endId}&size={size}&begin_time={beginTime}&end_time={endTime}",
            StarRailQueryType.Relic => $"{prefixUrl}/common/hkrpg_self_help_inquiry/Relic/GetList{authQuery}&end_id={endId}&size={size}&begin_time={beginTime}&end_time={endTime}",
            StarRailQueryType.Cone => $"{prefixUrl}/common/hkrpg_self_help_inquiry/Cone/GetList{authQuery}&end_id={endId}&size={size}&begin_time={beginTime}&end_time={endTime}",
            StarRailQueryType.Power => $"{prefixUrl}/common/hkrpg_self_help_inquiry/Power/GetList{authQuery}&end_id={endId}&size={size}&begin_time={beginTime}&end_time={endTime}",
            _ => throw new ArgumentOutOfRangeException($"Unknown query type ({type})", nameof(type)),
        };
        var wrapper = await CommonGetAsync<SelfQueryListWrapper<StarRailQueryItem>>(url, cancellationToken);
        var list = wrapper.List ?? new List<StarRailQueryItem>(0);
        foreach (var item in list)
        {
            item.Type = type;
        }
        return list;
    }




    #endregion




    #region ZZZ



    public async Task<SelfQueryUserInfo> GetZZZUserInfoAsync(CancellationToken cancellationToken = default)
    {
        string url = $"{prefixUrl}/common/nap_self_help_query/UserInfo/GetUserInfo{authQuery}";
        UserInfo ??= await CommonGetAsync<SelfQueryUserInfo>(url, cancellationToken);
        UserInfo.GameBiz = gameBiz;
        return UserInfo;
    }



    public async Task<List<ZZZQueryItem>> GetZZZQueryItemsAsync(ZZZQueryType type, long endId, int size = 20, DateTime? beginTime = null, DateTime? endTime = null, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        string url = type switch
        {
            ZZZQueryType.Monochrome => $"{prefixUrl}/common/nap_self_help_query/Coin/GetList{authQuery}&coin_type=monochrome_film&end_id={endId}&size={size}&begin_time={beginTime}&end_time={endTime}",
            ZZZQueryType.Ploychrome => $"{prefixUrl}/common/nap_self_help_query/Coin/GetList{authQuery}&coin_type=film&end_id={endId}&size={size}&begin_time={beginTime}&end_time={endTime}",
            ZZZQueryType.PurchaseGift => $"{prefixUrl}/common/nap_self_help_query/Coin/GetList{authQuery}&coin_type=purchase_gift&end_id={endId}&size={size}&begin_time={beginTime}&end_time={endTime}",
            ZZZQueryType.Battery => $"{prefixUrl}/common/nap_self_help_query/Battery/GetList{authQuery}&end_id={endId}&size={size}&begin_time={beginTime}&end_time={endTime}",
            ZZZQueryType.Engine => $"{prefixUrl}/common/nap_self_help_query/Engine/GetList{authQuery}&end_id={endId}&size={size}&begin_time={beginTime}&end_time={endTime}",
            ZZZQueryType.Disk => $"{prefixUrl}/common/nap_self_help_query/Disk/GetList{authQuery}&end_id={endId}&size={size}&begin_time={beginTime}&end_time={endTime}",
            ZZZQueryType.BattlePass => $"{prefixUrl}/common/nap_self_help_query/Coin/GetList{authQuery}&coin_type=battle_pass&end_id={endId}&size={size}&begin_time={beginTime}&end_time={endTime}",
            _ => throw new ArgumentOutOfRangeException($"Unknown query type ({type})", nameof(type)),
        };
        var wrapper = await CommonGetAsync<SelfQueryListWrapper<ZZZQueryItem>>(url, cancellationToken);
        var list = wrapper.List ?? new List<ZZZQueryItem>(0);
        foreach (var item in list)
        {
            item.Type = type;
        }
        return list;
    }




    #endregion




}
