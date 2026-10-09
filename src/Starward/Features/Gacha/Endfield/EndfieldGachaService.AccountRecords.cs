using Dapper;
using Microsoft.Extensions.Logging;
using Starward.Features.Database;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.Gacha.Endfield;

internal sealed partial class EndfieldGachaService
{
    private const string CustomerServiceApi = "https://customer-service.hypergryph.com/api/center/open/v1/endfield/game_logs";
    private const string ItemIconBigBaseUrl = "https://data.akedata.wiki/public/images/assets/beyond/dynamicassets/gameplay/ui/sprites/itemiconbig";
    private const int CustomerServicePageSize = 10;
    private const int MaximumCustomerServicePages = 500;

    private static readonly Dictionary<int, string> CurrencyNames = new()
    {
        [1] = "源石",
        [2] = "嵌晶玉",
        [3] = "武库配额",
        [4] = "协议通行证",
    };

    private static readonly Dictionary<int, string> CurrencyReasons = new()
    {
        [2] = "邮件领取",
        [3] = "源石交易所获取",
        [4] = "采购中心组合包",
        [5] = "购买月卡立得",
        [6] = "解锁源石配给",
        [7] = "兑换嵌晶玉",
        [8] = "衍质源石兑换武库配额",
        [9] = "恢复理智",
        [10] = "干员寻访赠送",
        [11] = "寻访消耗剩余",
        [12] = "协议通行证奖励",
        [13] = "任务奖励",
        [14] = "世界探索奖励",
        [15] = "副本奖励",
        [16] = "活动中心奖励",
        [17] = "提交醚质后获得",
        [18] = "行动手册节点奖励",
        [19] = "行动手册日常活跃度奖励",
        [20] = "权限等阶提升奖励",
        [21] = "月卡每日领取",
        [22] = "信用交易所兑换",
        [23] = "购买通行证等级",
        [24] = "系统玩法奖励",
        [25] = "武库交易所消耗",
    };

    private static readonly Dictionary<string, string> AccountRecordIconIds = new(StringComparer.Ordinal)
    {
        ["stamina"] = "item_ap",
        ["currency:1"] = "item_originium_recharge",
        ["currency:2"] = "item_diamond",
        ["currency:3"] = "item_gachabyproducts_weapongold",
        ["battle_pass"] = "item_cbp_exp",
        ["monthly_card"] = "item_monthlycard_1",
    };

    internal static string GetAccountRecordIcon(string recordType)
    {
        return AccountRecordIconIds.TryGetValue(recordType, out string? iconId)
            ? $"{ItemIconBigBaseUrl}/{Uri.EscapeDataString(iconId)}.png"
            : "";
    }

    public EndfieldAccountRecordSyncResult GetAccountRecords(string accountKey)
    {
        using var connection = DatabaseService.CreateConnection();
        List<EndfieldAccountRecordItem> items = connection.Query<EndfieldAccountRecordItem>(
            """
            SELECT * FROM EndfieldAccountRecord
            WHERE AccountKey = @accountKey
            ORDER BY Timestamp DESC, Id DESC;
            """, new { accountKey }).ToList();
        foreach (EndfieldAccountRecordItem item in items)
        {
            string icon = GetAccountRecordIcon(item.RecordType);
            if (!string.IsNullOrWhiteSpace(icon))
            {
                item.Icon = icon;
            }
        }
        string? updateTimeText = connection.QueryFirstOrDefault<string>(
            "SELECT UpdateTime FROM EndfieldAccountRecordSync WHERE AccountKey = @accountKey;",
            new { accountKey });
        DateTimeOffset syncTime = DateTimeOffset.TryParse(updateTimeText, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out DateTimeOffset parsedTime)
            ? parsedTime
            : DateTimeOffset.MinValue;
        return new EndfieldAccountRecordSyncResult(items, BuildAccountRecordSummaries(items), syncTime, []);
    }

    public async Task<EndfieldAccountRecordSyncResult> SyncAccountRecordsAsync(EndfieldGachaAccount account,
        IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        string loginToken = GetSavedLoginToken(account.AccountKey);
        progress?.Report("正在刷新客服中心授权");
        string bindingToken = await GetOAuthTokenAsync(loginToken, cancellationToken);
        string roleToken = await GetU8TokenAsync(account.Uid, bindingToken, cancellationToken);

        var items = new List<EndfieldAccountRecordItem>();
        var failedCategories = new List<string>();
        int successfulCategoryCount = 0;

        await FetchCategoryAsync("理智记录", async () =>
        {
            progress?.Report("正在读取理智记录");
            List<JsonElement> records = await FetchCustomerServicePagesAsync("sanity", "seqId",
                loginToken, roleToken, account.ServerId, null, cancellationToken);
            items.AddRange(records.Select(ParseStaminaRecord));
        }, failedCategories);

        foreach ((int currencyType, string currencyName) in CurrencyNames)
        {
            await FetchCategoryAsync($"{currencyName}记录", async () =>
            {
                progress?.Report($"正在读取{currencyName}记录");
                string endpoint = currencyType == 4 ? "bp" : "currency";
                Dictionary<string, object?>? parameters = currencyType == 4
                    ? null
                    : new() { ["currencyType"] = currencyType, ["changeType"] = 0 };
                List<JsonElement> records = await FetchCustomerServicePagesAsync(endpoint, "seqId",
                    loginToken, roleToken, account.ServerId, parameters, cancellationToken);
                items.AddRange(currencyType == 4
                    ? records.Select(ParseBattlePassRecord)
                    : records.Select(x => ParseCurrencyRecord(x, currencyType, currencyName)));
            }, failedCategories);
        }

        await FetchCategoryAsync("月卡记录", async () =>
        {
            progress?.Report("正在读取月卡记录");
            List<JsonElement> records = await FetchCustomerServicePagesAsync("monthly_card", "seqId",
                loginToken, roleToken, account.ServerId, null, cancellationToken);
            items.AddRange(records.Select(ParseMonthCardRecord));
        }, failedCategories);

        await FetchCategoryAsync("邮件记录", async () =>
        {
            progress?.Report("正在读取邮件记录");
            List<JsonElement> records = await FetchCustomerServicePagesAsync("mail", "seqId",
                loginToken, roleToken, account.ServerId, null, cancellationToken);
            items.AddRange(records.Select(ParseMailRecord));
        }, failedCategories);

        await FetchCategoryAsync("登录记录", async () =>
        {
            progress?.Report("正在读取登录记录");
            List<JsonElement> records = await FetchCustomerServicePagesAsync("login", "eventTs",
                loginToken, roleToken, account.ServerId, null, cancellationToken);
            items.AddRange(records.Select(ParseLoginRecord));
        }, failedCategories);

        if (successfulCategoryCount == 0)
        {
            throw new InvalidOperationException("客服中心记录读取失败，请稍后重试。");
        }

        DateTimeOffset syncTime = DateTimeOffset.Now;
        SaveAccountRecords(account.AccountKey, items, syncTime);
        EndfieldAccountRecordSyncResult localResult = GetAccountRecords(account.AccountKey);
        return localResult with { FailedCategories = failedCategories.ToArray() };

        async Task FetchCategoryAsync(string name, Func<Task> action, List<string> failures)
        {
            try
            {
                await action();
                successfulCategoryCount++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                failures.Add(name);
                _logger.LogWarning("Failed to sync Endfield customer-service category {Category}: {ErrorType}",
                    name, ex.GetType().Name);
            }
        }
    }

    private static void SaveAccountRecords(string accountKey, IReadOnlyList<EndfieldAccountRecordItem> items,
        DateTimeOffset syncTime)
    {
        foreach (EndfieldAccountRecordItem item in items)
        {
            item.AccountKey = accountKey;
        }
        using var connection = DatabaseService.CreateConnection();
        using var transaction = connection.BeginTransaction();
        connection.Execute(
            """
            INSERT OR REPLACE INTO EndfieldAccountRecord
            (AccountKey, RecordType, Id, Category, TypeName, Title, Subtitle, Detail, Icon,
             Timestamp, Amount, HasAmount, CountValue)
            VALUES
            (@AccountKey, @RecordType, @Id, @Category, @TypeName, @Title, @Subtitle, @Detail, @Icon,
             @Timestamp, @Amount, @HasAmount, @CountValue);
            """, items, transaction);
        connection.Execute(
            """
            INSERT OR REPLACE INTO EndfieldAccountRecordSync (AccountKey, UpdateTime)
            VALUES (@accountKey, @updateTime);
            """, new
            {
                accountKey,
                updateTime = syncTime.ToString("O", CultureInfo.InvariantCulture),
            }, transaction);
        transaction.Commit();
    }

    private async Task<List<JsonElement>> FetchCustomerServicePagesAsync(string endpoint, string cursorName,
        string accountToken, string roleToken, string serverId, Dictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        var result = new List<JsonElement>();
        string cursor = "";
        for (int page = 1; page <= MaximumCustomerServicePages; page++)
        {
            var body = parameters is null
                ? new Dictionary<string, object?>()
                : new Dictionary<string, object?>(parameters);
            body["limit"] = CustomerServicePageSize;
            if (!string.IsNullOrWhiteSpace(cursor))
            {
                body[cursorName] = cursor;
            }

            using var request = CreateBrowserRequest(HttpMethod.Post, $"{CustomerServiceApi}/{endpoint}");
            request.Headers.TryAddWithoutValidation("Origin", "https://customer-service.hypergryph.com");
            request.Headers.TryAddWithoutValidation("Referer", "https://customer-service.hypergryph.com/app/endfield/gamelogs");
            request.Headers.TryAddWithoutValidation("x-hg-language", "zh-cn");
            request.Headers.TryAddWithoutValidation("x-account-token", accountToken);
            request.Headers.TryAddWithoutValidation("x-role-token", roleToken);
            request.Headers.TryAddWithoutValidation("x-role-server-id", serverId);
            request.Content = JsonContent.Create(body);

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException("客服中心登录状态已失效，请重新登录鹰角账号。");
            }
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"客服中心接口请求失败（HTTP {(int)response.StatusCode}）。");
            }

            using JsonDocument document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            JsonElement data = GetCustomerServiceData(document.RootElement);
            if (!data.TryGetProperty("list", out JsonElement list) || list.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("客服中心接口未返回记录列表。");
            }

            List<JsonElement> pageItems = list.EnumerateArray().Select(x => x.Clone()).ToList();
            result.AddRange(pageItems);
            if (!GetBoolean(data, "hasNext") || pageItems.Count == 0)
            {
                break;
            }

            string nextCursor = GetString(pageItems[^1], cursorName);
            if (string.IsNullOrWhiteSpace(nextCursor) || nextCursor == cursor)
            {
                break;
            }
            cursor = nextCursor;
            await Task.Delay(Random.Shared.Next(80, 161), cancellationToken);
        }
        return result;
    }

    private static JsonElement GetCustomerServiceData(JsonElement root)
    {
        int code = GetInt32(root, "code");
        if (root.TryGetProperty("code", out _) && code != 0)
        {
            if (code is 401 or 403)
            {
                throw new UnauthorizedAccessException("客服中心登录状态已失效，请重新登录鹰角账号。");
            }
            throw new InvalidOperationException(GetString(root, "msg", "客服中心接口返回异常。"));
        }
        return root.TryGetProperty("data", out JsonElement data) ? data : root;
    }

    private static EndfieldAccountRecordItem ParseStaminaRecord(JsonElement item)
    {
        string reason = GetString(item, "changeReason");
        string title = reason switch
        {
            "E_ApChangeReason_Restore" => "转化为理智精粹药剂",
            "E_ApChangeReason_GameReward" => $"领取奖励 {GetString(item, "gameId")}",
            _ => "其他理智变动",
        };
        return new EndfieldAccountRecordItem
        {
            RecordType = "stamina",
            Category = EndfieldAccountRecordCategory.Stamina,
            Id = GetString(item, "seqId"),
            TypeName = "理智",
            Title = title,
            Timestamp = GetInt64(item, "changeTime"),
            Amount = -Math.Abs(GetInt64(item, "changeNum")),
            HasAmount = true,
            Icon = GetAccountRecordIcon("stamina"),
        };
    }

    private static EndfieldAccountRecordItem ParseCurrencyRecord(JsonElement item, int currencyType,
        string currencyName)
    {
        long change = Math.Abs(GetInt64(item, "changeNum"));
        if (GetInt32(item, "changeType") != 1)
        {
            change = -change;
        }
        int reason = GetInt32(item, "changeReason");
        return new EndfieldAccountRecordItem
        {
            RecordType = $"currency:{currencyType}",
            Category = EndfieldAccountRecordCategory.Currency,
            Id = GetString(item, "seqId"),
            TypeName = currencyName,
            Title = CurrencyReasons.GetValueOrDefault(reason, "其他"),
            Subtitle = currencyName,
            Detail = $"变动后存量：{GetInt64(item, "after").ToString("N0", CultureInfo.CurrentCulture)}",
            Timestamp = GetInt64(item, "changeTime"),
            Amount = change,
            HasAmount = true,
            Icon = GetAccountRecordIcon($"currency:{currencyType}"),
        };
    }

    private static EndfieldAccountRecordItem ParseBattlePassRecord(JsonElement item)
    {
        string type = GetString(item, "bpType") switch
        {
            "bp_track_pay" => "协议定制",
            "bp_track_originium" => "源石配给",
            _ => "协议通行证",
        };
        return new EndfieldAccountRecordItem
        {
            RecordType = "battle_pass",
            Category = EndfieldAccountRecordCategory.Currency,
            Id = GetString(item, "seqId"),
            TypeName = "协议通行证",
            Title = $"{type}解锁",
            Subtitle = "协议通行证",
            Timestamp = GetInt64(item, "activateTime"),
            Icon = GetAccountRecordIcon("battle_pass"),
        };
    }

    private static EndfieldAccountRecordItem ParseMonthCardRecord(JsonElement item)
    {
        string actionType = GetString(item, "actionType");
        return new EndfieldAccountRecordItem
        {
            RecordType = "monthly_card",
            Category = EndfieldAccountRecordCategory.MonthCard,
            Id = GetString(item, "seqId"),
            TypeName = actionType,
            Title = actionType == "monthcard_activate" ? "月卡激活" : "月卡奖励领取",
            Timestamp = GetInt64(item, "actionTime"),
            Icon = GetAccountRecordIcon("monthly_card"),
        };
    }

    private static EndfieldAccountRecordItem ParseMailRecord(JsonElement item)
    {
        var attachments = new List<string>();
        int attachmentCount = 0;
        if (item.TryGetProperty("attachmentItems", out JsonElement attachmentItems) &&
            attachmentItems.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement attachment in attachmentItems.EnumerateArray())
            {
                int count = GetInt32(attachment, "count");
                attachmentCount += count;
                attachments.Add($"{GetString(attachment, "itemName")} x{count}");
            }
        }
        return new EndfieldAccountRecordItem
        {
            RecordType = "mail",
            Category = EndfieldAccountRecordCategory.Mail,
            Id = GetString(item, "seqId"),
            TypeName = "邮件",
            Title = GetString(item, "title", "邮件领取"),
            Detail = string.Join("，", attachments),
            Timestamp = GetInt64(item, "actionTime"),
            CountValue = attachmentCount,
        };
    }

    private static EndfieldAccountRecordItem ParseLoginRecord(JsonElement item)
    {
        string device = GetString(item, "deviceOs", "Unknown") switch
        {
            "iOS" => "Apple",
            "Android" => "Android",
            "Windows" => "Windows",
            "PS5" => "PS5",
            "Xbox" => "Xbox",
            _ => "未知设备",
        };
        return new EndfieldAccountRecordItem
        {
            RecordType = "login",
            Category = EndfieldAccountRecordCategory.Login,
            Id = GetString(item, "eventTs", GetString(item, "loginTime")),
            TypeName = device,
            Title = $"{device} 登录",
            Subtitle = $"IP：{GetString(item, "ip", "-")}",
            Timestamp = GetInt64(item, "loginTime"),
        };
    }

    private static List<EndfieldAccountRecordSummary> BuildAccountRecordSummaries(
        IReadOnlyList<EndfieldAccountRecordItem> items)
    {
        long now = DateTimeOffset.Now.ToUnixTimeSeconds();
        long staminaCutoff = now - (long)TimeSpan.FromDays(7).TotalSeconds;
        long generalCutoff = now - (long)TimeSpan.FromDays(90).TotalSeconds;
        long stamina = items.Where(x => x.RecordType == "stamina" && ToUnixTimeSeconds(x.Timestamp) >= staminaCutoff)
            .Sum(x => Math.Abs(Math.Min(0, x.Amount)));
        var summaries = new List<EndfieldAccountRecordSummary>
        {
            CreateSummary("理智消耗", stamina, "近 7 天", GetAccountRecordIcon("stamina")),
        };
        foreach ((string recordType, string currency) in new[]
        {
            ("currency:1", "源石"),
            ("currency:2", "嵌晶玉"),
            ("currency:3", "武库配额"),
        })
        {
            List<EndfieldAccountRecordItem> currencyItems = items
                .Where(x => x.RecordType == recordType && ToUnixTimeSeconds(x.Timestamp) >= generalCutoff)
                .ToList();
            long increased = currencyItems.Where(x => x.Amount > 0).Sum(x => x.Amount);
            long deducted = Math.Abs(currencyItems.Where(x => x.Amount < 0).Sum(x => x.Amount));
            summaries.Add(new EndfieldAccountRecordSummary
            {
                Title = currency,
                AddAmount = increased,
                SubAmount = -deducted,
                HasAmountBreakdown = true,
                Detail = $"近 90 天 · {currencyItems.Count} 条",
                Icon = GetAccountRecordIcon(recordType),
            });
        }

        List<EndfieldAccountRecordItem> loginItems = items
            .Where(x => x.RecordType == "login" && ToUnixTimeSeconds(x.Timestamp) >= generalCutoff).ToList();
        summaries.Add(new EndfieldAccountRecordSummary
        {
            Title = "登录记录",
            Value = loginItems.Count.ToString("N0", CultureInfo.CurrentCulture),
            Detail = $"近 90 天 · {loginItems.Select(x => x.TypeName).Distinct().Count()} 类设备",
            Glyph = "\uE77B",
        });
        List<EndfieldAccountRecordItem> mailItems = items
            .Where(x => x.RecordType == "mail" && ToUnixTimeSeconds(x.Timestamp) >= generalCutoff).ToList();
        summaries.Add(new EndfieldAccountRecordSummary
        {
            Title = "邮件领取",
            Value = mailItems.Count.ToString("N0", CultureInfo.CurrentCulture),
            Detail = $"近 90 天 · {mailItems.Sum(x => x.CountValue)} 件附件",
            Glyph = "\uE715",
        });
        return summaries;

        static EndfieldAccountRecordSummary CreateSummary(string title, long value, string detail, string icon)
        {
            string text = value > 0
                ? $"+{value.ToString("N0", CultureInfo.CurrentCulture)}"
                : value.ToString("N0", CultureInfo.CurrentCulture);
            return new EndfieldAccountRecordSummary { Title = title, Value = text, Detail = detail, Icon = icon };
        }
    }

    private static long ToUnixTimeSeconds(long timestamp)
    {
        return timestamp >= 100_000_000_000 ? timestamp / 1000 : timestamp;
    }

    private static long GetInt64(JsonElement element, string name)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long number))
            {
                return number;
            }
            if (long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
            {
                return number;
            }
        }
        return 0;
    }
}
