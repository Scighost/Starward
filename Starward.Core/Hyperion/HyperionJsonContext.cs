using Starward.Core.Hyperion.Genshin;
using Starward.Core.Hyperion.Genshin.SpiralAbyss;
using Starward.Core.Hyperion.StarRail;
using Starward.Core.Hyperion.StarRail.Ledger;
using System.Text.Json.Serialization;

namespace Starward.Core.Hyperion;


[JsonSerializable(typeof(MihoyoApiWrapper<HyperionUserWrapper>))]
[JsonSerializable(typeof(MihoyoApiWrapper<StarRailRoleWrapper>))]
[JsonSerializable(typeof(MihoyoApiWrapper<LedgerSummary>))]
[JsonSerializable(typeof(MihoyoApiWrapper<LedgerDetail>))]
[JsonSerializable(typeof(MihoyoApiWrapper<GenshinRoleWrapper>))]
[JsonSerializable(typeof(MihoyoApiWrapper<SpiralAbyssInfo>))]
internal partial class HyperionJsonContext : JsonSerializerContext
{

}
