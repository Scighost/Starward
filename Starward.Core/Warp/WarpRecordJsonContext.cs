using System.Text.Json.Serialization;

namespace Starward.Core.Warp;

[JsonSerializable(typeof(MihoyoApiWrapper<WarpRecordResult>))]
[JsonSerializable(typeof(WarpRecordResult))]
[JsonSerializable(typeof(WarpRecordItem))]
[JsonSerializable(typeof(WarpRecordItem[]))]
[JsonSerializable(typeof(WarpRecordItem[]))]
internal partial class WarpRecordJsonContext : JsonSerializerContext
{

}
