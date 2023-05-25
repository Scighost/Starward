using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

[JsonSerializable(typeof(MihoyoApiWrapper<LauncherContent>))]
[JsonSerializable(typeof(MihoyoApiWrapper<CloudGameBackgroundWrapper>))]
internal partial class LauncherJsonContext : JsonSerializerContext
{

}
