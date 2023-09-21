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
        string game_biz = Regex.Match(url, "game_biz=([^&]+)").Groups[1].Value;
        if (game_biz != gameBiz.ToString())
        {
            throw new ArgumentException($"Input url doesn't match the game region ({gameBiz}).", nameof(url));
        }
        this.gameBiz = gameBiz;
        authQuery = new Uri(url).Query;
        if (gameBiz.ToGame() is GameBiz.GenshinImpact)
        {
            if (url.StartsWith("https://webstatic.mihoyo.com/csc-service-center-fe/index.html"))
            {
                prefixUrl = "https://hk4e-api.mihoyo.com";
            }
            if (url.StartsWith("https://webstatic-sea.hoyoverse.com/csc-service-center-fe/index.html"))
            {
                prefixUrl = "https://hk4e-api-os.hoyoverse.com";
            }
            if (string.IsNullOrWhiteSpace(prefixUrl))
            {
                throw new ArgumentException($"Input url is invalid.", nameof(url));
            }
            await GetGenshinUserInfoAsync(cancellationToken);
        }
        if (gameBiz.ToGame() is GameBiz.StarRail)
        {
            if (url.StartsWith("https://webstatic.mihoyo.com/csc-service-center-fe/index.html"))
            {
                prefixUrl = "https://api-takumi.mihoyo.com";
            }
            if (url.StartsWith("https://webstatic-sea.hoyoverse.com/csc-service-center-fe/index.html"))
            {
                prefixUrl = "https://api-os-takumi.hoyoverse.com";
            }
            if (string.IsNullOrWhiteSpace(prefixUrl))
            {
                throw new ArgumentException($"Input url is invalid.", nameof(url));
            }
            await GetStarRailUserInfoAsync(cancellationToken);
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
        if (gameBiz is GameBiz.None
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




    public async Task<List<StarRailQueryItem>> GetStarRailQueryItemsAsync(StarRailQueryType type, int page, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        string url = type switch
        {
            StarRailQueryType.Stellar => $"{prefixUrl}/common/hkrpg_self_help_inquiry/Stellar/GetList{authQuery}&page={page}&page_size={pageSize}",
            StarRailQueryType.Dreams => $"{prefixUrl}/common/hkrpg_self_help_inquiry/Dreams/GetList{authQuery}&page={page}&page_size={pageSize}",
            StarRailQueryType.Relic => $"{prefixUrl}/common/hkrpg_self_help_inquiry/Relic/GetList{authQuery}&page={page}&page_size={pageSize}",
            StarRailQueryType.Cone => $"{prefixUrl}/common/hkrpg_self_help_inquiry/Cone/GetList{authQuery}&page={page}&page_size={pageSize}",
            StarRailQueryType.Power => $"{prefixUrl}/common/hkrpg_self_help_inquiry/Power/GetList{authQuery}&page={page}&page_size={pageSize}",
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





}
