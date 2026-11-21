using System;
using System.Collections.Generic;
using System.Globalization;

namespace Starward.Features.Gacha.Endfield;

public sealed class EndfieldGachaAccount
{
    public string AccountKey { get; set; } = "";

    public string Uid { get; set; } = "";

    public string RoleId { get; set; } = "";

    public string RoleName { get; set; } = "";

    public string ServerId { get; set; } = "1";

    public string ServerName { get; set; } = "";

    public DateTimeOffset LastSyncTime { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(RoleName) ? Uid : $"{RoleName}  {Uid}";

    public override string ToString() => DisplayName;
}


public sealed class EndfieldGachaItem
{
    public string AccountKey { get; set; } = "";

    public string RecordType { get; set; } = "";

    public string SeqId { get; set; } = "";

    public string ItemId { get; set; } = "";

    public string ItemName { get; set; } = "";

    public string ItemType { get; set; } = "";

    public string Icon { get; set; } = "";

    public int Rarity { get; set; }

    public bool IsNew { get; set; }

    public bool IsFree { get; set; }

    public string PoolId { get; set; } = "";

    public string PoolName { get; set; } = "";

    public string PoolType { get; set; } = "";

    public string GachaTime { get; set; } = "";

    public int Pity { get; set; }

    public bool IsPityPlaceholder { get; set; }

    public string PityText => IsFree ? "加急" : IsPityPlaceholder || Pity > 0
        ? Pity.ToString(CultureInfo.CurrentCulture)
        : "-";

    public string TimeText => FormatTimestamp(GachaTime);

    public string RarityText => $"{Rarity} 星";

    public string ExtraText => IsFree ? "加急招募" : IsNew ? "NEW" : "";

    public bool HasIcon => !string.IsNullOrWhiteSpace(Icon);

    private static string FormatTimestamp(string value)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long timestamp))
        {
            try
            {
                long milliseconds = value.Length <= 10 ? timestamp * 1000 : timestamp;
                return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }
        return value;
    }
}


public sealed class EndfieldGachaPoolStats
{
    public string Key { get; set; } = "";

    public string Name { get; set; } = "";

    public int Count { get; set; }

    public string StartTimeText { get; set; } = "";

    public string EndTimeText { get; set; } = "";

    public int Count6 { get; set; }

    public int Count5 { get; set; }

    public int Count4 { get; set; }

    public int Pity6 { get; set; }

    public int Pity5 { get; set; }

    public double Average6 { get; set; }

    public double Ratio6 => Count == 0 ? 0 : (double)Count6 / Count;

    public double Ratio5 => Count == 0 ? 0 : (double)Count5 / Count;

    public double Ratio4 => Count == 0 ? 0 : (double)Count4 / Count;

    public List<EndfieldGachaItem> List6 { get; set; } = [];

    public List<EndfieldGachaItem> List5 { get; set; } = [];
}


public sealed class EndfieldGachaPoolOption
{
    public string Key { get; set; } = "";

    public string Name { get; set; } = "";

    public override string ToString() => Name;
}


public sealed class EndfieldGachaStats
{
    public int Total { get; set; }

    public int Count6 { get; set; }

    public int Count5 { get; set; }

    public int Count4 { get; set; }

    public string PityText { get; set; } = "-";

    public string SixRate => Total == 0 ? "0.00%" : ((double)Count6 / Total).ToString("P2", CultureInfo.CurrentCulture);

    public string RaritySummary => $"6 星 {Count6}  ·  5 星 {Count5}  ·  4 星 {Count4}";
}


internal sealed record EndfieldGachaSyncResult(EndfieldGachaAccount Account, int NewCount, int TotalCount, string[] FailedPools);
