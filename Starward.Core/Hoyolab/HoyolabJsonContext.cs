using Starward.Core.Hoyolab.Genshin;
using Starward.Core.Hoyolab.Genshin.SpiralAbyss;
using Starward.Core.Hoyolab.StarRail;
using Starward.Core.Hoyolab.StarRail.Ledger;
using System.Text.Json.Serialization;

namespace Starward.Core.Hoyolab;


[JsonSerializable(typeof(MihoyoApiWrapper<HoyolabUserWrapper>))]
[JsonSerializable(typeof(MihoyoApiWrapper<StarRailRoleWrapper>))]
[JsonSerializable(typeof(MihoyoApiWrapper<LedgerSummary>))]
[JsonSerializable(typeof(MihoyoApiWrapper<LedgerDetail>))]
[JsonSerializable(typeof(MihoyoApiWrapper<GenshinRoleWrapper>))]
[JsonSerializable(typeof(MihoyoApiWrapper<SpiralAbyssInfo>))]
public partial class HoyolabJsonContext : JsonSerializerContext
{

}
