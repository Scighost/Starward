using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

[JsonSerializable(typeof(miHoYoApiWrapper<LauncherContent>))]
[JsonSerializable(typeof(miHoYoApiWrapper<LauncherResource>))]
[JsonSerializable(typeof(miHoYoApiWrapper<CloudGameBackgroundWrapper>))]
internal partial class LauncherJsonContext : JsonSerializerContext
{

}
