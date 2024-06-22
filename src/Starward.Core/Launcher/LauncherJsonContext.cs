using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

[JsonSerializable(typeof(miHoYoApiWrapper<LauncherContent>))]
[JsonSerializable(typeof(miHoYoApiWrapper<LauncherGameResource>))]
[JsonSerializable(typeof(miHoYoApiWrapper<CloudGameBackgroundWrapper>))]
[JsonSerializable(typeof(miHoYoApiWrapper<AlertAnn>))]
[JsonSerializable(typeof(miHoYoApiWrapper<JsonNode>))]
internal partial class LauncherJsonContext : JsonSerializerContext
{

}
