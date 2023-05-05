using System.Text.Json.Serialization;

namespace Starward.Core.Gacha;

[JsonSerializable(typeof(MihoyoApiWrapper<GachaLogResult>))]
[JsonSerializable(typeof(GachaLogResult))]
[JsonSerializable(typeof(GachaLogItem))]
[JsonSerializable(typeof(GachaLogItem[]))]
[JsonSerializable(typeof(GachaLogItem[]))]
internal partial class GachaLogJsonContext : JsonSerializerContext
{

}
