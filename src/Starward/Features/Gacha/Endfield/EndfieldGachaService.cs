using Dapper;
using Microsoft.Extensions.Logging;
using Starward.Features.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.Gacha.Endfield;

internal sealed partial class EndfieldGachaService
{
    private const string WebViewHost = "https://ef-webview.hypergryph.com";
    private const string RoleApi = "https://u8.hypergryph.com/game/role/v1/query_role_list";
    private const string OAuthGrantApi = "https://as.hypergryph.com/user/oauth2/v2/grant";
    private const string BindingListApi = "https://binding-api-account-prod.hypergryph.com/account/binding/v1/binding_list";
    private const string U8TokenApi = "https://binding-api-account-prod.hypergryph.com/account/binding/v1/u8_token_by_uid";
    private const string BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.6422.112 Safari/537.36";
    private const string EndfieldAppCode = "be36d44aa36bfb5b";

    private static readonly (string Key, string Name)[] CharacterPools =
    [
        ("E_CharacterGachaPoolType_Special", "特许寻访"),
        ("E_CharacterGachaPoolType_Joint", "辉光庆典"),
        ("E_CharacterGachaPoolType_Standard", "基础寻访"),
        ("E_CharacterGachaPoolType_Beginner", "启程寻访"),
    ];

    private readonly ILogger<EndfieldGachaService> _logger;
    private readonly HttpClient _httpClient;

    public EndfieldGachaService(ILogger<EndfieldGachaService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public List<EndfieldGachaAccount> GetAccounts()
    {
        using var connection = DatabaseService.CreateConnection();
        return connection.Query<EndfieldGachaAccount>(
            "SELECT * FROM EndfieldGachaAccount ORDER BY LastSyncTime DESC;").ToList();
    }

    public List<EndfieldGachaItem> GetItems(string accountKey, string recordType)
    {
        using var connection = DatabaseService.CreateConnection();
        List<EndfieldGachaItem> items = connection.Query<EndfieldGachaItem>(
            """
            SELECT item.*, info.Icon
            FROM EndfieldGachaItem item
            LEFT JOIN EndfieldGachaInfo info ON item.RecordType = info.RecordType AND item.ItemId = info.ItemId
            WHERE item.AccountKey = @accountKey AND item.RecordType = @recordType;
            """, new { accountKey, recordType }).ToList();
        foreach (EndfieldGachaItem item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Icon))
            {
                item.Icon = GetFallbackIconUrl(item.RecordType, item.ItemId);
            }
        }
        return items;
    }

    public bool HasSavedLoginToken(string accountKey)
    {
        using var connection = DatabaseService.CreateConnection();
        return connection.QueryFirst<int>(
            "SELECT COUNT(*) FROM EndfieldGachaAuth WHERE AccountKey = @accountKey;", new { accountKey }) > 0;
    }

    public async Task<List<EndfieldGachaAccount>> AddAccountsFromLoginTokenAsync(string loginToken,
        IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        loginToken = loginToken.Trim();
        if (string.IsNullOrWhiteSpace(loginToken))
        {
            throw new ArgumentException("登录 Token 不能为空。", nameof(loginToken));
        }

        progress?.Report("正在验证鹰角账号");
        string oauthToken = await GetOAuthTokenAsync(loginToken, cancellationToken);
        progress?.Report("正在读取终末地角色");
        List<EndfieldGachaAccount> accounts = await QueryBindingAccountsAsync(oauthToken, cancellationToken);
        if (accounts.Count == 0)
        {
            throw new InvalidOperationException("该鹰角账号下没有可用的终末地角色。");
        }

        SaveAccountsAndLoginToken(accounts, loginToken);
        return accounts;
    }

    public async Task<EndfieldGachaSyncResult> SyncAccountAsync(EndfieldGachaAccount account,
        IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        string loginToken = GetSavedLoginToken(account.AccountKey);
        progress?.Report("正在刷新账号授权");
        string oauthToken = await GetOAuthTokenAsync(loginToken, cancellationToken);
        string u8Token = await GetU8TokenAsync(account.Uid, oauthToken, cancellationToken);
        return await SyncWithU8TokenAsync(account, u8Token, account.ServerId, progress, cancellationToken);
    }

    public async Task<EndfieldGachaSyncResult> SyncFromGameLogAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report("正在读取游戏内寻访记录链接");
        string url = ReadLatestGachaUrl();
        Dictionary<string, string> parameters = ParseQueryParameters(url);
        if (!parameters.TryGetValue("u8_token", out string? token) || string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("寻访记录链接中缺少授权参数，请在游戏内重新打开寻访记录后再试。");
        }

        string serverId = parameters.GetValueOrDefault("server_id",
            parameters.GetValueOrDefault("server", "1"));
        progress?.Report("正在识别终末地账号");
        EndfieldGachaAccount account = await QueryAccountAsync(token, serverId, cancellationToken);
        return await SyncWithU8TokenAsync(account, token, serverId, progress, cancellationToken);
    }

    private async Task<EndfieldGachaSyncResult> SyncWithU8TokenAsync(EndfieldGachaAccount account, string token,
        string serverId, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        HashSet<string> knownCharacters = GetKnownSeqIds(account.AccountKey, "char");
        HashSet<string> knownWeapons = GetKnownSeqIds(account.AccountKey, "weapon");
        var items = new List<EndfieldGachaItem>();
        var failedPools = new List<string>();

        foreach ((string poolType, string poolName) in CharacterPools)
        {
            try
            {
                items.AddRange(await FetchCharacterPoolAsync(account.AccountKey, token, serverId, poolType,
                    poolName, knownCharacters, progress, cancellationToken));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                failedPools.Add(poolName);
                _logger.LogWarning("Failed to sync Endfield character pool {PoolName}: {ErrorType}", poolName, ex.GetType().Name);
            }
        }

        try
        {
            List<(string Id, string Name)> weaponPools = await FetchWeaponPoolsAsync(token, serverId, cancellationToken);
            foreach ((string poolId, string poolName) in weaponPools)
            {
                try
                {
                    items.AddRange(await FetchWeaponPoolAsync(account.AccountKey, token, serverId, poolId,
                        poolName, knownWeapons, progress, cancellationToken));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failedPools.Add(poolName);
                    _logger.LogWarning("Failed to sync Endfield weapon pool {PoolName}: {ErrorType}", poolName, ex.GetType().Name);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            failedPools.Add("武器池列表");
            _logger.LogWarning("Failed to sync Endfield weapon pool list: {ErrorType}", ex.GetType().Name);
        }

        try
        {
            progress?.Report("正在更新角色与武器图标");
            await UpdateGachaInfoAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to update Endfield gacha icons: {ErrorType}", ex.GetType().Name);
        }

        account.LastSyncTime = DateTimeOffset.Now;
        int newCount = Save(account, items);
        using var connection = DatabaseService.CreateConnection();
        int totalCount = connection.QueryFirst<int>(
            "SELECT COUNT(*) FROM EndfieldGachaItem WHERE AccountKey = @accountKey;",
            new { accountKey = account.AccountKey });
        return new(account, newCount, totalCount, failedPools.Distinct().ToArray());
    }

    private void SaveAccountsAndLoginToken(List<EndfieldGachaAccount> accounts, string loginToken)
    {
        byte[] protectedToken = EndfieldTokenProtector.Protect(loginToken);
        using var connection = DatabaseService.CreateConnection();
        using var transaction = connection.BeginTransaction();
        foreach (EndfieldGachaAccount account in accounts)
        {
            connection.Execute(
                """
                INSERT INTO EndfieldGachaAccount
                (AccountKey, Uid, RoleId, RoleName, ServerId, ServerName, LastSyncTime)
                VALUES (@AccountKey, @Uid, @RoleId, @RoleName, @ServerId, @ServerName, @LastSyncTime)
                ON CONFLICT(AccountKey) DO UPDATE SET
                    Uid = excluded.Uid,
                    RoleId = excluded.RoleId,
                    RoleName = excluded.RoleName,
                    ServerId = excluded.ServerId,
                    ServerName = excluded.ServerName;
                """, account, transaction);
            connection.Execute(
                """
                INSERT INTO EndfieldGachaAuth (AccountKey, LoginToken, UpdateTime)
                VALUES (@AccountKey, @LoginToken, @UpdateTime)
                ON CONFLICT(AccountKey) DO UPDATE SET
                    LoginToken = excluded.LoginToken,
                    UpdateTime = excluded.UpdateTime;
                """, new
                {
                    account.AccountKey,
                    LoginToken = protectedToken,
                    UpdateTime = DateTimeOffset.Now,
                }, transaction);
        }
        transaction.Commit();
    }

    private string GetSavedLoginToken(string accountKey)
    {
        using var connection = DatabaseService.CreateConnection();
        byte[]? protectedToken = connection.QueryFirstOrDefault<byte[]>(
            "SELECT LoginToken FROM EndfieldGachaAuth WHERE AccountKey = @accountKey;", new { accountKey });
        if (protectedToken is null || protectedToken.Length == 0)
        {
            throw new InvalidOperationException("此角色尚未登录鹰角账号，请重新登录后同步。");
        }
        try
        {
            return EndfieldTokenProtector.Unprotect(protectedToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("已保存的鹰角账号登录信息无法读取，请重新登录。", ex);
        }
    }

    private async Task<string> GetOAuthTokenAsync(string loginToken, CancellationToken cancellationToken)
    {
        using var request = CreateBrowserRequest(HttpMethod.Post, OAuthGrantApi);
        request.Content = JsonContent.Create(new { token = loginToken, appCode = EndfieldAppCode, type = 1 });
        using JsonDocument document = await SendJsonAsync(request, "鹰角账号授权", cancellationToken);
        JsonElement root = document.RootElement;
        if (GetInt32(root, "status") != 0 || !root.TryGetProperty("data", out JsonElement data))
        {
            throw new InvalidOperationException("鹰角账号登录状态已失效，请重新登录。");
        }
        string token = GetString(data, "token");
        return !string.IsNullOrWhiteSpace(token)
            ? token
            : throw new InvalidOperationException("鹰角账号授权未返回有效凭证，请重新登录。");
    }

    private async Task<List<EndfieldGachaAccount>> QueryBindingAccountsAsync(string oauthToken,
        CancellationToken cancellationToken)
    {
        string url = $"{BindingListApi}?token={Uri.EscapeDataString(oauthToken)}&appCode=endfield";
        using var request = CreateBrowserRequest(HttpMethod.Get, url);
        using JsonDocument document = await SendJsonAsync(request, "终末地角色查询", cancellationToken);
        JsonElement root = document.RootElement;
        if (GetInt32(root, "status") != 0 || !root.TryGetProperty("data", out JsonElement data) ||
            !data.TryGetProperty("list", out JsonElement list) || list.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("无法读取该鹰角账号的终末地角色。");
        }

        var accounts = new List<EndfieldGachaAccount>();
        foreach (JsonElement app in list.EnumerateArray())
        {
            if (!string.Equals(GetString(app, "appCode"), "endfield", StringComparison.OrdinalIgnoreCase) ||
                !app.TryGetProperty("bindingList", out JsonElement bindings) || bindings.ValueKind != JsonValueKind.Array)
            {
                continue;
            }
            foreach (JsonElement binding in bindings.EnumerateArray())
            {
                string uid = GetString(binding, "uid");
                if (string.IsNullOrWhiteSpace(uid) || !binding.TryGetProperty("roles", out JsonElement roles) ||
                    roles.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }
                foreach (JsonElement role in roles.EnumerateArray())
                {
                    string roleId = GetString(role, "roleId");
                    if (string.IsNullOrWhiteSpace(roleId))
                    {
                        continue;
                    }
                    string serverId = GetString(role, "serverId", "1");
                    accounts.Add(new EndfieldGachaAccount
                    {
                        AccountKey = $"{uid}_{roleId}",
                        Uid = uid,
                        RoleId = roleId,
                        RoleName = GetString(role, "nickName"),
                        ServerId = string.IsNullOrWhiteSpace(serverId) ? "1" : serverId,
                        ServerName = GetString(role, "serverName", "中国"),
                        LastSyncTime = DateTimeOffset.UnixEpoch,
                    });
                }
            }
        }
        return accounts.DistinctBy(x => x.AccountKey).ToList();
    }

    private async Task<string> GetU8TokenAsync(string uid, string oauthToken, CancellationToken cancellationToken)
    {
        using var request = CreateBrowserRequest(HttpMethod.Post, U8TokenApi);
        request.Content = JsonContent.Create(new { uid, token = oauthToken });
        using JsonDocument document = await SendJsonAsync(request, "寻访授权刷新", cancellationToken);
        JsonElement root = document.RootElement;
        if (GetInt32(root, "status") != 0 || !root.TryGetProperty("data", out JsonElement data))
        {
            throw new InvalidOperationException("无法刷新寻访授权，请重新登录鹰角账号。");
        }
        string token = GetString(data, "token");
        return !string.IsNullOrWhiteSpace(token)
            ? token
            : throw new InvalidOperationException("官方接口未返回有效的寻访授权，请重新登录。");
    }

    private async Task<JsonDocument> SendJsonAsync(HttpRequestMessage request, string operation,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"{operation}失败（HTTP {(int)response.StatusCode}）。");
        }
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);
    }

    private static HttpRequestMessage CreateBrowserRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("User-Agent", BrowserUserAgent);
        return request;
    }

    private HashSet<string> GetKnownSeqIds(string accountKey, string recordType)
    {
        using var connection = DatabaseService.CreateConnection();
        return connection.Query<string>(
            "SELECT SeqId FROM EndfieldGachaItem WHERE AccountKey = @accountKey AND RecordType = @recordType;",
            new { accountKey, recordType }).ToHashSet(StringComparer.Ordinal);
    }

    private int Save(EndfieldGachaAccount account, List<EndfieldGachaItem> items)
    {
        using var connection = DatabaseService.CreateConnection();
        using var transaction = connection.BeginTransaction();
        connection.Execute(
            """
            INSERT OR REPLACE INTO EndfieldGachaAccount
            (AccountKey, Uid, RoleId, RoleName, ServerId, ServerName, LastSyncTime)
            VALUES (@AccountKey, @Uid, @RoleId, @RoleName, @ServerId, @ServerName, @LastSyncTime);
            """, account, transaction);
        int count = connection.Execute(
            """
            INSERT OR IGNORE INTO EndfieldGachaItem
            (AccountKey, RecordType, SeqId, ItemId, ItemName, ItemType, Rarity, IsNew, IsFree, PoolId, PoolName, PoolType, GachaTime)
            VALUES (@AccountKey, @RecordType, @SeqId, @ItemId, @ItemName, @ItemType, @Rarity, @IsNew, @IsFree, @PoolId, @PoolName, @PoolType, @GachaTime);
            """, items, transaction);
        transaction.Commit();
        return count;
    }

    private async Task<EndfieldGachaAccount> QueryAccountAsync(string token, string serverId, CancellationToken cancellationToken)
    {
        using var request = CreateBrowserRequest(HttpMethod.Post, RoleApi);
        request.Content = JsonContent.Create(new { token, serverId });
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"游戏日志授权不可用（HTTP {(int)response.StatusCode}），请改用鹰角账号登录同步。");
        }
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        JsonElement root = document.RootElement;
        int status = root.TryGetProperty("status", out JsonElement statusElement) ? statusElement.GetInt32() : -1;
        if (status != 0 || !root.TryGetProperty("data", out JsonElement data))
        {
            throw new InvalidOperationException("游戏日志授权已失效，请改用鹰角账号登录同步。");
        }

        string uid = GetString(data, "uid");
        JsonElement selectedRole = default;
        if (data.TryGetProperty("roles", out JsonElement roles) && roles.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement role in roles.EnumerateArray())
            {
                if (selectedRole.ValueKind == JsonValueKind.Undefined || GetString(role, "serverId") == serverId)
                {
                    selectedRole = role;
                }
                if (GetString(role, "serverId") == serverId)
                {
                    break;
                }
            }
        }
        string roleId = GetString(selectedRole, "roleId");
        if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(roleId))
        {
            throw new InvalidOperationException("官方账号接口未返回有效角色信息。");
        }
        string roleName = GetString(selectedRole, "nickname");
        if (string.IsNullOrWhiteSpace(roleName))
        {
            roleName = GetString(selectedRole, "nickName");
        }
        return new EndfieldGachaAccount
        {
            AccountKey = $"{uid}_{roleId}",
            Uid = uid,
            RoleId = roleId,
            RoleName = roleName,
            ServerId = serverId,
            ServerName = GetString(selectedRole, "serverName"),
        };
    }

    private async Task<List<EndfieldGachaItem>> FetchCharacterPoolAsync(string accountKey, string token, string serverId,
        string poolType, string poolName, HashSet<string> known, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var result = new List<EndfieldGachaItem>();
        string nextSeqId = "";
        int page = 0;
        bool hasMore;
        do
        {
            page++;
            progress?.Report($"正在同步{poolName} · 第 {page} 页");
            var query = new Dictionary<string, string>
            {
                ["lang"] = "zh-cn",
                ["token"] = token,
                ["server_id"] = serverId,
                ["pool_type"] = poolType,
            };
            if (!string.IsNullOrWhiteSpace(nextSeqId))
            {
                query["seq_id"] = nextSeqId;
            }
            using JsonDocument document = await GetApiJsonAsync($"{WebViewHost}/api/record/char", query, cancellationToken);
            JsonElement data = GetApiData(document.RootElement);
            List<JsonElement> list = data.GetProperty("list").EnumerateArray().ToList();
            bool reachedKnownItem = false;
            foreach (JsonElement item in list)
            {
                string seqId = GetString(item, "seqId");
                if (known.Contains(seqId))
                {
                    reachedKnownItem = true;
                    continue;
                }
                known.Add(seqId);
                result.Add(new EndfieldGachaItem
                {
                    AccountKey = accountKey,
                    RecordType = "char",
                    SeqId = seqId,
                    ItemId = GetString(item, "charId"),
                    ItemName = GetString(item, "charName"),
                    ItemType = "干员",
                    Rarity = GetInt32(item, "rarity"),
                    IsNew = GetBoolean(item, "isNew"),
                    IsFree = GetBoolean(item, "isFree"),
                    PoolId = GetString(item, "poolId"),
                    PoolName = GetString(item, "poolName", poolName),
                    PoolType = poolType,
                    GachaTime = GetString(item, "gachaTs"),
                });
            }
            hasMore = !reachedKnownItem && GetBoolean(data, "hasMore") && list.Count > 0;
            nextSeqId = list.Count > 0 ? GetString(list[^1], "seqId") : "";
            if (hasMore)
            {
                await Task.Delay(Random.Shared.Next(500, 1001), cancellationToken);
            }
        }
        while (hasMore);
        return result;
    }

    private async Task<List<(string Id, string Name)>> FetchWeaponPoolsAsync(string token, string serverId, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string>
        {
            ["lang"] = "zh-cn",
            ["token"] = token,
            ["server_id"] = serverId,
        };
        using JsonDocument document = await GetApiJsonAsync($"{WebViewHost}/api/record/weapon/pool", query, cancellationToken);
        JsonElement data = GetApiData(document.RootElement);
        return data.EnumerateArray()
            .Select(x => (GetString(x, "poolId"), GetString(x, "poolName")))
            .Where(x => !string.IsNullOrWhiteSpace(x.Item1))
            .ToList();
    }

    private async Task<List<EndfieldGachaItem>> FetchWeaponPoolAsync(string accountKey, string token, string serverId,
        string poolId, string poolName, HashSet<string> known, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var result = new List<EndfieldGachaItem>();
        string nextSeqId = "";
        int page = 0;
        bool hasMore;
        do
        {
            page++;
            progress?.Report($"正在同步{poolName} · 第 {page} 页");
            var query = new Dictionary<string, string>
            {
                ["lang"] = "zh-cn",
                ["token"] = token,
                ["server_id"] = serverId,
                ["pool_id"] = poolId,
            };
            if (!string.IsNullOrWhiteSpace(nextSeqId))
            {
                query["seq_id"] = nextSeqId;
            }
            using JsonDocument document = await GetApiJsonAsync($"{WebViewHost}/api/record/weapon", query, cancellationToken);
            JsonElement data = GetApiData(document.RootElement);
            List<JsonElement> list = data.GetProperty("list").EnumerateArray().ToList();
            bool reachedKnownItem = false;
            foreach (JsonElement item in list)
            {
                string seqId = GetString(item, "seqId");
                if (known.Contains(seqId))
                {
                    reachedKnownItem = true;
                    continue;
                }
                known.Add(seqId);
                result.Add(new EndfieldGachaItem
                {
                    AccountKey = accountKey,
                    RecordType = "weapon",
                    SeqId = seqId,
                    ItemId = GetString(item, "weaponId"),
                    ItemName = GetString(item, "weaponName"),
                    ItemType = GetString(item, "weaponType", "武器"),
                    Rarity = GetInt32(item, "rarity"),
                    IsNew = GetBoolean(item, "isNew"),
                    PoolId = poolId,
                    PoolName = GetString(item, "poolName", poolName),
                    GachaTime = GetString(item, "gachaTs"),
                });
            }
            hasMore = !reachedKnownItem && GetBoolean(data, "hasMore") && list.Count > 0;
            nextSeqId = list.Count > 0 ? GetString(list[^1], "seqId") : "";
            if (hasMore)
            {
                await Task.Delay(Random.Shared.Next(500, 1001), cancellationToken);
            }
        }
        while (hasMore);
        return result;
    }

    private async Task<JsonDocument> GetApiJsonAsync(string endpoint, Dictionary<string, string> query, CancellationToken cancellationToken)
    {
        string url = endpoint + "?" + string.Join('&', query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var request = CreateBrowserRequest(HttpMethod.Get, url);
                using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"HTTP {(int)response.StatusCode}");
                }
                return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch when (attempt < 3)
            {
                await Task.Delay(Random.Shared.Next(500, 901), cancellationToken);
            }
        }
        throw new InvalidOperationException("官方寻访记录接口连续请求失败。");
    }

    private static JsonElement GetApiData(JsonElement root)
    {
        int code = root.TryGetProperty("code", out JsonElement codeElement) ? codeElement.GetInt32() : -1;
        if (code == 40100)
        {
            throw new InvalidOperationException("寻访授权已失效，请重新登录鹰角账号后再试。");
        }
        if (code != 0 || !root.TryGetProperty("data", out JsonElement data))
        {
            throw new InvalidOperationException("官方寻访记录接口返回异常。");
        }
        return data;
    }

    private static string ReadLatestGachaUrl()
    {
        string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData", "LocalLow", "Hypergryph", "Endfield", "sdklogs", "HGWebview.log");
        if (!File.Exists(logPath))
        {
            throw new FileNotFoundException("未找到终末地寻访日志，请先启动游戏并打开一次寻访记录。", logPath);
        }

        string? latestUrl = null;
        using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is string line)
        {
            Match match = GachaUrlRegex().Match(line);
            if (match.Success)
            {
                latestUrl = WebUtility.HtmlDecode(match.Value).TrimEnd(']', ')', ',', ';');
            }
        }
        return latestUrl ?? throw new InvalidOperationException("日志中没有寻访记录链接，请在游戏内打开一次寻访记录后再试。");
    }

    private static Dictionary<string, string> ParseQueryParameters(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            throw new InvalidOperationException("日志中的寻访记录链接格式无效。");
        }
        return uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split('=', 2))
            .Where(x => x.Length == 2)
            .ToDictionary(x => WebUtility.UrlDecode(x[0]), x => WebUtility.UrlDecode(x[1]), StringComparer.OrdinalIgnoreCase);
    }

    private static string GetString(JsonElement element, string name, string defaultValue = "")
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement value))
        {
            return value.ValueKind == JsonValueKind.String ? value.GetString() ?? defaultValue : value.ToString();
        }
        return defaultValue;
    }

    private static int GetInt32(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out JsonElement value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number))
            {
                return number;
            }
            if (int.TryParse(value.ToString(), out number))
            {
                return number;
            }
        }
        return 0;
    }

    private static bool GetBoolean(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value))
        {
            return false;
        }
        return value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.Number && value.GetInt32() != 0 ||
               value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out bool result) && result;
    }

    [GeneratedRegex("https://ef-webview\\.hypergryph\\.com/page/gacha_[^\\s\\\"']+")]
    private static partial Regex GachaUrlRegex();
}
