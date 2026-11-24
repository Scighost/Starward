using System.Text.Json.Serialization;

namespace Starward.GameInstall.HotUpdate;

[JsonSerializable(typeof(CutsceneCollection))]
[JsonSerializable(typeof(QueryCurRegionResponse))]
[JsonSerializable(typeof(HotPatchItem))]
[JsonSerializable(typeof(DownloadPref))]
[JsonSerializable(typeof(PatchNodeVersionItem))]
internal partial class GenshinHotUpdateJsonContext : JsonSerializerContext
{

}