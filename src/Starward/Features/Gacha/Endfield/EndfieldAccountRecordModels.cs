using System;
using System.Collections.Generic;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Starward.Features.Gacha.Endfield;

public enum EndfieldAccountRecordCategory
{
    Stamina,
    Currency,
    MonthCard,
    Mail,
    Login,
}


public sealed class EndfieldAccountRecordItem
{
    public string AccountKey { get; set; } = "";

    public string RecordType { get; set; } = "";

    public EndfieldAccountRecordCategory Category { get; set; }

    public string Id { get; set; } = "";

    public string TypeName { get; set; } = "";

    public string Title { get; set; } = "";

    public string Subtitle { get; set; } = "";

    public string Detail { get; set; } = "";

    public string Icon { get; set; } = "";

    public long Timestamp { get; set; }

    public long Amount { get; set; }

    public bool HasAmount { get; set; }

    public int CountValue { get; set; }

    public string TimeText
    {
        get
        {
            try
            {
                long milliseconds = Timestamp < 100_000_000_000 ? Timestamp * 1000 : Timestamp;
                return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).ToLocalTime()
                    .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
            }
            catch (ArgumentOutOfRangeException)
            {
                return "-";
            }
        }
    }

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);

    public bool HasIcon => !string.IsNullOrWhiteSpace(Icon);

    public string FallbackGlyph => Category switch
    {
        EndfieldAccountRecordCategory.Mail => "\uE715",
        EndfieldAccountRecordCategory.Login => "\uE77B",
        _ => "\uE946",
    };
}


public sealed class EndfieldAccountRecordSummary
{
    public string Title { get; set; } = "";

    public string Value { get; set; } = "";

    public string Detail { get; set; } = "";

    public string Icon { get; set; } = "";

    public string Glyph { get; set; } = "\uE8C7";

    public long AddAmount { get; set; }

    public long SubAmount { get; set; }

    public bool HasAmountBreakdown { get; set; }

    public bool HasIcon => !string.IsNullOrWhiteSpace(Icon);
}


public sealed class EndfieldAccountRecordFilterOption : ObservableObject
{
    private int _count;

    public string Key { get; set; } = "";

    public string Name { get; set; } = "";

    public string Icon { get; set; } = "";

    public string Glyph { get; set; } = "\uE946";

    public bool HasIcon => !string.IsNullOrWhiteSpace(Icon);

    public int Count
    {
        get => _count;
        set
        {
            if (SetProperty(ref _count, value))
            {
                OnPropertyChanged(nameof(CountText));
            }
        }
    }

    public string CountText => $"{Count.ToString("N0", CultureInfo.CurrentCulture)} 条";

    public override string ToString() => Name;
}


internal sealed record EndfieldAccountRecordSyncResult(
    IReadOnlyList<EndfieldAccountRecordItem> Items,
    IReadOnlyList<EndfieldAccountRecordSummary> Summaries,
    DateTimeOffset SyncTime,
    string[] FailedCategories);
