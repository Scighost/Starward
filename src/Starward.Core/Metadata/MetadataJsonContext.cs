using Starward.Core.Metadata.Github;
using System.Text.Json.Serialization;

namespace Starward.Core.Metadata;

[JsonSerializable(typeof(List<GameInfo>))]
[JsonSerializable(typeof(ReleaseVersion))]
[JsonSerializable(typeof(GithubRelease))]
[JsonSerializable(typeof(List<GithubRelease>))]
[JsonSerializable(typeof(GithubMarkdownRequest))]
internal partial class MetadataJsonContext : JsonSerializerContext
{

}
