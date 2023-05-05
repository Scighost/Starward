using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

[JsonSerializable(typeof(MihoyoApiWrapper<LauncherContent>))]
[JsonSerializable(typeof(LauncherContent))]
[JsonSerializable(typeof(BackgroundImage))]
[JsonSerializable(typeof(LauncherBanner))]
[JsonSerializable(typeof(LauncherBanner[]))]
[JsonSerializable(typeof(LauncherPost))]
[JsonSerializable(typeof(LauncherPost[]))]
[JsonSerializable(typeof(PostType))]
internal partial class LauncherJsonContext : JsonSerializerContext
{

}
