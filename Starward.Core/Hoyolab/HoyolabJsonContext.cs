using Starward.Core.Hoyolab.StarRail;
using Starward.Core.Hoyolab.StarRail.Ledger;
using System.Text.Json.Serialization;

namespace Starward.Core.Hoyolab;


[JsonSerializable(typeof(MihoyoApiWrapper<HoyolabUserWrapper>))]
[JsonSerializable(typeof(HoyolabUserWrapper))]
[JsonSerializable(typeof(MihoyoApiWrapper<StarRailRoleWrapper>))]
[JsonSerializable(typeof(StarRailRole))]
[JsonSerializable(typeof(StarRailRole[]))]
[JsonSerializable(typeof(MihoyoApiWrapper<LedgerSummary>))]
[JsonSerializable(typeof(LedgerSummary))]
[JsonSerializable(typeof(LedgerMonthData))]
[JsonSerializable(typeof(LedgerDayData))]
[JsonSerializable(typeof(LedgerMonthDataGroupBy))]
[JsonSerializable(typeof(LedgerMonthDataGroupBy[]))]
[JsonSerializable(typeof(MihoyoApiWrapper<LedgerDetail>))]
[JsonSerializable(typeof(LedgerDetail))]
[JsonSerializable(typeof(LedgerDetailItem))]
[JsonSerializable(typeof(LedgerDetailItem[]))]
internal partial class HoyolabJsonContext : JsonSerializerContext
{

}
