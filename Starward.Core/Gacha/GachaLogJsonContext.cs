using Starward.Core.Gacha.Genshin;
using Starward.Core.Gacha.StarRail;
using System.Text.Json.Serialization;

namespace Starward.Core.Gacha;


[JsonSerializable(typeof(miHoYoApiWrapper<GachaLogResult<GachaLogItem>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GachaLogResult<StarRailGachaItem>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GachaLogResult<GenshinGachaItem>>))]
internal partial class GachaLogJsonContext : JsonSerializerContext
{

}
