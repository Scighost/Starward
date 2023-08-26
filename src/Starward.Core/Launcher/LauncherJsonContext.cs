using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

[JsonSerializable(typeof(miHoYoApiWrapper<LauncherContent>))]
[JsonSerializable(typeof(miHoYoApiWrapper<LauncherResource>))]
[JsonSerializable(typeof(miHoYoApiWrapper<CloudGameBackgroundWrapper>))]
[JsonSerializable(typeof(miHoYoApiWrapper<AlertAnn>))]
internal partial class LauncherJsonContext : JsonSerializerContext
{

}
