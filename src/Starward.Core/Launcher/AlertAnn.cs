using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

internal class AlertAnn
{
    [JsonPropertyName("alert")]
    public bool Alert { get; set; }

    [JsonPropertyName("alert_id")]
    public int AlertId { get; set; }

    [JsonPropertyName("remind")]
    public bool Remind { get; set; }

    [JsonPropertyName("extra_remind")]
    public bool ExtraRemind { get; set; }
}
