using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;


[JsonSerializable(typeof(miHoYoApiWrapper<JsonNode>))]
[JsonSerializable(typeof(List<GameInfo>))]
[JsonSerializable(typeof(List<GameBackgroundInfo>))]
[JsonSerializable(typeof(GameContent))]
[JsonSerializable(typeof(List<GamePackage>))]
[JsonSerializable(typeof(List<GameChannelSDK>))]
[JsonSerializable(typeof(List<GameDeprecatedFileConfig>))]
[JsonSerializable(typeof(List<GameConfig>))]
[JsonSerializable(typeof(List<GameBranch>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GameSophonChunkBuild>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GameSophonPatchBuild>))]

internal partial class HoYoPlayJsonContext : JsonSerializerContext
{

}
