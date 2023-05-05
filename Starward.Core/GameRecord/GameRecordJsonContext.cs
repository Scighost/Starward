using Starward.Core.GameRecord.Ledger;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord;

[JsonSerializable(typeof(MihoyoApiWrapper<GameRoleWrapper>))]
[JsonSerializable(typeof(GameRoleInfo))]
[JsonSerializable(typeof(GameRoleInfo[]))]
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
internal partial class GameRecordJsonContext : JsonSerializerContext
{

}
