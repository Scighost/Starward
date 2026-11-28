using Dapper;
using Starward.Features.Database;
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.Gacha.Endfield;

internal sealed partial class EndfieldGachaService
{
    internal async Task<byte[]> ExportAkegfAsync(EndfieldGachaAccount account,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(account.RoleId) || string.IsNullOrWhiteSpace(account.ServerId))
        {
            throw new InvalidOperationException("本地记录缺少角色 ID 或服务器 ID，请先同步该角色。");
        }

        var rows = new JsonArray();
        using (var connection = DatabaseService.CreateConnection())
        {
            var query = new CommandDefinition("""
                SELECT * FROM EndfieldGachaItem
                WHERE AccountKey = @AccountKey
                ORDER BY RecordType, length(SeqId), SeqId;
                """, new { account.AccountKey }, cancellationToken: cancellationToken);
            foreach (var record in await connection.QueryAsync<EndfieldGachaItem>(query))
            {
                rows.Add(CreateAkegfRecord(record));
            }
        }

        var info = new JsonObject
        {
            ["version"] = "v0.4",
            ["export_app"] = "Starward",
            ["export_timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };
        AddOptionalText(info, "export_app_version", AppConfig.AppVersion);
        var group = new JsonObject
        {
            ["region"] = account.Region == EndfieldGachaRegion.Global ? "global" : "cn",
            ["server_id"] = account.ServerId,
            ["uid"] = account.RoleId,
            ["lang"] = "zh-cn",
            ["list"] = rows,
        };
        var archive = new JsonObject { ["info"] = info, ["endfield"] = new JsonArray(group) };
        return JsonSerializer.SerializeToUtf8Bytes(archive, AppConfig.JsonSerializerOptions);
    }

    private static JsonObject CreateAkegfRecord(EndfieldGachaItem record)
    {
        // The existing cache has no event kind. Expedited character records are known draws.
        bool isDraw = record.RecordType == "char" && record.IsFree;
        var row = new JsonObject { ["kind"] = isDraw ? "draw" : "unknown" };
        AddOptionalText(row, "pool_id", record.PoolId);
        AddOptionalText(row, "item_name", record.ItemName);
        if (record.Rarity is >= 1 and <= 99)
        {
            row["rarity"] = record.Rarity;
        }
        if (long.TryParse(record.GachaTime, NumberStyles.None, CultureInfo.InvariantCulture, out long timestamp))
        {
            if (timestamp is >= 1_000_000_000 and <= 9_999_999_999)
            {
                timestamp *= 1000;
            }
            if (timestamp is >= 1_000_000_000_000 and <= 9_999_999_999_999)
            {
                row["gacha_ts"] = timestamp;
            }
        }
        if (isDraw && !(row.ContainsKey("pool_id") && row.ContainsKey("item_name") &&
                        row.ContainsKey("rarity") && row.ContainsKey("gacha_ts")))
        {
            throw new InvalidOperationException($"抽取流水 {record.SeqId} 缺少有效的卡池、获得物、星级或时间，无法导出。");
        }
        if (record.RecordType is "char" or "weapon")
        {
            row["record_type"] = record.RecordType;
        }
        AddOptionalText(row, "seq_id", record.SeqId);
        AddOptionalText(row, "item_id", record.ItemId);
        AddOptionalText(row, "pool_name", record.PoolName);
        AddOptionalText(row, "pool_type", record.PoolType);
        if (record.RecordType == "weapon" && record.ItemType != "武器")
        {
            AddOptionalText(row, "weapon_type", record.ItemType);
        }
        if (isDraw)
        {
            row["is_free"] = true;
        }
        if (record.IsNew)
        {
            row["is_new"] = true;
        }
        return row;
    }

    private static void AddOptionalText(JsonObject row, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            row[name] = value;
        }
    }
}
