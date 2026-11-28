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
    private const string BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.6422.112 Safari/537.36";

    internal static readonly IReadOnlyDictionary<string, string> CharacterPoolNames = new Dictionary<string, string>
    {
        ["E_CharacterGachaPoolType_Special"] = "特许寻访",
        ["E_CharacterGachaPoolType_Rerun"] = "重构寻访",
        ["E_CharacterGachaPoolType_Joint"] = "辉光庆典",
        ["E_CharacterGachaPoolType_Standard"] = "基础寻访",
        ["E_CharacterGachaPoolType_Beginner"] = "启程寻访",
    };

    internal static string GetProviderDomain(EndfieldGachaRegion region) =>
        region == EndfieldGachaRegion.Global ? "gryphline.com" : "hypergryph.com";

    private readonly ILogger<EndfieldGachaService> _logger;
    private readonly HttpClient _httpClient;

    public EndfieldGachaService(ILogger<EndfieldGachaService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public List<EndfieldGachaAccount> GetAccounts(EndfieldGachaRegion region)
    {
        using var connection = DatabaseService.CreateConnection();
        return connection.Query<EndfieldGachaAccount>(
            """
            SELECT AccountKey, Uid AS AccountUid, RoleId, RoleName, ServerId, ServerName, LastSyncTime, Region
            FROM EndfieldGachaAccount WHERE Region = @region ORDER BY LastSyncTime DESC;
            """, new { region }).ToList();
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

    public async Task<List<EndfieldGachaAccount>> AddAccountsFromLoginTokenAsync(EndfieldGachaRegion region, string loginToken,
        IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        loginToken = loginToken.Trim();
        if (string.IsNullOrWhiteSpace(loginToken))
        {
            throw new ArgumentException("登录 Token 不能为空。", nameof(loginToken));
        }

        progress?.Report("正在验证鹰角账号");
        string oauthToken = await GetOAuthTokenAsync(region, loginToken, cancellationToken);
        progress?.Report("正在读取终末地角色");
        List<EndfieldGachaAccount> accounts = await QueryBindingAccountsAsync(region, oauthToken, cancellationToken);
        if (accounts.Count == 0)
        {
            throw new InvalidOperationException(region == EndfieldGachaRegion.Bilibili
                ? "该通行证下没有可用的 B 服角色，请先绑定 B 服角色，或从 B 服游戏日志同步。"
                : "该通行证下没有可用的当前区服角色。");
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
        string oauthToken = await GetOAuthTokenAsync(account.Region, loginToken, cancellationToken);
        string u8Token = await GetU8TokenAsync(account.Region, account.AccountUid, oauthToken, cancellationToken);
        return await SyncWithU8TokenAsync(account, u8Token, progress, cancellationToken);
    }

    public async Task<EndfieldGachaSyncResult> SyncFromGameLogAsync(EndfieldGachaRegion region,
        IProgress<string>? progress = null, CancellationToken cancellationToken = default, string? expectedAccountKey = null)
    {
        progress?.Report("正在读取游戏内寻访记录链接");
        var (token, serverId) = ReadGameLogAuthorization(region);
        progress?.Report("正在识别终末地账号");
        EndfieldGachaAccount account = await QueryAccountAsync(region, token, serverId, cancellationToken);
        if (expectedAccountKey is not null && account.AccountKey != expectedAccountKey)
        {
            throw new InvalidOperationException("游戏日志中的角色与当前所选角色不同。请在游戏内打开所选角色的寻访记录后再同步。");
        }
        return await SyncWithU8TokenAsync(account, token, progress, cancellationToken);
    }

    private async Task<EndfieldGachaSyncResult> SyncWithU8TokenAsync(EndfieldGachaAccount account, string token,
        IProgress<string>? progress, CancellationToken cancellationToken)
    {
        HashSet<string> knownCharacters = GetKnownSeqIds(account.AccountKey, "char");
        HashSet<string> knownWeapons = GetKnownSeqIds(account.AccountKey, "weapon");
        var items = new List<EndfieldGachaItem>();
        var failedPools = new List<string>();

        var characterPools = await FetchCharacterPoolsAsync(account, token, cancellationToken);
        foreach ((string poolType, string poolName) in characterPools)
        {
            try
            {
                items.AddRange(await FetchRecordPoolAsync(account, token, "char", "pool_type", poolType,
                    poolName, knownCharacters, progress, cancellationToken));
            }
            catch (Exception ex) when (ex is not (OperationCanceledException or GachaAuthorizationExpiredException))
            {
                failedPools.Add(poolName);
                _logger.LogWarning("Failed to sync Endfield character pool {PoolName}: {ErrorType}", poolName, ex.GetType().Name);
            }
        }

        try
        {
            var weaponPools = await FetchWeaponPoolsAsync(account, token, cancellationToken);
            foreach ((string parameter, string poolId, string poolName) in weaponPools)
            {
                try
                {
                    items.AddRange(await FetchRecordPoolAsync(account, token, "weapon", parameter, poolId,
                        poolName, knownWeapons, progress, cancellationToken));
                }
                catch (Exception ex) when (ex is not (OperationCanceledException or GachaAuthorizationExpiredException))
                {
                    failedPools.Add(poolName);
                    _logger.LogWarning("Failed to sync Endfield weapon pool {PoolName}: {ErrorType}", poolName, ex.GetType().Name);
                }
            }
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or GachaAuthorizationExpiredException))
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
                (AccountKey, Uid, RoleId, RoleName, ServerId, ServerName, LastSyncTime, Region)
                VALUES (@AccountKey, @AccountUid, @RoleId, @RoleName, @ServerId, @ServerName, @LastSyncTime, @Region)
                ON CONFLICT(AccountKey) DO UPDATE SET
                    Uid = excluded.Uid,
                    RoleId = excluded.RoleId,
                    RoleName = excluded.RoleName,
                    ServerId = excluded.ServerId,
                    ServerName = excluded.ServerName,
                    Region = excluded.Region;
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

    internal async Task ValidateLoginTokenAsync(EndfieldGachaRegion region, string loginToken, CancellationToken cancellationToken)
    {
        // Validate without saving accounts or replacing the previously stored credentials.
        _ = await GetOAuthTokenAsync(region, loginToken, cancellationToken);
    }

    private async Task<string> GetOAuthTokenAsync(EndfieldGachaRegion region, string loginToken, CancellationToken cancellationToken)
    {
        string appCode = region == EndfieldGachaRegion.Global ? "3dacefa138426cfe" : "be36d44aa36bfb5b";
        using var request = CreateBrowserRequest(HttpMethod.Post, $"https://as.{GetProviderDomain(region)}/user/oauth2/v2/grant");
        request.Content = JsonContent.Create(new { token = loginToken, appCode, type = 1 });
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

    private async Task<List<EndfieldGachaAccount>> QueryBindingAccountsAsync(EndfieldGachaRegion region, string oauthToken,
        CancellationToken cancellationToken)
    {
        string url = $"https://binding-api-account-prod.{GetProviderDomain(region)}/account/binding/v1/binding_list"
            + $"?token={Uri.EscapeDataString(oauthToken)}&appCode=endfield";
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
                string accountUid = GetString(binding, "uid");
                if (!MatchesBindingRegion(region, binding) || string.IsNullOrWhiteSpace(accountUid)
                    || !binding.TryGetProperty("roles", out JsonElement roles) ||
                    roles.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }
                foreach (JsonElement role in roles.EnumerateArray())
                {
                    string serverId = region == EndfieldGachaRegion.Global ? GetString(role, "serverId").Trim() : "1";
                    if (!string.IsNullOrWhiteSpace(GetString(role, "roleId")) && serverId.Length > 0)
                    {
                        accounts.Add(CreateAccount(region, accountUid, role, serverId));
                    }
                }
            }
        }
        return accounts.DistinctBy(x => x.AccountKey).ToList();
    }

    private static bool MatchesBindingRegion(EndfieldGachaRegion region, JsonElement binding)
    {
        if (region == EndfieldGachaRegion.Global)
        {
            return true;
        }
        string channel = GetString(binding, "channelName").Trim();
        string channelId = GetString(binding, "channelMasterId");
        bool official = GetBoolean(binding, "isOfficial");
        bool bilibili = !official && (channel.Contains("bilibili", StringComparison.OrdinalIgnoreCase)
            || channel.Contains("哔哩哔哩", StringComparison.Ordinal)
            || channel.Equals("B服", StringComparison.OrdinalIgnoreCase)
            || channel.Equals("B 服", StringComparison.OrdinalIgnoreCase)
            || channelId == "2");
        return region == EndfieldGachaRegion.Bilibili ? bilibili
            : !bilibili && (official || channelId == "1" || !binding.TryGetProperty("isOfficial", out _) && channelId.Length == 0);
    }

    private static EndfieldGachaAccount CreateAccount(EndfieldGachaRegion region, string accountUid, JsonElement role, string serverId)
    {
        string roleId = GetString(role, "roleId");
        string serverName = GetString(role, "serverName");
        return new EndfieldGachaAccount
        {
            AccountKey = region == EndfieldGachaRegion.Global ? $"global_{serverId}_{accountUid}_{roleId}" : $"{accountUid}_{roleId}",
            AccountUid = accountUid,
            Region = region,
            RoleId = roleId,
            RoleName = GetString(role, "nickname", GetString(role, "nickName")),
            ServerId = serverId,
            ServerName = !string.IsNullOrWhiteSpace(serverName) ? serverName : region == EndfieldGachaRegion.Global ? serverId : "中国",
            LastSyncTime = DateTimeOffset.UnixEpoch,
        };
    }

    private async Task<string> GetU8TokenAsync(EndfieldGachaRegion region, string accountUid, string oauthToken,
        CancellationToken cancellationToken)
    {
        using var request = CreateBrowserRequest(HttpMethod.Post,
            $"https://binding-api-account-prod.{GetProviderDomain(region)}/account/binding/v1/u8_token_by_uid");
        request.Content = JsonContent.Create(new { uid = accountUid, token = oauthToken });
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
            (AccountKey, Uid, RoleId, RoleName, ServerId, ServerName, LastSyncTime, Region)
            VALUES (@AccountKey, @AccountUid, @RoleId, @RoleName, @ServerId, @ServerName, @LastSyncTime, @Region);
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

    private async Task<EndfieldGachaAccount> QueryAccountAsync(EndfieldGachaRegion region, string token, string serverId,
        CancellationToken cancellationToken)
    {
        using var request = CreateBrowserRequest(HttpMethod.Post, $"https://u8.{GetProviderDomain(region)}/game/role/v1/query_role_list");
        request.Content = JsonContent.Create(new { token, serverId });
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"游戏日志授权不可用（HTTP {(int)response.StatusCode}），请在游戏内重新打开寻访记录或改用账号登录同步。");
        }
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        JsonElement root = document.RootElement;
        int status = root.TryGetProperty("status", out JsonElement statusElement) ? statusElement.GetInt32() : -1;
        if (status != 0 || !root.TryGetProperty("data", out JsonElement data))
        {
            throw new InvalidOperationException("游戏日志授权已失效，请在游戏内重新打开寻访记录或改用账号登录同步。");
        }

        string accountUid = GetString(data, "uid");
        JsonElement selectedRole = default;
        if (data.TryGetProperty("roles", out JsonElement roles) && roles.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement role in roles.EnumerateArray())
            {
                string returnedServerId = GetString(role, "serverId");
                if (returnedServerId == serverId)
                {
                    selectedRole = role;
                    break;
                }
                if (selectedRole.ValueKind == JsonValueKind.Undefined && (region != EndfieldGachaRegion.Global
                    || (roles.GetArrayLength() == 1 && string.IsNullOrWhiteSpace(returnedServerId))))
                {
                    selectedRole = role;
                }
            }
        }
        string roleId = GetString(selectedRole, "roleId");
        if (string.IsNullOrWhiteSpace(accountUid) || string.IsNullOrWhiteSpace(roleId))
        {
            throw new InvalidOperationException("官方账号接口未返回有效角色信息。");
        }
        return CreateAccount(region, accountUid, selectedRole, serverId);
    }

    private async Task<List<(string Type, string Name)>> FetchCharacterPoolsAsync(EndfieldGachaAccount account, string token,
        CancellationToken cancellationToken)
    {
        try
        {
            using JsonDocument document = await GetRecordApiJsonAsync(account, token, "char/meta", [], cancellationToken);
            JsonElement data = GetApiData(document.RootElement);
            if (data.TryGetProperty("tabs", out JsonElement tabs) && tabs.ValueKind == JsonValueKind.Array)
            {
                var pools = tabs.EnumerateArray()
                    .Select(tab => GetString(tab, "poolType"))
                    .Where(type => !string.IsNullOrWhiteSpace(type))
                    .Distinct(StringComparer.Ordinal)
                    .Select(type => (type, CharacterPoolNames.GetValueOrDefault(type, type)))
                    .ToList();
                if (pools.Count > 0)
                {
                    return pools;
                }
            }
        }
        catch (Exception ex) when (ex is not GachaAuthorizationExpiredException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Could not discover Endfield character pools: {ErrorType}", ex.GetType().Name);
        }
        return CharacterPoolNames.Select(pair => (pair.Key, pair.Value)).ToList();
    }

    private async Task<List<(string Parameter, string Id, string Name)>> FetchWeaponPoolsAsync(EndfieldGachaAccount account,
        string token, CancellationToken cancellationToken)
    {
        using JsonDocument document = await GetRecordApiJsonAsync(account, token, "weapon/pool", [], cancellationToken);
        var pools = new List<(string Parameter, string Id, string Name)>();
        var rerunIds = new List<string>();
        foreach (JsonElement pool in GetApiData(document.RootElement).EnumerateArray())
        {
            string id = GetString(pool, "poolId");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }
            if (GetString(pool, "poolType") == "rerun" || id.Contains("rerun_", StringComparison.OrdinalIgnoreCase))
            {
                rerunIds.Add(id);
            }
            else
            {
                pools.Add(("pool_id", id, GetString(pool, "poolName", id)));
            }
        }
        if (rerunIds.Count > 0)
        {
            pools.Add(("pool_ids", string.Join(',', rerunIds.Distinct()), "重构申领"));
        }
        return pools;
    }

    private async Task<List<EndfieldGachaItem>> FetchRecordPoolAsync(EndfieldGachaAccount account, string token,
        string recordType, string poolParameter, string pool, string poolName, HashSet<string> known,
        IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var result = new List<EndfieldGachaItem>();
        var seenCursors = new HashSet<string>(StringComparer.Ordinal);
        var query = new Dictionary<string, string> { [poolParameter] = pool };
        bool isCharacter = recordType == "char";

        for (int page = 1; ; page++)
        {
            progress?.Report($"正在同步{poolName} · 第 {page} 页");
            using JsonDocument document = await GetRecordApiJsonAsync(account, token, recordType, query, cancellationToken);
            JsonElement data = GetApiData(document.RootElement);
            JsonElement list = data.GetProperty("list");
            if (list.GetArrayLength() == 0)
            {
                break;
            }

            bool reachedKnownItem = false;
            string cursor = "";
            foreach (JsonElement item in list.EnumerateArray())
            {
                cursor = GetString(item, "seqId");
                if (string.IsNullOrWhiteSpace(cursor))
                {
                    continue;
                }
                if (known.Contains(cursor))
                {
                    reachedKnownItem = true;
                    continue;
                }

                string poolId = GetString(item, "poolId", poolParameter == "pool_id" ? pool : "");
                if (poolParameter == "pool_ids" && string.IsNullOrWhiteSpace(poolId))
                {
                    throw new InvalidOperationException("合并武器池记录缺少卡池 ID。");
                }
                result.Add(new EndfieldGachaItem
                {
                    AccountKey = account.AccountKey,
                    RecordType = recordType,
                    SeqId = cursor,
                    ItemId = GetString(item, isCharacter ? "charId" : "weaponId"),
                    ItemName = GetString(item, isCharacter ? "charName" : "weaponName"),
                    ItemType = isCharacter ? "干员" : GetString(item, "weaponType", "武器"),
                    Rarity = GetInt32(item, "rarity"),
                    IsNew = GetBoolean(item, "isNew"),
                    IsFree = isCharacter && GetBoolean(item, "isFree"),
                    PoolId = poolId,
                    PoolName = GetString(item, "poolName", poolParameter == "pool_ids" ? poolId : poolName),
                    PoolType = isCharacter ? pool : "",
                    GachaTime = GetString(item, "gachaTs"),
                });
            }

            // The combined rerun list can gain additional pools between syncs, so read past cached records.
            if (!GetBoolean(data, "hasMore") || (reachedKnownItem && poolParameter != "pool_ids"))
            {
                break;
            }
            if (string.IsNullOrWhiteSpace(cursor) || !seenCursors.Add(cursor))
            {
                throw new InvalidOperationException("寻访记录分页游标无效。");
            }
            query["seq_id"] = cursor;
            await Task.Delay(Random.Shared.Next(500, 1001), cancellationToken);
        }
        return result;
    }

    private Task<JsonDocument> GetRecordApiJsonAsync(EndfieldGachaAccount account, string token, string route,
        Dictionary<string, string> parameters, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string>(parameters)
        {
            ["lang"] = "zh-cn",
            ["token"] = token,
            ["server_id"] = account.ServerId,
        };
        return GetApiJsonAsync($"https://ef-webview.{GetProviderDomain(account.Region)}/api/record/{route}", query, cancellationToken);
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
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new GachaAuthorizationExpiredException();
                }
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"HTTP {(int)response.StatusCode}");
                }
                var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                try
                {
                    _ = GetApiData(document.RootElement);
                    return document;
                }
                catch
                {
                    document.Dispose();
                    throw;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is not GachaAuthorizationExpiredException && attempt < 3)
            {
                await Task.Delay(Random.Shared.Next(500, 901), cancellationToken);
            }
        }
        throw new InvalidOperationException("官方寻访记录接口连续请求失败。");
    }

    private static JsonElement GetApiData(JsonElement root)
    {
        string code = GetString(root, "code");
        if (code == "40100")
        {
            throw new GachaAuthorizationExpiredException();
        }
        if (code != "0" || !root.TryGetProperty("data", out JsonElement data) || data.ValueKind is JsonValueKind.Null)
        {
            throw new InvalidOperationException("官方寻访记录接口返回异常。");
        }
        return data;
    }

    private static (string Token, string ServerId) ReadGameLogAuthorization(EndfieldGachaRegion region)
    {
        string vendor = region == EndfieldGachaRegion.Global ? "Gryphline" : "Hypergryph";
        string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData", "LocalLow", vendor, "Endfield", "sdklogs", "HGWebview.log");
        if (!File.Exists(logPath))
        {
            throw new FileNotFoundException("未找到终末地寻访日志，请先启动游戏并打开一次寻访记录。", logPath);
        }

        using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return FindGameLogAuthorization(reader, region)
            ?? throw new InvalidOperationException("未找到当前区服有效的寻访记录链接，请在对应客户端打开一次寻访记录后再试。");
    }

    internal static (string Token, string ServerId)? FindGameLogAuthorization(TextReader reader, EndfieldGachaRegion region)
    {
        string host = $"ef-webview.{GetProviderDomain(region)}";
        (string Token, string ServerId)? latest = null;
        while (reader.ReadLine() is string line)
        {
            foreach (Match match in GachaUrlRegex().Matches(line))
            {
                string url = WebUtility.HtmlDecode(match.Value).TrimEnd(']', ')', ',', ';');
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || !uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var query = ParseQueryParameters(uri);
                string? token = query.GetValueOrDefault("u8_token");
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }
                if (region == EndfieldGachaRegion.Global)
                {
                    string? server = new[] { "server_id", "serverId", "server" }
                        .Select(key => query.GetValueOrDefault(key)?.Trim())
                        .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
                    if (server is not null)
                    {
                        latest = (token, server);
                    }
                    continue;
                }
                string? channel = query.GetValueOrDefault("channel");
                string? subChannel = query.GetValueOrDefault("subChannel") ?? query.GetValueOrDefault("sub_channel");
                bool matches = region == EndfieldGachaRegion.Bilibili
                    ? channel == "2" && subChannel == "2"
                    : (channel is null or "1") && (subChannel is null or "1");
                if (matches)
                {
                    latest = (token, "1");
                }
            }
        }
        return latest;
    }

    private static Dictionary<string, string> ParseQueryParameters(Uri uri)
    {
        var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string param in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = param.Split('=', 2);
            if (parts.Length == 2)
            {
                query[WebUtility.UrlDecode(parts[0])] = WebUtility.UrlDecode(parts[1]);
            }
        }
        return query;
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

    [GeneratedRegex("https://ef-webview\\.(?:hypergryph|gryphline)\\.com/page/gacha_[^\\s\\\"']+", RegexOptions.IgnoreCase)]
    private static partial Regex GachaUrlRegex();

    private sealed class GachaAuthorizationExpiredException()
        : InvalidOperationException("寻访授权已失效，请重新登录或在游戏中打开寻访记录后再试。")
    {
    }
}
