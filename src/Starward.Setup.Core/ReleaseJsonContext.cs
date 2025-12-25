using Starward.Setup.Core.Github;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Starward.Setup.Core;

[JsonSerializable(typeof(ReleaseInfo))]
[JsonSerializable(typeof(ReleaseManifest))]
[JsonSerializable(typeof(GithubRelease))]
[JsonSerializable(typeof(List<GithubRelease>))]
[JsonSerializable(typeof(GithubMarkdownRequest))]
internal partial class ReleaseJsonContext : JsonSerializerContext { }
