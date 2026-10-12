using Dapper;
using Starward.Features.Database;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.Gacha.Endfield;

internal sealed partial class EndfieldGachaService
{
    private const string AkeDataBaseUrl = "https://data.akedata.wiki";
    private const string AkeDataManifestUrl = $"{AkeDataBaseUrl}/manifest.json";
    private const string CharacterIconBaseUrl = $"{AkeDataBaseUrl}/public/images/assets/beyond/dynamicassets/gameplay/ui/sprites/charremoteicon/icon_";
    private const string WeaponIconBaseUrl = $"{AkeDataBaseUrl}/public/images/assets/beyond/dynamicassets/gameplay/ui/sprites/itemicon/";
    private const string GachaInfoVersionKey = "EndfieldGachaInfoVersion";

    private readonly SemaphoreSlim _gachaInfoLock = new(1, 1);

    public async Task<bool> UpdateGachaInfoAsync(CancellationToken cancellationToken = default)
    {
        await _gachaInfoLock.WaitAsync(cancellationToken);
        try
        {
            using JsonDocument manifest = await GetJsonAsync(AkeDataManifestUrl, cancellationToken);
            (string version, string tableCfgPath) = GetLatestTableCfg(manifest.RootElement);

            using var connection = DatabaseService.CreateConnection();
            int cachedItemCount = connection.QueryFirst<int>(
                "SELECT COUNT(*) FROM EndfieldGachaInfo WHERE RecordType = 'weapon';");
            string cachedVersion = DatabaseService.GetValue<string>(GachaInfoVersionKey, out _) ?? "";
            if (cachedItemCount > 0 && string.Equals(cachedVersion, version, StringComparison.Ordinal))
            {
                return false;
            }

            string itemTableUrl = $"{AkeDataBaseUrl}/{tableCfgPath.Trim('/')}/ItemTable.json";
            using JsonDocument itemTable = await GetJsonAsync(itemTableUrl, cancellationToken);
            List<EndfieldGachaInfo> infoList = ParseWeaponInfo(itemTable.RootElement);
            if (infoList.Count == 0)
            {
                throw new InvalidOperationException("终末地武器图标元数据为空。");
            }

            using var transaction = connection.BeginTransaction();
            connection.Execute("DELETE FROM EndfieldGachaInfo WHERE RecordType = 'weapon';", transaction: transaction);
            connection.Execute(
                """
                INSERT INTO EndfieldGachaInfo (RecordType, ItemId, IconId, Icon)
                VALUES (@RecordType, @ItemId, @IconId, @Icon);
                """, infoList, transaction);
            transaction.Commit();
            DatabaseService.SetValue(GachaInfoVersionKey, version);
            return true;
        }
        finally
        {
            _gachaInfoLock.Release();
        }
    }

    internal static string GetFallbackIconUrl(string recordType, string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return "";
        }
        string escapedItemId = Uri.EscapeDataString(itemId);
        return recordType switch
        {
            "char" => $"{CharacterIconBaseUrl}{escapedItemId}.png",
            "weapon" => $"{WeaponIconBaseUrl}{escapedItemId}.png",
            _ => "",
        };
    }

    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using HttpResponseMessage response = await _httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
    }

    private static (string Version, string TableCfgPath) GetLatestTableCfg(JsonElement manifest)
    {
        string version = GetString(manifest, "latest");
        if (string.IsNullOrWhiteSpace(version) ||
            !manifest.TryGetProperty("versions", out JsonElement versions) ||
            versions.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("AKEDatabase 数据清单格式无效。");
        }
        foreach (JsonElement item in versions.EnumerateArray())
        {
            if (string.Equals(GetString(item, "id"), version, StringComparison.Ordinal))
            {
                string tableCfgPath = GetString(item, "tableCfgPath");
                if (!string.IsNullOrWhiteSpace(tableCfgPath))
                {
                    return (version, tableCfgPath);
                }
                break;
            }
        }
        throw new InvalidOperationException("AKEDatabase 数据清单缺少最新版本路径。");
    }

    private static List<EndfieldGachaInfo> ParseWeaponInfo(JsonElement itemTable)
    {
        if (itemTable.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("终末地物品元数据格式无效。");
        }
        var result = new List<EndfieldGachaInfo>();
        foreach (JsonProperty property in itemTable.EnumerateObject())
        {
            string itemId = property.Name;
            if (!itemId.StartsWith("wpn_", StringComparison.Ordinal) || property.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }
            string iconId = GetString(property.Value, "iconId");
            if (string.IsNullOrWhiteSpace(iconId))
            {
                iconId = itemId;
            }
            result.Add(new EndfieldGachaInfo
            {
                RecordType = "weapon",
                ItemId = itemId,
                IconId = iconId,
                Icon = $"{WeaponIconBaseUrl}{Uri.EscapeDataString(iconId)}.png",
            });
        }
        return result;
    }

    private sealed class EndfieldGachaInfo
    {
        public string RecordType { get; set; } = "";

        public string ItemId { get; set; } = "";

        public string IconId { get; set; } = "";

        public string Icon { get; set; } = "";
    }
}
