using System.Text.Json.Serialization;

namespace Starward.Core.Metadata;

[JsonSerializable(typeof(List<GameInfo>))]
[JsonSerializable(typeof(ReleaseVersion))]
internal partial class MetadataJsonContext : JsonSerializerContext
{

}
