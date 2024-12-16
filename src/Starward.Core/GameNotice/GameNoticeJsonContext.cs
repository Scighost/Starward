using System.Text.Json.Serialization;

namespace Starward.Core.GameNotice;


[JsonSerializable(typeof(miHoYoApiWrapper<AlertAnn>))]
internal partial class GameNoticeJsonContext : JsonSerializerContext
{

}
