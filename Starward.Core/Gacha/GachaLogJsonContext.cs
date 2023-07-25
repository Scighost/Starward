using Starward.Core.Gacha.Genshin;
using Starward.Core.Gacha.StarRail;
using System.Text.Json.Serialization;

namespace Starward.Core.Gacha;


[JsonSerializable(typeof(miHoYoApiWrapper<GachaLogResult<GachaLogItem>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GachaLogResult<StarRailGachaItem>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GachaLogResult<GenshinGachaItem>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GenshinGachaWiki>))]
[JsonSerializable(typeof(miHoYoApiWrapper<StarRailGachaWiki>))]
[JsonSerializable(typeof(miHoYoApiWrapper<StarRailGachaInfoWrapper>))]
internal partial class GachaLogJsonContext : JsonSerializerContext
{

}
