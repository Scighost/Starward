using System.Text.Json.Serialization;

namespace Starward.Core.SelfQuery;


[JsonSerializable(typeof(miHoYoApiWrapper<SelfQueryUserInfo>))]
[JsonSerializable(typeof(miHoYoApiWrapper<SelfQueryListWrapper<GenshinQueryItem>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<SelfQueryListWrapper<StarRailQueryItem>>))]
[JsonSerializable(typeof(miHoYoApiWrapper<SelfQueryListWrapper<ZZZQueryItem>>))]
internal partial class SelfQueryJsonContext : JsonSerializerContext
{

}
