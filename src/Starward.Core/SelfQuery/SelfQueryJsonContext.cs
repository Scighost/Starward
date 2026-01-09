using System.Text.Json.Serialization;

namespace Starward.Core.SelfQuery;


[JsonSerializable(typeof(miHoYoApiWrapper<SelfQueryUserInfo>))]
[JsonSerializable(typeof(miHoYoApiWrapper<SelfQueryListWrapper<GenshinQueryItem>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<SelfQueryListWrapper<GenshinSelfQueryItem_BattlePath>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<SelfQueryListWrapper<GenshinSelfQueryItem_BeyondBattlePath>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<SelfQueryListWrapper<GenshinSelfQueryItem_BeyondCostume>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<SelfQueryListWrapper<GenshinSelfQueryItem_BeyondCostumeSuit>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<SelfQueryListWrapper<StarRailQueryItem>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<SelfQueryListWrapper<ZZZQueryItem>>))]
internal partial class SelfQueryJsonContext : JsonSerializerContext
{

}
