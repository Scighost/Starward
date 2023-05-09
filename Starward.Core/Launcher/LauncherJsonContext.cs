using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

[JsonSerializable(typeof(MihoyoApiWrapper<LauncherContent>))]
internal partial class LauncherJsonContext : JsonSerializerContext
{

}
