using Starward.Core.Gacha.Genshin;
using Starward.Core.Gacha.StarRail;
using System.Text.Json.Serialization;

namespace Starward.Core.Gacha;


[JsonSerializable(typeof(MihoyoApiWrapper<GachaLogResult<GachaLogItem>>))]
[JsonSerializable(typeof(GachaLogItem))]
[JsonSerializable(typeof(GachaLogItem[]))]
[JsonSerializable(typeof(MihoyoApiWrapper<GachaLogResult<WarpRecordItem>>))]
[JsonSerializable(typeof(WarpRecordItem))]
[JsonSerializable(typeof(WarpRecordItem[]))]
[JsonSerializable(typeof(MihoyoApiWrapper<GachaLogResult<WishRecordItem>>))]
[JsonSerializable(typeof(WishRecordItem))]
[JsonSerializable(typeof(WishRecordItem[]))]
internal partial class GachaLogJsonContext : JsonSerializerContext
{

}
