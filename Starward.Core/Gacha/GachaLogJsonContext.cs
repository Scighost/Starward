using Starward.Core.Gacha.StarRail;
using System.Text.Json.Serialization;

namespace Starward.Core.Gacha;


[JsonSerializable(typeof(MihoyoApiWrapper<GachaLogResult<GachaLogItem>>))]
[JsonSerializable(typeof(GachaLogItem))]
[JsonSerializable(typeof(GachaLogItem[]))]
[JsonSerializable(typeof(MihoyoApiWrapper<GachaLogResult<WarpRecordItem>>))]
[JsonSerializable(typeof(WarpRecordItem))]
[JsonSerializable(typeof(WarpRecordItem[]))]
internal partial class GachaLogJsonContext : JsonSerializerContext
{

}
