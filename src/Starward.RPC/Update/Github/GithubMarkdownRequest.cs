using System.Text.Json.Serialization;

namespace Starward.RPC.Update.Github;

internal class GithubMarkdownRequest
{

    [JsonPropertyName("text")]
    public string Text { get; set; }


    [JsonPropertyName("mode")]
    public string Mode { get; set; }


    [JsonPropertyName("context")]
    public string Context { get; set; }

}
