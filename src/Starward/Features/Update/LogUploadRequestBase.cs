using System;
using System.Text.Json.Serialization;

namespace Starward.Features.Update;

public class LogUploadRequestBase
{
    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; set; }

    [JsonPropertyName("app_name")]
    public string AppName { get; set; }

    [JsonPropertyName("app_version")]
    public string AppVersion { get; set; }

    [JsonPropertyName("architecture")]
    public string Architecture { get; set; }

    [JsonPropertyName("system_version")]
    public string SystemVersion { get; set; }

    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; }

    [JsonPropertyName("session_id")]
    public string SessionId { get; set; }

    [JsonPropertyName("event_category")]
    public string EventCategory { get; set; }

    [JsonPropertyName("event_name")]
    public string EventName { get; set; }

    [JsonPropertyName("event_param")]
    public object? EventParam { get; set; }


    public LogUploadRequestBase() { }

    public LogUploadRequestBase(string eventCategory, string eventName, object? eventParam = null)
    {
        EventCategory = eventCategory;
        EventName = eventName;
        EventParam = eventParam;
    }

}