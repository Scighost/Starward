using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.Gacha.Endfield;

internal sealed record EndfieldAkegfArchive(string Version, List<EndfieldAkegfRole> Roles, EndfieldAkegfVerification Verification)
{
    public bool IsVerified => Verification == EndfieldAkegfVerification.Verified;

    private const long MinTimestamp = 1_000_000_000_000;
    private const long MaxTimestamp = 9_999_999_999_999;

    internal static async Task<EndfieldAkegfArchive> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        using JsonDocument document = await JsonDocument.ParseAsync(stream,
            new JsonDocumentOptions { AllowDuplicateProperties = false }, cancellationToken);
        JsonElement root = document.RootElement;
        CheckMembers(root, "info", "endfield");
        JsonElement info = Require(root, "info", JsonValueKind.Object);
        string version = ReadText(info, "version") ?? throw Invalid("缺少版本号。");
        if (version is not ("v0.4" or "v1.1" or "v1.2"))
        {
            throw Invalid("支持 AKEGF v0.4、v1.1 和 v1.2。");
        }
        CheckMembers(info, "version", "export_app", "export_app_version", "export_timestamp", "export_provider", "rsa_signature");
        _ = ReadText(info, "export_app");
        _ = ReadText(info, "export_app_version");
        _ = ReadInteger(info, "export_timestamp", MinTimestamp, MaxTimestamp);
        string? provider = ReadText(info, "export_provider");
        string? signature = ReadText(info, "rsa_signature");
        ValidateSignatureFields(version, provider, signature);

        var roles = new List<EndfieldAkegfRole>();
        var roleKeys = new HashSet<(string Region, string Server, string Uid)>();
        foreach (JsonElement group in Require(root, "endfield", JsonValueKind.Array).EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            CheckMembers(group, "region", "server_id", "uid", "account_uid", "lang", "list");
            string region = ReadText(group, "region") ?? "cn";
            string server = ReadText(group, "server_id") ?? (region == "cn" ? "1" : throw Invalid("国际服分组缺少服务器 ID。"));
            string uid = ReadText(group, "uid") ?? throw Invalid("角色分组缺少 UID。");
            _ = ReadText(group, "account_uid");
            _ = ReadText(group, "lang");
            if (region is not ("cn" or "global") || !roleKeys.Add((region, server, uid)))
            {
                throw Invalid("角色分组的区服无效或重复。");
            }
            roles.Add(new EndfieldAkegfRole(region, server, uid,
                ReadRecords(Require(group, "list", JsonValueKind.Array), cancellationToken)));
        }
        if (roles.Count == 0)
        {
            throw Invalid("文件没有角色分组。");
        }
        EndfieldAkegfVerification verification = (version, provider) switch
        {
            (_, null) => EndfieldAkegfVerification.Unsigned,
            ("v1.2", "akegf-example") => EndfieldAkegfVerification.Example,
            ("v1.2", _) => await EndfieldAkegfSignature.VerifyAsync(root, provider, signature!, cancellationToken),
            _ => EndfieldAkegfVerification.LegacySignature,
        };
        return new EndfieldAkegfArchive(version, roles, verification);
    }

    private static List<EndfieldAkegfRecord> ReadRecords(JsonElement list, CancellationToken cancellationToken)
    {
        var records = new List<EndfieldAkegfRecord>();
        var recordKeys = new HashSet<(string Type, string SeqId)>();
        foreach (JsonElement row in list.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            CheckMembers(row, "record_type", "seq_id", "pool_id", "item_id", "gacha_ts", "rarity", "pool_name",
                "item_name", "pool_type", "weapon_type", "is_new", "is_free", "kind", "name_text", "seq_id_source", "gift_reward_label");
            string kind = ReadText(row, "kind") ?? throw Invalid("流水缺少事件类型。");
            string? type = ReadText(row, "record_type");
            string? seqId = ReadText(row, "seq_id");
            string? poolId = ReadText(row, "pool_id");
            string? itemName = ReadText(row, "item_name");
            long? stars = ReadInteger(row, "rarity", 1, 99);
            long? timestamp = ReadInteger(row, "gacha_ts", MinTimestamp, MaxTimestamp);
            string? seqIdSource = ReadText(row, "seq_id_source");
            string? weaponType = ReadText(row, "weapon_type");
            bool? isFree = ReadBoolean(row, "is_free");
            _ = ReadBoolean(row, "is_new");
            _ = ReadText(row, "item_id");
            _ = ReadText(row, "pool_type");
            string? poolName = ReadText(row, "pool_name");
            string? nameText = ReadText(row, "name_text");
            string? giftLabel = ReadText(row, "gift_reward_label");

            if (type is not (null or "char" or "weapon")
                || (type == "char" && weaponType is not null)
                || (type == "weapon" && isFree.HasValue)
                || (seqIdSource is not null && (seqIdSource != "batch_end" || seqId is null)))
            {
                throw Invalid("流水的来源类别或附加字段无效。");
            }
            if (type is not null && seqId is not null && !recordKeys.Add((type, seqId)))
            {
                throw Invalid("同一角色出现重复的流水号。");
            }
            if (kind == "draw" && (poolId is null || itemName is null || stars is null || timestamp is null))
            {
                throw Invalid("抽取流水缺少卡池、获得物、星级或时间。");
            }
            records.Add(new EndfieldAkegfRecord(kind, type, seqId, itemName ?? nameText, poolName ?? poolId,
                stars, timestamp, isFree, giftLabel));
        }
        return records;
    }

    private static void ValidateSignatureFields(string version, string? provider, string? signature)
    {
        if ((version == "v1.1" && (provider is not null || signature is not null))
            || (version == "v1.2" && (provider is null || signature is null))
            || (provider is null) != (signature is null))
        {
            throw Invalid("提供方和签名字段与档案版本不符。");
        }
        if (provider is not null &&
            (!Regex.IsMatch(provider, @"\A[a-z0-9](?:[a-z0-9-]{0,62}[a-z0-9])?\z") ||
             !Regex.IsMatch(signature!, @"\A[A-Za-z0-9_-]{42}[AEIMQUYcgkosw048]\.(?:[A-Za-z0-9_-]{341}[AQgw]|[A-Za-z0-9_-]{512}|[A-Za-z0-9_-]{682}[AEIMQUYcgkosw048])\z")))
        {
            throw Invalid("提供方或签名的格式无效。");
        }
    }

    private static void CheckMembers(JsonElement element, params string[] allowed)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw Invalid("档案中的对象格式无效。");
        }
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (Array.IndexOf(allowed, property.Name) < 0)
            {
                throw Invalid("档案包含格式未定义的字段。");
            }
        }
    }

    private static JsonElement Require(JsonElement element, string name, JsonValueKind kind)
    {
        if (!element.TryGetProperty(name, out JsonElement value) || value.ValueKind != kind)
        {
            throw Invalid($"字段 {name} 缺失或类型无效。");
        }
        return value;
    }

    private static string? ReadText(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value))
        {
            return null;
        }
        if (value.ValueKind != JsonValueKind.String || value.GetString() is not { } text || string.IsNullOrWhiteSpace(text))
        {
            throw Invalid($"字段 {name} 应为非空字符串。");
        }
        return text;
    }

    private static long? ReadInteger(JsonElement element, string name, long minimum, long maximum)
    {
        if (!element.TryGetProperty(name, out JsonElement value))
        {
            return null;
        }
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out decimal number)
            || number != decimal.Truncate(number) || number < minimum || number > maximum)
        {
            throw Invalid($"字段 {name} 应为指定范围内的整数。");
        }
        return (long)number;
    }

    private static bool? ReadBoolean(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value))
        {
            return null;
        }
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw Invalid($"字段 {name} 应为布尔值。"),
        };
    }

    private static InvalidDataException Invalid(string message) => new(message);
}

internal sealed record EndfieldAkegfRole(string Region, string Server, string Uid, List<EndfieldAkegfRecord> Records)
{
    public override string ToString() => $"{(Region == "cn" ? "国服" : "国际服")} · 服务器 {Server} · UID {Uid}";
}

internal sealed record EndfieldAkegfRecord(string Kind, string? RecordType, string? SeqId, string? ItemName,
    string? Pool, long? Rarity, long? Timestamp, bool? IsFree, string? GiftLabel)
{
    public override string ToString()
    {
        string kind = Kind switch
        {
            "draw" when IsFree == true => "加急招募",
            "draw" when RecordType == "char" => "干员寻访",
            "draw" when RecordType == "weapon" => "武器申领",
            "draw" => "抽取",
            "gift_intel_book" => "寻访情报书",
            "gift_operator_token" => "干员信物",
            "gift_weapon_box" => "补充武库箱",
            "gift_weapon" => "赠送武器",
            "unknown" => "类型未知",
            _ => Kind,
        };
        string? stars = Rarity.HasValue ? $"{Rarity} 星" : null;
        string? time = Timestamp.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(Timestamp.Value).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)
            : null;
        return string.Join(" · ", new[] { time, kind, ItemName, GiftLabel, stars, Pool, SeqId }
            .Where(x => !string.IsNullOrEmpty(x)));
    }
}
